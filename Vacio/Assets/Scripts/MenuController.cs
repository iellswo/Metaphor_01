using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuController : MonoBehaviour
{
    // public int StartingSceneIndex = 0;
    public string StartingSceneName = "Level1";

    public void OnButton_Start()
    {
        Initiate.Fade(StartingSceneName, Color.black, 1f);
        // SceneManager.LoadScene(StartingSceneIndex);
    }

    public void OnButton_Quit()
    {
        Application.Quit();
    }
}
