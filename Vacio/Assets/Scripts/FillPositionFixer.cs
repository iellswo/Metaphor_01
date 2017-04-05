using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FillPositionFixer : MonoBehaviour
{
    public SpriteRenderer renderer;

    private float lastSavedX = 0f;
	
	// Late Update is called after update each frame
	void LateUpdate ()
    {
        if (renderer.flipX && gameObject.transform.localPosition.x != lastSavedX * -1)
        {
            var pos = gameObject.transform.localPosition;
            lastSavedX = pos.x;
            pos.x = lastSavedX * -1;
            gameObject.transform.localPosition = pos;
        }
	}
}
