using UnityEngine;
using System.Collections;

public class ObjectSpawner : MonoBehaviour
{
    public SpawnableObject objectToSpawn;

    public enum ESpawnProperty
    {
        PowerupDuration,
    }

    [System.Serializable]
    public struct SSpawnData
    {
        public ESpawnProperty property;
        public float value;
    }

    public SSpawnData[] spawnData;

    public bool respawnOnTimer = false;
    public bool respawnOnDeath = true;

    public float timeToRespawn = 10.0f;

    private float timeLeftBeforeNextRespawn = 0.0f;
    private bool reported = false;
    private bool deathMarked = true;

    private SpawnableObject spawnedObject = null;

    // Use this for initialization
    void Start()
    {
        timeLeftBeforeNextRespawn = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (respawnOnTimer && CanRespawn())
        {
            timeLeftBeforeNextRespawn -= Time.deltaTime;
            if (timeLeftBeforeNextRespawn <= 0.0f)
            {
                Spawn();
                timeLeftBeforeNextRespawn = timeToRespawn;
            }
        }

        if (respawnOnDeath && !reported)
        {
            SpawnTracker.ReportSpawner(this);
            reported = true;
        }

        if (respawnOnDeath && deathMarked)
        {
            deathMarked = false;
            //if (CanRespawn())
            //{
                Spawn();
            //}
        }
    }

    public void FlagForRespawnOnDeath()
    {
        deathMarked = true;
    }

    private void Spawn()
    {
        if (spawnedObject == null)
        {
            spawnedObject = GameObject.Instantiate<SpawnableObject>(objectToSpawn);
        }
        spawnedObject.Spawn(transform.position, spawnData);
    }

    private bool CanRespawn()
    {
        return spawnedObject == null || spawnedObject.NeedsToRespawn();
    }
}
