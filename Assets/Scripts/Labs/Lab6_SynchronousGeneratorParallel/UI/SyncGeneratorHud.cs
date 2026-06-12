using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SyncGeneratorHud : MonoBehaviour
{
    [SerializeField] private TMP_Text mainText;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private Graphic mainBackground;
    [SerializeField] private Graphic hintBackground;

    private void Awake()
    {
        transform.localScale = Vector3.one;

        if (mainText == null)
        {
            mainText = GetComponent<TMP_Text>();
        }

        NormalizeScale(mainText);
        NormalizeScale(hintText);
        NormalizeScale(mainBackground);
        NormalizeScale(hintBackground);
        DisableRaycasts(mainText);
        DisableRaycasts(hintText);
        DisableRaycasts(mainBackground);
        DisableRaycasts(hintBackground);
    }

    public void SetMainText(TMP_Text value)
    {
        mainText = value;
        NormalizeScale(mainText);
        DisableRaycasts(mainText);
    }

    public void SetHintText(TMP_Text value)
    {
        hintText = value;
        NormalizeScale(hintText);
        DisableRaycasts(hintText);
    }

    public void SetMainBackground(Graphic value)
    {
        mainBackground = value;
        NormalizeScale(mainBackground);
        DisableRaycasts(mainBackground);
    }

    public void SetHintBackground(Graphic value)
    {
        hintBackground = value;
        NormalizeScale(hintBackground);
        DisableRaycasts(hintBackground);
    }

    public void SetHudVisible(bool visible)
    {
        gameObject.SetActive(true);

        if (mainText != null)
        {
            NormalizeScale(mainText);
            mainText.gameObject.SetActive(true);
            mainText.enabled = visible;
            mainText.raycastTarget = false;
        }

        if (mainBackground != null)
        {
            NormalizeScale(mainBackground);
            mainBackground.gameObject.SetActive(true);
            mainBackground.enabled = visible;
            mainBackground.raycastTarget = false;
        }

        if (hintText != null)
        {
            NormalizeScale(hintText);
            hintText.gameObject.SetActive(true);
            hintText.enabled = !visible;
            hintText.raycastTarget = false;
        }

        if (hintBackground != null)
        {
            NormalizeScale(hintBackground);
            hintBackground.gameObject.SetActive(true);
            hintBackground.enabled = !visible;
            hintBackground.raycastTarget = false;
        }
    }

    public void SetText(string value)
    {
        if (mainText == null)
        {
            mainText = GetComponent<TMP_Text>();
        }

        if (mainText != null)
        {
            NormalizeScale(mainText);
            mainText.text = value;
        }
    }

    public void SetHint(string value)
    {
        if (hintText != null)
        {
            NormalizeScale(hintText);
            hintText.text = value;
            hintText.raycastTarget = false;
        }
    }

    private void NormalizeScale(Component target)
    {
        if (target != null)
        {
            target.transform.localScale = Vector3.one;
        }
    }

    private void DisableRaycasts(TMP_Text target)
    {
        if (target != null)
        {
            target.raycastTarget = false;
        }
    }

    private void DisableRaycasts(Graphic target)
    {
        if (target != null)
        {
            target.raycastTarget = false;
        }
    }
}
