using TMPro;
using UnityEngine;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] TextMeshProUGUI joinCodeText;
    [SerializeField] TMP_InputField joinCodeInput;
    [SerializeField] GameObject menuPanel; // drag your menu panel here

    // ================= HOST =================
    public async void HostGame()
    {
        Debug.Log("Starting Host...");

        string code = await RelayManager.Instance.StartHostWithRelay();

        Debug.Log("JOIN CODE: " + code);

        // show code on screen
        if (joinCodeText != null)
            joinCodeText.text = "Code: " + code;

        // hide menu
        if (menuPanel != null)
            menuPanel.SetActive(false);

        // load game scene (clients will follow automatically)
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

        await RelayManager.Instance.JoinRelay(code);

        // hide menu AFTER successful join
        if (menuPanel != null)
            menuPanel.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}