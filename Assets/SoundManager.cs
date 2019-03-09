using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    [SerializeField] AudioSource _audioSource;
    [SerializeField] AudioClip _step0;
    [SerializeField] AudioClip _step1;
    [SerializeField] AudioClip _step2;
    [SerializeField] AudioClip _squeak0;
    [SerializeField] AudioClip _squeak1;

    void RandomizeClips(bool randomizeVol = false, float randomPitchScale = 0.05f, params AudioClip[] clips)
    {
        int index = Random.Range(0, clips.Length);
        float pitch = Random.Range(1.0f - randomPitchScale, 1.0f + randomPitchScale);
        if (randomizeVol)
        {
            float volScale = Random.Range(0.5f, 1.0f);
            _audioSource.volume = volScale;
        }
        _audioSource.pitch = pitch;
        _audioSource.clip = clips[index];
        _audioSource.Play();
    }

    public void Step()
    {
        RandomizeClips(true, 0.05f, _step0);
    }

    public void Hit()
    {
        RandomizeClips(true, 0.5f, _squeak0, _squeak1);
    }
}
