using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class CameraTVController : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera tvCamera;

    [Header("UI")]
    [SerializeField] private Canvas tvCanvas;
    [SerializeField] private Canvas statusText;
    [SerializeField] private Canvas crosshair;

    [SerializeField] private InputActionReference toggleTVAction; 

    private Vector3 originalPosition;
    private Quaternion originalRotation;

    private bool isTVCameraActive = false;
    private void Awake()
    {
        if (tvCamera != null)
        {
            originalPosition = tvCamera.transform.position;
            originalRotation = tvCamera.transform.rotation;

            tvCamera.gameObject.SetActive(false);
        }

        if (tvCanvas != null)
            tvCanvas.enabled = false;
    }


    private void OnEnable()
    {
        toggleTVAction.action.Enable();
        toggleTVAction.action.performed += OnToggle;
    }

    private void OnDisable()
    {
        toggleTVAction.action.performed -= OnToggle;
        toggleTVAction.action.Disable();
    }

    private void OnToggle(InputAction.CallbackContext ctx)
    {
        ToggleTVCamera();
    }

    private void ToggleTVCamera()
    {
        isTVCameraActive = !isTVCameraActive;

        if (isTVCameraActive)
        {
            ActivateTVCamera();
        }
        else
        {
            ActivateMainCamera();
        }

        UpdateUI();
    }

    private void ActivateTVCamera()
    {
        if (mainCamera != null)
            mainCamera.gameObject.SetActive(false);

        if (tvCamera != null)
        {
            tvCamera.transform.SetPositionAndRotation(originalPosition, originalRotation);
            tvCamera.gameObject.SetActive(true);
            ApplyCameraPerformanceProfile(tvCamera);
        }

        TogglePlayerControl(false);
    }

    private void ActivateMainCamera()
    {
        if (tvCamera != null)
            tvCamera.gameObject.SetActive(false);

        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(true);
            ApplyCameraPerformanceProfile(mainCamera);
        }

        TogglePlayerControl(true);
    }

    private void TogglePlayerControl(bool enable)
    {
        var playerInput = mainCamera.GetComponentInParent<PlayerInput>();
        if (playerInput != null)
            playerInput.enabled = enable;

        // Если нужно:
        Cursor.visible = !enable;
        Cursor.lockState = enable ? CursorLockMode.Locked : CursorLockMode.None;
    }

    private void UpdateUI()
    {
        if (tvCanvas != null)
            tvCanvas.enabled = isTVCameraActive;

        if (crosshair != null)
            crosshair.enabled = !isTVCameraActive;

        if (statusText != null)
            statusText.enabled = !isTVCameraActive;
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
