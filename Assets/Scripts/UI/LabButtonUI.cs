using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LabButtonUI : MonoBehaviour
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text themeText;
    [SerializeField] private Button button;

    public void Init(string title, string theme, UnityEngine.Events.UnityAction onClick)
    {
        titleText.text = title;
        themeText.text = theme;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(onClick);
    }
}