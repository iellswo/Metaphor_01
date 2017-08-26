using UnityEngine;

public class PauseController : MonoBehaviour
{
    public Canvas MenuCanvas;
    public PlayerController Controller;

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

    #region Button Commands
    public void ContinuePressed()
    {
        UnpauseGame();
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
