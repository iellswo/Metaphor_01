using UnityEngine;
using System.Collections;

public class ObjectSpawner : MonoBehaviour
{
    public ISpawnableObject objectToSpawn;

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

    public float timeToRespawn = 10.0f;
    private float timeLeftBeforeNextRespawn = 0.0f;

    private ISpawnableObject spawnedObject = null;

    // Use this for initialization
    void Start()
    {
        timeLeftBeforeNextRespawn = 0.0f;
    }

    // Update is called once per frame
    void Update()
    {
        if (CanRespawn())
        {
            timeLeftBeforeNextRespawn -= Time.deltaTime;
            if (timeLeftBeforeNextRespawn <= 0.0f)
            {
                Spawn();
                timeLeftBeforeNextRespawn = timeToRespawn;
            }
        }
    }

    private void Spawn()
    {
        if (spawnedObject == null)
        {
            spawnedObject = GameObject.Instantiate<ISpawnableObject>(objectToSpawn);
        }
        spawnedObject.Spawn(transform.position, spawnData);
    }

    private bool CanRespawn()
    {
        return spawnedObject == null || spawnedObject.NeedsToRespawn();
    }
}
