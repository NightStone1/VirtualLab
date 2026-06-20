using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;

public class CameraController : MonoBehaviour
{
    private enum CameraMode
    {
        Main,
        TV,
        Engine,
        Schema
    }

    [Header("Cameras")]
    [SerializeField] private Camera mainCamera;
    [SerializeField] private Camera tvCamera;
    [SerializeField] private Camera engineCamera;
    [SerializeField] private Camera schemaCamera;

    [Header("Camera Switching")]
    [SerializeField] private bool allowTvCameraSwitch = true;
    [SerializeField] private bool allowEngineCameraSwitch = true;

    [Header("UI")]
    [SerializeField] private Canvas tvCanvas;
    [SerializeField] private Canvas statusText;
    [SerializeField] private Canvas crosshair;
    [SerializeField] private Canvas schemaUI;

    [Header("Input")]
    [SerializeField] private InputActionReference toggleTVAction;
    [SerializeField] private InputActionReference toggleEngineAction;
    [SerializeField] private InputActionReference toggleSchemaAction;

    private Vector3 tvOriginalPosition;
    private Quaternion tvOriginalRotation;
    private Vector3 schemaOriginalPosition;
    private Quaternion schemaOriginalRotation;
    private CameraMode currentMode = CameraMode.Main;

    private void Awake()
    {
        // === ����������� ���������� ���� ����� ����� MAIN ===

        // TV ������
        if (tvCamera != null)
        {
            tvOriginalPosition = tvCamera.transform.position;
            tvOriginalRotation = tvCamera.transform.rotation;
            tvCamera.gameObject.SetActive(false);  // ���������� ���������
        }

        // Engine ������
        if (engineCamera != null)
        {
            engineCamera.gameObject.SetActive(false);  // ���������� ���������
        }

        // === Schema ������ - ���������� ��������� ������������� ===
        if (schemaCamera != null)
        {
            schemaOriginalPosition = schemaCamera.transform.position;
            schemaOriginalRotation = schemaCamera.transform.rotation;

            // �������������� ����������� ����������
            schemaCamera.gameObject.SetActive(false);

            // �������������� ���������: ��������� ��������� ������
            schemaCamera.enabled = false;

            Debug.Log("Schema camera ���������� ��������� ��� �������");
        }

        // Main ������ - ������������ �������� ��� ������
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(true);
            mainCamera.enabled = true;
        }

        // ���������� ��������� UI
        if (tvCanvas != null)
            tvCanvas.enabled = false;

        if (schemaUI != null)
            schemaUI.enabled = false;

        UpdateUI();

