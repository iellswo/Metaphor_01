using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Platform : SpawnableObject
{
    public float TimeUntilFall = 2f;
    public Animator PlatformAnimator;

    private float _timeSinceLanding = 0f;
    private bool _landedOn = false;
    private float _timeSinceFalling = 0f;
    private bool _falling = false;

    private float _timeOfFall = 1.036f;

    // Use this for initialization
    void Start()
    {
        if (PlatformAnimator != null)
        {
            // TODO: Change this name when we make final plaform sprite and animations.
            PlatformAnimator.Play("anim_temp_platform");
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
                // TODO: Change this name when we make final plaform sprite and animations.
                PlatformAnimator.Play("anim_temp_fall");
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
        // TODO: Change this name when we make final plaform sprite and animations.
        PlatformAnimator.Play("anim_temp_shake");
        _landedOn = true;
    }

    protected override void ApplySpawnData(ObjectSpawner.SSpawnData[] data)
    {
        _timeSinceLanding = 0f;
        _landedOn = false;
        _timeSinceFalling = 0f;
        _falling = false;

        // TODO: Change this name when we make final plaform sprite and animations.
        PlatformAnimator.Play("anim_temp_platform");

    }
}
