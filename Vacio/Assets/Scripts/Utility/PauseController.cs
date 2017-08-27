using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseController : MonoBehaviour
{
    public Canvas MenuCanvas;
    public PlayerController Controller;

    private string MainMenuLevelName = "Main Menu";

    // Use this for initialization
    void Start()
    {
        UnpauseGame();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        if (Input.GetButtonDown("Pause") && Time.timeScale == 1)
        {
            PauseGame();
        }
        else if ((Input.GetButtonDown("Pause") || Input.GetButtonDown("Cancel")) && Time.timeScale == 0)
        {
            UnpauseGame();
        }
    }

    #region Button Callbacks
    public void OnButton_Continue()
    {
        UnpauseGame();
    }

    public void OnButton_Restart()
    {
        Initiate.Fade(SceneManager.GetActiveScene().name, Color.black, 1.5f);
        UnpauseGame();
    }

    public void OnButton_MainMenu()
    {
        Initiate.Fade(MainMenuLevelName, Color.black, 1f);
        UnpauseGame();
    }

    public void OnButton_Quit()
    {
        Application.Quit();
    }
    #endregion

    private void PauseGame()
    {
        Time.timeScale = 0;
        MenuCanvas.gameObject.SetActive(true);
        Controller.enabled = false;
    }

    private void UnpauseGame()
    {
        Time.timeScale = 1;
        MenuCanvas.gameObject.SetActive(false);
        Controller.enabled = true;
    }
}
