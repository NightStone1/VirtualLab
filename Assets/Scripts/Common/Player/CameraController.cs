using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;

public class CameraController : MonoBehaviour
{
    private enum CameraMode
    {
        Main,
        TV,
        Engine
    }

    [Header("Cameras")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera tvCamera;
    [SerializeField] private Camera engineCamera;

    [Header("UI")]
    [SerializeField] private Canvas tvCanvas;
    [SerializeField] private Canvas statusText;
    [SerializeField] private Canvas crosshair;

    [Header("Input")]
    [SerializeField] private InputActionReference toggleTVAction;
    [SerializeField] private InputActionReference toggleEngineAction;
    [SerializeField] private bool disableSecondaryCameraSwitchingInLab2 = true;

    private Vector3 tvOriginalPosition;
    private Quaternion tvOriginalRotation;
    private CameraMode currentMode = CameraMode.Main;
    private bool secondaryCameraSwitchingDisabled;

    private void Awake()
    {
        secondaryCameraSwitchingDisabled = disableSecondaryCameraSwitchingInLab2 && IsLab2Scene();

        if (tvCamera != null)
        {
            tvOriginalPosition = tvCamera.transform.position;
            tvOriginalRotation = tvCamera.transform.rotation;
            tvCamera.gameObject.SetActive(false);
        }

        if (engineCamera != null)
            engineCamera.gameObject.SetActive(false);

        if (mainCamera != null)
            mainCamera.gameObject.SetActive(true);

        if (tvCanvas != null)
            tvCanvas.enabled = false;

        UpdateUI();
    }

    private void OnEnable()
    {
        if (secondaryCameraSwitchingDisabled)
            return;

        if (toggleTVAction != null)
        {
            toggleTVAction.action.Enable();
            toggleTVAction.action.performed += OnToggleTV;
        }

        if (toggleEngineAction != null)
        {
            toggleEngineAction.action.Enable();
            toggleEngineAction.action.performed += OnToggleEngine;
        }
    }

    private void OnDisable()
    {
        if (secondaryCameraSwitchingDisabled)
            return;

        if (toggleTVAction != null)
        {
            toggleTVAction.action.performed -= OnToggleTV;
            toggleTVAction.action.Disable();
        }

        if (toggleEngineAction != null)
        {
            toggleEngineAction.action.performed -= OnToggleEngine;
            toggleEngineAction.action.Disable();
        }
    }

    private void OnToggleTV(InputAction.CallbackContext ctx)
    {
        if (secondaryCameraSwitchingDisabled)
            return;

        SwitchCamera(currentMode == CameraMode.TV ? CameraMode.Main : CameraMode.TV);
    }

    private void OnToggleEngine(InputAction.CallbackContext ctx)
    {
        if (secondaryCameraSwitchingDisabled)
            return;

        SwitchCamera(currentMode == CameraMode.Engine ? CameraMode.Main : CameraMode.Engine);
    }

    private void SwitchCamera(CameraMode mode)
    {
        currentMode = mode;

        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(mode == CameraMode.Main);

            if (mode == CameraMode.Main)
                ApplyCameraPerformanceProfile(mainCamera);
        }

        if (tvCamera != null)
        {
            if (mode == CameraMode.TV)
            {
                tvCamera.transform.SetPositionAndRotation(tvOriginalPosition, tvOriginalRotation);
                tvCamera.gameObject.SetActive(true);
                ApplyCameraPerformanceProfile(tvCamera);
            }
            else
            {
                tvCamera.gameObject.SetActive(false);
            }
        }

        if (engineCamera != null)
        {
            if (mode == CameraMode.Engine)
            {
                engineCamera.gameObject.SetActive(true);
                ApplyCameraPerformanceProfile(engineCamera);
            }
            else
            {
                engineCamera.gameObject.SetActive(false);
            }
        }

        TogglePlayerControl(mode == CameraMode.Main);
        UpdateUI();
    }

    private void TogglePlayerControl(bool enable)
    {
        if (mainCamera == null)
            return;

        var playerInput = mainCamera.GetComponentInParent<PlayerInput>();
        if (playerInput != null)
            playerInput.enabled = enable;

        Cursor.visible = !enable;
        Cursor.lockState = enable ? CursorLockMode.Locked : CursorLockMode.None;
    }

    private void UpdateUI()
    {
        bool isSecondaryCameraActive = currentMode != CameraMode.Main;
        bool isTVActive = currentMode == CameraMode.TV;

        if (tvCanvas != null)
            tvCanvas.enabled = isTVActive;

        if (crosshair != null)
            crosshair.enabled = !isSecondaryCameraActive;

        if (statusText != null)
            statusText.enabled = !isSecondaryCameraActive;
    }

    private static bool IsLab2Scene()
    {
        return SceneManager.GetActiveScene().name == "Lab2_StatorWinding";
    }

    private static void ApplyCameraPerformanceProfile(Camera targetCamera)
    {
        if (targetCamera == null || !ShouldUseLowSpecProfile())
        {
            return;
        }

        targetCamera.allowHDR = false;
        targetCamera.allowMSAA = false;

        if (targetCamera.TryGetComponent(out UniversalAdditionalCameraData cameraData))
        {
            cameraData.renderPostProcessing = false;
            cameraData.antialiasing = AntialiasingMode.None;

            if (ShouldUseUltraLowProfile())
            {
                cameraData.renderShadows = false;
            }
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

    private static bool ShouldUseUltraLowProfile()
    {
        int graphicsMemoryMb = SystemInfo.graphicsMemorySize;
        int systemMemoryMb = SystemInfo.systemMemorySize;
        int cpuThreads = SystemInfo.processorCount;

        bool ultraWeakGpu = graphicsMemoryMb > 0 && graphicsMemoryMb <= 1024;
        bool ultraWeakCpu = cpuThreads > 0 && cpuThreads <= 2;
        bool ultraLowRam = systemMemoryMb > 0 && systemMemoryMb <= 4096;

        return ultraWeakGpu || ultraWeakCpu || ultraLowRam;
    }
}
