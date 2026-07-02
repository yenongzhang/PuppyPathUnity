# PuppyPath Existing Project V2 Audit

Last updated: 2026-07-01

## Scope of Review

Reviewed and organized content under `Assets/PuppyPath`, including:

- `3dModel`
- `Animations`
- `Audio`
- `Images`
- `Logo`
- `Material`
- `Prefabs`
- `Scenes`
- `Scripts`
- `UI`

Binary assets, models, audio, images, prefabs, and scenes were inspected primarily through file structure and naming. C# scripts were read for their current behavior and public interfaces.

## Asset Inventory

### 3dModel

Contains beagle FBX files, animation FBX files, puppy eye and mouth textures, puppy material files, and texture maps. This is the foundation for the V2 puppy character.

V2 reusable:

- Puppy model.
- Existing move, sit, bark, happy, turn, and sniff animation files.
- Face textures used for expression changes.

V2 needs:

- Add accessory anchor transforms on the puppy prefab.
- Add accessory models such as hats, glasses, clothing, and collar charms.
- Refine animations and expressions for item reveal, sit-and-wait, and reward moments.

### Animations

Contains `DogAnimationController.controller`.

V2 reusable:

- The existing puppy animation controller can serve as a base.

V2 needs:

- Confirm animation state names match serialized fields in `DogGuideController`.
- Add or adjust animation transitions for V2 states as needed.

### Audio

Contains dog bark sound effects and `track1.mp3`.

V2 reusable:

- Dog bark sound effects can be used for puppy reactions and reward moments.

V2 needs:

- Decide whether the intro and reward moments need additional sound effects.
- Adjust volume for the live event environment.

### Images

Contains legacy icons and map assets, including `EuropaParkMap1-modified.png`.

V2 reusable:

- Existing map UI approach and marker / icon assets.

V2 needs:

- Import the latest event venue map image.
- Create paw-print marker icons if no ready-made assets exist.
- Replace legacy Europa-Park map data with the real event venue map.

### Logo

Contains PuppyPath logo PDF and PNG files.

V2 reusable:

- Existing logo assets should continue to be used in the boot flow.

### Material

Contains puppy eye / mouth materials, path preview material, shadow receiver material / shader, and firework additive material.

V2 reusable:

- `M_PathPreview` or similar materials can be used for ground route lines.
- Eye / mouth materials for expression switching.
- Firework material for reward or surprise effects.

### Prefabs

Contains:

- Puppy prefabs.
- Map marker prefab.
- OVRCameraRig variant.
- RouteRoot prefab.
- Firework prefab.
- Legacy path prefabs.

V2 reusable:

- Puppy prefabs are the foundation for the main character.
- Firework prefab can serve as a reference for surprise effects, although the user currently prioritizes shaking animation.
- Map marker prefab may be replaceable with a paw-print marker.

V2 needs:

- Legacy path prefabs are static route definitions for the old map and are largely unsuitable for the real event venue.
- Route root is currently spawned relative to the user and should be replaced with venue-fixed route generation.

### Scenes

Contains:

- `DogTestScene.unity`
- `UIScene.unity`

V2 reusable:

- `DogTestScene` can be used to test puppy animation and expressions.
- `UIScene` likely contains the current app UI flow.

V2 needs:

- Once implementation begins, add or duplicate a V2 scene / prefab setup.
- Keep legacy scenes until V2 is stable.

### UI

Contains font assets and UI images for bubbles, buttons, maps, locations, friends, arrows, and more.

V2 reusable:

- Speech bubble assets can be used for puppy self-introduction and item hints.
- Existing fonts and button styling can maintain visual consistency.

V2 needs:

- New minimap layout.
- Centered big map.
- Top status text.
- Attraction item hint UI.
- Reward UI.

## Script Audit

### `UIBootSequence`

Current behavior:

- Shows logo root.
- Controls logo canvas group fade-in, hold, and fade-out.
- Hides main canvas during logo.
- Safely shows main canvas after `CanvasFollowHead` snap.

V2 usage:

- Reuse boot flow.
- Add completion event or callback so the V2 controller can spawn the puppy and play the intro dialogue after the logo ends.

### `CanvasFollowHead`

Current behavior:

- Places canvas in front of the user's head.
- Smoothly follows position and rotation.
- Snaps when too far away, behind the user, or too close.

V2 usage:

- Can be reused for head-follow UI.
- Whether minimap / top HUD uses it depends on the final XR UI setup.

