using UnityEngine;
using UnityEngine.SceneManagement;

public class MenuCanvasReviver : MonoBehaviour
{
    [SerializeField] GameObject menuCanvas;

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
        if (scene.name == "MainMenu")
        {
            menuCanvas.SetActive(true);

            Debug.Log("MENU CANVAS RE-ENABLED");
        }
    }
}