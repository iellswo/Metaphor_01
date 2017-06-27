using UnityEngine;
using System.Collections;

public class PowerUp : SpawnableObject
{
    public Animator orbAnimator;
    public string animationName = "powerup_hover";
    public GameObject proxyPrefab;

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

    void Start()
    {
        var startTime = Random.Range(0f, 1f);
        orbAnimator.Play(animationName, layer: -1, normalizedTime: startTime);
    }
}
