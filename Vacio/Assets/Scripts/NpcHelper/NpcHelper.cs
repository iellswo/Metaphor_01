using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcHelper : MonoBehaviour
{
    private enum EHelperState
    {
        
    }
    private PlayerController playerTarget = null;

    private void Start()
    {
        playerTarget = GameObject.FindObjectOfType<PlayerController>();
    }

    void Update()
    {
        if (playerTarget != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, playerTarget.transform.position, Time.deltaTime);
        }
        else
        {
            Debug.LogError("Could not find playerTarget", gameObject);
        }
    }
}
