using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;
using UnityEngine.InputSystem.Controls;
using System.Collections;

public class PlayerMovement : MonoBehaviour
{
    //Player Components
    GroundCheck groundCheck;
    CameraHandle cameraHandle;

    [Header("Animations")]
    // public Animator _playerAnim;

    [Header("Movement Settings")]
    public float runSpeed = 5f;
    public float walkSpeed = 3f;
    public float sprintSpeed = 8f;
    public float crouchSpeed = 2f;
    public float jumpHeight = 2f;
    public int maxJumps = 2;
    public float airControlLimiter = 2f;

    [Header("Other Settings")]
    public float gravity = -9.81f;

    [Tooltip("The maximum downwards velocity the player can reach while falling")]
    public float terminalVelocity = -50f;

    [Tooltip("How fast the velocity is going to update")]
    public float verticalAccelleration = 2f;

    [Header("Crouch Settings")]
    public Transform standCameraPosition;
    public Transform crouchCameraPosition;
    public bool isCrouchToggle;

    [Header("Slide Settings")]
    public float slideDuration = .6f;
    public float slideSpeed = 7f;
    public float slideCooldown = 0.5f; // Add this line to set cooldown time in seconds

    [Range(0, 1)]
    public float minSlideDurationPercent = 0.33f;

    [Header("Wall Slide Settings")]
    [SerializeField]
    private float wallSlideAccelleration = 2f;
    public float zRotationOnWall = 20f;
    public float wallSlideJumpMultiplier = 2f;

    [Header("Camera")]
    public CinemachineVirtualCamera virtualCamera;
    public Transform virtualCameraHolder;
    public float fovLerpSpeed = 3.0f;

    private CharacterController characterController;

    //Cached useful values
    private Vector2 currentMovementInput;
    private float targetFOV;

    [HideInInspector]
    public float verticalVelocity;
    private int jumpCount;
    private float nextSlideTime;

    [HideInInspector]
    public float currentSpeed;

    //Action checks
    [HideInInspector]
    public bool isMoving;

    [HideInInspector]
    public bool isJumping;

    [HideInInspector]
    public bool wasGrounded;

    [HideInInspector]
    public bool isCrouching;

    [HideInInspector]
    public bool isSprinting;

    [HideInInspector]
    public bool isSliding = false;
    private bool slideQueued = false;
    private bool isMovementPressed;
    private bool isTouchingWall;
    private bool wasTouchingWallLastFrame = false;
    private bool wasRotatedRight = false;
    private bool isWallSliding = false;
    private float desiredSpeed;
    private const float lerpSpeed = 5f;
    private bool hasFinishedCrouching = false;
    private bool isFalling;

