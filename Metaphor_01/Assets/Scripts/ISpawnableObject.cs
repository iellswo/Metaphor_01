using UnityEngine;
using System.Collections;
using System;

public class ISpawnableObject : MonoBehaviour
{
    public void Spawn(Vector3 position)
    {
        gameObject.SetActive(true);
        transform.position = position;
    }

    public void DeSpawn()
    {
        gameObject.SetActive(false);
    }

    public bool NeedsToRespawn()
    {
        return !gameObject.activeInHierarchy;
    }
}
