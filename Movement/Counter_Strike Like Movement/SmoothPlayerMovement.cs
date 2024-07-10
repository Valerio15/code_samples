 using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// DEPENDENCIES:
// Variables

struct Cmd
{
    public float forwardMove;
    public float rightMove;
    public float upMove;
}

public class SmoothPlayerMovement : MonoBehaviour
{
    // Create a variable where you will save the Variables script for easy access
    private Variables variables;
    private PlayerInputActions playerControls;

    private Cmd _cmd;
    private Vector2 movementInput;
    private Vector3 playerVelocity = Vector3.zero;

    private bool wishJump = false;
    private bool isDoubleJumpReady = true;
    private bool wishSprint = false;
    private bool wishCrouch = false;
    private bool wishWalk = false;

    private bool isSprinting = false;
    private bool isCrouching = false;
    private bool isSliding = false;
    private bool isSlideReady = true;
    private float slideTimeElapsed = 0.0f;

    [SerializeField] private float slopeForce;
    [SerializeField] private float slopeForceRayLength;

    // Used to display real time fricton values
    private float playerFriction = 0.0f;

    // Start is called before the first frame update
    void Start()
    {
        variables._controller = GetComponent<CharacterController>();

        // Subscribe to the Move action
        playerControls.Player.Move.performed += ctx => movementInput = ctx.ReadValue<Vector2>();
        playerControls.Player.Move.canceled += ctx => movementInput = Vector2.zero;

        // Subscribe to the Sprint action and change it depending if it's toggle or not
        if (variables.isSprintToggle)
        {
            playerControls.Player.Sprint.started += _ => OnSprintPressed();
        }
        else
        {
            playerControls.Player.Sprint.performed += ctx => wishSprint = true;
            playerControls.Player.Sprint.canceled += ctx => wishSprint = false;
        }

        // Subscribe to the Jump action
        playerControls.Player.Jump.started += _ => OnJumpPressed();

        // Subscribe to the Crouch action
        if (variables.isCrouchToggle)
        {
            playerControls.Player.Crouch.started += _ => OnCrouchPressed();
        }
        else
        {
            playerControls.Player.Crouch.performed += ctx => wishCrouch = true;
            playerControls.Player.Crouch.canceled += ctx => wishCrouch = false;
        }

        // Subscribe to the Walk action
        if (variables.isCrouchToggle)
        {
            playerControls.Player.Walk.started += _ => OnWalkPressed();
        }
        else
        {
            playerControls.Player.Walk.performed += ctx => wishWalk = true;
            playerControls.Player.Walk.canceled += ctx => wishWalk = false;
        }

    }

    private void Awake()
    {
        playerControls = new PlayerInputActions();
    }

    void OnEnable()
    {
        // Connect the variable to the Variables script
        variables = gameObject.GetComponent<Variables>();

        playerControls.Enable();
    }

    private void OnDisable()
    {
        playerControls.Disable();
    }

    // Update is called once per frame
    void Update()
    {

        // Movement
        if (variables._controller.isGrounded)
        {
            GroundMove();
        }
        else if (!variables._controller.isGrounded)
            AirMove();
        AnimationsHandler();

    }

    private void LateUpdate()
    {
        // Move the controller
        variables._controller.Move(playerVelocity * Time.deltaTime);
    }

    /*******************************************************************************************************\
    |* MOVEMENT
    \*******************************************************************************************************/

    /**
     * Sets the movement direction based on player input
     */
    private void SetMovementDir()
    {
        _cmd.forwardMove = movementInput.y;
        _cmd.rightMove = movementInput.x;
    }

    /**
     * Queues the sprint if the toggle option is active
     */

    private void OnSprintPressed()
    {
        if (!wishSprint)
            wishSprint = true;
        else
            wishSprint = false;
    }

    /**
     * Queues the next jump
     */
    private void OnJumpPressed()
    {
        if (!wishJump)
            wishJump = true;
        else
            wishJump = false;
    }

    /**
     * Queues the crouch if the toggle option is active
     */
    private void OnCrouchPressed()
    {
        if (!wishCrouch)
            wishCrouch = true;
        else
            wishCrouch = false;
    }

    /**
     * Queues the walk if the toggle option is active
     */
    private void OnWalkPressed()
    {
        if (!wishWalk)
            wishWalk = true;
        else
            wishWalk = false;
    }

