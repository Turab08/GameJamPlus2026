using System;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [SerializeField] private AudioSource sfxSource;
    public AudioSource submarineSource;

    public AudioClip WallExplosion;
    public AudioClip VictorySound;
    public AudioClip GameOverSound;
    public AudioClip SubmarineMoving;
    public AudioClip MineExplosion;
    public AudioClip GaiserSound;



    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
    }

    public void PlaySFX(AudioClip audioClip)
    {
        sfxSource.PlayOneShot(audioClip);
    }

    public void PlaySubmarine()
    {
        submarineSource.Play();
    }
    public void StopSubmarine()
    {
        submarineSource.Stop();
    }
}
