using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleCoordinationDirector : MonoBehaviour
{
    public ParticleSystem DirtParticles;
    public ParticleSystem GrassParticles;
    public ParticleSystem MushroomParticles;
    
    public void PlayParticles()
    {
        ClearAll();
        PlayAll();
    }

    private void ClearAll()
    {
        DirtParticles.Clear();
        GrassParticles.Clear();
        MushroomParticles.Clear();
    }

    private void PlayAll()
    {
        DirtParticles.Play();
        GrassParticles.Play();
        MushroomParticles.Play();
    }
}
