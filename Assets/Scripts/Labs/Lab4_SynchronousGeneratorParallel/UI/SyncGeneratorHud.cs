using UnityEngine;
using UnityEngine.UI;

public class SyncGeneratorHud : MonoBehaviour
{
    [SerializeField] private Text text;

    private void Awake()
    {
        if (text == null)
        {
            text = GetComponent<Text>();
        }
    }

    public void SetText(string value)
    {
        if (text == null)
        {
            text = GetComponent<Text>();
        }

        if (text != null)
        {
            text.text = value;
        }
    }
}