### `PuppyPathSelectionUI`

Current behavior:

- Handles legacy map clicks.
- Converts map clicks to grid row / column.
- Selects path id from a 3x5 grid.
- Supports friend marker.
- Updates legacy flow phase text and buttons.

V2 usage:

- Can serve as a reference for UI pointer click and marker placement logic.

V2 replacement direction:

- Replace grid selection with attraction marker selection.
- Remove legacy Europa-Park place names and friend-based flow unless find-a-friend functionality is restored later.

### `NavigationController`

Current behavior:

- Manages legacy intro phases, preview, start, complete, fireworks, destination beacon, and reset.

V2 usage:

- Reuse flow organization ideas.
- Reuse arrival / reward effect ideas if helpful.

V2 replacement direction:

- A new high-level V2 game controller should manage `Boot`, `Intro`, `FreeWalk`, `MapOpen`, `Navigating`, `AttractionReveal`, `ItemGrab`, and `Reward`.

### `PathPreviewController`

Current behavior:

- Maps path id to `PathDefinition` prefab.
- Spawns route root.
- Instantiates path prefab under route root.
- Draws animated route via waypoints using `LineRenderer`.
- Exposes current path and destination externally.

V2 usage:

- Reuse route drawing approach.

V2 replacement direction:

- Routes should be generated from venue graph data, not selected from legacy static path prefabs.
- Routes should be fixed in real venue coordinates, not spawned relative to the user.

### `RouteRootSpawner`

Current behavior:

- Spawns route root in front of the XR camera.
- Uses eye-to-ground offset.

V2 usage:

- Only suitable for the legacy prototype.

V2 replacement direction:

- A venue-fixed route parent should exist in the real venue coordinate system.

### `NavigationRuntimeController`

Current behavior:

- Reads current path waypoints.
- Tracks user progress along the path.
- Computes states: `Neutral`, `Waiting`, `GettingCloser`, `GettingFarther`, `Lost`, `Arrived`.
- Updates HUD.
- Notifies `DogGuideController`.

V2 usage:

- Reuse navigation state concepts.

V2 modification direction:

- Use generated venue routes instead of legacy path prefab waypoints.
- After arrival, return to free walk after a few seconds.
- Attraction item reveal should take priority over ordinary arrival completion logic.

### `NavigationHUDController`

Current behavior:

- Hides legacy friend / map / intro panels during navigation.
- Shows navigation HUD and status text.

V2 usage:

- Reuse text update patterns.

V2 replacement direction:

- Replace current legacy panel assumptions with V2 UI groups.

### `DogGuideController`

Current behavior:

- Instantiates puppy prefab.
- Stores runtime path.
- Applies navigation state.
- Plays stand / walk / trot / canter / sniff / bark / happy / sit / turn animations.
- Plays dog bark audio.
- Executes random behaviors.
- Switches eye / mouth expression textures.
- Moves puppy near the user based on route direction and state.

V2 usage:

- Strong reuse candidate for puppy animation, expressions, audio, and follow behavior.

V2 modification direction:

- Puppy movement targets must be constrained to venue walkable areas.
- Add explicit commands for free walk, guiding, sit-and-wait, attraction reveal, and reward.
- Integrate accessory manager.
- Avoid legacy behavior that lets the puppy run behind the user; V2 requires the puppy not to be behind the user.

### `DogStateTester`

Current behavior:

- Tests puppy expressions and animation bools / triggers via keyboard.

V2 usage:

- Keep for testing puppy animation and expressions.

### `DogNavStateTester`

Current behavior:

- Creates fake paths and tests puppy navigation states via key presses.

V2 usage:

- Keep or duplicate a V2 version for testing puppy behavior.

### `DogEyeFollowRay`

Current behavior:

- Makes UI eye `RectTransform` face the mouse, controller, or hand ray hit point on the canvas.

V2 usage:

- Can be reused if puppy UI or intro face graphics need pointer-aware eye movement.

### `FriendButtonUI` *(deprecated — not part of V2)*

Current behavior:

- Legacy friend button selection helper.

V2 usage:

- **Deprecated.** Not part of V2. Do not use unless find-a-friend functionality is explicitly reintroduced later.

### `PathDefinition`

Current behavior:

- Stores path id and child waypoints.

V2 usage:

- Still useful for hand-authored test paths, but insufficient for dynamic real-venue navigation.

### `FireworkAutoDestroy`

Current behavior:

