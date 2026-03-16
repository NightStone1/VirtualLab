using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using static UnityEngine.Timeline.DirectorControlPlayable;

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
        }

        TogglePlayerControl(false);
    }

    private void ActivateMainCamera()
    {
        if (tvCamera != null)
            tvCamera.gameObject.SetActive(false);

        if (mainCamera != null)
            mainCamera.gameObject.SetActive(true);

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
}
