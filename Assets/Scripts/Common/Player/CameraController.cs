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
        // === ПРОГРАММНОЕ ОТКЛЮЧЕНИЕ ВСЕХ КАМЕР КРОМЕ MAIN ===

        // TV камера
        if (tvCamera != null)
        {
            tvOriginalPosition = tvCamera.transform.position;
            tvOriginalRotation = tvCamera.transform.rotation;
            tvCamera.gameObject.SetActive(false);  // Программно отключаем
        }

        // Engine камера
        if (engineCamera != null)
        {
            engineCamera.gameObject.SetActive(false);  // Программно отключаем
        }

        // === Schema камера - ПРОГРАММНО ОТКЛЮЧАЕМ ПРИНУДИТЕЛЬНО ===
        if (schemaCamera != null)
        {
            schemaOriginalPosition = schemaCamera.transform.position;
            schemaOriginalRotation = schemaCamera.transform.rotation;

            // Принудительное программное отключение
            schemaCamera.gameObject.SetActive(false);

            // Дополнительная страховка: отключаем компонент камеры
            schemaCamera.enabled = false;

            Debug.Log("Schema camera программно отключена при запуске");
        }

        // Main камера - единственная активная при старте
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(true);
            mainCamera.enabled = true;
        }

        // Программно отключаем UI
        if (tvCanvas != null)
            tvCanvas.enabled = false;

        if (schemaUI != null)
            schemaUI.enabled = false;

        UpdateUI();

        // Финальная проверка: убеждаемся что schemaCamera выключена
        if (schemaCamera != null && schemaCamera.gameObject.activeSelf)
        {
            Debug.LogError("Schema camera всё ещё активна! Принудительное отключение...");
            schemaCamera.gameObject.SetActive(false);
            schemaCamera.enabled = false;
        }
    }

    private void Start()
    {
        // Дополнительная проверка в Start() на всякий случай
        if (schemaCamera != null && schemaCamera.gameObject.activeSelf)
        {
            Debug.LogWarning("Повторное отключение schema camera в Start()");
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
        SwitchCamera(currentMode == CameraMode.TV ? CameraMode.Main : CameraMode.TV);
    }

    private void OnToggleEngine(InputAction.CallbackContext ctx)
    {
        SwitchCamera(currentMode == CameraMode.Engine ? CameraMode.Main : CameraMode.Engine);
    }

    private void OnToggleSchema(InputAction.CallbackContext ctx)
    {
        SwitchCamera(currentMode == CameraMode.Schema ? CameraMode.Main : CameraMode.Schema);
    }

    private void SwitchCamera(CameraMode mode)
    {
        currentMode = mode;

        // Основная камера
        if (mainCamera != null)
        {
            mainCamera.gameObject.SetActive(mode == CameraMode.Main);
            mainCamera.enabled = (mode == CameraMode.Main);

            if (mode == CameraMode.Main)
                ApplyCameraPerformanceProfile(mainCamera);
        }

        // TV камера
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

        // Engine камера
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

        // Schema камера - включается ТОЛЬКО по нажатию R
        if (schemaCamera != null)
        {
            if (mode == CameraMode.Schema)
            {
                schemaCamera.transform.SetPositionAndRotation(schemaOriginalPosition, schemaOriginalRotation);
                schemaCamera.gameObject.SetActive(true);
                schemaCamera.enabled = true;
                ApplyCameraPerformanceProfile(schemaCamera);
                Debug.Log("Schema camera включена по нажатию R");
            }
            else
            {
                schemaCamera.gameObject.SetActive(false);
                schemaCamera.enabled = false;
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