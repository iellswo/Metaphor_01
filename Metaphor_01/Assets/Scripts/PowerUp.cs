using UnityEngine;
using System.Collections;

public class PowerUp : ISpawnableObject
{
    public enum EPowerUpType
    {
        AirWalk = 0,
    }
    public EPowerUpType powerUpType;

    public float strength = 5.0f;
}
