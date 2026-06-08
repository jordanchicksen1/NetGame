using UnityEngine;

public class MenuCanvasReviver : MonoBehaviour
{
    void Awake()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);

        if (canvas != null)
        {
            canvas.gameObject.SetActive(true);


            Debug.Log("MENU CANVAS RE-ENABLED");
        }
        else
        {
            Debug.LogError("No Canvas found!");
        }
    }
}