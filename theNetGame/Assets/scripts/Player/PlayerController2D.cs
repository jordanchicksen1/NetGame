using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 8f;
    [SerializeField] float jumpForce = 14f;
    [SerializeField] float groundAcceleration = 60f;
    [SerializeField] float airAcceleration = 25f;
    [SerializeField] float maxSpeed = 8f;
    [SerializeField] float fallMultiplier = 2.5f;
    [SerializeField] float lowJumpMultiplier = 3.5f;
    bool jumpHeld;
    bool facingRight = true;

    [Header("Jump Assist")]
    [SerializeField] float coyoteTime = 0.1f;
    float coyoteTimeCounter;

    [Header("Ground Pound")]
    [SerializeField] float groundPoundForce = 25f;
    [SerializeField] float groundPoundDuration = 0.2f;
    [SerializeField] float minAirTimeForGroundPound = 0.2f;

    bool isGroundPounding;
    float groundPoundTimer;
    float airTimeCounter;
    bool downPressed;

    [Header("Checks")]
    [SerializeField] Transform groundCheck;
    [SerializeField] float groundCheckRadius = 0.2f;
    [SerializeField] LayerMask groundLayer;

    Rigidbody2D rb;
    PlayerControls controls;

    Vector2 moveInput;
    bool jumpPressed;
    bool isGrounded;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        controls = new PlayerControls();
    }

    void OnEnable()
    {
        controls.Enable();

        controls.Player.Jump.performed += OnJumpPressed;
        controls.Player.Jump.canceled += OnJumpReleased;
    }

    void OnDisable()
    {
        controls.Player.Jump.performed -= OnJumpPressed;
        controls.Player.Jump.canceled -= OnJumpReleased;
        controls.Disable();
    }

    void Update()
    {
        HandleInput();
    }

    void FixedUpdate()
    {
        CheckGround();

        // Track airtime
        if (isGrounded)
        {
            airTimeCounter = 0f;
        }
        else
        {
            airTimeCounter += Time.fixedDeltaTime;
        }

        HandleGroundPound();
        ApplyMovement();
    }

    // ---------------- INPUT ----------------
    void HandleInput()
    {
        Vector2 previousInput = moveInput;

        moveInput = controls.Player.Move.ReadValue<Vector2>();

        // Detect DOWN press (not hold)
        if (previousInput.y >= -0.5f && moveInput.y < -0.5f)
        {
            downPressed = true;
        }

        if (!isGrounded
            && airTimeCounter > minAirTimeForGroundPound
            && downPressed
            && !isGroundPounding)
        {
            StartGroundPound();
        }

        downPressed = false;
    }

    void StartGroundPound()
    {
        isGroundPounding = true;
        groundPoundTimer = groundPoundDuration;

        // Slam downward
        rb.linearVelocity = new Vector2(0f, -groundPoundForce);
    }

    void HandleGroundPound()
    {
        if (!isGroundPounding) return;

        groundPoundTimer -= Time.fixedDeltaTime;

        // Force downward velocity
        rb.linearVelocity = new Vector2(0f, -groundPoundForce);

        // Stop on ground OR timeout
        if (isGrounded || groundPoundTimer <= 0f)
        {
            isGroundPounding = false;
        }
    }

    void OnJumpPressed(InputAction.CallbackContext context)
    {
        jumpPressed = true;
        jumpHeld = true;
    }

    void OnJumpReleased(InputAction.CallbackContext context)
    {
        jumpHeld = false;
    }

    // ---------------- PHYSICS ----------------
    void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;

            // Safety reset
            isGroundPounding = false;
        }
        else
        {
            coyoteTimeCounter -= Time.fixedDeltaTime;
        }
    }

    // ---------------- MOVEMENT ----------------
    void ApplyMovement()
    {
        if (isGroundPounding) return;

        // Horizontal movement
        float targetSpeed = moveInput.x * maxSpeed;

        float accel = isGrounded ? groundAcceleration : airAcceleration;

        float newVelocityX = Mathf.MoveTowards(
            rb.linearVelocity.x,
            targetSpeed,
            accel * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);
        
        // Flip character based on movement direction
        if (moveInput.x > 0 && !facingRight)
        {
            Flip();
        }
        else if (moveInput.x < 0 && facingRight)
        {
            Flip();
        }

        // Jump
        if (jumpPressed && coyoteTimeCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            coyoteTimeCounter = 0f;
        }

        // Better jump physics
        if (rb.linearVelocity.y < 0)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
        }
        else if (rb.linearVelocity.y > 0 && !jumpHeld)
        {
            rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
        }

        jumpPressed = false;
    }

    void Flip()
    {
        facingRight = !facingRight;

        Vector3 scale = transform.localScale;
        scale.x *= -1;
        transform.localScale = scale;
    }
}