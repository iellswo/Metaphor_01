using UnityEngine;

public class NPCIntroController : MonoBehaviour
{
    public GameObject InvisibleWall;

    public void ZoneEntered()
    {
        Debug.Log("Entered NPC Introduction trigger zone.");
    }
}