- Destroys effect objects after a delay.

V2 usage:

- Can be reused for temporary reward / surprise VFX.

## V2 Key Refactor Summary

Keep:

- Logo boot flow.
- Puppy model, prefab, animation, and expression assets.
- Foundation of puppy animation and expression logic.
- `LineRenderer` route visualization approach.
- UI visual assets and fonts.
- Firework / effect auto-cleanup approach.

Replace or heavily rework:

- Legacy grid map.
- Legacy friend / location selection.
- Legacy path prefab library.
- User-relative route root spawning.
- Legacy Europa-Park map content.
- Legacy `NavigationController` phase model.

New systems needed:

- Venue coordinate calibration.
- Walkable area and obstacle data.
- Attraction registry.
- Minimap and big map based on attraction data.
- Dynamic route generation.
- Puppy position selection within walkable areas.
- Collectible reveal / grab / placement.
- Puppy accessory attachment.
- Reward display flow.

## 2026-07-01 V2 New Script Log

Phase 1 implementation began with the venue calibration prototype. New scripts are located in `Assets/PuppyPath/Scripts/V2`:

- `VenueMapDefinition.cs`: New `ScriptableObject` data asset type for storing real venue map dimensions, Photo Wall origin, 3.45 m scale line, map-to-Unity coordinate conversion parameters, and attraction / collectible spawn point data.
- `VenueCoordinateMapper.cs`: New pure conversion utility that unifies map pixel coordinates and Unity world coordinates; current convention is map north corresponds to Unity `+Z`.
- `VenueCalibrationDebugView.cs`: New Scene view debug component that draws VenueOrigin, map bounds, 3.45 m scale line, walkable areas, nav graph, test routes, and attraction markers via Gizmos.
- `VenuePathfinder.cs`: New first-version hand-authored waypoint graph pathfinding utility.
- `VenueRouteLineController.cs`: New first-version venue-fixed route `LineRenderer` drawing component; accepts map pixel start/end points or precomputed world-coordinate routes.
- `VenueNavigationRuntime.cs`: New first-version V2 venue navigation runtime that generates real-venue routes from HMD world position to attractions, refreshes `VenueRouteLineController`, and can temporarily drive `DogGuideController`.
- `VenueMapUiController.cs`: New first-version V2 minimap / big map UI controller that maps user and attraction real-venue coordinates to UI markers and connects big map attraction clicks to `VenueNavigationRuntime`.
- `VenueMapMarker.cs`: New map marker component for storing attraction id, showing selected state, and handling clicks.
- `VenueMapOpenButton.cs`: New lightweight click entry to open the big map from the minimap.
- `PuppyPathV2FlowController.cs`: New storyboard flow controller that switches `Intro`, `FreeRoam`, `BigMap`, `Navigation`, `Reward`, `ItemGrab`, `RewardPopup`, and other UI panels while reusing the legacy Canvas.
- `VenueAlignmentManager.cs`: New on-site calibration component that aligns `VenueContentRoot` to the current HMD when the user stands at the real `VenueOrigin` facing map north.
- `VenueSpatialAnchorBootstrap.cs`: New Meta Spatial Anchor bootstrap that creates an `OVRSpatialAnchor` at `VenueOrigin` and parents `VenueContentRoot` under the anchor.
- `VenueWalkableGridVisualizer.cs`: New yellow walkable-area grid visualizer for confirming map alignment, scale, and orientation on Quest hardware.
- `VenueMapReferencePlane.cs`: New map reference plane component that lays the current map image onto the XZ plane using venue coordinates.
- `Editor/VenueCalibrationDebugViewEditor.cs`: New Scene view editing tool for dragging and modifying point data in `VenueMapDefinition`, and manually connecting / disconnecting nav graph blue lines in the Scene top-left `Nav Graph Editing` panel.

These scripts do not replace the existing `DogGuideController`, legacy UI, or legacy path system; they only lay the foundation for V2's real-venue coordinate layer.

`VenueMapDefinition` currently also provides debug menu items:

