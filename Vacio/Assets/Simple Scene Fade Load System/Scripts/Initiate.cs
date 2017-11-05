using UnityEngine;
using System.Collections;

public static class Initiate {
    //Create Fader object and assing the fade scripts and assign all the variables
    public static void Fade (string scene,Color col,float damp){
		GameObject init = new GameObject ();
		init.name = "Fader";
		init.AddComponent<Fader> ();
		Fader scr = init.GetComponent<Fader> ();
		scr.fadeDamp = damp;
		scr.fadeScene = scene;
		scr.fadeColor = col;
		scr.start = true;
	}

    public static void FadeOut(Color col, float damp)
    {
        GameObject init = new GameObject();
        init.name = "Fader";
        init.AddComponent<NonTransitionFader>();
        NonTransitionFader scr = init.GetComponent<NonTransitionFader>();
        scr.fadeDamp = damp;
        scr.fadeColor = col;
        scr.start = true;
    }

    public static void FadeIn(Color col, float damp)
    {
        NonTransitionFader fader = Object.FindObjectOfType<NonTransitionFader>();
        if (fader == null) return;

        fader.isFadeIn = true;
    }
}
