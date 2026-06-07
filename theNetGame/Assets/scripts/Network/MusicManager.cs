using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance;

    [SerializeField] AudioSource musicSource;
    [SerializeField] AudioClip gameplayMusic;
    [SerializeField] AudioClip victoryMusic;

    void Awake()
    {
        Instance = this;
    }

    

    public void PlayVictoryMusic()
    {
        musicSource.Stop();

        musicSource.clip = victoryMusic;
        musicSource.Play();
    }
}