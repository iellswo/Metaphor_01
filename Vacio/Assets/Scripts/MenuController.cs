using UnityEngine;

public class MenuController : MonoBehaviour
{
    // public int StartingSceneIndex = 0;
    public string StartingSceneName = "Level1";
    public string CreditsSceneName = "Temp Credits";

    public void OnButton_Start()
    {
        Initiate.Fade(StartingSceneName, Color.black, 1f);
        // SceneManager.LoadScene(StartingSceneIndex);
    }

    public void OnButton_Credits()
    {
        Initiate.Fade(CreditsSceneName, Color.black, 1f);
        // SceneManager.LoadScene(StartingSceneIndex);
    }

    public void OnButton_Quit()
    {
        Application.Quit();
    }
}
