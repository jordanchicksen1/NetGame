using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;
using static GemManager;

[System.Serializable]
public class PodiumSlot
{
    public GameObject root;
    public UnityEngine.UI.Image playerImage;
    public TextMeshProUGUI playerName;
    public TextMeshProUGUI gemCount;
}

public class WinUI : NetworkBehaviour 
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
    [SerializeField] GameObject confettiPrefab;
    [SerializeField] Transform confettiContainer;


    void Awake()
    {
        Instance = this;
        panel.SetActive(false);

        
    }

    public void ShowWin(PlayerResultData[] results)
    {
        panel.SetActive(true);
        MusicManager.Instance.PlayVictoryMusic();

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
        SpawnConfetti(GetWinnerColor(results[0].playerId));

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

    void SpawnConfetti(Color color)
    {
        for (int i = 0; i < 100; i++)
        {
            GameObject piece = Instantiate(confettiPrefab, confettiContainer);

            RectTransform rect = piece.GetComponent<RectTransform>();

            RectTransform containerRect = confettiContainer.GetComponent<RectTransform>();

            float width = containerRect.rect.width;
            float height = containerRect.rect.height;

            rect.anchoredPosition = new Vector2(Random.Range( -width * 0.5f, width * 0.5f), height * 0.5f + Random.Range(0f, 200f));

            Image image = piece.GetComponent<Image>();

            image.color = color;

            Debug.Log($"Container Width: {width}");
            Debug.Log($"Container Height: {height}");
        }
        
    }

    Color GetWinnerColor(ulong playerId)
    {
        switch (playerId)
        {
            case 0:
                return Color.blue;

            case 1:
                return Color.red;

            case 2:
                return Color.green;

            case 3:
                return Color.yellow;

            default:
                return Color.white;
        }
    }

    [ClientRpc]
    void LoadMainMenuClientRpc()
    {
        Debug.Log("CLIENT RECEIVED RETURN TO LOBBY RPC");
        StartCoroutine(
            ShutdownAndReturnToMenu());
    }

    public void ReturnToLobby()
    {
        Debug.Log("Return to Lobby has been pressed");

        if (NetworkManager.Singleton.IsHost)
        {
            LoadMainMenuClientRpc();

            StartCoroutine(ShutdownAndReturnToMenu());
        }
        else
        {
            RequestReturnToLobbyRpc();
        }
    }


    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    void RequestReturnToLobbyRpc()
    {
        LoadMainMenuClientRpc();

        StartCoroutine(ShutdownAndReturnToMenu());
    }

    IEnumerator ShutdownAndReturnToMenu()
    {
        Time.timeScale = 1f;

        ShutdownNetwork();

        yield return null;
        yield return null;

        SceneManager.LoadScene(
            "MainMenu",
            LoadSceneMode.Single);
    }

    void ShutdownNetwork()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();

            //NetworkManagerPersist.ResetPersistence();

            //Destroy(NetworkManager.Singleton.gameObject);
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}