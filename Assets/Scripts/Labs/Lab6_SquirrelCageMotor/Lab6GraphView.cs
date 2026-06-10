using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Lab6GraphView : MonoBehaviour
{
    [SerializeField] private RectTransform graphArea;
    [SerializeField] private Image pointPrefab;
    [SerializeField] private Image linePrefab;
    [SerializeField] private TextMeshProUGUI graphTitleText;
    [SerializeField] private TextMeshProUGUI graphLegendText;

    private readonly List<GameObject> spawnedObjects = new List<GameObject>();
    private string lastGraphTitle;
    private int lastGraphPointCount = -1;
    private bool missingGraphSetupWarningLogged;

    public void ShowCurrentByPowerGraph(IReadOnlyList<Lab6Measurement> points)
    {
        ShowGraph(points, "I1 = f(P2)", "X: P2, Вт; Y: I1, А", p => p.powerOutput, p => p.current);
    }

    public void ShowSpeedByPowerGraph(IReadOnlyList<Lab6Measurement> points)
    {
        ShowGraph(points, "n2 = f(P2)", "X: P2, Вт; Y: n2, об/мин", p => p.powerOutput, p => p.speed);
    }

    public void ShowEfficiencyByPowerGraph(IReadOnlyList<Lab6Measurement> points)
    {
        ShowGraph(points, "η = f(P2)", "X: P2, Вт; Y: η", p => p.powerOutput, p => p.efficiency);
    }

    public void ClearGraph()
    {
        for (int i = spawnedObjects.Count - 1; i >= 0; i--)
        {
            if (spawnedObjects[i] != null)
            {
                Destroy(spawnedObjects[i]);
            }
        }

        spawnedObjects.Clear();
        lastGraphTitle = null;
        lastGraphPointCount = -1;
    }

    private void ShowGraph(IReadOnlyList<Lab6Measurement> points, string title, string legend, Func<Lab6Measurement, float> getX, Func<Lab6Measurement, float> getY)
    {
        int sourcePointCount = points != null ? points.Count : 0;
        if (title == lastGraphTitle && sourcePointCount == lastGraphPointCount)
        {
            return;
        }

        ClearGraph();
        SetText(graphTitleText, title);
        SetText(graphLegendText, legend);

        if (!HasRequiredSetup())
        {
            return;
        }

        List<Vector2> rawPoints = new List<Vector2>();
        if (points != null)
        {
            for (int i = 0; i < points.Count; i++)
            {
                Lab6Measurement point = points[i];
                if (point == null)
                {
                    continue;
                }

                float x = SafeFinite(getX(point));
                float y = SafeFinite(getY(point));
                rawPoints.Add(new Vector2(x, y));
            }
        }

        if (rawPoints.Count == 0)
        {
            SetText(graphLegendText, legend + "\nНет записанных точек нагрузки.");
            lastGraphTitle = title;
            lastGraphPointCount = sourcePointCount;
            return;
        }

        float minX = rawPoints[0].x;
        float maxX = rawPoints[0].x;
        float minY = rawPoints[0].y;
        float maxY = rawPoints[0].y;

        for (int i = 1; i < rawPoints.Count; i++)
        {
            minX = Mathf.Min(minX, rawPoints[i].x);
            maxX = Mathf.Max(maxX, rawPoints[i].x);
            minY = Mathf.Min(minY, rawPoints[i].y);
            maxY = Mathf.Max(maxY, rawPoints[i].y);
        }

        Vector2[] uiPoints = new Vector2[rawPoints.Count];
        Rect rect = graphArea.rect;
        float width = Mathf.Max(1f, rect.width);
        float height = Mathf.Max(1f, rect.height);

        for (int i = 0; i < rawPoints.Count; i++)
        {
            float x = Normalize(rawPoints[i].x, minX, maxX) * width;
            float y = Normalize(rawPoints[i].y, minY, maxY) * height;
            uiPoints[i] = new Vector2(x, y);
        }

        for (int i = 1; i < uiPoints.Length; i++)
        {
            CreateLine(uiPoints[i - 1], uiPoints[i]);
        }

        for (int i = 0; i < uiPoints.Length; i++)
        {
            CreatePoint(uiPoints[i]);
        }

        lastGraphTitle = title;
        lastGraphPointCount = sourcePointCount;
    }

    private bool HasRequiredSetup()
    {
        if (graphArea != null && pointPrefab != null && linePrefab != null)
        {
            return true;
        }

        if (!missingGraphSetupWarningLogged)
        {
            Debug.LogWarning("Lab6GraphView: graphArea, pointPrefab and linePrefab must be assigned to draw graphs.");
            missingGraphSetupWarningLogged = true;
        }

        return false;
    }

    private void CreatePoint(Vector2 position)
    {
        Image point = Instantiate(pointPrefab, graphArea);
        point.gameObject.SetActive(true);
        point.name = "GraphPoint";
        RectTransform rect = point.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        spawnedObjects.Add(point.gameObject);
    }

    private void CreateLine(Vector2 from, Vector2 to)
    {
        Image line = Instantiate(linePrefab, graphArea);
        line.gameObject.SetActive(true);
        line.name = "GraphLine";

        Vector2 delta = to - from;
        RectTransform rect = line.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = from + delta * 0.5f;

        float height = rect.sizeDelta.y > 0f ? rect.sizeDelta.y : 4f;
        rect.sizeDelta = new Vector2(delta.magnitude, height);
        rect.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);
        spawnedObjects.Add(line.gameObject);
    }

    private static float Normalize(float value, float min, float max)
    {
        if (Mathf.Approximately(min, max))
        {
            return 0.5f;
        }

        return Mathf.Clamp01((value - min) / (max - min));
    }

    private static float SafeFinite(float value)
    {
        return float.IsNaN(value) || float.IsInfinity(value) ? 0f : value;
    }

    private static void SetText(TextMeshProUGUI target, string value)
    {
        if (target != null && target.text != value)
        {
            target.text = value;
        }
    }
}
