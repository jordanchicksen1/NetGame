using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController2D : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] float moveSpeed = 8f;
    [SerializeField] float jumpForce = 14f;

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

        controls.Player.Jump.performed += OnJump;
    }

    void OnDisable()
    {
        controls.Player.Jump.performed -= OnJump;
        controls.Disable();
    }

    void Update()
    {
        HandleInput();
    }

    void FixedUpdate()
    {
        CheckGround();
        ApplyMovement();
    }

    // ---------------- INPUT ----------------
    void HandleInput()
    {
        moveInput = controls.Player.Move.ReadValue<Vector2>();
    }

    void OnJump(InputAction.CallbackContext context)
    {
        jumpPressed = true;
    }

    // ---------------- PHYSICS ----------------
    void CheckGround()
    {
        isGrounded = Physics2D.OverlapCircle(
            groundCheck.position,
            groundCheckRadius,
            groundLayer
        );
    }

    // ---------------- MOVEMENT ----------------
    void ApplyMovement()
    {
        rb.linearVelocity = new Vector2(moveInput.x * moveSpeed, rb.linearVelocity.y);

        if (jumpPressed && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        }

        jumpPressed = false;
    }
}