using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class LocalUIButtons : MonoBehaviour
{
    public GameObject controlsPage;
    public GameObject howToStartAGamePage;
    public GameObject howToPlayPanel;
    public GameObject instruction1;
    public GameObject instruction2;
    public GameObject instruction3;
    public void StartButton()
    {
        StartCoroutine(StartTheGame());
    }

    public IEnumerator StartTheGame()
    {
        yield return new WaitForSeconds(0.5f);
        SceneManager.LoadScene("MainMenu");

    }

    public void ControlsPageButton()
    {
        controlsPage.SetActive(true);
    }

    public void ExitControlsPageButton()
    {
        controlsPage.SetActive(false);
    }

    public void HowToStartPageButton()
    {
        howToStartAGamePage.SetActive(true);
    }

    public void ExitHowToStartPageButton()
    {
        howToStartAGamePage.SetActive(false);
    }

    public void HowToPlayButton()
    {
        howToPlayPanel.SetActive(true);
        instruction1.SetActive(true);
        instruction2.SetActive(false);
        instruction3.SetActive(false);
    }

    public void nextInstruction1()
    {
        instruction1.SetActive(false);
        instruction2.SetActive(true);
        instruction3.SetActive(false);
    }

    public void nextInstruction2()
    {
        instruction1.SetActive(false);
        instruction2.SetActive(false);
        instruction3.SetActive(true);
    }

    public void previousInstruction2()
    {
        instruction1.SetActive(true);
        instruction2.SetActive(false);
        instruction3.SetActive(false);
    }

    public void previousInstruction3()
    {
        instruction1.SetActive(false);
        instruction2.SetActive(true);
        instruction3.SetActive(false);
    }

    public void ExitHowToPlayButton()
    {
        howToPlayPanel.SetActive(false);
        instruction1.SetActive(false);
        instruction2.SetActive(false);
        instruction3.SetActive(false);
    }
}
