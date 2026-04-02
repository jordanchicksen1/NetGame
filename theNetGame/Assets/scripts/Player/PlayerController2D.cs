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

    [Header("Wall Movement")]
    [SerializeField] Transform wallCheck;
    [SerializeField] float wallCheckDistance = 0.3f;
    [SerializeField] float wallSlideSpeed = 2f;
    [SerializeField] Vector2 wallJumpForce = new Vector2(10f, 14f);
    bool isTouchingWall;
    bool isWallSliding;
    bool isWallJumping;
    float wallJumpDirection;
    [SerializeField] float wallJumpControlDelay = 0.15f;
    float wallJumpTimer;
    [SerializeField] float flipLockTime = 0.2f;
    float flipLockTimer;

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

    [Header("Spell System")]
    [SerializeField] Transform firePoint;
    [SerializeField] GameObject fireProjectilePrefab;
    [SerializeField] GameObject iceProjectilePrefab;
    [SerializeField] GameObject poisonProjectilePrefab;

    [SerializeField] float shootCooldown = 0.5f;

    SpellType currentSpell = SpellType.None;
    float shootTimer;

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
        CheckWall();

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
        HandleWallSlide();
        ApplyMovement();

        if (isGrounded)
        {
            isWallJumping = false;
        }

        if (wallJumpTimer > 0)
        {
            wallJumpTimer -= Time.fixedDeltaTime;
        }
        else
        {
            isWallJumping = false;
        }

        if (flipLockTimer > 0)
        {
            flipLockTimer -= Time.fixedDeltaTime;
        }

        if (shootTimer > 0)
        {
            shootTimer -= Time.fixedDeltaTime;
        }
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

        if (controls.Player.Shoot.triggered)
        {
            TryShoot();
        }
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

    void HandleWallSlide()
    {
        bool pushingIntoWall =
            (isTouchingWall && moveInput.x > 0 && facingRight) ||
            (isTouchingWall && moveInput.x < 0 && !facingRight);

        if (pushingIntoWall && !isGrounded && rb.linearVelocity.y < 0)
        {
            isWallSliding = true;

            // Clamp fall speed instead of forcing it
            if (rb.linearVelocity.y < -wallSlideSpeed)
            {
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
            }
        }
        else
        {
            isWallSliding = false;
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

    void TryShoot()
    {
        if (currentSpell == SpellType.None) return;

        if (shootTimer > 0f) return;

        GameObject projectilePrefab = null;

        switch (currentSpell)
        {
            case SpellType.Fire:
                projectilePrefab = fireProjectilePrefab;
                break;
            case SpellType.Ice:
                projectilePrefab = iceProjectilePrefab;
                break;
            case SpellType.Poison:
                projectilePrefab = poisonProjectilePrefab;
                break;
        }

        if (projectilePrefab == null) return;

        GameObject projectile = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.identity
        );

        // Set direction
        float direction = facingRight ? 1f : -1f;
        projectile.GetComponent<Projectile>().Initialize(direction, gameObject);

        shootTimer = shootCooldown;
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

    void CheckWall()
    {
        isTouchingWall = Physics2D.Raycast(
            wallCheck.position,
            transform.right,
            wallCheckDistance,
            groundLayer
        );
    }

    // ---------------- MOVEMENT ----------------
    void ApplyMovement()
    {
        if (isGroundPounding) return;

        if (isWallJumping && wallJumpTimer > 0f) return;

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
        if (flipLockTimer <= 0f)
        {
            if (moveInput.x > 0 && !facingRight)
            {
                Flip();
            }
            else if (moveInput.x < 0 && facingRight)
            {
                Flip();
            }
        }

        // Wall Jump
        if (jumpPressed && isWallSliding)
        {
            flipLockTimer = flipLockTime;
            isWallJumping = true;
            wallJumpTimer = wallJumpControlDelay;

            wallJumpDirection = -transform.localScale.x;

            rb.linearVelocity = new Vector2(
                wallJumpDirection * wallJumpForce.x,
                wallJumpForce.y
            );

            // Flip player to face jump direction
            if ((wallJumpDirection > 0 && !facingRight) ||
                (wallJumpDirection < 0 && facingRight))
            {
                Flip();
            }
        }
        //normal jump
        else if (jumpPressed && coyoteTimeCounter > 0f)
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

    public void LoseSpell()
    {
        currentSpell = SpellType.None;

        // later: add visual feedback here
    }

    public void SetSpell(SpellType newSpell)
    {
        currentSpell = newSpell;
    }
}