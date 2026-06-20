using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Lab4UCurveTableView : MonoBehaviour
{
    public SyncGeneratorLabController controller;
    public bool autoFindController = true;
    public TMP_Text targetText;
    public TMP_Text currentSeriesText;
    public bool autoFindText = true;
    public bool showNoLoad = true;
    public bool showHalfLoad = true;
    public bool showFullLoad = true;
    public int maxRowsPerSeries = 10;
    public bool refreshEveryFrame = false;

    private readonly StringBuilder builder = new StringBuilder(2048);

    private void Awake()
    {
        ResolveReferences();
    }

    private void OnEnable()
    {
        ResolveReferences();
        if (controller != null)
        {
            controller.OnUCurveChanged += Refresh;
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (controller != null)
        {
            controller.OnUCurveChanged -= Refresh;
        }
    }

    private void Update()
    {
        if (refreshEveryFrame)
        {
            RefreshInternal(false);
        }
    }

    public void Refresh()
    {
        RefreshInternal(true);
    }

    private void RefreshInternal(bool rebuildLayout)
    {
        ResolveReferences();
        if (targetText == null)
        {
            UpdateCurrentSeriesText();
            if (rebuildLayout)
            {
                RebuildLayouts();
            }

            return;
        }

        if (controller == null)
        {
            targetText.text = "Нет данных U-кривых";
            UpdateCurrentSeriesText();
            if (rebuildLayout)
            {
                RebuildLayouts();
            }

            return;
        }

        UpdateCurrentSeriesText();
        builder.Clear();

        if (showNoLoad)
        {
            AppendSeries("P0", UCurveSeries.NoLoad);
        }

        if (showHalfLoad)
        {
            AppendSeries("0.5Pн", UCurveSeries.HalfLoad);
        }

        if (showFullLoad)
        {
            AppendSeries("Pн", UCurveSeries.FullLoad);
        }

        targetText.text = builder.ToString();
        if (rebuildLayout)
        {
            RebuildLayouts();
        }
    }

    private void AppendSeries(string title, UCurveSeries series)
    {
        IReadOnlyList<UCurvePoint> points = controller.GetUCurvePoints(series);
        int count = points.Count;
        int rows = Mathf.Min(Mathf.Max(0, maxRowsPerSeries), count);

        builder.AppendLine(title);
        builder.AppendLine("№ | If | Iст | Iакт | Iреакт | cosφ | P%");

        if (rows == 0)
        {
            builder.AppendLine("нет точек");
            builder.AppendLine();
            return;
        }

        int startIndex = Mathf.Max(0, count - rows);
        for (int i = startIndex; i < count; i++)
        {
            UCurvePoint point = points[i];
            builder.Append(i + 1).Append(" | ")
                .Append(point.If.ToString("F2")).Append(" | ")
                .Append(point.Istat.ToString("F2")).Append(" | ")
                .Append(point.Iactive.ToString("F2")).Append(" | ")
                .Append(point.Ireactive.ToString("F2")).Append(" | ")
                .Append(FormatCosPhi(point)).Append(" | ")
                .Append(point.loadPercent.ToString("F0"))
                .AppendLine();
        }

        builder.AppendLine();
    }

    private string FormatCosPhi(UCurvePoint point)
    {
        if (point.loadPercent <= 0.001f || Mathf.Abs(point.Iactive) <= 0.001f || point.Istat <= 0.001f)
        {
            return "—";
        }

        return point.cosPhi.ToString("F2");
    }

    private void UpdateCurrentSeriesText()
    {
        if (currentSeriesText == null)
        {
            return;
        }

        if (controller == null)
        {
            currentSeriesText.text = "Текущая серия: нет данных\nДля U-кривой: настройте РНО, изменяйте R1 и нажимайте «Записать точку».";
            return;
        }

        currentSeriesText.text = controller.GetCurrentPowerSeriesStatusText()
            + "\nДля U-кривой: настройте РНО в диапазон P0, 0.5Pн или Pн, изменяйте R1 и нажимайте «Записать точку».";
    }

    private string GetSeriesDisplayName(UCurveSeries series)
    {
        switch (series)
        {
            case UCurveSeries.HalfLoad:
                return "0.5Pн";
            case UCurveSeries.FullLoad:
                return "Pн";
            default:
                return "P0";
        }
    }

    private void RebuildLayouts()
    {
        RebuildLayout(targetText);
        RebuildLayout(currentSeriesText);
    }

    private void RebuildLayout(TMP_Text text)
    {
        if (text == null)
        {
            return;
        }

        RectTransform rectTransform = text.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(rectTransform);
        }
    }

    private void ResolveReferences()
    {
        if (controller == null && autoFindController)
        {
            controller = FindFirstObjectByType<SyncGeneratorLabController>();
        }

        if (targetText == null && autoFindText)
        {
            targetText = GetComponent<TMP_Text>();
            if (targetText == null)
            {
                targetText = GetComponentInChildren<TMP_Text>(true);
            }
        }
    }
}
