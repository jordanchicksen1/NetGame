using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LocalUIButtons : MonoBehaviour
{
    public void StartButton()
    {
        StartCoroutine(StartTheGame());
    }

    public IEnumerator StartTheGame()
    {
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene("MainMenu");

    }
}
