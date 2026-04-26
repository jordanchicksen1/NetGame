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

        if (joinCodeText == null)
        {
            Debug.LogError("JoinCodeText is NOT assigned!");
            return;
        }

        joinCodeText.gameObject.SetActive(true);
        joinCodeText.text = "Code: " + code;
    }

    public void StartGame()
    {
        Debug.Log("Starting Game Scene...");

        if (menuPanel != null)
            menuPanel.SetActive(false);

        NetworkManager.Singleton.SceneManager.LoadScene(
            "Game",
            UnityEngine.SceneManagement.LoadSceneMode.Single
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