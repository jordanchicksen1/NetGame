using TMPro;
using UnityEngine;
using System.Collections;

public class CountdownManager : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI countdownText;

    IEnumerator Start()
    {
        PlayerController2D.CanMove = false;

        countdownText.gameObject.SetActive(true);

        countdownText.text = "3";
        yield return new WaitForSeconds(1f);

        countdownText.text = "2";
        yield return new WaitForSeconds(1f);

        countdownText.text = "1";
        yield return new WaitForSeconds(1f);

        countdownText.text = "GO!";

        PlayerController2D.CanMove = true;

        yield return new WaitForSeconds(1f);

        countdownText.gameObject.SetActive(false);
    }
}