- `Use PuppyPath Source Map Size`: Sets map size to source image `2468 x 2160`.
- `Use Confirmed V2 Orientation`: Sets `originWorldPosition` to `(0, 0, 0)` and `venueYawDegrees` to `0`.
- `Populate Default Attractions`: Generates 10 default attraction data entries.
- `Populate Detected Attraction Spawn Pixels`: Fills in 10 orange-dot coordinates auto-detected from `map_with_spawn_points.jpg`.
- `Populate Detected Walkable Draft`: Fills in the first-version auto-detected walkable outer contour and central obstacle area.
- `Populate Detected Nav Graph Draft`: Fills in the first-version waypoint graph draft.
- `Populate Detected Draft Map Data`: Runs all of the above map draft population steps in one pass.
- `Log Calibration Summary`: Prints current scale, meters/pixel, world scale-line distance, and attraction count to the Console.

`VenueCalibrationDebugView` currently also provides:

- `Create Map Reference Plane`: Creates or updates a `VenueMapReferencePlane` child under the current debug object.
- `Show Map Reference Plane` / `Hide Map Reference Plane`: Shows or hides the map reference plane.
- Scene Editing toggle: Allows dragging calibration points, attractions, walkable polygons, obstacle polygons, and nav graph directly in the Scene view; when `Edit Nav Graph In Scene` is enabled, also supports selecting two waypoints and manually `Connect` / `Disconnect` neighbor links.

`VenueNavigationRuntime` currently provides:

- `StartNavigationToAttraction(string attractionId)`: Starts route generation from the current HMD / XR Camera position to the specified attraction.
- `StopNavigation()`: Clears the route and stops temporary puppy navigation.
- `Start Test Navigation` / `Stop Navigation` context menu items: For direct in-scene testing without UI.
- Sends only non-negative navigation states to the legacy `DogGuideController` to avoid triggering legacy angry / lost feedback during V2 free-movement phases.

Current on-device calibration / visualization tools provide:

- `VenueContentRoot` structure convention: All venue-fixed content should be a child of this root, moved and rotated uniformly by calibration components.
- `VenueAlignmentManager`: Uses current HMD position and facing for quick on-site alignment testing.
- `VenueSpatialAnchorBootstrap`: Lays groundwork for persistent Spatial Anchor alignment later; requires `Anchor Support` enabled on `OVRManager` before use.
- `VenueWalkableGridVisualizer`: Displays current `walkableAreas - obstacleAreas` results as semi-transparent yellow grid cells to address the lack of visible content on Quest.

## 2026-07-01 Storyboard / Canvas Reuse Conclusions

The storyboard clarifies that `MiniMap` is a game-HUD-style local minimap, not a full venue thumbnail. The legacy `Canvas` should continue as the V2 main UI container:

- Keep `UIBootSequence`, `CanvasFollowHead`, `OVROverlayCanvas`, `GraphicRaycaster`, `PointableCanvasModule`.
- Keep legacy buttons, fonts, bubbles, and map image assets as visual assets.
- Disable legacy `PuppyPathSelectionUI` grid selection logic, legacy `NavigationController` path prefab flow, and legacy `PathPreviewController` static path library.
- Add or rework panels in the legacy Canvas: `IntroPanel`, `FreeRoamHud`, `BigMapPanel`, `NavigationHud`, `RewardPanel`, `ItemGrabPanel`, `RewardPopupPanel`.
- Use `PuppyPathV2FlowController` to manage storyboard state flow and `VenueMapUiController` to manage the local minimap and full big map.

## 2026-07-01 Legacy Navigation Reuse Boundaries

Parts of the legacy navigation system that can be reused:

- Puppy prefab instantiation, animation state playback, expression texture switching, bark audio, and basic movement interpolation in `DogGuideController`.
- Ideas from `NavigationRuntimeController` for progress along route, distance from route centerline, arrival distance, and HMD movement detection.
- `LineRenderer` route drawing approach from `PathPreviewController`.

Parts of the legacy navigation system that should not be used directly:

- Static path prefabs as the real-venue route data source.
- `RouteRootSpawner` user-relative route root spawning.
- Legacy `Waiting` / `GettingFarther` / `Lost` negative feedback logic.
- Any behavior that actively moves the puppy behind the user, makes it sit and wait in place, or makes it "angry" because the user moves freely.

V2 should add wrapper / runtime components:

- `DogVenueFollower`: Keeps the puppy ahead of the user in the real venue, follows HMD velocity, avoids non-walkable areas, and provides happy feedback when approaching treasure.
- `VenueNavigationRuntime`: Uses `VenuePathfinder` results instead of legacy path prefabs and provides recommended direction to the puppy.

## Required Documentation Sync Rule

Whenever any existing script is repurposed, replaced, deleted, or heavily modified for V2, this document must be updated so subsequent developers know which legacy content the project still depends on.
