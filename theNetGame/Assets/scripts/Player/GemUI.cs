using UnityEngine;
using TMPro;
using System.Collections;

public class GemUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI gemText;

    PlayerController2D player;

    void Start()
    {
        StartCoroutine(FindPlayerRoutine());
    }

    IEnumerator FindPlayerRoutine()
    {
        while (player == null)
        {
            var players = FindObjectsByType<PlayerController2D>(FindObjectsSortMode.None);

            foreach (var p in players)
            {
                if (p.IsOwner)
                {
                    player = p;
                    break;
                }
            }

            yield return null; 
        }
    }

    void Update()
    {
        if (player == null) return;

        gemText.text = $"Gems: {player.GetGemCount()} / 10";
    }
}