using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.InputSystem.DualShock;
using UnityEngine.InputSystem.XInput;
using System.Collections;

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
    MovingPlatform currentPlatform;
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

    [Header("Spike Damage")]
    [SerializeField] float spikeKnockbackForce = 12f;
    [SerializeField] float spikeHorizontalForce = 4f;
    [SerializeField] float spikeCooldown = 1f;
    bool canTakeSpikeDamage = true;
    bool isSpikeKnockback;

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



    //Haptic Stuff for Controller
    private Gamepad Gamepad;


    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public override void OnNetworkSpawn()
    {
        Debug.Log($"OwnerClientId: {OwnerClientId} | IsOwner: {IsOwner}");
        Debug.Log($"Player spawned → Owner: {OwnerClientId}");

        Debug.Log($"Player {OwnerClientId} | IsOwner={IsOwner} | BodyType={rb.bodyType}");

        Camera playerCam = GetComponentInChildren<Camera>();

        currentEffect.OnValueChanged += OnEffectChanged;


        
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
        for (int i = 0; i < gamepads.Count; i++)
        {
            Debug.Log(
                $"Gamepad {i}: {gamepads[i].displayName}"
            );
        }

#if UNITY_EDITOR
        int playerIndex = (int)OwnerClientId;

        if (playerIndex < gamepads.Count)
        {
            playerInput.SwitchCurrentControlScheme(
                gamepads[playerIndex]
            );

            Debug.Log(
                $"Player {OwnerClientId} assigned to: " +
                $"{gamepads[playerIndex].displayName}"
            );
        }
        else if (gamepads.Count > 0)
        {
            playerInput.SwitchCurrentControlScheme(
                gamepads[0]
            );

            Debug.Log(
                $"Player {OwnerClientId} fallback to: " +
                $"{gamepads[0].displayName}"
            );
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

        if (playerInput.user.pairedDevices.Count > 0)
        {
            Gamepad = playerInput.user.pairedDevices[0] as Gamepad;

            if (Gamepad != null)
            {
                Debug.Log(
                    $"Player {OwnerClientId} paired with {Gamepad.displayName}"
                );

                StartCoroutine(TestController());
            }
        }

        playerInput.ActivateInput();

        Debug.Log($"Player {OwnerClientId} Input Activated");

        moveAction = playerInput.actions["Move"];
        jumpAction = playerInput.actions["Jump"];
        shootAction = playerInput.actions["Shoot"];

        Debug.Log($"Player {OwnerClientId} Move Action Found: {moveAction != null}");

        Debug.Log($"Player {OwnerClientId} Jump Action Found: {jumpAction != null}");

        Debug.Log($"Player {OwnerClientId} Shoot Action Found: {shootAction != null}");

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

    IEnumerator TestController()
    {
        yield return new WaitForSeconds(2f);

        Debug.Log(
            $"Controller Connected: {Gamepad != null}"
        );
    }

    void Update()
    {
        if (!IsOwner) return;

        moveInput = moveAction.ReadValue<Vector2>();
        
        if (Gamepad != null)
        {
            if (Gamepad.buttonSouth.wasPressedThisFrame)
            {
                Debug.Log(
                    $"Player {OwnerClientId} received Gamepad buttonSouth"
                );
            }
        }

        bool walking = Mathf.Abs(moveInput.x) > 0.1f && isGrounded;
        
        if (anim == null) return;
        anim.SetBool("Walking", walking);

        if (jumpAction.WasPressedThisFrame())
        {
            jumpPressed = true;
            jumpHeld = true;

            anim.SetBool("Jumping", true); 
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

        if (!IsOwner) return;

        moveInput = moveAction.ReadValue<Vector2>();

        if (moveInput != Vector2.zero)
        {
            Debug.Log(
                $"Player {OwnerClientId} Input: {moveInput}"
            );
        }
    }

    void FixedUpdate()
    {
        if (IsServer)
        {
            HandleStatusEffect();
        }

        if (!IsOwner) return;

        if (currentPlatform != null)
        {
            Debug.Log($"Player {OwnerClientId} on platform. IsOwner={IsOwner}. LocalClientId={NetworkManager.Singleton.LocalClientId}");
            rb.position += (Vector2)currentPlatform.DeltaMovement;
        }

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

        float newVelocityX = Mathf.MoveTowards( rb.linearVelocity.x, targetSpeed, accel * Time.fixedDeltaTime);

        rb.linearVelocity = new Vector2(newVelocityX,rb.linearVelocity.y);

        if (!isWallJumping)
        {
            if (inputX > 0 && !facingRight)
                Flip();
            else if (inputX < 0 && facingRight)
                Flip();
        }

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

            PlaySFXLocal(jumpSFX);          
            PlaySFXClientRpc(0);            
        }

        if (!isSpikeKnockback)
        {
            if (rb.linearVelocity.y < 0)
            {
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (fallMultiplier - 1) * Time.fixedDeltaTime;
            }
            else if (rb.linearVelocity.y > 0 && !jumpHeld)
            {
                rb.linearVelocity += Vector2.up * Physics2D.gravity.y * (lowJumpMultiplier - 1) * Time.fixedDeltaTime;
            }
        }


        jumpPressed = false;
    }

    public void SetCurrentPlatform(MovingPlatform platform)
    {
        currentPlatform = platform;
    }

    public void ClearCurrentPlatform()
    {
        currentPlatform = null;
    }

    void OnEffectChanged(StatusEffectType oldEffect, StatusEffectType newEffect)
    {
        
        if (activeEffectVFX != null)
        {
            Destroy(activeEffectVFX);
            activeEffectVFX = null;
        }

        
        if (newEffect == StatusEffectType.Ice)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (newEffect == StatusEffectType.Fire)
        {
            forcedMoveDirection = facingRight ? 1f : -1f;
        }

        
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

    public void TakeSpikeHit(Vector2 knockbackDir)
    {
        Debug.Log("1");

        if (!IsServer) return;

        Debug.Log("2");

        if (!canTakeSpikeDamage)
            return;

        Debug.Log("3");

        DropGem();

        Debug.Log("4");

        LoseSpell();

        Debug.Log("5");

        isSpikeKnockback = true;

        Debug.Log("6");

        rb.linearVelocity = Vector2.zero;

        Debug.Log("7");

        rb.linearVelocity = knockbackDir.normalized * spikeKnockbackForce;

        Debug.Log("8");

        StartCoroutine(EndSpikeKnockback());

        StartCoroutine(SpikeCooldownRoutine());
    }

    IEnumerator SpikeCooldownRoutine()
    {
        canTakeSpikeDamage = false;

        yield return new WaitForSeconds(spikeCooldown);

        canTakeSpikeDamage = true;
    }

    IEnumerator EndSpikeKnockback()
    {
        yield return new WaitForSeconds(0.25f);

        isSpikeKnockback = false;
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
        bool pushingIntoWall = isTouchingWall && Mathf.Abs(moveInput.x) > 0.1f;

        if (pushingIntoWall && !isGrounded && rb.linearVelocity.y < 0)
        {
            isWallSliding = true;

            if (anim == null) return;
            anim.SetBool("Sliding", true); 

            if (rb.linearVelocity.y < -wallSlideSpeed)
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallSlideSpeed);
        }
        else
        {
            isWallSliding = false;

            if (anim == null) return;
            anim.SetBool("Sliding", false); 
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
        Vector2 direction =
            facingRight ? Vector2.right : Vector2.left;

        RaycastHit2D hit = Physics2D.Raycast(
            wallCheck.position,
            -direction,
            wallCheckDistance,
            groundLayer
        );

        isTouchingWall = hit.collider != null;

        Debug.DrawRay(
            wallCheck.position,
            direction * wallCheckDistance,
            isTouchingWall ? Color.green : Color.red
        );
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

                if(Gamepad is Gamepad gamepad) 
                {
                    if(gamepad is DualShockGamepad) 
                    {
                        StartCoroutine(HeavyDualShockRumble());
                    }

                    if(gamepad is DualSenseGamepad) 
                    {
                        StartCoroutine(HeavyDualSenseRumble());
                    }

                    if (gamepad is XInputController) 
                    {
                        StartCoroutine(HeavyXboxRumble());
                    }
                }

                break;

            case StatusEffectType.Fire:
                effectTimer = fireDuration;
                forcedMoveDirection = facingRight ? 1f : -1f;
               
                if (Gamepad is Gamepad gamepad1)
                {
                    if (gamepad1 is DualShockGamepad)
                    {
                        StartCoroutine(HeavyDualShockRumble());
                    }

                    if (gamepad1 is DualSenseGamepad)
                    {
                        StartCoroutine(HeavyDualSenseRumble());
                    }

                    if (gamepad1 is XInputController)
                    {
                        StartCoroutine(HeavyXboxRumble());
                    }
                }

                break;

            case StatusEffectType.Poison:
                effectTimer = poisonDuration;

               if (Gamepad is Gamepad gamepad2)
                {
                    if (gamepad2 is DualShockGamepad)
                    {
                        StartCoroutine(HeavyDualShockRumble());
                    }

                    if (gamepad2 is DualSenseGamepad)
                    {
                        StartCoroutine(HeavyDualSenseRumble());
                    }

                    if (gamepad2 is XInputController)
                    {
                        StartCoroutine(HeavyXboxRumble());
                    }
                }

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

        if (Gamepad is Gamepad gamepad)
        {
            Gamepad = gamepad;

            if (gamepad is DualShockGamepad)
            {
                StartCoroutine(DualShockRumble());
            }

            if (gamepad is DualSenseGamepad)
            {
                StartCoroutine(DualSenseRumble());
            }

            if (gamepad is XInputController)
            {
                StartCoroutine(XboxRumble());
            }
        }
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

    public IEnumerator DualShockRumble()
    {
        //Gamepad = Gamepad.current;
        Debug.Log(Gamepad.displayName);
        Gamepad.SetMotorSpeeds(0.5f, 0.5f);
        yield return new WaitForSeconds(0.5f);
        Gamepad.SetMotorSpeeds(0f, 0f);
    }

    public IEnumerator DualSenseRumble()
    {
        //Gamepad = Gamepad.current;
        Debug.Log(Gamepad.displayName);
        Gamepad.SetMotorSpeeds(0.3f, 0.3f);
        yield return new WaitForSeconds(0.5f);
        Gamepad.SetMotorSpeeds(0f, 0f);
    }

    public IEnumerator XboxRumble()
    {
        //Gamepad = Gamepad.current;
        Debug.Log(Gamepad.displayName);
        Gamepad.SetMotorSpeeds(0.6f, 0.6f);
        yield return new WaitForSeconds(0.5f);
        Gamepad.SetMotorSpeeds(0f, 0f);
    }

    public IEnumerator HeavyDualShockRumble()
    {
        //Gamepad = Gamepad.current;
        Debug.Log(Gamepad.displayName);
        Gamepad.SetMotorSpeeds(0.6f, 0.6f);
        yield return new WaitForSeconds(0.5f);
        Gamepad.SetMotorSpeeds(0f, 0f);
    }

    public IEnumerator HeavyDualSenseRumble()
    {
        //Gamepad = Gamepad.current;
        Debug.Log(Gamepad.displayName);
        Gamepad.SetMotorSpeeds(0.4f, 0.4f);
        yield return new WaitForSeconds(0.5f);
        Gamepad.SetMotorSpeeds(0f, 0f);
    }

    public IEnumerator HeavyXboxRumble()
    {
        //Gamepad = Gamepad.current;
        Debug.Log(Gamepad.displayName);
        Gamepad.SetMotorSpeeds(0.7f, 0.7f);
        yield return new WaitForSeconds(0.5f);
        Gamepad.SetMotorSpeeds(0f, 0f);
    }
}