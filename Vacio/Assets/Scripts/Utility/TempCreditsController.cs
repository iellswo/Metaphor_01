using UnityEngine;

public class TempCreditsController : MonoBehaviour
{
    public void OnButton_Return()
    {
        Initiate.Fade("Main Menu", Color.black, 1f);
    }
}
