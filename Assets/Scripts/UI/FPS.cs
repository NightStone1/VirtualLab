using TMPro;
using UnityEngine;

public class FPS : MonoBehaviour
{
    public TextMeshProUGUI text;
    private float timer;
    private int frames;

    void Update()
    {
        frames++;
        timer += Time.unscaledDeltaTime;

        if (timer >= 0.5f)
        {
            float fps = frames / timer;
            text.text = $"FPS: {fps:0}";
            timer = 0f;
            frames = 0;
        }
    }
}