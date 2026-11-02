using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class AdvancedPlayerController : MonoBehaviour
{
    public enum MovementState
    {
        Walking,
        Sprinting,
        Crouching,
        Sliding,
        Dashing,
        Wallrunning,
        Airborne
    }

    [Header("State")]
    [SerializeField] private MovementState currentState = MovementState.Walking;

    [Header("Movement")]
    [SerializeField] private float walkSpeed = 6.0f;
    [SerializeField] private float sprintSpeed = 10.5f;
    [SerializeField] private float groundAcceleration = 120.0f;
    [SerializeField] private float groundDeceleration = 140.0f;
    [SerializeField] private float airAcceleration = 40.0f;
    [SerializeField] private float airBrakeAcceleration = 60.0f;
    [SerializeField] private float airStrafeSteerSpeed = 8.0f;
    [SerializeField] private float airDeceleration = 0.5f;

    [Header("Bunny Hopping")]
    [SerializeField] private bool enableBhopping = true;
    [SerializeField] private float bhopLandingGraceWindow = 0.15f;
    [SerializeField] private float bhopPreservationMultiplier = 1.0f;
    [SerializeField] private float excessMomentumDecayRate = 25.0f;

    [Header("Jump & Gravity")]
    [SerializeField] private float jumpHeight = 2.0f;
    [SerializeField] private float gravity = -24.0f;
    [SerializeField] private float terminalVelocity = -40.0f;
    [SerializeField] private float coyoteTime = 0.18f;
    [SerializeField] private float jumpBufferTime = 0.15f;
    [SerializeField] private int extraAirJumps = 0;

    [Header("Crouch & Slide")]
    [SerializeField] private float crouchSpeed = 3.5f;
    [SerializeField] private float slideSpeedBoost = 3.0f;
    [SerializeField] private float slideDecayDelay = 0.15f;
    [SerializeField] private float slideFriction = 5.0f;
    [SerializeField] private float minSlideSpeed = 4.0f;
    [SerializeField] private float slideSlopeBoostMultiplier = 28.0f;
    [SerializeField] private float maxSlideSpeed = 150.0f;
    [SerializeField] private float standingHeight = 2.0f;
    [SerializeField] private float crouchingHeight = 1.0f;
    [SerializeField] private float heightTransitionSpeed = 14.0f;

    [Header("Dash")]
    [SerializeField] private float dashSpeedBoost = 15.0f;
    [SerializeField] private float minDashSpeed = 18.0f;
    [SerializeField] private float maxDashSpeed = 50.0f;
    [SerializeField] private float dashDuration = 0.2f;
    [SerializeField] private float dashCooldown = 0.8f;
    [SerializeField] private bool allowAirDash = true;
    [SerializeField] private int maxAirDashes = 1;

    [Header("Slopes")]
    [SerializeField] private float maxSlopeAngle = 45.0f;
    [SerializeField] private float slopeStickForce = 8.0f;
    [SerializeField] private float steepSlopeSlideSpeed = 12.0f;

    [Header("Wallrun")]
    [SerializeField] private LayerMask wallrunLayers;
    [SerializeField] private float wallrunSpeed = 11.5f;
    [SerializeField] private float wallrunAcceleration = 4.0f;
    [SerializeField] private float maxWallrunSpeed = 90.0f;
    [SerializeField] private float maxWallrunDuration = 2.5f;
    [SerializeField] private float wallrunGravity = -4.0f;
    [SerializeField] private float wallClimbSpeed = 3.5f;
    [SerializeField] private float wallCheckDistance = 0.75f;
    [SerializeField] private float minWallrunGroundHeight = 1.0f;
    [SerializeField] private float wallJumpUpForce = 7.5f;
    [SerializeField] private float wallJumpSideForce = 9.0f;
    [SerializeField] private float wallJumpForwardForce = 5.0f;
    [SerializeField] private float wallrunCooldown = 0.35f;

    [Header("Camera")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float baseFOV = 60.0f;
    [SerializeField] private float sprintFOV = 68.0f;
    [SerializeField] private float highSpeedFOV = 76.0f;
    [SerializeField] private float wallrunFOV = 72.0f;
    [SerializeField] private float fovChangeSpeed = 10.0f;
    [SerializeField] private float wallrunTiltAngle = 14.0f;
    [SerializeField] private float slideTiltAngle = 3.5f;
    [SerializeField] private float tiltChangeSpeed = 12.0f;
    [SerializeField] private float lookSensitivity = 0.12f;
    [SerializeField] private float maxPitchAngle = 88.0f;
    [SerializeField] private bool lockCursorOnStart = true;

    public float LookSensitivity
    {
        get => lookSensitivity;
        set => lookSensitivity = Mathf.Max(0.01f, value);
    }

    // Public properties for HUD
    public MovementState CurrentState => currentState;
    public bool IsGrounded => isGrounded;
    public bool IsSliding => currentState == MovementState.Sliding;
    public bool IsDashing => currentState == MovementState.Dashing;
    public bool IsWallrunning => currentState == MovementState.Wallrunning;
    public Vector3 HorizontalVelocity => horizontalVelocity;
    public float CurrentSpeed => horizontalVelocity.magnitude;

    // Components & scene setup
    private CharacterController controller;
    private Camera playerCam;
    private LayerMask groundLayers;

    // Physics & Movement state
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private bool isGrounded;
    private bool wasGrounded;
    private RaycastHit groundHit;
    private float groundSlopeAngle;

    // Slide state
    private Vector3 slideDirection;
    private float currentSlideSpeed;
    private float slideDecayTimer;

    // Dash state
    private float dashTimer;
    private float dashCooldownTimer;
    private int airDashesLeft;

    // Wallrun state
    private bool wallLeft;
    private bool wallRight;
    private RaycastHit wallHit;
    private Vector3 wallForward;
    private float wallrunTimer;
    private float wallrunCooldownTimer;
    private float currentWallrunSpeed;

    // Jump & Timer state
    private float coyoteTimer;
    private float jumpBufferTimer;
    private float bhopTimer;
    private int airJumpsLeft;

    // Camera values
    private float cameraPitch;
    private float targetTilt;
    private float currentTilt;
    private float targetFOV;
    private Vector3 standingCameraPosition;
    private readonly Collider[] standingHits = new Collider[8];

    // Keyboard inputs
private Vector2 moveInput;
    private bool sprintHeld;
    private bool crouchHeld;
    private bool crouchButtonHeld;
    private bool jumpPressed;
    private bool dashPressed;
    private bool crouchPressed;
    private PauseMenuManager pauseMenu;
    private LevelCompleteUI completeUI;
    private InputActionMap movementActions;
    private InputAction moveAction;
    private InputAction lookAction;
    private InputAction sprintAction;
    private InputAction crouchAction;
    private InputAction crouchCAction;

    private void Awake()
    {
        enabled = true;
        controller = GetComponent<CharacterController>();
        pauseMenu = FindAnyObjectByType<PauseMenuManager>();
        completeUI = FindAnyObjectByType<LevelCompleteUI>();
        SetupInput();

        if (PlayerPrefs.HasKey("MouseSensitivity"))
        {
            lookSensitivity = PlayerPrefs.GetFloat("MouseSensitivity", lookSensitivity);
        }

        if (cameraTransform == null)
        {
            playerCam = GetComponentInChildren<Camera>();
            if (playerCam != null) cameraTransform = playerCam.transform;
        }
        else
        {
            playerCam = cameraTransform.GetComponent<Camera>();
        }

        groundLayers = ~LayerMask.GetMask("Ignore Raycast", "TransparentFX");
        if (wallrunLayers.value == 0)
            wallrunLayers = groundLayers;

        if (cameraTransform != null)
            standingCameraPosition = cameraTransform.localPosition;

        targetFOV = baseFOV;
        if (playerCam != null) playerCam.fieldOfView = baseFOV;
}

    private void OnEnable()
    {
        movementActions?.Enable();
        if (lockCursorOnStart && CanUseInput)
            SetCursorLock(true);
    }

    private void OnDisable()
    {
        movementActions?.Disable();
        dashPressed = false;
        crouchPressed = false;
    }

    private void OnDestroy()
    {
        movementActions?.Dispose();
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (hasFocus && lockCursorOnStart && enabled && CanUseInput)
            SetCursorLock(true);
    }

    private void Start()
    {
        if (lockCursorOnStart)
            SetCursorLock(true);

        if (cameraTransform != null)
        {
            cameraPitch = cameraTransform.localEulerAngles.x;
            if (cameraPitch > 180f) cameraPitch -= 360f;
        }

        airJumpsLeft = extraAirJumps;
        airDashesLeft = maxAirDashes;
    }

    private void Update()
    {
        float dt = Time.deltaTime;
        if (dt <= 0f || !CanUseInput)
        {
            dashPressed = false;
            crouchPressed = false;
            jumpBufferTimer = 0f;
            return;
        }

        ReadKeyboardMouseInputs();
        TickTimers(dt);
        HandleMouseLook(dt);

        CheckSurroundings();
        UpdateStateAndLogic(dt);
        ApplyMovement(dt);
        UpdateCamera(dt);

        wasGrounded = isGrounded;
        dashPressed = false;
        crouchPressed = false;
}

    private void SetupInput()
    {
        movementActions = new InputActionMap("Movement");
        moveAction = movementActions.AddAction("Move", InputActionType.Value);
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/w").With("Down", "<Keyboard>/s")
            .With("Left", "<Keyboard>/a").With("Right", "<Keyboard>/d");
        moveAction.AddCompositeBinding("2DVector")
            .With("Up", "<Keyboard>/upArrow").With("Down", "<Keyboard>/downArrow")
            .With("Left", "<Keyboard>/leftArrow").With("Right", "<Keyboard>/rightArrow");
        lookAction = movementActions.AddAction("Look", InputActionType.Value, "<Mouse>/delta");
        sprintAction = movementActions.AddAction("Sprint", InputActionType.Button, "<Keyboard>/leftShift");
        crouchAction = movementActions.AddAction("Crouch", InputActionType.Button, "<Keyboard>/leftCtrl");
        crouchCAction = movementActions.AddAction("Crouch C", InputActionType.Button, "<Keyboard>/c");
        crouchAction.performed += OnCrouch;
        crouchCAction.performed += OnCrouch;
        var jump = movementActions.AddAction("Jump", InputActionType.Button, "<Keyboard>/space");
        jump.performed += OnJump;
        var dash = movementActions.AddAction("Dash", InputActionType.Button, "<Keyboard>/e");
        dash.AddBinding("<Keyboard>/leftAlt");
        dash.performed += OnDash;
        var cursor = movementActions.AddAction("Cursor", InputActionType.Button, "<Mouse>/leftButton");
        cursor.AddBinding("<Mouse>/rightButton");
        cursor.performed += OnCursor;
    }

    private bool CanUseInput => Time.timeScale > 0f &&
        (pauseMenu == null || !pauseMenu.IsPaused) &&
        (completeUI == null || !completeUI.IsCompleted);

    private void OnJump(InputAction.CallbackContext context)
    {
        if (CanUseInput) jumpBufferTimer = jumpBufferTime;
    }

    private void OnDash(InputAction.CallbackContext context)
    {
        if (CanUseInput) dashPressed = true;
    }

    private void OnCrouch(InputAction.CallbackContext context)
    {
        if (CanUseInput) crouchPressed = true;
    }

    private void OnCursor(InputAction.CallbackContext context)
    {
        if (CanUseInput && Cursor.lockState != CursorLockMode.Locked)
            SetCursorLock(true);
    }

    private void ReadKeyboardMouseInputs()
    {
        moveInput = moveAction.ReadValue<Vector2>();
        sprintHeld = sprintAction.IsPressed();
        crouchHeld = crouchAction.IsPressed() || crouchCAction.IsPressed();
        crouchButtonHeld = crouchHeld;
        if (!crouchHeld && controller.height < standingHeight - 0.01f)
        {
            float radius = controller.radius;
            Vector3 bottom = transform.position + Vector3.up * radius;
            Vector3 top = transform.position + Vector3.up * (standingHeight - radius);
            int count = Physics.OverlapCapsuleNonAlloc(bottom, top, radius,
                standingHits, groundLayers, QueryTriggerInteraction.Ignore);
            crouchHeld = count == standingHits.Length;
            for (int i = 0; i < count; i++)
            {
                if (standingHits[i] != controller &&
                    standingHits[i].bounds.max.y > transform.position.y + controller.height)
                    crouchHeld = true;
                standingHits[i] = null;
            }
        }
    }

    public void SetCursorLock(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    private void HandleMouseLook(float dt)
    {
        if (Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 mouseDelta = lookAction.ReadValue<Vector2>();
        float yaw = mouseDelta.x * lookSensitivity;
        float pitch = mouseDelta.y * lookSensitivity;
        Quaternion yawRotation = Quaternion.Euler(0f, yaw, 0f);

        transform.Rotate(Vector3.up, yaw);

        if (horizontalVelocity.sqrMagnitude > 0.01f && Mathf.Abs(yaw) > 0.001f)
            horizontalVelocity = yawRotation * horizontalVelocity;

        if (currentState == MovementState.Sliding && slideDirection.sqrMagnitude > 0.01f)
            slideDirection = yawRotation * slideDirection;

        cameraPitch = Mathf.Clamp(cameraPitch - pitch, -maxPitchAngle, maxPitchAngle);
    }

    // Timers 
    private void TickTimers(float dt)
    {
        if (!isGrounded && coyoteTimer > 0f) coyoteTimer -= dt;
        if (jumpBufferTimer > 0f) jumpBufferTimer -= dt;
        if (dashTimer > 0f) dashTimer -= dt;
        if (dashCooldownTimer > 0f) dashCooldownTimer -= dt;
        if (wallrunCooldownTimer > 0f) wallrunCooldownTimer -= dt;
        if (bhopTimer > 0f) bhopTimer -= dt;
    }

    // Ground & Wall Detection 
    private void CheckSurroundings()
    {
        float rayLen = (controller.height * 0.5f) + 0.35f;
        Vector3 origin = transform.position + Vector3.up * 0.1f;

        if (Physics.Raycast(origin, Vector3.down, out groundHit, rayLen, groundLayers, QueryTriggerInteraction.Ignore))
        {
            groundSlopeAngle = Vector3.Angle(Vector3.up, groundHit.normal);
            isGrounded = controller.isGrounded || groundSlopeAngle <= maxSlopeAngle;
        }
        else
        {
            groundSlopeAngle = 0f;
            isGrounded = controller.isGrounded;
        }

        if (isGrounded && !wasGrounded)
        {
            bhopTimer = bhopLandingGraceWindow;
            coyoteTimer = coyoteTime;
            airJumpsLeft = extraAirJumps;
            airDashesLeft = maxAirDashes;
        }

        if (isGrounded) coyoteTimer = coyoteTime;

        wallLeft = false;
        wallRight = false;
        if (isGrounded || moveInput.y <= 0.1f || wallrunCooldownTimer > 0f)
            return;

        RaycastHit leftHit, rightHit;
        wallRight = Physics.Raycast(transform.position, transform.right, out rightHit, wallCheckDistance, wallrunLayers, QueryTriggerInteraction.Ignore);
        wallLeft = Physics.Raycast(transform.position, -transform.right, out leftHit, wallCheckDistance, wallrunLayers, QueryTriggerInteraction.Ignore);
        wallHit = wallRight ? rightHit : leftHit;
}

    // State Machine 
    private void UpdateStateAndLogic(float dt)
    {
        bool hasMoveInput = moveInput.sqrMagnitude > 0.01f;

        // Dash
        if (dashPressed && dashCooldownTimer <= 0f && (isGrounded || (allowAirDash && airDashesLeft > 0)))
        {
            StartDash();
            return;
        }

        if (currentState == MovementState.Dashing)
        {
            if (dashTimer <= 0f)
            {
                currentState = isGrounded ? MovementState.Walking : MovementState.Airborne;
            }
            else
            {
                if (jumpBufferTimer > 0f) TryJump();
                return;
            }
        }

        // Wallrun
        bool canWallrun = !isGrounded && (wallLeft || wallRight) && moveInput.y > 0.1f && wallrunCooldownTimer <= 0f &&
                          !Physics.Raycast(transform.position, Vector3.down, minWallrunGroundHeight, groundLayers, QueryTriggerInteraction.Ignore);

        if (canWallrun)
        {
            if (currentState != MovementState.Wallrunning)
                StartWallrun();

            UpdateWallrun(dt);
            return;
        }
        else if (currentState == MovementState.Wallrunning)
        {
            EndWallrun();
        }

        // Slide
        bool landedWithCrouch = isGrounded && !wasGrounded && crouchButtonHeld &&
                                horizontalVelocity.magnitude > walkSpeed + 0.5f;
        if (currentState != MovementState.Sliding &&
            (landedWithCrouch || (crouchPressed && isGrounded && hasMoveInput &&
             (sprintHeld || horizontalVelocity.magnitude > walkSpeed + 0.5f))))
        {
            StartSlide(landedWithCrouch);
            return;
        }

        if (currentState == MovementState.Sliding)
        {
            bool onSlope = groundSlopeAngle > 1f;
            if (!crouchHeld || (!onSlope && currentSlideSpeed < minSlideSpeed))
            {
                currentState = MovementState.Crouching;
                targetTilt = 0f;
            }
            else
            {
                UpdateSlide(dt);
                if (jumpBufferTimer > 0f)
                {
                    verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
                    horizontalVelocity = slideDirection * (currentSlideSpeed * bhopPreservationMultiplier);
                    currentState = MovementState.Airborne;
                    jumpBufferTimer = 0f;
                    targetTilt = 0f;
                    return;
                }
                return;
            }
        }

        // Jump
        if (jumpBufferTimer > 0f)
        {
            TryJump();
        }

        // Normal grounded/airborne states
        if (isGrounded)
        {
            targetTilt = 0f;
            if (crouchHeld)
            {
                currentState = MovementState.Crouching;
                targetFOV = baseFOV;
            }
            else if (sprintHeld && hasMoveInput)
            {
                currentState = MovementState.Sprinting;
                targetFOV = sprintFOV;
            }
            else
            {
                currentState = MovementState.Walking;
                targetFOV = horizontalVelocity.magnitude > sprintSpeed + 1f ? highSpeedFOV : baseFOV;
            }
        }
        else
        {
            currentState = MovementState.Airborne;
            targetTilt = 0f;
            targetFOV = horizontalVelocity.magnitude > sprintSpeed + 1f ? highSpeedFOV : baseFOV;
        }
    }

    private void TryJump()
    {
        if (coyoteTimer > 0f || (isGrounded && bhopTimer > 0f))
        {
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            if (enableBhopping && horizontalVelocity.magnitude > sprintSpeed)
                horizontalVelocity *= bhopPreservationMultiplier;

            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            bhopTimer = 0f;
        }
        else if (airJumpsLeft > 0)
        {
            airJumpsLeft--;
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            jumpBufferTimer = 0f;
        }
    }

    private void StartDash()
    {
        currentState = MovementState.Dashing;
        dashTimer = dashDuration;
        dashCooldownTimer = dashCooldown;
        if (!isGrounded) airDashesLeft--;

        Vector3 dir = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
        if (dir.sqrMagnitude < 0.01f) dir = transform.forward;

        horizontalVelocity = dir * Mathf.Clamp(horizontalVelocity.magnitude + dashSpeedBoost, minDashSpeed, maxDashSpeed);
        verticalVelocity = 0f;
        targetFOV = highSpeedFOV;
    }

    private void StartWallrun()
    {
        currentState = MovementState.Wallrunning;
        wallrunTimer = maxWallrunDuration;
        verticalVelocity = 0f;
        airJumpsLeft = extraAirJumps;
        airDashesLeft = maxAirDashes;

        wallForward = Vector3.Cross(wallHit.normal, Vector3.up);
        if (Vector3.Dot(wallForward, transform.forward) < 0f) wallForward = -wallForward;

        currentWallrunSpeed = Mathf.Max(horizontalVelocity.magnitude, wallrunSpeed);
        targetTilt = wallRight ? wallrunTiltAngle : -wallrunTiltAngle;
        targetFOV = wallrunFOV;
    }

    private void UpdateWallrun(float dt)
    {
        wallrunTimer -= dt;
        if (wallrunTimer <= 0f)
        {
            EndWallrun();
            wallrunCooldownTimer = wallrunCooldown;
            return;
        }

        if (jumpBufferTimer > 0f)
        {
            EndWallrun();
            wallrunCooldownTimer = wallrunCooldown;
            horizontalVelocity = wallHit.normal * wallJumpSideForce + transform.forward * Mathf.Max(wallJumpForwardForce, currentWallrunSpeed);
            verticalVelocity = wallJumpUpForce;
            jumpBufferTimer = 0f;
            return;
        }

        wallForward = Vector3.Cross(wallHit.normal, Vector3.up);
        if (Vector3.Dot(wallForward, transform.forward) < 0f) wallForward = -wallForward;

        currentWallrunSpeed = Mathf.Min(currentWallrunSpeed + wallrunAcceleration * dt, maxWallrunSpeed);
        horizontalVelocity = wallForward * currentWallrunSpeed - wallHit.normal * 1.5f;
        verticalVelocity = sprintHeld ? wallClimbSpeed : Mathf.MoveTowards(verticalVelocity, wallrunGravity, 8f * dt);
    }

    private void EndWallrun()
    {
        targetTilt = 0f;
        targetFOV = baseFOV;
        currentState = isGrounded ? MovementState.Walking : MovementState.Airborne;
        if (wallForward.sqrMagnitude > 0.01f)
            horizontalVelocity = wallForward * currentWallrunSpeed;
    }

    private void StartSlide(bool useMomentum = false)
    {
        currentState = MovementState.Sliding;
        Vector3 inputDir = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
        slideDirection = useMomentum ? horizontalVelocity.normalized :
            (inputDir.sqrMagnitude > 0.01f ? inputDir : transform.forward);
        currentSlideSpeed = horizontalVelocity.magnitude + slideSpeedBoost;
        slideDecayTimer = slideDecayDelay;
        horizontalVelocity = slideDirection * currentSlideSpeed;
        targetFOV = highSpeedFOV;
        targetTilt = slideTiltAngle;
    }

    private void UpdateSlide(float dt)
    {
        Vector3 inputDir = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
        if (inputDir.sqrMagnitude > 0.01f)
            slideDirection = Vector3.RotateTowards(slideDirection, inputDir, 3f * dt, 0f);

        float decayDt = Mathf.Max(0f, dt - slideDecayTimer);
        slideDecayTimer = Mathf.Max(0f, slideDecayTimer - dt);

        if (groundSlopeAngle > 1f)
        {
            Vector3 downhill = Vector3.ProjectOnPlane(Vector3.down, groundHit.normal).normalized;
            float dot = Vector3.Dot(slideDirection, downhill);
            if (dot > 0f)
                currentSlideSpeed = Mathf.Min(currentSlideSpeed + slideSlopeBoostMultiplier * dot * dt, maxSlideSpeed);
            else
                currentSlideSpeed -= slideFriction * decayDt + slideSlopeBoostMultiplier * Mathf.Abs(dot) * dt;
        }
        else
        {
            currentSlideSpeed = Mathf.MoveTowards(currentSlideSpeed, 0f, slideFriction * decayDt);
        }

        horizontalVelocity = slideDirection * currentSlideSpeed;
    }

    //  Movement Physics 
    private void ApplyMovement(float dt)
    {
        if (currentState == MovementState.Dashing || currentState == MovementState.Wallrunning)
        {
            controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * dt);
            return;
        }

        if (currentState != MovementState.Sliding)
        {
            Vector3 targetDir = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
            float targetSpeed = currentState == MovementState.Sprinting ? sprintSpeed :
                                currentState == MovementState.Crouching ? crouchSpeed : walkSpeed;
            Vector3 targetVel = targetDir * targetSpeed;

            if (isGrounded)
            {
                // Keep speed between jumps.
                if (enableBhopping && bhopTimer > 0f && horizontalVelocity.magnitude > targetSpeed)
                {
                    if (targetDir.sqrMagnitude > 0.01f)
                        horizontalVelocity = Vector3.RotateTowards(horizontalVelocity, targetDir * horizontalVelocity.magnitude, airStrafeSteerSpeed * dt, 0f);
                }
                else if (targetDir.sqrMagnitude > 0.01f)
                {
                    float moveDotVel = horizontalVelocity.sqrMagnitude > 0.01f ? Vector3.Dot(targetDir, horizontalVelocity.normalized) : 1f;
                    if (moveDotVel < -0.2f)
                    {
                        // Slow down when turning back.
                        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVel, groundDeceleration * 1.5f * dt);
                    }
                    else if (horizontalVelocity.magnitude > targetSpeed + 0.1f)
                    {
                        // Return to normal speed.
                        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVel, excessMomentumDecayRate * dt);
                    }
                    else
                    {
                        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVel, groundAcceleration * dt);
                    }
                }
                else
                {
                    // Stop when no keys are held.
                    horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, groundDeceleration * dt);
                    if (horizontalVelocity.sqrMagnitude < 0.04f)
                        horizontalVelocity = Vector3.zero;
                }
            }
            else
            {
                // Brake in the air.
                if (moveInput.y < -0.2f)
                {
                    Vector3 brakeDir = (transform.forward * moveInput.y + transform.right * moveInput.x).normalized;
                    Vector3 brakeTargetVel = brakeDir * sprintSpeed;
                    horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, brakeTargetVel, airBrakeAcceleration * dt);
                }
                // Steer in the air.
                else if (targetDir.sqrMagnitude > 0.01f)
                {
                    if (horizontalVelocity.magnitude <= targetSpeed)
                    {
                        horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVel, airAcceleration * dt);
                    }
                    else
                    {
                        // Turn without losing speed.
                        horizontalVelocity = Vector3.RotateTowards(horizontalVelocity, targetDir * horizontalVelocity.magnitude, airStrafeSteerSpeed * dt, 0f);
                    }
                }
                else
                {
                    // Apply a little air drag.
                    horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, Vector3.zero, airDeceleration * dt);
                }
            }
        }

        // Gravity & Slope sticking
        if (isGrounded)
        {
            if (verticalVelocity < 0f) verticalVelocity = -slopeStickForce;
        }
        else
        {
            verticalVelocity = Mathf.Max(verticalVelocity + gravity * dt, terminalVelocity);
        }

        // Steep slope sliding
        if (isGrounded && groundSlopeAngle > maxSlopeAngle)
        {
            Vector3 steepSlide = new Vector3(groundHit.normal.x, -groundHit.normal.y, groundHit.normal.z);
            horizontalVelocity += steepSlide * (steepSlopeSlideSpeed * dt);
        }

        Vector3 finalVelocity = isGrounded && groundSlopeAngle <= maxSlopeAngle && groundSlopeAngle > 1f
            ? Vector3.ProjectOnPlane(horizontalVelocity, groundHit.normal).normalized * horizontalVelocity.magnitude
            : horizontalVelocity;

        controller.Move((finalVelocity + Vector3.up * verticalVelocity) * dt);
    }

    // Camera & Crouch Height Transition
    private void UpdateCamera(float dt)
    {
        bool crouching = currentState == MovementState.Crouching || currentState == MovementState.Sliding;
        float targetHeight = crouching ? crouchingHeight : standingHeight;
        if (Mathf.Abs(controller.height - targetHeight) > 0.01f)
        {
            float h = Mathf.MoveTowards(controller.height, targetHeight, heightTransitionSpeed * dt);
            controller.height = h;
            controller.center = new Vector3(0f, h * 0.5f, 0f);
        }

        currentTilt = Mathf.Lerp(currentTilt, targetTilt, 1f - Mathf.Exp(-tiltChangeSpeed * dt));
        if (cameraTransform != null)
        {
            Vector3 cameraPosition = standingCameraPosition;
            cameraPosition.y *= controller.height / standingHeight;
            cameraTransform.localPosition = cameraPosition;
            cameraTransform.localRotation = Quaternion.Euler(cameraPitch, 0f, currentTilt);
        }

        if (playerCam != null)
playerCam.fieldOfView = Mathf.Lerp(playerCam.fieldOfView, targetFOV, 1f - Mathf.Exp(-fovChangeSpeed * dt));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawRay(transform.position, Vector3.down * (standingHeight * 0.5f + 0.35f));
        Gizmos.color = Color.blue;
        Gizmos.DrawRay(transform.position, transform.right * wallCheckDistance);
        Gizmos.DrawRay(transform.position, -transform.right * wallCheckDistance);
    }
}
