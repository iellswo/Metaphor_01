using UnityEngine;
using System.Collections;

public class PowerUp : ISpawnableObject
{
    public enum EPowerUpType
    {
        AirWalk = 0,
        LowGravity = 1,
        Flying = 2,
    }
    public EPowerUpType powerUpType;

    [HideInInspector]
    public float? overrideDuration = null;

    protected override void ApplySpawnData(ObjectSpawner.SSpawnData[] data)
    {
        overrideDuration = null;
        if (data != null)
        {
            foreach (ObjectSpawner.SSpawnData d in data)
            {
                if (d.property == ObjectSpawner.ESpawnProperty.PowerupDuration)
                {
                    overrideDuration = d.value;
                }
            }
        }
    }
}
