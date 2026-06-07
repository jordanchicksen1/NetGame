using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using static GemManager;

[System.Serializable]
public class PodiumSlot
{
    public GameObject root;
    public UnityEngine.UI.Image playerImage;
    public TextMeshProUGUI playerName;
    public TextMeshProUGUI gemCount;
}

public class WinUI : MonoBehaviour
{
    public class PlayerResult
    {
        public ulong playerId;
        public int gemCount;
    }

    public static WinUI Instance;

    [SerializeField] GameObject panel;
    [SerializeField] TextMeshProUGUI winText;
    [SerializeField] PodiumSlot firstPlace;
    [SerializeField] PodiumSlot secondPlace;
    [SerializeField] PodiumSlot thirdPlace;
    [SerializeField] PodiumSlot fourthPlace;
    [SerializeField] Sprite player1Sprite;
    [SerializeField] Sprite player2Sprite;
    [SerializeField] Sprite player3Sprite;
    [SerializeField] Sprite player4Sprite;


    void Awake()
    {
        Instance = this;
        panel.SetActive(false);

        
    }

    public void ShowWin(PlayerResultData[] results)
    {
        panel.SetActive(true);

        List<PlayerResult> leaderboard =
            new List<PlayerResult>();

        foreach (var result in results)
        {
            leaderboard.Add(
                new PlayerResult
                {
                    playerId = result.playerId,
                    gemCount = result.gemCount
                });
        }

        PopulatePodium(leaderboard);

        winText.text =
            $" Player {results[0].playerId + 1} Wins! ";

        Time.timeScale = 0f;
    }



    void PopulatePodium(List<PlayerResult> results)
    {
        PodiumSlot[] slots =
        {
        firstPlace,
        secondPlace,
        thirdPlace,
        fourthPlace
    };

        for (int i = 0; i < slots.Length; i++)
        {
            if (i >= results.Count)
            {
                slots[i].root.SetActive(false);
                continue;
            }

            slots[i].root.SetActive(true);

            slots[i].playerName.text =
                $"Player {results[i].playerId + 1}";

            slots[i].gemCount.text =
                $"{results[i].gemCount} Gems";

            slots[i].playerImage.sprite = GetPlayerSprite(results[i].playerId);
        }
    }

    Sprite GetPlayerSprite(ulong playerId)
    {
        switch (playerId)
        {
            case 0:
                return player1Sprite;

            case 1:
                return player2Sprite;

            case 2:
                return player3Sprite;

            case 3:
                return player4Sprite;

            default:
                return null;
        }
    }

    public void ReturnToLobby()
    {
        Time.timeScale = 1f;

        if (NetworkManager.Singleton.IsHost)
        {
            NetworkManager.Singleton.SceneManager.LoadScene("MainMenu", LoadSceneMode.Single);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}