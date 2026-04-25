using UnityEngine;

public class FireworkAutoDestroy : MonoBehaviour
{
    [SerializeField] private float destroyDelay = 3f;

    private void Start()
    {
        Destroy(gameObject, destroyDelay);
    }
}