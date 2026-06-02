using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Lab2StandLabels : MonoBehaviour
{
    [SerializeField] private float labelFontSize = 0.15f;
    [SerializeField] private Color labelColor = new(0.92f, 0.92f, 0.92f, 1f);
    [SerializeField] private Vector3 labelRotationEuler = new(0f, 90f, 0f);
    [SerializeField] private Vector3 terminalLabelOffset = new(0f, 0.01f, -0.005f);
    [SerializeField] private Vector3 instrumentLabelOffset = new(0f, 0.055f, -0.02f);
    [SerializeField] private Vector3 switchLabelOffset = new(-0.015f, 0.03f, -0.005f);
    [SerializeField] private Vector3 q1LabelOffset = new(0.055f, 0.04f, -0.08f);
    [SerializeField] private Vector3 q2LabelOffset = new(0.075f, 0.04f, -0.08f);
    [SerializeField] private Vector3 motorLabelOffset = new(0f, 0.035f, 0f);

    private const string Lab2SceneName = "Lab2_StatorWinding";
    private const string RootName = "Lab2StandLabels";

    private readonly List<LabelBinding> labels = new();
    private readonly HashSet<string> activeLabelObjectNames = new();

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        EnsureLabelsForScene(SceneManager.GetActiveScene());
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        EnsureLabelsForScene(scene);
    }

    private static void EnsureLabelsForScene(Scene scene)
    {
        if (!scene.IsValid() || scene.name != Lab2SceneName)
            return;

        GameObject root = FindRootObject(scene, RootName);

        if (root == null)
            root = new GameObject(RootName);

        if (!root.TryGetComponent(out Lab2StandLabels _))
            root.AddComponent<Lab2StandLabels>();
    }

    private void Start()
    {
        if (SceneManager.GetActiveScene().name != Lab2SceneName)
        {
            enabled = false;
            return;
        }

        gameObject.name = RootName;
        BuildLabels();
    }

    private void LateUpdate()
    {
        Quaternion labelRotation = Quaternion.Euler(labelRotationEuler);

        for (int i = 0; i < labels.Count; i++)
        {
            LabelBinding label = labels[i];

            if (label.Target == null || label.Text == null)
                continue;

            label.Text.transform.position = label.Target.position + label.Offset;
            label.Text.transform.rotation = labelRotation;
        }
    }

    private void BuildLabels()
    {
        labels.Clear();
        activeLabelObjectNames.Clear();
        AddLabel("PA", instrumentLabelOffset, "Stend2/CenterPA", "Stend2/AmmeterNeddle", "CenterPA", "AmmeterNeddle");
        AddLabel("PV", instrumentLabelOffset, "Stend2/LeftPV", "Stend2/VoltNeedle", "LeftPV", "VoltNeedle");
        AddLabel("Q1", switchLabelOffset, "Stend2/switcherQ1", "switcherQ1");
        AddLabel("Q2", switchLabelOffset, "Stend2/switcherQ2", "switcherQ2");

        for (int i = 1; i <= 6; i++)
        {
            string terminalName = $"C{i}";
            AddLabel(terminalName, terminalLabelOffset, $"Stend2/{terminalName}", terminalName);
        }

        AddLabel("Двигатель", motorLabelOffset, "dvgatelstend2/rotor", "rotor", "dvgatelstend2");
        RemoveUnusedLabelObjects();
        LateUpdate();
    }

    private void AddLabel(string labelText, Vector3 offset, params string[] targetPathsOrNames)
    {
        Transform target = FindTransform(targetPathsOrNames);

        if (target == null)
        {
            Debug.LogWarning($"Lab2 labels: target for '{labelText}' was not found.");
            return;
        }

        TextMeshPro text = CreateOrReuseLabel(labelText);
        text.text = labelText;
        text.fontSize = labelFontSize;
        text.color = labelColor;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        text.transform.position = target.position + offset;
        text.transform.rotation = Quaternion.Euler(labelRotationEuler);
        labels.Add(new LabelBinding(target, text, offset));
    }

    private TextMeshPro CreateOrReuseLabel(string labelText)
    {
        string objectName = GetLabelObjectName(labelText);
        activeLabelObjectNames.Add(objectName);
        Transform existing = transform.Find(objectName);
        GameObject labelObject;

        if (existing != null)
        {
            labelObject = existing.gameObject;
        }
        else
        {
            labelObject = new GameObject(objectName);
            labelObject.transform.SetParent(transform, false);
        }

        Collider collider = labelObject.GetComponent<Collider>();

        if (collider != null)
            Destroy(collider);

        if (!labelObject.TryGetComponent(out TextMeshPro text))
            text = labelObject.AddComponent<TextMeshPro>();

        return text;
    }

    private void RemoveUnusedLabelObjects()
    {
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            if (!child.name.StartsWith("Label_", System.StringComparison.Ordinal)
                || activeLabelObjectNames.Contains(child.name))
                continue;

            Destroy(child.gameObject);
        }
    }

    private static string GetLabelObjectName(string labelText)
    {
        return "Label_" + labelText.Replace(" ", "_");
    }

    private static Transform FindTransform(params string[] pathsOrNames)
    {
        for (int i = 0; i < pathsOrNames.Length; i++)
        {
            Transform byPath = FindTransformByPath(pathsOrNames[i]);

            if (byPath != null)
                return byPath;
        }

        for (int i = 0; i < pathsOrNames.Length; i++)
        {
            Transform byName = FindTransformByName(pathsOrNames[i]);

            if (byName != null)
                return byName;
        }

        return null;
    }

    private static Transform FindTransformByPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return null;

        string[] parts = path.Split('/');
        Scene scene = SceneManager.GetActiveScene();

        if (!scene.IsValid())
            return null;

        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            if (!string.Equals(roots[i].name, parts[0], System.StringComparison.OrdinalIgnoreCase))
                continue;

            Transform current = roots[i].transform;

            for (int partIndex = 1; partIndex < parts.Length; partIndex++)
            {
                current = FindDirectChild(current, parts[partIndex]);

                if (current == null)
                    break;
            }

            if (current != null)
                return current;
        }

        return null;
    }

    private static Transform FindTransformByName(string targetName)
    {
        if (string.IsNullOrWhiteSpace(targetName))
            return null;

        Scene scene = SceneManager.GetActiveScene();

        if (!scene.IsValid())
            return null;

        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            Transform found = FindChildRecursive(roots[i].transform, targetName);

            if (found != null)
                return found;
        }

        return null;
    }

    private static Transform FindDirectChild(Transform root, string childName)
    {
        if (root == null)
            return null;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);

            if (string.Equals(child.name, childName, System.StringComparison.OrdinalIgnoreCase))
                return child;
        }

        return null;
    }

    private static Transform FindChildRecursive(Transform root, string targetName)
    {
        if (root == null)
            return null;

        if (string.Equals(root.name, targetName, System.StringComparison.OrdinalIgnoreCase))
            return root;

        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindChildRecursive(root.GetChild(i), targetName);

            if (found != null)
                return found;
        }

        return null;
    }

    private static GameObject FindRootObject(Scene scene, string objectName)
    {
        GameObject[] roots = scene.GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            if (string.Equals(roots[i].name, objectName, System.StringComparison.OrdinalIgnoreCase))
                return roots[i];
        }

        return null;
    }

    private readonly struct LabelBinding
    {
        public LabelBinding(Transform target, TextMeshPro text, Vector3 offset)
        {
            Target = target;
            Text = text;
            Offset = offset;
        }

        public Transform Target { get; }
        public TextMeshPro Text { get; }
        public Vector3 Offset { get; }
    }
}