        // ��������� ��������: ���������� ��� schemaCamera ���������
        if (schemaCamera != null && schemaCamera.gameObject.activeSelf)
        {
            Debug.LogError("Schema camera �� ��� �������! �������������� ����������...");
            schemaCamera.gameObject.SetActive(false);
            schemaCamera.enabled = false;
        }
    }

    private void Start()
    {
        // �������������� �������� � Start() �� ������ ������
        if (schemaCamera != null && schemaCamera.gameObject.activeSelf)
        {
            Debug.LogWarning("��������� ���������� schema camera � Start()");
            schemaCamera.gameObject.SetActive(false);
            schemaCamera.enabled = false;
        }
    }

    private void OnEnable()
    {
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

        if (toggleSchemaAction != null)
        {
            toggleSchemaAction.action.Enable();
            toggleSchemaAction.action.performed += OnToggleSchema;
        }
    }

    private void OnDisable()
    {
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

        if (toggleSchemaAction != null)
        {
            toggleSchemaAction.action.performed -= OnToggleSchema;
            toggleSchemaAction.action.Disable();
        }
    }

    private void OnToggleTV(InputAction.CallbackContext ctx)
    {
        if (!allowTvCameraSwitch)
            return;

        SwitchCamera(currentMode == CameraMode.TV ? CameraMode.Main : CameraMode.TV);
    }

    private void OnToggleEngine(InputAction.CallbackContext ctx)
    {
        if (!allowEngineCameraSwitch)
            return;

        SwitchCamera(currentMode == CameraMode.Engine ? CameraMode.Main : CameraMode.Engine);
    }

    private void OnToggleSchema(InputAction.CallbackContext ctx)
    {
        SwitchCamera(currentMode == CameraMode.Schema ? CameraMode.Main : CameraMode.Schema);
    }

    private void SwitchCamera(CameraMode mode)
    {
        if (!CanSwitchToCamera(mode))
            return;

        currentMode = mode;

        // �������� ������
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(mode == CameraMode.Main);
            mainCamera.enabled = (mode == CameraMode.Main);

            if (mode == CameraMode.Main)
                ApplyCameraPerformanceProfile(mainCamera);
        }

        // TV ������
        if (tvCamera != null)
        {
            if (mode == CameraMode.TV)
            {
                tvCamera.transform.SetPositionAndRotation(tvOriginalPosition, tvOriginalRotation);
                tvCamera.gameObject.SetActive(true);
                tvCamera.enabled = true;
                ApplyCameraPerformanceProfile(tvCamera);
            }
            else
            {
                tvCamera.gameObject.SetActive(false);
                tvCamera.enabled = false;
            }
        }

        // Engine ������
        if (engineCamera != null)
        {
            if (mode == CameraMode.Engine)
            {
                engineCamera.gameObject.SetActive(true);
                engineCamera.enabled = true;
                ApplyCameraPerformanceProfile(engineCamera);
            }
            else
            {
                engineCamera.gameObject.SetActive(false);
                engineCamera.enabled = false;
            }
        }

        // Schema ������ - ���������� ������ �� ������� R
        if (schemaCamera != null)
        {
            if (mode == CameraMode.Schema)
            {
                schemaCamera.transform.SetPositionAndRotation(schemaOriginalPosition, schemaOriginalRotation);
                schemaCamera.gameObject.SetActive(true);
                schemaCamera.enabled = true;
                ApplyCameraPerformanceProfile(schemaCamera);
                Debug.Log("Schema camera �������� �� ������� R");
            }
            else
            {
                schemaCamera.gameObject.SetActive(false);
                schemaCamera.enabled = false;
            }
        }

        TogglePlayerControl(mode == CameraMode.Main);
        UpdateUI();
        EnsureAtLeastOneCameraRendering();
    }

    private bool CanSwitchToCamera(CameraMode mode)
    {
        if (mode == CameraMode.TV)
        {
            if (!allowTvCameraSwitch)
                return false;

            if (tvCamera == null)
            {
                Debug.LogWarning("TV camera is not assigned. Camera switch ignored.");
                EnsureMainCameraEnabledAfterFailedSwitch();
                return false;
            }
        }
        else if (mode == CameraMode.Engine)
        {
            if (!allowEngineCameraSwitch)
                return false;

            if (engineCamera == null)
            {
                Debug.LogWarning("Engine camera is not assigned. Camera switch ignored.");
                EnsureMainCameraEnabledAfterFailedSwitch();
                return false;
            }
        }
        else if (mode == CameraMode.Schema && schemaCamera == null)
        {
            EnsureMainCameraEnabledAfterFailedSwitch();
            return false;
        }

        return true;
    }

    private void EnsureMainCameraEnabledAfterFailedSwitch()
    {
        currentMode = CameraMode.Main;

        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(true);
            mainCamera.enabled = true;
            ApplyCameraPerformanceProfile(mainCamera);
        }

        TogglePlayerControl(true);
        UpdateUI();
    }

    private void EnsureAtLeastOneCameraRendering()
    {
        if (IsCameraRendering(mainCamera) ||
            IsCameraRendering(tvCamera) ||
            IsCameraRendering(engineCamera) ||
            IsCameraRendering(schemaCamera))
        {
            return;
        }

        EnsureMainCameraEnabledAfterFailedSwitch();
    }

    private static bool IsCameraRendering(Camera targetCamera)
    {
        return targetCamera != null && targetCamera.gameObject.activeInHierarchy && targetCamera.enabled;
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
        bool isSchemaActive = currentMode == CameraMode.Schema;

        if (tvCanvas != null)
            tvCanvas.enabled = isTVActive;

        if (schemaUI != null)
            schemaUI.enabled = isSchemaActive;

        if (crosshair != null)
            crosshair.enabled = !isSecondaryCameraActive;

        if (statusText != null)
            statusText.enabled = !isSecondaryCameraActive;
    }

    private static void ApplyCameraPerformanceProfile(Camera targetCamera)
    {
        if (targetCamera == null || !ShouldUseLowSpecProfile())
            return;

        targetCamera.allowHDR = false;
        targetCamera.allowMSAA = false;

        if (targetCamera.TryGetComponent(out UniversalAdditionalCameraData cameraData))
        {
            cameraData.renderPostProcessing = false;
            cameraData.antialiasing = AntialiasingMode.None;

            if (ShouldUseUltraLowProfile())
                cameraData.renderShadows = false;
        }
    }

    private static bool ShouldUseLowSpecProfile()
    {
        int graphicsMemoryMb = SystemInfo.graphicsMemorySize;
        int systemMemoryMb = SystemInfo.systemMemorySize;
        int cpuThreads = SystemInfo.processorCount;

        return (graphicsMemoryMb > 0 && graphicsMemoryMb <= 2048) ||
               (cpuThreads > 0 && cpuThreads <= 4) ||
               (systemMemoryMb > 0 && systemMemoryMb <= 8192);
    }

    private static bool ShouldUseUltraLowProfile()
    {
        int graphicsMemoryMb = SystemInfo.graphicsMemorySize;
        int systemMemoryMb = SystemInfo.systemMemorySize;
        int cpuThreads = SystemInfo.processorCount;

        return (graphicsMemoryMb > 0 && graphicsMemoryMb <= 1024) ||
               (cpuThreads > 0 && cpuThreads <= 2) ||
               (systemMemoryMb > 0 && systemMemoryMb <= 4096);
    }
}
