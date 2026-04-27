using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
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
    NetworkVariable<int> coinCount = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
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

    [Header("Animation Stuff")]
    [SerializeField] GameObject hostVisual;
    [SerializeField] GameObject clientVisual;

    Animator anim;

    [Header("VFX")]
    [SerializeField] GameObject fireEffect;
    [SerializeField] GameObject iceEffect;
    [SerializeField] GameObject poisonEffect;

    GameObject activeEffectVFX;

    [Header("SFX")]
    [SerializeField] AudioSource audioSource;

    [SerializeField] AudioClip jumpSFX;
    [SerializeField] AudioClip coinSFX;
    [SerializeField] AudioClip powerUpSFX;
    [SerializeField] AudioClip gemSFX;
    [SerializeField] AudioClip hitSFX;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"Player spawned → Owner: {OwnerClientId}");

        Camera playerCam = GetComponentInChildren<Camera>();

        currentEffect.OnValueChanged += OnEffectChanged;


        // Animation + visual setup
        if (OwnerClientId == NetworkManager.ServerClientId)
        {
            hostVisual.SetActive(true);
            clientVisual.SetActive(false);

            anim = hostVisual.GetComponent<Animator>();
        }
        else
        {
            hostVisual.SetActive(false);
            clientVisual.SetActive(true);

            anim = clientVisual.GetComponent<Animator>();
        }

        // SAFETY CHECK (VERY IMPORTANT)
        if (anim == null)
        {
            Debug.LogError("Animator NOT FOUND on visual!");
        }

        if (!IsOwner)
        {
            netFacingRight.OnValueChanged += OnFacingDirectionChanged;
            //currentEffect.OnValueChanged += OnEffectChanged;

            facingRight = netFacingRight.Value;
            ApplyFlipVisual();

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

#if UNITY_EDITOR
        int playerIndex = (int)OwnerClientId;

        if (playerIndex < gamepads.Count)
        {
            InputUser.PerformPairingWithDevice(gamepads[playerIndex], playerInput.user);
        }
        else if (gamepads.Count > 0)
        {
            InputUser.PerformPairingWithDevice(gamepads[0], playerInput.user);
        }
#else
        if (gamepads.Count > 0)
        {
            InputUser.PerformPairingWithDevice(gamepads[0], playerInput.user);
        }
        else if (Keyboard.current != null)
        {
            InputUser.PerformPairingWithDevice(Keyboard.current, playerInput.user);
        }
#endif

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

        bool walking = Mathf.Abs(moveInput.x) > 0.1f && isGrounded;
        
        if (anim == null) return;
        anim.SetBool("Walking", walking);

        if (jumpAction.WasPressedThisFrame())
        {
            jumpPressed = true;
            jumpHeld = true;

            anim.SetBool("Jumping", true); //  trigger jump immediately
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
        if (IsServer)
        {
            HandleStatusEffect();
        }

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

        UpdateAnimations();

        if (isGrounded) isWallJumping = false;

        wallJumpTimer -= Time.fixedDeltaTime;
        flipLockTimer -= Time.fixedDeltaTime;
        shootTimer -= Time.fixedDeltaTime;

        downPressed = false;
    }

    void UpdateAnimations()
    {
        if (!IsOwner || anim == null) return;

        bool walking = Mathf.Abs(rb.linearVelocity.x) > 0.1f && isGrounded;
        bool jumping = !isGrounded && rb.linearVelocity.y > 0.1f;
        bool sliding = isWallSliding;

        anim.SetBool("Walking", walking);
        anim.SetBool("Jumping", jumping);
        anim.SetBool("Sliding", sliding);

        
    }

    void PlaySFXLocal(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    [ClientRpc]
    void PlaySFXClientRpc(int soundId)
    {
        switch (soundId)
        {
            case 0: PlaySFXLocal(jumpSFX); break;
            case 1: PlaySFXLocal(coinSFX); break;
            case 2: PlaySFXLocal(powerUpSFX); break;
            case 3: PlaySFXLocal(gemSFX); break;
            case 4: PlaySFXLocal(hitSFX); break;
        }
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
            if (forcedMoveDirection == 0f)
            {
                forcedMoveDirection = facingRight ? 1f : -1f;
            }

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

            PlaySFXLocal(jumpSFX);          // instant feedback
            PlaySFXClientRpc(0);            // everyone hears it
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
        // Remove old VFX
        if (activeEffectVFX != null)
        {
            Destroy(activeEffectVFX);
            activeEffectVFX = null;
        }

        // Apply gameplay logic (keep your existing stuff)
        if (newEffect == StatusEffectType.Ice)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (newEffect == StatusEffectType.Fire)
        {
            forcedMoveDirection = facingRight ? 1f : -1f;
        }

        // Spawn new VFX
        GameObject prefab = null;

        switch (newEffect)
        {
            case StatusEffectType.Fire:
                prefab = fireEffect;
                break;

            case StatusEffectType.Ice:
                prefab = iceEffect;
                break;

            case StatusEffectType.Poison:
                prefab = poisonEffect;
                break;
        }

        if (prefab != null)
        {
            activeEffectVFX = Instantiate(prefab, transform);
            activeEffectVFX.transform.localPosition = Vector3.zero;
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

            if (anim == null) return;
            anim.SetBool("Sliding", true); // HERE

            if (rb.linearVelocity.y < -wallSlideSpeed)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
        }
        else
        {
            isWallSliding = false;

            if (anim == null) return;
            anim.SetBool("Sliding", false); //  HERE
        }
    }

    void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundCheckRadius, groundLayer);

        if (isGrounded)
        {
            coyoteTimeCounter = coyoteTime;
            isGroundPounding = false;

            //  IMPORTANT
            if (anim == null) return;
            anim.SetBool("Jumping", false);
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

        if (!canBeHit) return;

        currentEffect.Value = effect;
        PlaySFXClientRpc(4);

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

        canBeHit = false;
    }

    void HandleStatusEffect()
    {
        if (!IsServer) return;

        if (currentEffect.Value != StatusEffectType.None)
        {
            effectTimer -= Time.fixedDeltaTime;

            if (effectTimer <= 0f)
            {
                currentEffect.Value = StatusEffectType.None;
                hitCooldownTimer = hitCooldownDuration;
            }

            return;
        }

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
        PlaySFXClientRpc(1);

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
        PlaySFXClientRpc(2);
    }

    public int GetCoinCount()
    {
        return coinCount.Value;
    }

    public void AddGem()
    {
        if (!IsServer) return;

        gemCount.Value++;
        PlaySFXClientRpc(3);
    }

    public int GetGemCount()
    {
        return gemCount.Value;
    }

    public void DropGem()
    {
        if (!IsServer) return;

        if (gemCount.Value <= 0) return;

        gemCount.Value--;

        Vector3 dropPos = transform.position + Vector3.up;

        GameObject gem = Instantiate(GemManager.Instance.gemPrefab, dropPos, Quaternion.identity);

        var netObj = gem.GetComponent<NetworkObject>();
        netObj.Spawn();

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