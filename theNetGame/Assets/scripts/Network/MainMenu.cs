using TMPro;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TextMeshProUGUI joinCodeText;
    [SerializeField] TMP_InputField joinCodeInput;
    [SerializeField] TextMeshProUGUI statusText; // 👈 NEW
    [SerializeField] GameObject menuPanel;

    void Start()
    {
        if (statusText != null)
            statusText.text = "Not connected";
    }

    // ================= HOST =================
    public async void HostGame()
    {
        Debug.Log("Starting Host...");

        string code = await RelayManager.Instance.StartHostWithRelay();

        Debug.Log("JOIN CODE: " + code);

        if (joinCodeText == null)
        {
            Debug.LogError("JoinCodeText is NOT assigned!");
            return;
        }

        joinCodeText.gameObject.SetActive(true);
        joinCodeText.text = "Code: " + code;

        // Optional host feedback
        if (statusText != null)
            statusText.text = "Hosting...\nWaiting for player...";
    }

    public void StartGame()
    {
        int count = NetworkManager.Singleton.ConnectedClientsList.Count;

        Debug.Log("Players connected: " + count);

        if (count < 2)
        {
            Debug.Log("Not enough players yet!");
            if (statusText != null)
                statusText.text = "Waiting for player...";
            return;
        }

        Debug.Log("Starting Game Scene...");

        if (menuPanel != null)
            menuPanel.SetActive(false);

        NetworkManager.Singleton.SceneManager.LoadScene(
            "Game",
            LoadSceneMode.Single
        );
    }

    // ================= CLIENT =================
    public async void JoinGame()
    {
        string code = joinCodeInput.text;

        Debug.Log("Joining with code: " + code);

        if (statusText != null)
            statusText.text = "Connecting...";

        try
        {
            await RelayManager.Instance.JoinRelay(code);

            Debug.Log("Joined relay successfully");

            if (statusText != null)
                statusText.text = "Connected!\nHost must press Start!";
        }
        catch
        {
            Debug.LogError("Failed to join relay");

            if (statusText != null)
                statusText.text = "Connection failed.\nCheck code and try again.";
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    // ================= SCENE HANDLING =================
    void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (scene.name == "Game")
        {
            if (menuPanel != null)
            {
                menuPanel.SetActive(false);
            }
        }
    }
}