    /**
     * Execs when the player is in the air
    */
    private void AirMove()
    {
        Vector3 wishdir;
        float wishvel = variables.airAcceleration;
        float accel;

        SetMovementDir();

        wishdir = new Vector3(_cmd.rightMove, 0, _cmd.forwardMove);
        wishdir = transform.TransformDirection(wishdir);

        float wishspeed = wishdir.magnitude;
        wishspeed *= variables.moveSpeed;

        if (wishWalk)
        {
            wishspeed *= variables.walkMultiplier;
        }

        wishdir.Normalize();

        // Aircontrol
        float wishspeed2 = wishspeed;
        if (Vector3.Dot(playerVelocity, wishdir) < 0)
            accel = variables.airDecceleration;
        else
            accel = variables.airAcceleration;
        // If the player is ONLY strafing left or right
        if (_cmd.forwardMove == 0 && _cmd.rightMove != 0)
        {
            if (wishspeed > variables.sideStrafeSpeed)
                wishspeed = variables.sideStrafeSpeed;
            accel = variables.sideStrafeAcceleration;
        }

        Accelerate(wishdir, wishspeed, accel);
        if (variables.airControl > 0)
            AirControl(wishdir, wishspeed2);

        // Apply gravity
        playerVelocity.y -= variables.gravity * Time.deltaTime;

        if (wishJump && isDoubleJumpReady)
        {
            variables.playerAnimator.SetTrigger("Jump");
            playerVelocity.y = variables.jumpSpeed;
            wishJump = false;
            isDoubleJumpReady = false;
        }
        else if (wishJump)
        {
            wishJump = false;
        }
    }

    /**
     * Air control occurs when the player is in the air, it allows
     * players to move side to side much faster rather than being
     * 'sluggish' when it comes to cornering.
     */
    private void AirControl(Vector3 wishdir, float wishspeed)
    {
        float zspeed;
        float speed;
        float dot;
        float k;

        // Can't control movement if not moving forward or backward
        if (Mathf.Abs(_cmd.forwardMove) < 0.001 || Mathf.Abs(wishspeed) < 0.001)
            return;
        zspeed = playerVelocity.y;
        playerVelocity.y = 0;
        /* Next two lines are equivalent to idTech's VectorNormalize() */
        speed = playerVelocity.magnitude;
        playerVelocity.Normalize();

        dot = Vector3.Dot(playerVelocity, wishdir);
        k = 32;
        k *= variables.airControl * dot * dot * Time.deltaTime;

        // Change direction while slowing down
        if (dot > 0)
        {
            playerVelocity.x = playerVelocity.x * speed + wishdir.x * k;
            playerVelocity.y = playerVelocity.y * speed + wishdir.y * k;
            playerVelocity.z = playerVelocity.z * speed + wishdir.z * k;

            playerVelocity.Normalize();
        }

        playerVelocity.x *= speed;
        playerVelocity.y = zspeed; // Note this line
        playerVelocity.z *= speed;
    }

    /**
     * Called every frame when the engine detects that the player is on the ground
     */
    private void GroundMove()
    {
        Vector3 wishdir;
        isDoubleJumpReady = true;

        // The player can only slide when the speed reaches at least 90% of the max running speed
        if (variables._controller.velocity.magnitude > variables.moveSpeed * variables.sprintMultiplier * 0.9 && !isSliding)
            isSlideReady = true;
        else
            isSlideReady = false;

        SetMovementDir();

        wishdir = new Vector3(_cmd.rightMove, 0, _cmd.forwardMove);
        wishdir = transform.TransformDirection(wishdir);
        var wishspeed = wishdir.magnitude;

        wishdir.Normalize();

        // Disable the sprint toggle when the player is not moving
        if (wishspeed == 0)
        {
            wishSprint = false;
        }

        // The player starts sliding
        if (wishSprint && wishCrouch && isSlideReady)
        {
            isSliding = true;
            variables.playerAnimator.Play("fp_slide");
            slideTimeElapsed = 0.0f;
            isSlideReady = false; // Prevent continuous sliding
        }

        // isSliding is active as long as the player has not jumped or the timer has not finished counting
        if (isSliding)
        {
            
            slideTimeElapsed += Time.deltaTime;

            // Timeout for the slide
            if (slideTimeElapsed >= variables.slideTime)
            {
                isSliding = false;
                wishSprint = false;
            }
            // Add the crouch speed to the slide speed so the transition between the two is great
            else
                wishspeed *= variables.moveSpeed * variables.slideSpeedMultiplier * (1 - slideTimeElapsed / variables.slideTime) + variables.moveSpeed * variables.crouchMultiplier;

        }

        // Change the friction depending on the action
        if (isSliding)
            ApplyFriction(variables.slideSpeedMultiplier * (1 - slideTimeElapsed / variables.slideTime) + variables.crouchMultiplier);

        // Check when crouching if the slow walk button is pressed and apply the relative friction with the speed multiplier
        else if (isCrouching)
        {
            if (wishWalk)
            {
                wishspeed *= variables.walkMultiplier;
                ApplyFriction(variables.walkMultiplier * variables.crouchMultiplier);
            }
            else
            {
                ApplyFriction(variables.crouchMultiplier);
            }
        }
        else if (isSprinting)
            ApplyFriction(variables.sprintMultiplier);

        // Check when walking normally if the slow walk button is pressed and apply the relative friction with the speed multiplier
        else if (!wishJump)
        {
            if (wishWalk)
            {
                wishspeed *= variables.walkMultiplier;
                ApplyFriction(variables.walkMultiplier * 1.0f);
            }
            else
            {
                ApplyFriction(1.0f);
            }
        }
        // Apply 0 friction when in air
        else
            ApplyFriction(0);

        // The player starts sprinting so disable all the other types of walks
        if (wishSprint && !isSliding)
        {
            wishspeed *= variables.moveSpeed * variables.sprintMultiplier;
            isSprinting = true;
            isCrouching = false;
            wishCrouch = false;
            wishWalk = false;
        }
        // The player starts crouching so disable all the other types of walks
        else if (wishCrouch && !isSliding)
        {
            wishspeed *= variables.moveSpeed * variables.crouchMultiplier;
            isCrouching = true;
            isSprinting = false;
            wishSprint = false;
        }
        // The player is walking normally
        else if (!wishSprint && !wishCrouch && !isSliding)
        {
            wishspeed *= variables.moveSpeed;
            isSprinting = false;
            isCrouching = false;
            wishCrouch = false;
            wishSprint = false;
        }

        // There are to differents accelerations when sliding and not
        if (!isSliding)
        {
            Accelerate(wishdir, wishspeed, variables.runAcceleration);
        }
        else
        {
            // Apply different acceleration for sliding
            Accelerate(wishdir, wishspeed, variables.runAcceleration * variables.slideAcceleration);
        }

        // Reset the gravity velocity
        playerVelocity.y = -variables.gravity * Time.deltaTime;

        if (_cmd.rightMove != 0 || _cmd.forwardMove != 0 && !wishJump && OnSlope())
        {
            playerVelocity.y = -variables.gravity * slopeForce * Time.deltaTime;

        }

        // If the player jumps change the crouch state and the sliding state
        if (wishJump)
        {
            variables.playerAnimator.SetTrigger("Jump");
            playerVelocity.y = variables.jumpSpeed;
            wishJump = false;
            wishCrouch = false;
            isCrouching = false;
            isSliding = false;
        }

        
    }

