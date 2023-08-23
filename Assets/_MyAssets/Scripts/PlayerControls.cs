using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;

public class PlayerControls : MonoBehaviour
{
    PInputActions pInputActions;
    private CharacterController CC;

    [Header("Movement")]
    public float currentSpeed = 4;
    [SerializeField] private float walkSpeed = 4;
    public float sprintSpeed;
    [SerializeField] private float crouchSpeed;
    [SerializeField] private float speedCap;
    private float recoverySpeed;
    public float setSpeed;

    [Header("Camera")]
    public Camera playerCam;
    float xRotation = 0f;
    public float xSensitivity = 30f;
    public float ySensitivity = 30f;

    [Header("Player Jump")]
    [SerializeField] private float gravity = -5;
    [SerializeField] private float jumpHeight = 4;
    Vector3 playerVelocity;
    public bool isGrounded;

    bool bCrouching;

    bool bSprinting;

    private void Awake()
    {
        pInputActions = new PInputActions();
        pInputActions.Player.Enable();

        CC = GetComponent<CharacterController>();
    }

    void Start()
    {
        setSpeed = walkSpeed;
        recoverySpeed = 0.2f;
    }

    #region Updates
    void Update()
    {
        isGrounded = CC.isGrounded;
    }

    private void FixedUpdate()
    {
        Movement();
    }

    private void LateUpdate()
    {
        Look();
    }
    #endregion

    public void Movement()
    {
        Vector2 inputVector = pInputActions.Player.Movement.ReadValue<Vector2>();
        Vector3 movementDirection = new Vector3(inputVector.x, 0, inputVector.y);
        CC.Move(transform.TransformDirection(movementDirection) * currentSpeed * Time.deltaTime);

        playerVelocity.y += gravity * Time.deltaTime;
        if (isGrounded && playerVelocity.y < 0)
            playerVelocity.y = -2f;

        CC.Move(playerVelocity * Time.deltaTime);

        currentSpeed = Mathf.Lerp(currentSpeed, setSpeed, recoverySpeed);
        currentSpeed = Mathf.Clamp(currentSpeed, crouchSpeed, speedCap); // Sets the min and max speed

    }

    public void Look()
    {
        Vector2 lookVector = pInputActions.Player.Look.ReadValue<Vector2>();

        xRotation -= (lookVector.y * Time.deltaTime) * ySensitivity;
        xRotation = Mathf.Clamp(xRotation, -75, 75);

        playerCam.transform.localRotation = Quaternion.Euler(xRotation, 0, 0);
        transform.Rotate(Vector3.up * (lookVector.x * Time.deltaTime) * xSensitivity);

    }

    public void Jump(InputAction.CallbackContext context)
    {

        if (context.performed)
        {

            if (isGrounded)
            {
                playerVelocity.y = Mathf.Sqrt(jumpHeight * -3 * gravity);
            }
        }
    }

    public void Sprint(InputAction.CallbackContext context)
    {
        if (context.started && !bCrouching && currentSpeed < sprintSpeed)
        {
            setSpeed = sprintSpeed;
            bSprinting = true;
        }

        if (context.canceled)
        {
            setSpeed = walkSpeed;
            bSprinting = false;
        }
    }
}
