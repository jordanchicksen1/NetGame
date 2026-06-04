using UnityEngine;
using TMPro;

public class CoinUI : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI coinText;

    PlayerController2D player;

    void Update()
    {
        
        if (player == null)
        {
            FindLocalPlayer();
            return;
        }

        coinText.text = $"{player.GetCoinCount()}/8";
    }

    void FindLocalPlayer()
    {
        var players = FindObjectsByType<PlayerController2D>(FindObjectsSortMode.None);

        foreach (var p in players)
        {
            if (p.IsOwner)
            {
                player = p;
                Debug.Log("UI linked to player: " + p.OwnerClientId);
                return;
            }
        }
    }
}