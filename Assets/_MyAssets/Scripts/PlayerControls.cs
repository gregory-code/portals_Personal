using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;
using static UnityEditor.SceneView;

public class PlayerControls : MonoBehaviour
{
    public PInputActions pInputActions;
    public CharacterController CC;
    private PortalController PC;

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
    public float cameraSpeed = 3.0f;
    public Quaternion TargetRotation;

    [Header("Player Jump")]
    [SerializeField] private float cashedGravity = -5;
    private float gravity = -5;
    [SerializeField] private float jumpHeight = 4;
    Vector3 playerVelocity;
    public bool isGrounded;

    bool bCrouching;

    bool bSprinting;

    bool bHasLost;
    [SerializeField] private GameObject bloodLoseScreen;
    [SerializeField] private GameObject warningSign;
    public Sprite[] warningLibrary;

    [SerializeField] private GameObject menu;

    [SerializeField] public Transform respawnPoint;

    private void Awake()
    {

        pInputActions = new PInputActions();
        pInputActions.Player.Enable();
        
        PC = GetComponent<PortalController>();

        CC = GetComponent<CharacterController>();
    }

    void Start()
    {
        setSpeed = walkSpeed;
        recoverySpeed = 0.005f;

        Cursor.lockState = CursorLockMode.Locked;   // Locks the cursor to the center of the screen
        Cursor.visible = false;                     // Hides the cursor

        gravity = cashedGravity;

        respawnPoint = GameObject.Find("respawn1").GetComponent<Transform>();
    }

    #region Updates
    void Update()
    {
        if (Time.timeScale == 0) return;

        isGrounded = CC.isGrounded;

        if(transform.position.y < -5 && bHasLost == false)
        {
            Lose();
        }
    }

    private void FixedUpdate()
    {
        if (Time.timeScale == 0) return;

        Movement();
    }

    private void LateUpdate()
    {
        if (Time.timeScale == 0) return;

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
        var rotation = new Vector2(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"));

        var targetEuler = TargetRotation.eulerAngles + (Vector3)rotation * cameraSpeed;
        if (targetEuler.x > 180.0f)
        {
            targetEuler.x -= 360.0f;
        }
        targetEuler.x = Mathf.Clamp(targetEuler.x, -75.0f, 75.0f);

        TargetRotation.z = Mathf.Clamp(TargetRotation.z, -40, 40);

        TargetRotation = Quaternion.Euler(targetEuler);

        transform.rotation = Quaternion.Slerp(transform.rotation, TargetRotation, Time.deltaTime * 15.0f);
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
            PC.FirePortal(0, 250.0f);
        }
    }

    public void PortalRight(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            PC.FirePortal(1, 250.0f);
        }
    }

    public void Menu(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if(menu.activeInHierarchy == true)
            {
                Time.timeScale = 1;
                menu.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
            else
            {
                Time.timeScale = 0;
                menu.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
        }
    }

    public void setGravity(float newGravity)
    {
        gravity = newGravity;
    }

    public void resetGravity()
    {
        gravity = cashedGravity;
    }

    public void sensSlider()
    {
        cameraSpeed = GameObject.Find("sensitivity").GetComponent<Slider>().value;
    }

    public void retryButton()
    {
        StartCoroutine(resetPos());
    }

    private IEnumerator resetPos()
    {
        Time.timeScale = 1;
        menu.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        bHasLost = false;

        bloodLoseScreen.GetComponent<Image>().color = new Color(1, 1, 1, 0);
        bloodLoseScreen.transform.Find("terminal").GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, 0);

        foreach (Transform t in warningSign.transform)
        {
            t.localScale = Vector3.zero;
        }

        CC.enabled = false;
        transform.position = respawnPoint.position;
        TargetRotation = respawnPoint.rotation;
        yield return new WaitForEndOfFrame();

        CC.Move(respawnPoint.position);
        CC.enabled = true;
    }

    public void quitButton()
    {
        Debug.Log("quit  out");
        Application.Quit();
    }

    public void Lose()
    {
        Debug.Log("You've lost");
        bHasLost = true;

        StartCoroutine(warningSigns());
    }

    void Shuffle<T>(List<T> list)
    {
        int number = list.Count;
        for(int i = 0; i < number; i++)
        {
            int random = i + Random.Range(0, number - i);
            T temp = list[i];
            list[i] = list[random];
            list[random] = temp;
        }
    }

    private IEnumerator warningSigns()
    {
        List<Transform> warningList = new List<Transform>();
        foreach(Transform t in warningSign.transform)
        {
            warningList.Add(t);
        }

        Shuffle(warningList);

        foreach (Transform t in warningList)
        {
            if (bHasLost == true)
            {
                float transparency = bloodLoseScreen.GetComponent<Image>().color.a + 0.01f;
                bloodLoseScreen.GetComponent<Image>().color = new Color(1, 1, 1, transparency);
                bloodLoseScreen.transform.Find("terminal").GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, transparency);

                t.SetSiblingIndex(0);
                t.localScale = Vector3.zero;
                t.GetComponent<Image>().sprite = warningLibrary[Random.Range(0, warningLibrary.Length)];

                while (t.localScale.x < 2.2f && bHasLost == true)
                {
                    yield return new WaitForSeconds(0.01f);
                    t.localScale += new Vector3(0.8f, 0.8f, 0.8f);
                }

                if(bHasLost == false)
                {
                    t.localScale = Vector3.zero;
                }
            }
        }
    }
}
