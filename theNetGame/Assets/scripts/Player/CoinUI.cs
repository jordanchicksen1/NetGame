using UnityEngine;
using TMPro;

public class CoinUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI coinText;

    PlayerController2D player;

    void Start()
    {
        FindLocalPlayer();
    }

    void Update()
    {
        if (player == null) return;

        coinText.text = $"Coins: {player.GetCoinCount()} / 8";
    }

    void FindLocalPlayer()
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
    }
}