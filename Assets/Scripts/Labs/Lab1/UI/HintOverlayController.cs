using TMPro;
using UnityEngine;

public class HintOverlayController : MonoBehaviour
{
    [SerializeField] private LabResultsManager resultsManager;

    [Header("UI")]
    [SerializeField] private GameObject root;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text shortHintText;
    [SerializeField] private TMP_Text detailedHintText;

    private void Start()
    {
        RefreshForCurrentMode();
        Hide();
    }

    public void Show()
    {
        RefreshForCurrentMode();

        if (root != null)
            root.SetActive(true);
        else
            gameObject.SetActive(true);
    }

    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
        else
            gameObject.SetActive(false);
    }

    public void RefreshForCurrentMode()
    {
        if (resultsManager == null)
        {
            Debug.LogError("HintOverlayController: resultsManager не назначен.");
            return;
        }

        Fill(resultsManager.CurrentMode);
    }

    private void Fill(LabMode mode)
    {
        switch (mode)
        {
            case LabMode.Table22_Working:
                SetTexts(
                    "Таблица 2.2 — Рабочие характеристики",
                    "Снимаются рабочие характеристики двигателя при U = const и If = const.",
                    "Что делать:\n" +
                    "1. Установить номинальное напряжение якоря.\n" +
                    "2. Установить номинальный ток возбуждения.\n" +
                    "3. Начиная с холостого хода, постепенно увеличивать нагрузку.\n" +
                    "4. Для каждой точки снимать измерения.\n\n" +
                    "Что должно быть постоянным:\n" +
                    "- U = Un = const\n" +
                    "- If = Ifn = const\n\n" +
                    "Что строится:\n" +
                    "- Ia = f(P2)\n" +
                    "- M = f(P2)\n" +
                    "- ω = f(P2)\n" +
                    "- η = f(P2)\n\n" +
                    "Типичные ошибки:\n" +
                    "- снимали точки до стабилизации режима;\n" +
                    "- изменялся ток возбуждения;\n" +
                    "- точки сняты в слишком узком диапазоне нагрузки."
                );
                break;

            case LabMode.Table23_OmegaFromU:
                SetTexts(
                    "Таблица 2.3 — Зависимость ω = f(U)",
                    "Снимается регулировочная характеристика ω = f(U) при If = const и M = const.",
                    "Что делать:\n" +
                    "1. Установить номинальный ток возбуждения.\n" +
                    "2. Установить постоянную нагрузку на валу.\n" +
                    "3. Постепенно изменять напряжение якоря U.\n" +
                    "4. После стабилизации режима снимать n и вычислять ω.\n\n" +
                    "Что должно быть постоянным:\n" +
                    "- If = Ifn = const\n" +
                    "- M = const\n\n" +
                    "Что строится:\n" +
                    "- ω = f(U)\n\n" +
                    "Типичные ошибки:\n" +
                    "- во время опыта изменилась нагрузка;\n" +
                    "- ток возбуждения не удерживался постоянным;\n" +
                    "- снимались точки до установления скорости."
                );
                break;

            case LabMode.Table24_IfFromIa:
                SetTexts(
                    "Таблица 2.4 — Зависимость If = f(Ia)",
                    "Снимается регулировочная характеристика If = f(Ia) при U = const и n = const.",
                    "Что делать:\n" +
                    "1. Установить постоянное напряжение якоря.\n" +
                    "2. Поддерживать постоянную скорость вращения.\n" +
                    "3. Изменять нагрузку и регулировать ток возбуждения так, чтобы скорость оставалась постоянной.\n" +
                    "4. Для каждой точки записывать Ia и If.\n\n" +
                    "Что должно быть постоянным:\n" +
                    "- U = const\n" +
                    "- n = const\n\n" +
                    "Что строится:\n" +
                    "- If = f(Ia)\n\n" +
                    "Типичные ошибки:\n" +
                    "- скорость реально не удерживалась постоянной;\n" +
                    "- напряжение якоря плавало;\n" +
                    "- точки сняты слишком редко."
                );
                break;

            case LabMode.Table25_OmegaFromIf:
                SetTexts(
                    "Таблица 2.5 — Зависимость ω = f(If)",
                    "Снимается регулировочная характеристика ω = f(If) при U = const и M = const.",
                    "Что делать:\n" +
                    "1. Установить постоянное напряжение якоря.\n" +
                    "2. Установить постоянную нагрузку.\n" +
                    "3. Изменять ток возбуждения If.\n" +
                    "4. После стабилизации записывать скорость n и вычислять ω.\n\n" +
                    "Что должно быть постоянным:\n" +
                    "- U = const\n" +
                    "- M = const\n\n" +
                    "Что строится:\n" +
                    "- ω = f(If)\n\n" +
                    "Типичные ошибки:\n" +
                    "- менялась нагрузка;\n" +
                    "- напряжение якоря не удерживалось постоянным;\n" +
                    "- снимались точки вне устойчивого режима."
                );
                break;

            default:
                SetTexts(
                    "Подсказка",
                    "Выберите таблицу.",
                    "После выбора таблицы здесь появится методическая подсказка по текущему режиму."
                );
                break;
        }
    }

    private void SetTexts(string title, string shortHint, string detailedHint)
    {
        if (titleText != null)
            titleText.text = title;

        if (shortHintText != null)
            shortHintText.text = shortHint;

        if (detailedHintText != null)
            detailedHintText.text = detailedHint;
    }
}