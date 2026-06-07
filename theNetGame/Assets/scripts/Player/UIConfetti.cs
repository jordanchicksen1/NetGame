using UnityEngine;

public class UIConfetti : MonoBehaviour
{
    float speed;
    float lifeTimer;

    void Start()
    {
        speed = Random.Range(250f, 400f);
    }

    void Update()
    {
        lifeTimer += Time.unscaledDeltaTime;

        if (lifeTimer >= 4f)
        {
            Destroy(gameObject);
            return;
        }

        transform.Translate(
            Vector3.down *
            speed *
            Time.unscaledDeltaTime
        );
    }
}