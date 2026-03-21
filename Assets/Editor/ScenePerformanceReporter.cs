using System.Collections.Generic;
using System.Text;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class ScenePerformanceReporter
{
    private struct MeshEntry
    {
        public string Name;
        public string Path;
        public int TriangleCount;
    }

    private struct CanvasEntry
    {
        public string Name;
        public string Path;
        public int GraphicCount;
        public int LayoutGroupCount;
        public int ContentSizeFitterCount;
    }

    [MenuItem("Tools/VLab/Performance/Report Active Scene")]
    public static void ReportActiveScene()
    {
        Scene scene = SceneManager.GetActiveScene();

        if (!scene.IsValid() || !scene.isLoaded)
        {
            Debug.LogWarning("ScenePerformanceReporter: no active scene loaded.");
            return;
        }

        List<MeshEntry> meshEntries = new List<MeshEntry>();
        List<CanvasEntry> canvasEntries = new List<CanvasEntry>();
        List<string> shadowLights = new List<string>();
        List<string> postProcessCameras = new List<string>();

        int cameraCount = 0;
        int lightCount = 0;
        int volumeCount = 0;
        int activeVolumeOverrides = 0;
        int particleSystemCount = 0;
        int meshRendererCount = 0;
        int skinnedMeshCount = 0;
        int canvasCount = 0;
        int graphicCount = 0;
        int layoutGroupCount = 0;
        int contentSizeFitterCount = 0;

        GameObject[] rootObjects = scene.GetRootGameObjects();

        for (int i = 0; i < rootObjects.Length; i++)
        {
            Camera[] cameras = rootObjects[i].GetComponentsInChildren<Camera>(true);
            for (int j = 0; j < cameras.Length; j++)
            {
                if (!cameras[j].enabled || !cameras[j].gameObject.activeInHierarchy)
                {
                    continue;
                }

                cameraCount++;

                if (cameras[j].TryGetComponent(out UniversalAdditionalCameraData cameraData) && cameraData.renderPostProcessing)
                {
                    postProcessCameras.Add(GetHierarchyPath(cameras[j].transform));
                }
            }

            Light[] lights = rootObjects[i].GetComponentsInChildren<Light>(true);
            for (int j = 0; j < lights.Length; j++)
            {
                if (!lights[j].enabled || !lights[j].gameObject.activeInHierarchy)
                {
                    continue;
                }

                lightCount++;

                if (lights[j].shadows != LightShadows.None)
                {
                    shadowLights.Add(GetHierarchyPath(lights[j].transform));
                }
            }

            Volume[] volumes = rootObjects[i].GetComponentsInChildren<Volume>(true);
            for (int j = 0; j < volumes.Length; j++)
            {
                if (!volumes[j].enabled || !volumes[j].gameObject.activeInHierarchy)
                {
                    continue;
                }

                volumeCount++;

                if (volumes[j].sharedProfile == null)
                {
                    continue;
                }

                for (int k = 0; k < volumes[j].sharedProfile.components.Count; k++)
                {
                    if (volumes[j].sharedProfile.components[k] != null && volumes[j].sharedProfile.components[k].active)
                    {
                        activeVolumeOverrides++;
                    }
                }
            }

            ParticleSystem[] particleSystems = rootObjects[i].GetComponentsInChildren<ParticleSystem>(true);
            for (int j = 0; j < particleSystems.Length; j++)
            {
                if (particleSystems[j].gameObject.activeInHierarchy)
                {
                    particleSystemCount++;
                }
            }

            MeshRenderer[] meshRenderers = rootObjects[i].GetComponentsInChildren<MeshRenderer>(true);
            for (int j = 0; j < meshRenderers.Length; j++)
            {
                if (!meshRenderers[j].enabled || !meshRenderers[j].gameObject.activeInHierarchy)
                {
                    continue;
                }

                meshRendererCount++;
                MeshFilter meshFilter = meshRenderers[j].GetComponent<MeshFilter>();

                if (meshFilter == null || meshFilter.sharedMesh == null)
                {
                    continue;
                }

                meshEntries.Add(new MeshEntry
                {
                    Name = meshRenderers[j].name,
                    Path = GetHierarchyPath(meshRenderers[j].transform),
                    TriangleCount = GetTriangleCount(meshFilter.sharedMesh)
                });
            }

            SkinnedMeshRenderer[] skinnedMeshRenderers = rootObjects[i].GetComponentsInChildren<SkinnedMeshRenderer>(true);
            for (int j = 0; j < skinnedMeshRenderers.Length; j++)
            {
                if (!skinnedMeshRenderers[j].enabled || !skinnedMeshRenderers[j].gameObject.activeInHierarchy)
                {
                    continue;
                }

                skinnedMeshCount++;

                if (skinnedMeshRenderers[j].sharedMesh == null)
                {
                    continue;
                }

                meshEntries.Add(new MeshEntry
                {
                    Name = skinnedMeshRenderers[j].name,
                    Path = GetHierarchyPath(skinnedMeshRenderers[j].transform),
                    TriangleCount = GetTriangleCount(skinnedMeshRenderers[j].sharedMesh)
                });
            }

            Canvas[] canvases = rootObjects[i].GetComponentsInChildren<Canvas>(true);
            for (int j = 0; j < canvases.Length; j++)
            {
                if (!canvases[j].enabled || !canvases[j].gameObject.activeInHierarchy)
                {
                    continue;
                }

                canvasCount++;

                Graphic[] graphics = canvases[j].GetComponentsInChildren<Graphic>(true);
                LayoutGroup[] layoutGroups = canvases[j].GetComponentsInChildren<LayoutGroup>(true);
                ContentSizeFitter[] fitters = canvases[j].GetComponentsInChildren<ContentSizeFitter>(true);

                int activeGraphics = CountEnabledGraphics(graphics);
                int activeLayoutGroups = CountEnabledBehaviours(layoutGroups);
                int activeFitters = CountEnabledBehaviours(fitters);

                graphicCount += activeGraphics;
                layoutGroupCount += activeLayoutGroups;
                contentSizeFitterCount += activeFitters;

                canvasEntries.Add(new CanvasEntry
                {
                    Name = canvases[j].name,
                    Path = GetHierarchyPath(canvases[j].transform),
                    GraphicCount = activeGraphics,
                    LayoutGroupCount = activeLayoutGroups,
                    ContentSizeFitterCount = activeFitters
                });
            }
        }

        meshEntries.Sort((left, right) => right.TriangleCount.CompareTo(left.TriangleCount));
        canvasEntries.Sort((left, right) => right.GraphicCount.CompareTo(left.GraphicCount));

        StringBuilder report = new StringBuilder();
        report.AppendLine("Scene Performance Report");
        report.AppendLine(scene.name);
        report.AppendLine();
        report.AppendLine("Counts");
        report.AppendLine($"- Cameras: {cameraCount}");
        report.AppendLine($"- Lights: {lightCount}");
        report.AppendLine($"- Volumes: {volumeCount} (active overrides: {activeVolumeOverrides})");
        report.AppendLine($"- Mesh renderers: {meshRendererCount}");
        report.AppendLine($"- Skinned meshes: {skinnedMeshCount}");
        report.AppendLine($"- Particle systems: {particleSystemCount}");
        report.AppendLine($"- Canvases: {canvasCount}");
        report.AppendLine($"- UI graphics: {graphicCount}");
        report.AppendLine($"- Layout groups: {layoutGroupCount}");
        report.AppendLine($"- Content size fitters: {contentSizeFitterCount}");
        report.AppendLine();
        report.AppendLine("Top Meshes");
        AppendTopMeshes(report, meshEntries);
        report.AppendLine();
        report.AppendLine("UI Hotspots");
        AppendTopCanvases(report, canvasEntries);
        report.AppendLine();
        report.AppendLine("Shadow Lights");
        AppendStringList(report, shadowLights);
        report.AppendLine();
        report.AppendLine("Post Process Cameras");
        AppendStringList(report, postProcessCameras);

        Debug.Log(report.ToString());
    }

    private static void AppendTopMeshes(StringBuilder report, List<MeshEntry> meshEntries)
    {
        int count = Mathf.Min(meshEntries.Count, 10);

        if (count == 0)
        {
            report.AppendLine("- none");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            report.AppendLine($"- {meshEntries[i].Name}: {meshEntries[i].TriangleCount} tris ({meshEntries[i].Path})");
        }
    }

    private static void AppendTopCanvases(StringBuilder report, List<CanvasEntry> canvasEntries)
    {
        int count = Mathf.Min(canvasEntries.Count, 5);

        if (count == 0)
        {
            report.AppendLine("- none");
            return;
        }

        for (int i = 0; i < count; i++)
        {
            report.AppendLine(
                $"- {canvasEntries[i].Name}: graphics {canvasEntries[i].GraphicCount}, layouts {canvasEntries[i].LayoutGroupCount}, fitters {canvasEntries[i].ContentSizeFitterCount} ({canvasEntries[i].Path})");
        }
    }

    private static void AppendStringList(StringBuilder report, List<string> values)
    {
        if (values.Count == 0)
        {
            report.AppendLine("- none");
            return;
        }

        for (int i = 0; i < values.Count; i++)
        {
            report.AppendLine($"- {values[i]}");
        }
    }

    private static int CountEnabledGraphics(Graphic[] graphics)
    {
        int count = 0;

        for (int i = 0; i < graphics.Length; i++)
        {
            if (graphics[i].enabled && graphics[i].gameObject.activeInHierarchy)
            {
                count++;
            }
        }

        return count;
    }

    private static int CountEnabledBehaviours(Behaviour[] behaviours)
    {
        int count = 0;

        for (int i = 0; i < behaviours.Length; i++)
        {
            if (behaviours[i].enabled && behaviours[i].gameObject.activeInHierarchy)
            {
                count++;
            }
        }

        return count;
    }

    private static int GetTriangleCount(Mesh mesh)
    {
        int triangleCount = 0;

        for (int i = 0; i < mesh.subMeshCount; i++)
        {
            triangleCount += (int)(mesh.GetIndexCount(i) / 3);
        }

        return triangleCount;
    }

    private static string GetHierarchyPath(Transform target)
    {
        List<string> parts = new List<string>();

        while (target != null)
        {
            parts.Add(target.name);
            target = target.parent;
        }

        parts.Reverse();
        return string.Join("/", parts);
    }
}
