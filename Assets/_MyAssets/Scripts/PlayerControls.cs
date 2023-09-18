using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.UIElements.Experimental;

public class PlayerControls : MonoBehaviour
{
    public PInputActions pInputActions;
    public CharacterController characterController;
    private PortalController _portalController;

    // Player Prefs
    private float playerSens = 4;
    private int minutesHS;
    private float secondsHS;

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
    public Vector3 playerVelocity;
    public bool isGrounded;

    public Slider sensitivitySlider;

    bool bHasLost;
    [SerializeField] private GameObject bloodLoseScreen;
    [SerializeField] private GameObject warningSign;
    public Sprite[] warningLibrary;

    [SerializeField] private GameObject menu;

    [SerializeField] public Transform respawnPoint;

    [SerializeField] private TextMeshProUGUI _timerText;
    [SerializeField] private TextMeshProUGUI _minutesText;
    [SerializeField] private TextMeshProUGUI _highScore;
    private int _minutes;
    private float _timer;

    private bool bWon;
    [SerializeField] private GameObject _wonScreen;

    private void Awake()
    {
        playerSens = PlayerPrefs.GetFloat("playerSens");
        cameraSpeed = playerSens;
        sensitivitySlider.value = playerSens;

        secondsHS = PlayerPrefs.GetFloat("seconds");
        minutesHS = PlayerPrefs.GetInt("minutes");

        pInputActions = new PInputActions();
        pInputActions.Player.Enable();

        _portalController = GetComponent<PortalController>();

        characterController = GetComponent<CharacterController>();


        _timer = 0;
    }

    void Start()
    {
        setSpeed = walkSpeed;
        recoverySpeed = 0.009f;

        _highScore.text = "Highscore: Minutes - " + minutesHS + "  Seconds - " + string.Format("{0:0.00}", secondsHS);

        Cursor.lockState = CursorLockMode.Locked;   // Locks the cursor to the center of the screen
        Cursor.visible = false;                     // Hides the cursor

        gravity = cashedGravity;

        respawnPoint = GameObject.Find("respawn1").GetComponent<Transform>();
    }

    #region Updates
    void Update()
    {
        if (Time.timeScale == 0) return;

        _timerText.text = string.Format("{0:0.00}", _timer);

        isGrounded = characterController.isGrounded;

        if(bWon && Time.timeScale > 0)
        {
            _wonScreen.transform.localPosition = Vector3.Lerp(_wonScreen.transform.localPosition, Vector3.zero, Time.timeScale * 5);
            Time.timeScale -= 0.01f;
            if (Time.timeScale > 0.1f && Time.timeScale < 0.2f)
            {
                if(_minutes < minutesHS)
                {
                    newHighScore();
                }
                if(_minutes == minutesHS && _timer < secondsHS)
                {
                    newHighScore();
                }
                Time.timeScale = 0;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            return;
        }

        _timer += Time.deltaTime;
        if (_timer > 60)
        {
            _timer = 0;
            ++_minutes;
            _minutesText.text = "Minutes: " + _minutes;
        }

        if (transform.position.y < -5 && bHasLost == false)
        {
            Lose();
        }
    }

    private void newHighScore()
    {
        PlayerPrefs.SetFloat("seconds", _timer);
        PlayerPrefs.SetInt("minutes", _minutes);
        _highScore.text = "Highscore: Minutes - " + minutesHS + "  Seconds - " + string.Format("{0:0.00}", secondsHS);
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

    public void runItBack()
    {
        SceneManager.LoadScene(2);
        Time.timeScale = 1;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        menu.SetActive(false);
    }

    public void WonGame()
    {
        bWon = true;
    }

    public void Movement()
    {
        Vector2 inputVector = pInputActions.Player.Movement.ReadValue<Vector2>();
        Vector3 movementDirection = new Vector3(inputVector.x, 0, inputVector.y);
        characterController.Move(transform.TransformDirection(movementDirection) * currentSpeed * Time.deltaTime);

        playerVelocity.y += gravity * Time.deltaTime;
        if (isGrounded && playerVelocity.y < 0)
            playerVelocity.y = -2f;

        characterController.Move(playerVelocity * Time.deltaTime);

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

    public void PortalLeft(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if(bHasLost == true)
            {
                StartCoroutine(resetPos());
            }
            else
            {
                _portalController.FirePortal(0, 75.0f);
            }
        }
    }

    public void PortalRight(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (bHasLost == true)
            {
                StartCoroutine(resetPos());
            }
            else
            {
                _portalController.FirePortal(1, 75.0f);
            }
        }
    }

    public void Menu(InputAction.CallbackContext context)
    {
        if (bWon == true) return;

        if (context.performed)
        {
            if(menu.activeInHierarchy == true)
            {
                Time.timeScale = 1;
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
                menu.SetActive(false);
            }
            else
            {
                Time.timeScale = 0;
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                menu.SetActive(true);
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

    public float getGravity()
    {
        return gravity;
    }

    public void sensSlider()
    {
        cameraSpeed = sensitivitySlider.value;
        PlayerPrefs.SetFloat("playerSens", cameraSpeed);
    }

    public void home()
    {
        SceneManager.LoadScene(0);
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

        GameObject[] balls = GameObject.FindGameObjectsWithTag("ball");
        foreach(GameObject b in balls)
        {
            Destroy(b);
        }

        foreach (Transform t in warningSign.transform)
        {
            t.localScale = Vector3.zero;
        }

        characterController.enabled = false;
        transform.position = respawnPoint.position;
        TargetRotation = respawnPoint.rotation;
        yield return new WaitForEndOfFrame();

        characterController.Move(respawnPoint.position);
        characterController.enabled = true;
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
                float transparency = bloodLoseScreen.GetComponent<Image>().color.a + 0.05f;
                bloodLoseScreen.GetComponent<Image>().color = new Color(1, 1, 1, transparency);
                bloodLoseScreen.transform.Find("terminal").GetComponent<TextMeshProUGUI>().color = new Color(1, 1, 1, transparency);

                t.SetSiblingIndex(0);
                t.localScale = Vector3.zero;
                t.GetComponent<Image>().sprite = warningLibrary[Random.Range(0, warningLibrary.Length)];

                while (t.localScale.x < 3.5f && bHasLost == true)
                {
                    yield return new WaitForSeconds(0.01f);
                    t.localScale += new Vector3(0.9f, 0.9f, 0.9f);
                }

                if(bHasLost == false)
                {
                    t.localScale = Vector3.zero;
                }
            }
        }
    }
}
