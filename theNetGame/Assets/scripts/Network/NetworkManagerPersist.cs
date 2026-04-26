using UnityEngine;

public class NetworkManagerPersist : MonoBehaviour
{
    static bool alreadyExists = false;

    void Awake()
    {
        if (alreadyExists)
        {
            Destroy(gameObject);
            return;
        }

        alreadyExists = true;
        DontDestroyOnLoad(gameObject);
    }
}