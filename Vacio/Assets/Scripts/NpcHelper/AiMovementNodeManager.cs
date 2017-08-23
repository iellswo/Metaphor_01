using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AiMovementNodeManager : MonoBehaviour
{
    public enum EConnectionType
    {
        Walking,
    }

    // This could be static, but making it a component should make it easier to get scene events and clean it up when loading a new level.
    private static AiMovementNodeManager singleton = null;
    public static bool TryGetInstance(out AiMovementNodeManager manager)
    {
        if (singleton == null)
        {
            GameObject singletonGameObject = new GameObject();
            singleton = singletonGameObject.AddComponent<AiMovementNodeManager>();
            singletonGameObject.name = "AiMovementNodeManager";
        }
        manager = singleton;
        return true;
    }
}
