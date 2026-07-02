#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(VenueCollectibleSpawner))]
public class VenueCollectibleSpawnerEditor : Editor
{
    private static readonly (string venueId, string rewardAssetName)[] DefaultAssignments =
    {
        ("chess", "bow_reward"),
        ("couch", "shirt_reward"),
        ("photo_wall", "camera_reward"),
        ("goodies", "hat_reward"),
        ("book_wall", "glasses_reward"),
        ("tap_water", "pinWater_reward"),
        ("ice_cream_shop", "socks_reward"),
        ("drink_shop", "collar_reward"),
        ("piano", "earring_reward"),
        ("plants", "pinXRCC_reward"),
    };

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        DrawPropertiesExcluding(serializedObject, "explicitPlacements", "m_Script");

        VenueCollectibleSpawner spawner = (VenueCollectibleSpawner)target;
        VenueMapDefinition mapDefinition = spawner.MapDefinition;

        EditorGUILayout.Space(8f);
        EditorGUILayout.LabelField("Venue Reward Assignments", EditorStyles.boldLabel);

        if (mapDefinition == null || mapDefinition.attractions == null || mapDefinition.attractions.Count == 0)
        {
            EditorGUILayout.HelpBox(
                "Assign a Venue Map Definition above to edit venue-to-reward mappings with attraction names.",
                MessageType.Info);
        }

        SerializedProperty placementsProperty = serializedObject.FindProperty("explicitPlacements");
        DrawPlacementList(spawner, mapDefinition, placementsProperty);

        EditorGUILayout.Space(6f);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Fill All Attractions"))
            {
                PopulateDefaultAssignments(spawner, placementsProperty);
                serializedObject.ApplyModifiedProperties();
                spawner.SyncRewardRevealControllerRewards();
                EditorUtility.SetDirty(spawner);
            }

            if (GUILayout.Button("Sync Reward Reveal List"))
            {
                serializedObject.ApplyModifiedProperties();
                spawner.SyncRewardRevealControllerRewards();
                EditorUtility.SetDirty(spawner);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    private static void DrawPlacementList(
        VenueCollectibleSpawner spawner,
        VenueMapDefinition mapDefinition,
        SerializedProperty placementsProperty)
    {
        for (int i = 0; i < placementsProperty.arraySize; i++)
        {
            SerializedProperty element = placementsProperty.GetArrayElementAtIndex(i);
            SerializedProperty venueIdProperty = element.FindPropertyRelative("venueAttractionId");
            SerializedProperty rewardProperty = element.FindPropertyRelative("reward");

            using (new EditorGUILayout.VerticalScope("box"))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField($"Assignment {i + 1}", EditorStyles.miniBoldLabel);
                    if (GUILayout.Button("Remove", GUILayout.Width(70f)))
                    {
                        placementsProperty.DeleteArrayElementAtIndex(i);
                        break;
                    }
                }

                DrawVenuePopup(mapDefinition, venueIdProperty);
                EditorGUILayout.PropertyField(rewardProperty, new GUIContent("Reward"));
            }
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Add Assignment"))
            {
                placementsProperty.arraySize++;
                SerializedProperty added = placementsProperty.GetArrayElementAtIndex(placementsProperty.arraySize - 1);
                added.FindPropertyRelative("venueAttractionId").stringValue = string.Empty;
                added.FindPropertyRelative("reward").objectReferenceValue = null;
            }

            if (GUILayout.Button("Clear All"))
                placementsProperty.ClearArray();
        }
    }

    private static void DrawVenuePopup(VenueMapDefinition mapDefinition, SerializedProperty venueIdProperty)
    {
        if (mapDefinition == null || mapDefinition.attractions == null || mapDefinition.attractions.Count == 0)
        {
            EditorGUILayout.PropertyField(venueIdProperty, new GUIContent("Venue Attraction Id"));
            return;
        }

        List<string> ids = new List<string> { string.Empty };
        List<string> labels = new List<string> { "(Select venue)" };

        int currentIndex = 0;
        for (int i = 0; i < mapDefinition.attractions.Count; i++)
        {
            AttractionDefinition attraction = mapDefinition.attractions[i];
            if (attraction == null || string.IsNullOrWhiteSpace(attraction.id))
                continue;

            ids.Add(attraction.id);
            string label = string.IsNullOrWhiteSpace(attraction.displayName)
                ? attraction.id
                : $"{attraction.displayName} ({attraction.id})";
            labels.Add(label);

            if (attraction.id == venueIdProperty.stringValue)
                currentIndex = ids.Count - 1;
        }

        int selectedIndex = EditorGUILayout.Popup("Venue Attraction", currentIndex, labels.ToArray());
        venueIdProperty.stringValue = ids[Mathf.Clamp(selectedIndex, 0, ids.Count - 1)];
    }

    private static void PopulateDefaultAssignments(VenueCollectibleSpawner spawner, SerializedProperty placementsProperty)
    {
        placementsProperty.ClearArray();

        for (int i = 0; i < DefaultAssignments.Length; i++)
        {
            (string venueId, string rewardAssetName) assignment = DefaultAssignments[i];
            RewardDefinition reward = LoadRewardAsset(assignment.rewardAssetName);
            if (reward == null)
            {
                Debug.LogWarning($"VenueCollectibleSpawnerEditor: could not load reward asset '{assignment.rewardAssetName}'.");
                continue;
            }

            placementsProperty.arraySize++;
            SerializedProperty element = placementsProperty.GetArrayElementAtIndex(placementsProperty.arraySize - 1);
            element.FindPropertyRelative("venueAttractionId").stringValue = assignment.venueId;
            element.FindPropertyRelative("reward").objectReferenceValue = reward;
        }

        EditorUtility.SetDirty(spawner);
    }

    private static RewardDefinition LoadRewardAsset(string assetName)
    {
        string path = $"Assets/PuppyPath/Rewards/{assetName}.asset";
        return AssetDatabase.LoadAssetAtPath<RewardDefinition>(path);
    }
}
#endif
