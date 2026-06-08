using Unity.Netcode;
using UnityEngine;

public class HidingSpot : NetworkBehaviour
{
    public PlayerController2D Occupant { get; private set; }

    public bool IsOccupied => Occupant != null;
    
    [SerializeField] AudioClip hideSound;
    [SerializeField] AudioClip exitSound;
    [SerializeField] AudioClip breakSound;
    AudioSource audioSource;
    [SerializeField] GameObject breakParticlesPrefab;

    void Awake()
    {
        audioSource = GetComponent<AudioSource>();
    }

    public bool TryHide(PlayerController2D player)
    {
        Debug.Log($"TryHide | IsServer={IsServer}");

        if (IsOccupied)
            return false;

        Occupant = player;

        PlayHideSoundClientRpc();

        return true;
    }

    public void ExitHide()
    {
        Occupant = null;

        PlayExitSoundClientRpc();
    }



    public void BreakSpot()
    {
        Debug.Log($"BreakSpot called. Occupant is null? {Occupant == null}");

        if (!IsServer)
            return;

        if (Occupant == null)
            return;

        SpawnBreakEffectsClientRpc();

        Occupant.ForceExitHide();

        GetComponent<NetworkObject>().Despawn();
    }

    public void SetOccupant(PlayerController2D player)
    {
        Occupant = player;
    }

    [ClientRpc]
    void PlayHideSoundClientRpc()
    {
        Debug.Log($"Hide Sound = {hideSound}");

        if (hideSound != null)
        {
            audioSource.PlayOneShot(hideSound);
        }
    }

    [ClientRpc]
    void PlayExitSoundClientRpc()
    {
        if (exitSound != null)
        {
            audioSource.PlayOneShot(exitSound);
        }
    }

    [ClientRpc]
    void SpawnBreakEffectsClientRpc()
    {
        if (breakParticlesPrefab != null)
        {
            Instantiate(
                breakParticlesPrefab,
                transform.position,
                Quaternion.identity
            );
        }

        if (breakSound != null)
        {
            GameObject tempAudio = new GameObject("BreakSound");

            AudioSource source =
                tempAudio.AddComponent<AudioSource>();

            source.PlayOneShot(breakSound);

            Destroy(
                tempAudio,
                breakSound.length
            );
        }
    }
}