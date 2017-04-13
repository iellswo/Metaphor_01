using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Platform : SpawnableObject
{
    public float TimeUntilFall = 2f;
    public Animator PlatformAnimator;
    public ParticleCoordinationDirector Particles;

    public string PlatformStableAnim = "platform_stationary";
    public string PlatformShakingAnim = "platform_shaking";
    public string PlatformVanishAnim = "platform_vanish";

    private float _timeSinceLanding = 0f;
    private bool _landedOn = false;
    private float _timeSinceFalling = 0f;
    private bool _falling = false;

    private float _timeOfFall = 1f;

    // Use this for initialization
    void Start()
    {
        if (PlatformAnimator != null)
        {
            // TODO: Change this name when we make final plaform sprite and animations.
            PlatformAnimator.Play(PlatformStableAnim);
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (_landedOn)
        {
            _timeSinceLanding += Time.deltaTime;
            if (_timeSinceLanding >= TimeUntilFall)
            {
                PlatformAnimator.Play(PlatformVanishAnim);
                if (Particles != null)
                {
                    Particles.PlayParticles();
                }

                _falling = true;
                _landedOn = false;
            }
        }
        else if (_falling)
        {
            _timeSinceFalling += Time.deltaTime;
            if (_timeSinceFalling >= _timeOfFall)
            {
                DeSpawn();
            }
        }
    }

    public void ReportLandedOn()
    {
        PlatformAnimator.Play(PlatformShakingAnim);
        _landedOn = true;
    }

    protected override void ApplySpawnData(ObjectSpawner.SSpawnData[] data)
    {
        _timeSinceLanding = 0f;
        _landedOn = false;
        _timeSinceFalling = 0f;
        _falling = false;
        
        PlatformAnimator.Play(PlatformStableAnim);

    }
}
