using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    // public int TargetScene;
    public string TargetSceneName;

    public void TriggerLoadScene()
    {
        Initiate.Fade(TargetSceneName, Color.black, 1f);
        // SceneManager.LoadScene(TargetScene);
    }
}
