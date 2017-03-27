using UnityEngine;
using System.Collections;
using System;

public class SpawnableObject : MonoBehaviour
{
    public void Spawn(Vector3 position, ObjectSpawner.SSpawnData[] data)
    {
        gameObject.SetActive(true);
        transform.position = position;
        ApplySpawnData(data);
    }

    protected virtual void ApplySpawnData(ObjectSpawner.SSpawnData[] data)
    {
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
