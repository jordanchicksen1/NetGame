using UnityEngine;
using TMPro;
using UnityEngine.SceneManagement;
using Unity.Netcode;

public class WinUI : MonoBehaviour
{
    public static WinUI Instance;

    [SerializeField] GameObject panel;
    [SerializeField] TextMeshProUGUI winText;

    void Awake()
    {
        Instance = this;
        panel.SetActive(false);
    }

    public void ShowWin(ulong winnerId)
    {
        panel.SetActive(true);
        winText.text = $"Player {winnerId + 1} Wins!";

        
        Time.timeScale = 0f;
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