    /**
     * Applies friction to the player, called in both the air and on the ground
     */
    private void ApplyFriction(float t)
    {
        Vector3 vec = playerVelocity; // Equivalent to: VectorCopy();
        float speed;
        float newspeed;
        float control;
        float drop;

        vec.y = 0.0f;
        speed = vec.magnitude;
        drop = 0.0f;

        /* Only if the player is on the ground then apply friction */
        if (variables._controller.isGrounded)
        {
            control = speed < variables.runDeacceleration ? variables.runDeacceleration : speed;
            drop = control * variables.friction * Time.deltaTime * t;
        }

        newspeed = speed - drop;
        playerFriction = newspeed;
        if (newspeed < 0)
            newspeed = 0;
        if (speed > 0)
            newspeed /= speed;

        playerVelocity.x *= newspeed;
        playerVelocity.z *= newspeed;
    }

    private void Accelerate(Vector3 wishdir, float wishspeed, float accel)
    {
        float addspeed;
        float accelspeed;
        float currentspeed;

        currentspeed = Vector3.Dot(playerVelocity, wishdir);
        addspeed = wishspeed - currentspeed;
        if (addspeed <= 0)
            return;

        accelspeed = accel * Time.deltaTime * wishspeed;
        if (accelspeed > addspeed)
            accelspeed = addspeed;

        playerVelocity.x += accelspeed * wishdir.x;
        playerVelocity.z += accelspeed * wishdir.z;
    }
    private void AnimationsHandler()
    {
        
        variables.playerAnimator.SetBool("Walking", (variables._controller.isGrounded && !isSliding && movementInput != Vector2.zero) ? !wishSprint : false);
        variables.playerAnimator.SetBool("Sprint", (variables._controller.isGrounded && !isSliding) ? wishSprint : false);
        variables.playerAnimator.SetBool("OnGround", variables._controller.isGrounded);
        variables.playerAnimator.SetBool("Slide", isSliding);
    }
       
    private bool OnSlope()
    {
        if (!variables._controller.isGrounded)
            return false;

        RaycastHit hit;
        if (Physics.Raycast(transform.position, Vector3.down, out hit, variables._controller.height / 2 * slopeForceRayLength))
            if (hit.normal != Vector3.up)
                return true;
        
        return false;
        
    }
    public Vector3 GetPlayerVelocity()
    {
        return playerVelocity;
    }

    public bool IsPlayerCrouching()
    {
        return (isCrouching || isSliding);
    }

    public void SetVelocity(Vector3 velocity)
    {
        playerVelocity = velocity;
    }

    public void AddVelocity(Vector3 velocity)
    {
        playerVelocity += velocity;
    }

    public float GetCmdForwardMove()
    {
        return _cmd.forwardMove;
    }

    public float GetCmdRightMove()
    {
        return _cmd.rightMove;
    }

    public float GetCmdUpMove()
    {
        return _cmd.upMove;
    }



}
