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
}
