using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Netcode;

public class PlayerController2D : NetworkBehaviour
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

    [Header("Status Effects")]
    [SerializeField] float iceDuration = 3f;
    [SerializeField] float fireDuration = 5f;
    [SerializeField] float poisonDuration = 3f;
    [SerializeField] float poisonSlowMultiplier = 0.4f;

    StatusEffectType currentEffect = StatusEffectType.None;
    float effectTimer;

    float forcedMoveDirection;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        Camera playerCam = GetComponentInChildren<Camera>();

        if (!IsOwner)
        {
            if (playerCam != null)
                playerCam.gameObject.SetActive(false);
        }
        else
        {
            Camera mainCam = Camera.main;
            if (mainCam != null)
                mainCam.gameObject.SetActive(false);

            if (playerCam != null)
                playerCam.gameObject.SetActive(true);

            CameraFollow2D camFollow = GetComponentInChildren<CameraFollow2D>();
            if (camFollow != null)
                camFollow.target = transform;
        }


        PlayerInput input = GetComponent<PlayerInput>();

        if (input != null)
        {
            input.enabled = IsOwner;
        }
    }

    void FixedUpdate()
    {
        if (!IsOwner) return;

        CheckGround();
        CheckWall();

        if (isGrounded)
            airTimeCounter = 0f;
        else
            airTimeCounter += Time.fixedDeltaTime;

        if (!isGrounded && airTimeCounter > minAirTimeForGroundPound && downPressed && !isGroundPounding)
        {
            StartGroundPound();
        }

        HandleGroundPound();
        HandleWallSlide();
        ApplyMovement();
        HandleStatusEffect();

        if (isGrounded)
            isWallJumping = false;

        if (wallJumpTimer > 0)
            wallJumpTimer -= Time.fixedDeltaTime;
        else
            isWallJumping = false;

        if (flipLockTimer > 0)
            flipLockTimer -= Time.fixedDeltaTime;

        if (shootTimer > 0)
            shootTimer -= Time.fixedDeltaTime;

        downPressed = false;
       
    }

    // ---------------- INPUT ----------------

    public void OnMove(InputValue value)
    {
        if (!IsOwner) return;

        Vector2 previousInput = moveInput;
        moveInput = value.Get<Vector2>();

        if (previousInput.y >= -0.5f && moveInput.y < -0.5f)
            downPressed = true;
    }

    public void OnJump(InputValue value)
    {
        if (!IsOwner) return;

        if (value.isPressed)
        {
            jumpPressed = true;
            jumpHeld = true;
        }
        else
        {
            jumpHeld = false;
            
        }
    }

    public void OnShoot(InputValue value)
    {
        if (!IsOwner) return;

        if (value.isPressed)
            TryShoot();
    }

    void TryShoot()
    {
        if (currentSpell == SpellType.None) return;
        if (shootTimer > 0f) return;

        GameObject prefab = null;

        switch (currentSpell)
        {
            case SpellType.Fire: prefab = fireProjectilePrefab; break;
            case SpellType.Ice: prefab = iceProjectilePrefab; break;
            case SpellType.Poison: prefab = poisonProjectilePrefab; break;
        }

        if (prefab == null) return;

        GameObject projectile = Instantiate(prefab, firePoint.position, Quaternion.identity);
        float dir = facingRight ? 1f : -1f;
        projectile.GetComponent<Projectile>().Initialize(dir, gameObject);

        shootTimer = shootCooldown;
    }

    // ---------------- PHYSICS ----------------

    void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            isGroundPounding = false;
        }
        else
        {
            coyoteTimeCounter -= Time.fixedDeltaTime;
        }
    }

    void CheckWall()
    {
        isTouchingWall = Physics2D.Raycast(wallCheck.position, transform.right, wallCheckDistance, groundLayer);
    }

    // ---------------- MOVEMENT ----------------

    void ApplyMovement()
    {
        if (isGroundPounding) return;
        if (isWallJumping && wallJumpTimer > 0f) return;

        if (currentEffect == StatusEffectType.Ice)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float inputX = moveInput.x;

        if (currentEffect == StatusEffectType.Fire)
        {
            inputX = forcedMoveDirection;

            if (moveInput.x != 0)
                forcedMoveDirection = Mathf.Sign(moveInput.x);
        }

        if (currentEffect == StatusEffectType.Poison)
            inputX *= poisonSlowMultiplier;

        float targetSpeed = inputX * maxSpeed;
        float accel = isGrounded ? groundAcceleration : airAcceleration;

        float newVelocityX = Mathf.MoveTowards(
            rb.linearVelocity.x,
            targetSpeed,
            accel * Time.fixedDeltaTime
        );

        rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);

        if (inputX > 0 && !facingRight) Flip();
        else if (inputX < 0 && facingRight) Flip();

        // Wall jump
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

            Flip();
        }
        // Normal jump
        else if (jumpPressed && coyoteTimeCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            coyoteTimeCounter = 0f;
        }

        // ORIGINAL GOOD JUMP LOGIC (restored)
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

    void StartGroundPound()
    {
        isGroundPounding = true;
        groundPoundTimer = groundPoundDuration;
        rb.linearVelocity = new Vector2(0f, -groundPoundForce);
    }

    void HandleGroundPound()
    {
        if (!isGroundPounding) return;

        groundPoundTimer -= Time.fixedDeltaTime;
        rb.linearVelocity = new Vector2(0f, -groundPoundForce);

        if (isGrounded || groundPoundTimer <= 0f)
            isGroundPounding = false;
    }

    void HandleWallSlide()
    {
        bool pushingIntoWall =
            (isTouchingWall && moveInput.x > 0 && facingRight) ||
            (isTouchingWall && moveInput.x < 0 && !facingRight);

        if (pushingIntoWall && !isGrounded && rb.linearVelocity.y < 0)
        {
            isWallSliding = true;

            if (rb.linearVelocity.y < -wallSlideSpeed)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
        }
        else
        {
            isWallSliding = false;
        }
    }

    // ---------------- STATUS ----------------

    public void ApplyEffect(StatusEffectType effect)
    {
        currentEffect = effect;

        switch (effect)
        {
            case StatusEffectType.Ice:
                effectTimer = iceDuration;
                break;

            case StatusEffectType.Fire:
                effectTimer = fireDuration;
                forcedMoveDirection = facingRight ? 1f : -1f;
                break;

            case StatusEffectType.Poison:
                effectTimer = poisonDuration;
                break;
        }
    }

    void HandleStatusEffect()
    {
        if (currentEffect == StatusEffectType.None) return;

        effectTimer -= Time.fixedDeltaTime;

        if (effectTimer <= 0f)
            currentEffect = StatusEffectType.None;
    }

    public void LoseSpell()
    {
        currentSpell = SpellType.None;
    }

    public void SetSpell(SpellType newSpell)
    {
        currentSpell = newSpell;
    }
}