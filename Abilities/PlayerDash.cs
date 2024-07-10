using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// DEPENDENCIES:
// Variables



public class PlayerDash : MonoBehaviour
{
    private Variables variables;
    private PlayerInputActions playerControls;
    private float lastDashTime = -Mathf.Infinity;

    [SerializeField] private Camera playerCamera; 
    private float originalFOV;
    [SerializeField] private float fovChange = -10; // Custom value for FOV change


    private void Awake()
    {
        playerControls = new PlayerInputActions();
    }

    private void OnEnable()
    {
        variables = gameObject.GetComponent<Variables>();
        originalFOV = playerCamera.fieldOfView; // Store the original FOV

        playerControls.Enable();
        playerControls.Player.Side.performed += ctx => HandleDash();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    // Existing methods (HandleDash, ExecuteDash)...
    private void HandleDash()
    {
        if (Time.time < lastDashTime + variables.dashCooldown) return;

        Vector2 moveInput = playerControls.Player.Move.ReadValue<Vector2>();
        if (moveInput != Vector2.zero)
        {
            ExecuteDash(moveInput);
            lastDashTime = Time.time;
        }
    }
    private void ExecuteDash(Vector2 direction)
    {
        Vector3 dashDirection = new Vector3(direction.x, 0, direction.y);
        dashDirection = transform.TransformDirection(dashDirection); // Make dash direction relative to player's rotation
        dashDirection = dashDirection.normalized;
        variables.playerAnimator.Play("fp_dash");
        StartCoroutine(SmoothDash(dashDirection * variables.dashDistance, variables.dashDuration));
    }

    private IEnumerator SmoothDash(Vector3 dashVector, float duration)
    {
        float startTime = Time.time;
        float targetFOV = originalFOV + fovChange; // Calculate the target FOV

        while (Time.time < startTime + duration)
        {
            // Calculate how far to move this frame
            Vector3 partialDashVector = (dashVector / duration) * Time.deltaTime;
            variables._controller.Move(partialDashVector);


            // Smoothly update FOV
            float elapsed = Time.time - startTime;
            float percentComplete = elapsed / duration;
            playerCamera.fieldOfView = Mathf.Lerp(originalFOV, targetFOV, percentComplete);

            yield return null;
        }


        // Reset the FOV back to the original value smoothly
        startTime = Time.time;
        while (playerCamera.fieldOfView != originalFOV)
        {
            float elapsed = Time.time - startTime;
            // This reset duration can be the same as the dash or faster/slower depending on the desired effect
            float resetPercentComplete = elapsed / (duration / 2); // Resetting over half the duration of the dash for example
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, originalFOV, resetPercentComplete);

            yield return null;
        }
    }
}

