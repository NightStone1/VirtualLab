using UnityEngine;
using UnityEngine.UI;

public class SyncGeneratorHud : MonoBehaviour
{
    [SerializeField] private Text mainText;
    [SerializeField] private Text hintText;

    private void Awake()
    {
        if (mainText == null)
        {
            mainText = GetComponent<Text>();
        }

        DisableRaycasts(mainText);
        DisableRaycasts(hintText);
    }

    public void SetMainText(Text value)
    {
        mainText = value;
        DisableRaycasts(mainText);
    }

    public void SetHintText(Text value)
    {
        hintText = value;
        DisableRaycasts(hintText);
    }

    public void SetHudVisible(bool visible)
    {
        gameObject.SetActive(true);

        if (mainText != null)
        {
            mainText.gameObject.SetActive(true);
            mainText.enabled = true;
            mainText.raycastTarget = false;
        }

        if (hintText != null)
        {
            hintText.gameObject.SetActive(true);
            hintText.enabled = false;
            hintText.raycastTarget = false;
        }
    }

    public void SetText(string value)
    {
        if (mainText == null)
        {
            mainText = GetComponent<Text>();
        }

        if (mainText != null)
        {
            mainText.text = value;
        }
    }

    public void SetHint(string value)
    {
        if (hintText != null)
        {
            hintText.text = value;
            hintText.raycastTarget = false;
        }
    }

    private void DisableRaycasts(Text target)
    {
        if (target != null)
        {
            target.raycastTarget = false;
        }
    }
}
