using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

public class PlayerController2D : NetworkBehaviour
{
    PlayerInput playerInput;
    InputAction moveAction;
    InputAction jumpAction;
    InputAction shootAction;

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
    NetworkVariable<bool> netFacingRight = new NetworkVariable<bool>(writePerm: NetworkVariableWritePermission.Owner);

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

    NetworkVariable<SpellType> currentSpell = new NetworkVariable<SpellType>(SpellType.None, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    float shootTimer;

    [Header("Status Effects")]
    [SerializeField] float iceDuration = 3f;
    [SerializeField] float fireDuration = 5f;
    [SerializeField] float poisonDuration = 3f;
    [SerializeField] float poisonSlowMultiplier = 0.4f;

    NetworkVariable<StatusEffectType> currentEffect = new NetworkVariable<StatusEffectType>(
        StatusEffectType.None,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    float effectTimer;
    float forcedMoveDirection;

    [Header("Coin Stuff")]
    NetworkVariable<int> coinCount = new NetworkVariable<int>(0,NetworkVariableReadPermission.Everyone,NetworkVariableWritePermission.Server);
    [SerializeField] GameObject[] powerUpPrefabs;

    [Header("Gem Stuff")]
    NetworkVariable<int> gemCount = new NetworkVariable<int>(
    0,
    NetworkVariableReadPermission.Everyone,
    NetworkVariableWritePermission.Server
);

    [Header("Hit Cooldown")]
    [SerializeField] float hitCooldownDuration = 2f;

    float hitCooldownTimer;
    bool canBeHit = true;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        Camera playerCam = GetComponentInChildren<Camera>();

        if (!IsOwner)
        {
            netFacingRight.OnValueChanged += OnFacingDirectionChanged;
            currentEffect.OnValueChanged += OnEffectChanged;
            

            facingRight = netFacingRight.Value;
            ApplyFlipVisual();

            // Apply effect immediately if already active
            OnEffectChanged(StatusEffectType.None, currentEffect.Value);

            if (playerCam != null)
                playerCam.gameObject.SetActive(false);

            var input = GetComponent<PlayerInput>();
            if (input != null)
                input.enabled = false;

            return;
        }

        playerInput = GetComponent<PlayerInput>();

        playerInput.neverAutoSwitchControlSchemes = true;
        playerInput.user.UnpairDevices();

        var gamepads = Gamepad.all;
        int playerIndex = (int)OwnerClientId;

        if (playerIndex < gamepads.Count)
        {
            InputUser.PerformPairingWithDevice(gamepads[playerIndex], playerInput.user);
        }

        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        shootAction = playerInput.actions["Shoot"];

        Camera mainCam = Camera.main;

        if (mainCam != null && mainCam != playerCam)
        {
            mainCam.gameObject.SetActive(false);
        }

        if (playerCam != null)
            playerCam.gameObject.SetActive(true);

        CameraFollow2D camFollow = GetComponentInChildren<CameraFollow2D>();
        if (camFollow != null)
            camFollow.target = transform;
    }

    void Update()
    {
        if (!IsOwner) return;

        moveInput = moveAction.ReadValue<Vector2>();

        if (jumpAction.WasPressedThisFrame())
        {
            jumpPressed = true;
            jumpHeld = true;
        }

        if (jumpAction.WasReleasedThisFrame())
        {
            jumpHeld = false;
        }

        if (shootAction.WasPressedThisFrame())
        {
            TryShoot();
        }

        if (moveInput.y < -0.5f)
        {
            downPressed = true;
        }
    }

    void FixedUpdate()
    {
        // ALWAYS run timer on server (for ALL players)
        if (IsServer)
        {
            HandleStatusEffect();
        }

        // Only movement is owner-only
        if (!IsOwner) return;

        if (Time.frameCount % 2 == 0)
        {
            CheckGround();
            CheckWall();
        }

        airTimeCounter = isGrounded ? 0f : airTimeCounter + Time.fixedDeltaTime;

        if (!isGrounded && airTimeCounter > minAirTimeForGroundPound && downPressed && !isGroundPounding)
            StartGroundPound();

        HandleGroundPound();
        HandleWallSlide();
        ApplyMovement();

        if (isGrounded) isWallJumping = false;

        wallJumpTimer -= Time.fixedDeltaTime;
        flipLockTimer -= Time.fixedDeltaTime;
        shootTimer -= Time.fixedDeltaTime;

        downPressed = false;
    }

    void ApplyMovement()
    {
        if (isGroundPounding) return;
        if (isWallJumping && wallJumpTimer > 0f) return;

        if (currentEffect.Value == StatusEffectType.Ice)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        float inputX = moveInput.x;

        if (currentEffect.Value == StatusEffectType.Fire)
        {
            // Ensure direction is always valid
            if (forcedMoveDirection == 0f)
            {
                forcedMoveDirection = facingRight ? 1f : -1f;
            }

            // Allow player to change direction
            if (moveInput.x != 0)
            {
                forcedMoveDirection = Mathf.Sign(moveInput.x);
            }

            inputX = forcedMoveDirection;
        }

        if (currentEffect.Value == StatusEffectType.Poison)
            inputX *= poisonSlowMultiplier;

        float targetSpeed = inputX * maxSpeed;
        float accel = isGrounded ? groundAcceleration : airAcceleration;

        float newVelocityX = Mathf.MoveTowards(rb.linearVelocity.x, targetSpeed, accel * Time.fixedDeltaTime);
        rb.linearVelocity = new Vector2(newVelocityX, rb.linearVelocity.y);

        if (inputX > 0 && !facingRight) Flip();
        else if (inputX < 0 && facingRight) Flip();

        if (jumpPressed && isWallSliding)
        {
            isWallJumping = true;
            wallJumpTimer = wallJumpControlDelay;

            wallJumpDirection = -transform.localScale.x;

            rb.linearVelocity = new Vector2(
                wallJumpDirection * wallJumpForce.x,
                wallJumpForce.y
            );

            Flip();
        }
        else if (jumpPressed && coyoteTimeCounter > 0f)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
            coyoteTimeCounter = 0f;
        }

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

    void OnEffectChanged(StatusEffectType oldEffect, StatusEffectType newEffect)
    {
        if (newEffect == StatusEffectType.Ice)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (newEffect == StatusEffectType.Fire)
        {
            forcedMoveDirection = facingRight ? 1f : -1f;
        }
    }

    void Flip()
    {
        facingRight = !facingRight;

        if (IsOwner)
        {
            netFacingRight.Value = facingRight;
        }

        ApplyFlipVisual();
    }

    void ApplyFlipVisual()
    {
        Vector3 scale = transform.localScale;
        scale.x = facingRight ? Mathf.Abs(scale.x) : -Mathf.Abs(scale.x);
        transform.localScale = scale;
    }

    void OnFacingDirectionChanged(bool previousValue, bool newValue)
    {
        facingRight = newValue;
        ApplyFlipVisual();
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

    public void ApplyEffect(StatusEffectType effect)
    {
        if (!IsServer) return;

        // block if on cooldown
        if (!canBeHit) return;

        currentEffect.Value = effect;

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

        // start cooldown AFTER effect ends
        canBeHit = false;
    }

    void HandleStatusEffect()
    {
        if (!IsServer) return;

        // effect running
        if (currentEffect.Value != StatusEffectType.None)
        {
            effectTimer -= Time.fixedDeltaTime;

            if (effectTimer <= 0f)
            {
                currentEffect.Value = StatusEffectType.None;

                // start cooldown AFTER effect ends
                hitCooldownTimer = hitCooldownDuration;
            }

            return;
        }

        // cooldown running
        if (!canBeHit)
        {
            hitCooldownTimer -= Time.fixedDeltaTime;

            if (hitCooldownTimer <= 0f)
            {
                canBeHit = true;
            }
        }
    }

    public void LoseSpell()
    {
        if (IsServer)
        {
            currentSpell.Value = SpellType.None;
        }
    }

    public void SetSpell(SpellType newSpell)
    {
        if (IsServer)
        {
            currentSpell.Value = newSpell;
        }
    }

    void TryShoot()
    {
        if (currentSpell.Value == SpellType.None) return;
        if (shootTimer > 0f) return;

        float dir = facingRight ? 1f : -1f;
        ShootServerRpc(dir, firePoint.position);

        shootTimer = shootCooldown;
    }

    [ServerRpc]
    void ShootServerRpc(float direction, Vector3 spawnPosition)
    {
        GameObject prefab = currentSpell.Value switch
        {
            SpellType.Fire => fireProjectilePrefab,
            SpellType.Ice => iceProjectilePrefab,
            SpellType.Poison => poisonProjectilePrefab,
            _ => null
        };

        if (prefab == null) return;

        Vector3 spawnPos = spawnPosition;
        spawnPos.x += direction * 0.5f;

        GameObject projectile = Instantiate(prefab, spawnPos, Quaternion.identity);

        projectile.GetComponent<NetworkObject>().Spawn();
        projectile.GetComponent<Projectile>().Initialize(direction, OwnerClientId);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void AddCoinRpc()
    {
        AddCoin();
    }

    public void AddCoin()
    {
        if (!IsServer) return;

        coinCount.Value++;

        Debug.Log($"ADD COIN CALLED → Player {OwnerClientId} = {coinCount.Value}");

        if (coinCount.Value >= 8)
        {
            coinCount.Value = 0;
            SpawnPowerUp();
        }
    }

    void SpawnPowerUp()
    {
        if (!IsServer) return;

        if (powerUpPrefabs.Length == 0)
        {
            Debug.LogWarning("No power-ups assigned!");
            return;
        }

        int index = Random.Range(0, powerUpPrefabs.Length);
        GameObject prefab = powerUpPrefabs[index];

        Vector3 spawnPos = transform.position + Vector3.up * 2f;

        GameObject power = Instantiate(prefab, spawnPos, Quaternion.identity);
        power.GetComponent<NetworkObject>().Spawn();
    }

    public int GetCoinCount()
    {
        return coinCount.Value;
    }

    public void AddGem()
    {
        if (!IsServer) return;

        gemCount.Value++;
    }

    public int GetGemCount()
    {
        return gemCount.Value;
    }

    public void DropGem()
    {
        if (!IsServer) return;

        if (gemCount.Value <= 0) return;

        // remove one gem from player
        gemCount.Value--;

        // spawn position slightly above player
        Vector3 dropPos = transform.position + Vector3.up;

        // create gem
        GameObject gem = Instantiate(GemManager.Instance.gemPrefab, dropPos, Quaternion.identity);

        // spawn on network
        var netObj = gem.GetComponent<NetworkObject>();
        netObj.Spawn();

        // initialize drop behaviour (launch + ignore owner)
        var gemScript = gem.GetComponent<Gem>();
        if (gemScript != null)
        {
            gemScript.InitializeDrop(OwnerClientId);
        }
    }

    public SpellType GetCurrentSpell()
    {
        return currentSpell.Value;
    }
}