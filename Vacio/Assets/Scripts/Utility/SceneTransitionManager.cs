using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneTransitionManager : MonoBehaviour
{
    public int TargetScene;
    // TODO: which entrance to the scene to load?

    public void TriggerLoadScene()
    {
        SceneManager.LoadScene(TargetScene);
    }
}
