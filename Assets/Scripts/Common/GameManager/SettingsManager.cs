using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance { get; private set; }

    [Header("Mouse Sensitivity")]
    public float sensX = 200f;
    public float sensY = 200f;

    private const string SensKey = "MouseSensitivity";
    private const int LowQualityIndex = 0;
    private const int StandardQualityIndex = 1;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadSettings();
        ApplyPerformanceProfile();
    }

    private void LoadSettings()
    {
        float savedSens = PlayerPrefs.GetFloat(SensKey, 200f);
        sensX = savedSens;
        sensY = savedSens;
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetFloat(SensKey, sensX);
        PlayerPrefs.Save();
    }

    private void ApplyPerformanceProfile()
    {
        bool useLowSpecProfile = ShouldUseLowSpecProfile();
        int qualityIndex = useLowSpecProfile ? LowQualityIndex : StandardQualityIndex;

        if (qualityIndex < QualitySettings.names.Length)
        {
            QualitySettings.SetQualityLevel(qualityIndex, true);
        }

        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        QualitySettings.globalTextureMipmapLimit = useLowSpecProfile ? 1 : 0;
        QualitySettings.anisotropicFiltering = useLowSpecProfile
            ? AnisotropicFiltering.Disable
            : AnisotropicFiltering.Enable;
        QualitySettings.skinWeights = useLowSpecProfile ? SkinWeights.TwoBones : SkinWeights.FourBones;
        QualitySettings.lodBias = useLowSpecProfile ? 0.7f : 1.5f;
        QualitySettings.shadowDistance = useLowSpecProfile ? 18f : 32f;

        if (GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset urp)
        {
            urp.renderScale = useLowSpecProfile ? 0.8f : 1f;
            urp.msaaSampleCount = 1;
            urp.shadowDistance = QualitySettings.shadowDistance;
        }
    }

    private static bool ShouldUseLowSpecProfile()
    {
        int graphicsMemoryMb = SystemInfo.graphicsMemorySize;
        int systemMemoryMb = SystemInfo.systemMemorySize;
        int cpuThreads = SystemInfo.processorCount;

        bool weakGpu = graphicsMemoryMb > 0 && graphicsMemoryMb <= 2048;
        bool weakCpu = cpuThreads > 0 && cpuThreads <= 4;
        bool lowRam = systemMemoryMb > 0 && systemMemoryMb <= 8192;

        return weakGpu || weakCpu || lowRam;
    }
}