    #region UNITY METHODS
    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        groundCheck = GetComponentInChildren<GroundCheck>();
        cameraHandle = GetComponentInChildren<CameraHandle>();
        targetFOV = virtualCamera.m_Lens.FieldOfView;
    }

    private void Update()
    {
        isSprinting = false;
        targetFOV = 60f;

        HandleMovementInput();
        HandleJumpInput();
        HandleCrouchInput();
        HandleWallSlideJumpInput();

        isTouchingWall = CheckWallSlide();

        if (groundCheck.CheckIfGrounded())
        {
            ResetJump();

            if (slideQueued && isSprinting && Time.time >= nextSlideTime)
            {
                nextSlideTime = Time.time + slideCooldown;
                StartCoroutine(Slide());
                slideQueued = false;
            }
        }

        wasGrounded = groundCheck.CheckIfGrounded();
        airControlLimiter = 1f;
    }

    private void FixedUpdate()
    {
        HandleCrouch();
        HandleMovement();
        ApplyGravity();
        UpdateCameraFOV();
        DebugWallRays();
        // HandleAnimations();
    }
    #endregion
    #region INPUT HANDLING METHODS
    private void HandleMovementInput()
    {
        bool isMovingForward = Keyboard.current.wKey.isPressed;

        if (
            isMovingForward
            || Keyboard.current.sKey.isPressed
            || Keyboard.current.aKey.isPressed
            || Keyboard.current.dKey.isPressed
        )
        {
            currentMovementInput = new Vector2(
                Keyboard.current.aKey.isPressed ? -1 : (Keyboard.current.dKey.isPressed ? 1 : 0),
                isMovingForward ? 1 : (Keyboard.current.sKey.isPressed ? -1 : 0)
            );
            isMovementPressed = true;
            desiredSpeed = runSpeed;

            if (Keyboard.current.leftShiftKey.isPressed && isMovingForward)
            {
                desiredSpeed = sprintSpeed;
                isCrouching = false;
                isSprinting = true;
                targetFOV = 80f;
            }
            else if (Keyboard.current.leftCtrlKey.isPressed && !isCrouching)
            {
                desiredSpeed = walkSpeed;
                targetFOV = 55f;
            }
        }
        else
        {
            isMovementPressed = false;
            currentMovementInput = Vector2.zero;
            desiredSpeed = 0; // stop smoothly
        }

        // Smoothly change the speed
        currentSpeed = Mathf.Lerp(currentSpeed, desiredSpeed, lerpSpeed * Time.deltaTime);
    }

    private void HandleJumpInput()
    {
        if (Keyboard.current.spaceKey.wasPressedThisFrame && jumpCount < maxJumps)
        {
            isJumping = true;
            jumpCount = wasGrounded ? jumpCount += 1 : 2;
            groundCheck.DisableCollisionCheck(.5f);
        }
    }

    private void HandleCrouchInput()
    {
        KeyControl crouchKey = Keyboard.current[Key.C];
        if (isSprinting && groundCheck.CheckIfGrounded())
        {
            if (crouchKey.wasPressedThisFrame && Time.time >= nextSlideTime)
            {
                nextSlideTime = Time.time + slideCooldown;
                StartCoroutine(Slide());
            }
            return;
        }

        if (isSprinting && !groundCheck.CheckIfGrounded() && crouchKey.wasPressedThisFrame)
        {
            slideQueued = true;
            return;
        }

        if (isCrouchToggle)
        {
            if (crouchKey.wasPressedThisFrame)
            {
                isCrouching = !isCrouching;
            }
        }
        else
        {
            if (crouchKey.isPressed)
            {
                isCrouching = true;
            }
            else
                isCrouching = false;
        }
    }

    private void HandleWallSlideJumpInput()
    {
        if (isWallSliding && isJumping)
        {
            StartCoroutine(HandleWallSlideJump());
        }
    }

    #endregion
    #region PHYSICS HANDLING METHODS

    private void HandleMovement()
    {
        if (isMovementPressed)
        {
            Vector3 move = new Vector3(
                currentMovementInput.x,
                0,
                currentMovementInput.y
            ).normalized;

            move = transform.TransformDirection(move);

            move *= currentSpeed / airControlLimiter * Time.fixedDeltaTime;

            characterController.Move(move);

            isMoving = true;
        }
        else
        {
            currentSpeed = 0;
            isMoving = false;
        }
    }

    private void ApplyGravity()
    {
        if (groundCheck.CheckIfGrounded() && !isJumping)
        {
            verticalVelocity = 0;
            jumpCount = 0;
        }
        else
        {
            if (isJumping)
            {
                if (isWallSliding)
                {
                    jumpCount -= 1;
                    return;
                }

                if (isSliding)
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight / 1.2f * -2f * gravity);
                    jumpCount = 3;
                    airControlLimiter = 1;
                    isSliding = false;
                    isJumping = false;
                }
                else if (isCrouching)
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * 1.5f * -2f * gravity);
                    jumpCount = 3;
                    isCrouching = false;
                    isJumping = false;
                }
                else
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    airControlLimiter = 2;
                    isJumping = false;
                }
                    // _playerAnim.SetTrigger("jump");
            }

            if (isTouchingWall && verticalVelocity < 0)
            {
                verticalVelocity += gravity * wallSlideAccelleration * Time.fixedDeltaTime;
            }
            else
            {
                verticalVelocity += gravity * verticalAccelleration * Time.fixedDeltaTime;
            }

            verticalVelocity = Mathf.Max(terminalVelocity, verticalVelocity);
        }

        characterController.Move(new Vector3(0, verticalVelocity * Time.fixedDeltaTime, 0));

        if (verticalVelocity < 0)
            isFalling = true;
        else
            isFalling = false;
    }

    private void HandleCrouch()
    {
        if (!groundCheck.CheckIfGrounded())
            return;

        if (isCrouching)
        {
            currentSpeed = crouchSpeed;
            cameraHandle.MoveCamera(
                virtualCameraHolder.transform.localPosition,
                crouchCameraPosition.localPosition,
                0.06f
            );
            targetFOV = 50f;
        }
        else if (
            !isCrouching
            && virtualCameraHolder.transform.localPosition != standCameraPosition.localPosition
        )
        {
            cameraHandle.MoveCamera(
                virtualCameraHolder.transform.localPosition,
                standCameraPosition.localPosition,
                0.06f
            );
        }
        if (virtualCameraHolder.transform.localPosition == crouchCameraPosition.localPosition)
            hasFinishedCrouching = true;
        else
            hasFinishedCrouching = false;
    }

    private IEnumerator HandleWallSlideJump()
    {
        float timeElapsed = 0;
        float totalJumpTime = 0.2f;
        Vector3 jumpDirection = virtualCameraHolder.up;

        Vector3 jumpForce = jumpDirection * jumpHeight * wallSlideJumpMultiplier / totalJumpTime;

        while (timeElapsed < totalJumpTime)
        {
            characterController.Move(jumpForce * Time.fixedDeltaTime);
            timeElapsed += Time.fixedDeltaTime;
            yield return new WaitForFixedUpdate();
        }

        isJumping = false;
        isWallSliding = false;
    }

    private IEnumerator Slide()
    {
        isSliding = true;
        float elapsedTime = 0;

        KeyControl crouchKey = Keyboard.current[Key.C];

        Vector3 slideDirection = transform
            .TransformDirection(currentMovementInput.x, 0, currentMovementInput.y)
            .normalized;

        while (elapsedTime < slideDuration && isSliding)
        {
            elapsedTime += Time.deltaTime;

            if (elapsedTime > slideDuration * minSlideDurationPercent)
            {
                if (isCrouchToggle && crouchKey.wasPressedThisFrame)
                {
                    break;
                }
                else if (!isCrouchToggle && !crouchKey.isPressed)
                {
                    break;
                }
            }

            cameraHandle.MoveCamera(
                virtualCameraHolder.transform.localPosition,
                crouchCameraPosition.localPosition,
                0.1f
            );
            targetFOV = 80f;

            characterController.Move(slideDirection * slideSpeed * Time.deltaTime);

            yield return null;
        }

        targetFOV = 60f;
        isSliding = false;
    }

    #endregion
    #region OTHER METHODS

    private void UpdateCameraFOV()
    {
        virtualCamera.m_Lens.FieldOfView = Mathf.Lerp(
            virtualCamera.m_Lens.FieldOfView,
            targetFOV,
            fovLerpSpeed * Time.fixedDeltaTime
        );
    }

    private void ResetJump()
    {
        jumpCount = 0;
    }

    private bool CheckWallSlide()
    {
        float distance = 1f;
        bool hitWallRight = Physics.Raycast(transform.position, transform.right, distance);
        bool hitWallLeft = Physics.Raycast(transform.position, -transform.right, distance);
        return hitWallRight || hitWallLeft;
    }

    private void DebugWallRays()
    {
        float rayLength = 1f;
        Vector3 leftRayDirection = -transform.right;
        Vector3 rightRayDirection = transform.right;

        Debug.DrawRay(transform.position, leftRayDirection * rayLength, Color.blue);
        Debug.DrawRay(transform.position, rightRayDirection * rayLength, Color.red);

        RaycastHit hit;
        if (Physics.Raycast(transform.position, leftRayDirection, out hit, rayLength))
        {
            if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Wall"))
            {
                isTouchingWall = true;
                if (
                    (!wasTouchingWallLastFrame || !wasRotatedRight)
                    && !groundCheck.CheckIfGrounded()
                )
                {
                    isWallSliding = true;
                    cameraHandle.RotateCameraZ(true, zRotationOnWall);
                    wasRotatedRight = true;
                }
            }
        }
        else if (Physics.Raycast(transform.position, rightRayDirection, out hit, rayLength))
        {
            if (hit.transform.gameObject.layer == LayerMask.NameToLayer("Wall"))
            {
                isTouchingWall = true;
                if (
                    (!wasTouchingWallLastFrame || wasRotatedRight) && !groundCheck.CheckIfGrounded()
                )
                {
                    isWallSliding = true;
                    cameraHandle.RotateCameraZ(false, zRotationOnWall);
                    wasRotatedRight = false;
                }
            }
        }
        else
        {
            isTouchingWall = false;
            isWallSliding = false;
            if (wasTouchingWallLastFrame)
            {
                cameraHandle.ResetCameraZRotation();
            }
        }
        wasTouchingWallLastFrame = isTouchingWall;
    }

    #endregion

    // #region ANIMATIONS
    // void HandleAnimations()
    // {
    //     _playerAnim.SetFloat("velocity", currentSpeed);
    //     _playerAnim.SetBool("isCrouching", isCrouching);
    //     _playerAnim.SetBool("hasFinishedCrouching", hasFinishedCrouching);
    //     _playerAnim.SetBool("isFalling", isFalling);
    //     _playerAnim.SetBool("isGrounded", groundCheck.CheckIfGrounded());
    // }
    // #endregion
}
