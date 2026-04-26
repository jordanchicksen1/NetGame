using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneLoadDebugger : MonoBehaviour
{
    void Awake()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log("SCENE LOADED: " + scene.name);
        Debug.Log("StackTrace:\n" + System.Environment.StackTrace);
    }
}