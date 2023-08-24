using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using static UnityEditor.SceneView;

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

    [Header("Portals")]
    [SerializeField] private LayerMask portalLayer;
    [SerializeField] private Portal[] portals = new Portal[2];

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

    public void PortalLeft(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            FirePortal(0, transform.position, transform.forward, 250.0f);
        }
    }

    public void PortalRight(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            FirePortal(1, transform.position, transform.forward, 250.0f);
        }
    }

    private void FirePortal(int portalID, Vector3 pos, Vector3 dir, float distance)
    {
        RaycastHit hit;
        Physics.Raycast(pos, dir, out hit, distance/*, portalLayer*/); // thinking this will prevent other features

        if (hit.collider != null)
        {
            Debug.Log("Hit a surface");
            // If we shoot a portal, recursively fire through the portal.
            if (hit.collider.tag == "Portal")
            {

                var inPortal = hit.collider.GetComponent<Portal>();

                if (inPortal == null)
                {
                    return;
                }

                var outPortal = inPortal.OtherPortal;

                // Update position of raycast origin with small offset.
                Vector3 relativePos = inPortal.transform.InverseTransformPoint(hit.point + dir);
                relativePos = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativePos;
                pos = outPortal.transform.TransformPoint(relativePos);

                // Update direction of raycast.
                Vector3 relativeDir = inPortal.transform.InverseTransformDirection(dir);
                relativeDir = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeDir;
                dir = outPortal.transform.TransformDirection(relativeDir);

                distance -= Vector3.Distance(pos, hit.point);

                FirePortal(portalID, pos, dir, distance);

                return;
            }

            // Orient the portal according to camera look direction and surface direction.
            /*var cameraRotation = ;
            var portalRight = cameraRotation * Vector3.right;

            if (Mathf.Abs(portalRight.x) >= Mathf.Abs(portalRight.z))
            {
                portalRight = (portalRight.x >= 0) ? Vector3.right : -Vector3.right;
            }
            else
            {
                portalRight = (portalRight.z >= 0) ? Vector3.forward : -Vector3.forward;
            }*/

            var portalRight = playerCam.transform.rotation * Vector3.right;
            var portalForward = -hit.normal;
            var portalUp = -Vector3.Cross(portalRight, portalForward);

            var portalRotation = Quaternion.LookRotation(portalForward, portalUp);

            // Attempt to place the portal.
            Debug.Log("trying to place");
            bool wasPlaced = portals[portalID].PlacePortal(hit.collider, hit.point, portalRotation);
        }
    }
}
