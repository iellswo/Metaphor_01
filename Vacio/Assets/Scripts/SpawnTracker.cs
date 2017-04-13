using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnTracker : MonoBehaviour
{
    private static SpawnTracker _singleton;

    public List<ObjectSpawner> Objects = new List<ObjectSpawner>();

    private CameraController _cameraControl;

	// Use this for initialization
	void Start ()
    {
        _singleton = this;
        _cameraControl = FindObjectOfType<CameraController>();
	}
	
	// Update is called once per frame
	void Update ()
    {
		
	}

    // An ObjectSpawner reports here and gets added to the list.
    public static void ReportSpawner(ObjectSpawner reportee)
    {
        _singleton.Objects.Add(reportee);
    }

    public static void TriggerReset()
    {
        _singleton._cameraControl.TriggerDeath();

        foreach (var spawner in _singleton.Objects)
        {
            spawner.FlagForRespawnOnDeath();
        }
    }
}
