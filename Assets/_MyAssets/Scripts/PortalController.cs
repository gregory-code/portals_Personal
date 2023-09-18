using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using RenderPipeline = UnityEngine.Rendering.RenderPipelineManager;

public class PortalController : MonoBehaviour
{
    [Header("Camera")]
    public Camera playerCam;
    public Camera portalCam;
    //public Quaternion TargetRotation;
    private Collider _originalCollider;
    private Collider _cashedCollider;

    [Header("Portals")]
    [SerializeField] private LayerMask _portalLayer;
    [SerializeField] private Portal[] _portals = new Portal[2];

    [SerializeField]
    private int _iterations = 7;

    public float sphereRadius = 1.5f;

    private RenderTexture _texture1;
    private RenderTexture _texture2;

    private Animator hand_Anim;
    [SerializeField] private Transform _spawnTransform;
    [SerializeField] private GameObject[] _portalProj;
    private RaycastHit _portalHit;
    private int _portalID;

    private int _burstDuration;
    [SerializeField] private float _velocityMultiplyer = 1.1f;
    [SerializeField] private int _burstAmount = 12; //in frames
    bool _bExitedHorizontalPortal;

    public float camY;

    PlayerControls _playerControls;

    private static readonly Quaternion _halfTurn = Quaternion.Euler(0.0f, 180.0f, 0.0f);

    private Portal _inPortal;
    private Portal _outPortal;

    private void Awake()
    {
        _playerControls = GetComponent<PlayerControls>();

        hand_Anim = playerCam.transform.Find("WhiteHand").GetComponent<Animator>();

        _texture1 = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        _texture2 = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
    }

    private void Start()
    {
        _portals[0].Renderer.material.mainTexture = _texture1;
        _portals[1].Renderer.material.mainTexture = _texture2;
    }

    private void OnEnable()
    {
        RenderPipeline.beginCameraRendering += UpdateCamera;
    }

    private void OnDisable()
    {
        RenderPipeline.beginCameraRendering -= UpdateCamera;
    }

    void UpdateCamera(ScriptableRenderContext SRC, Camera camera)
    {
        if (!_portals[0].bPlaced || !_portals[1].bPlaced)
        {
            return;
        }

        if (_portals[0].Renderer.isVisible)
        {
            portalCam.targetTexture = _texture1;
            for (int i = _iterations - 1; i >= 0; --i)
            {
                RenderCamera(_portals[0], _portals[1], i, SRC);
            }
        }

        if (_portals[1].Renderer.isVisible)
        {
            portalCam.targetTexture = _texture2;
            for (int i = _iterations - 1; i >= 0; --i)
            {
                RenderCamera(_portals[1], _portals[0], i, SRC);
            }
        }
    }

    private void RenderCamera(Portal inPortal, Portal outPortal, int iterationID, ScriptableRenderContext SRC)
    {
        Transform inTransform = inPortal.transform;
        Transform outTransform = outPortal.transform;

        Transform cameraTransform = portalCam.transform;
        cameraTransform.position = transform.position;
        cameraTransform.rotation = transform.rotation;

        for (int i = 0; i <= iterationID; ++i)
        {
            // Position the camera behind the other portal.
            Vector3 relativePos = inTransform.InverseTransformPoint(cameraTransform.position);
            relativePos = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativePos;
            cameraTransform.position = outTransform.TransformPoint(relativePos);

            portalCam.transform.Translate(0, camY, 0);

            // Rotate the camera to look through the other portal.
            Quaternion relativeRot = Quaternion.Inverse(inTransform.rotation) * cameraTransform.rotation;
            relativeRot = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeRot;
            cameraTransform.rotation = outTransform.rotation * relativeRot;
        }


        // Set the camera's oblique view frustum.
        Plane p = new Plane(-outTransform.forward, outTransform.position);
        Vector4 clipPlaneWorldSpace = new Vector4(p.normal.x, p.normal.y, p.normal.z, p.distance);
        Vector4 clipPlaneCameraSpace =
            Matrix4x4.Transpose(Matrix4x4.Inverse(portalCam.worldToCameraMatrix)) * clipPlaneWorldSpace;

        var newMatrix = playerCam.CalculateObliqueMatrix(clipPlaneCameraSpace);
        portalCam.projectionMatrix = newMatrix;

        // Render the camera to its render target.
        UniversalRenderPipeline.RenderSingleCamera(SRC, portalCam);
    }

    public void SetIsInPortal(Portal inPortal, Portal outPortal, Collider wallCollider)
    {
        this._inPortal = inPortal;
        this._outPortal = outPortal;

        _originalCollider = wallCollider;
        Physics.IgnoreCollision(GetComponent<CharacterController>(), wallCollider);
    }

    public void ExitPortal()
    {
        Physics.IgnoreCollision(GetComponent<CharacterController>(), _originalCollider, false);
        Physics.IgnoreCollision(GetComponent<CharacterController>(), _cashedCollider, false);
    }

    public virtual void Teleport()
    {
        StartCoroutine(launchTeleport());
    }

    public IEnumerator launchTeleport()
    {
        Vector3 currentVelocity = _playerControls.characterController.velocity;
        _playerControls.characterController.enabled = false;

        Transform inTransform = _inPortal.transform;
        Transform outTransform = _outPortal.transform;

        if (outTransform.localEulerAngles.x < 180 && outTransform.localEulerAngles.x > 60)
        {
            _bExitedHorizontalPortal = true;
        }

        Debug.Log("Out Transform: " + outTransform.localEulerAngles.x);

        inTransform.GetComponent<Portal>().SetCollisionEnabled(false);
        outTransform.GetComponent<Portal>().SetCollisionEnabled(false);

        _cashedCollider = outTransform.GetComponent<Portal>().getWall();
        Physics.IgnoreCollision(GetComponent<CharacterController>(), _cashedCollider);

        // teleport relative Position
        Vector3 relativePos = inTransform.InverseTransformPoint(transform.position);
        relativePos = _halfTurn * relativePos;
        transform.position = outTransform.TransformPoint(relativePos);

        // change camera rotation
        Quaternion relativeRot = Quaternion.Inverse(inTransform.rotation) * transform.rotation;
        relativeRot = _halfTurn * relativeRot;
        transform.rotation = outTransform.rotation * relativeRot; // rotates player camera
        _playerControls.TargetRotation = outTransform.rotation * relativeRot; // adjusts players perspective over time

        var tmp = _inPortal;
        _inPortal = _outPortal;
        _outPortal = tmp;

        yield return new WaitForEndOfFrame();
        _playerControls.characterController.enabled = true;

        _playerControls.currentSpeed *= _velocityMultiplyer;
        _burstDuration = _burstAmount;

        StartCoroutine(burstSpeed());
    }

    private IEnumerator burstSpeed()
    {
        if (!_playerControls.isGrounded) _playerControls.setGravity(0);

        yield return new WaitForEndOfFrame();

        _playerControls.TargetRotation.x = Mathf.Lerp(_playerControls.TargetRotation.x, 0, 9 * Time.deltaTime);
        _playerControls.TargetRotation.z = Mathf.Lerp(_playerControls.TargetRotation.z, 0, 9 * Time.deltaTime);

        if(_playerControls.isGrounded == false && _bExitedHorizontalPortal == true)
        {
            _playerControls.playerVelocity.y = _playerControls.currentSpeed;
            _playerControls.characterController.Move(_playerControls.playerVelocity * Time.deltaTime);
        }


        --_burstDuration;
        if(_burstDuration > 0)
        {
            StartCoroutine(burstSpeed());
        }
        else
        {
            _bExitedHorizontalPortal = false;
            _playerControls.resetGravity();
        }
    }

    public void firePortalProj()
    {
        GameObject proj = Instantiate(_portalProj[_portalID], _spawnTransform.position, _spawnTransform.rotation);
        proj.GetComponent<portalProj>().hit = _portalHit;
        proj.GetComponent<portalProj>().portalID = _portalID;
    }

    public void FirePortal(int portalID, float distance)
    {
        RaycastHit hit;
        Ray raycast = Camera.main.ScreenPointToRay(Input.mousePosition);
        Physics.Raycast(raycast, out hit, distance, _portalLayer);
        Debug.DrawLine(Camera.main.transform.position, hit.point, Color.red);

        if (hit.collider != null)
        {
            Collider[] nearbyColliders = Physics.OverlapSphere(hit.point, sphereRadius, LayerMask.GetMask("portalStatic"));
            foreach(Collider coll in nearbyColliders)
            {
                if(coll.CompareTag("Portal"))
                {
                    Debug.Log("Can't place a portal there!");
                    return;
                }
            }

            //Fire animation
            _portalHit = hit;
            this._portalID = portalID;

            int randomAnim = Random.Range(1, 4);
            hand_Anim.ResetTrigger("fire" + randomAnim);
            hand_Anim.SetTrigger("fire" + randomAnim);
        }
    }

    public void PlacePortal(RaycastHit hit, int portalID)
    {
        var cameraRotation = _playerControls.TargetRotation;
        var portalRight = cameraRotation * Vector3.right;

        if (Mathf.Abs(portalRight.x) >= Mathf.Abs(portalRight.z))
        {
            portalRight = (portalRight.x >= 0) ? Vector3.right : -Vector3.right;
        }
        else
        {
            portalRight = (portalRight.z >= 0) ? Vector3.forward : -Vector3.forward;
        }

        var portalForward = -hit.normal;
        var portalUp = -Vector3.Cross(portalRight, portalForward);

        var portalRotation = Quaternion.LookRotation(portalForward, portalUp);

        bool bPlaced = _portals[portalID].PlacePortal(hit.collider, hit.point, portalRotation);
        
        if (!bPlaced)
        {
            //Do a particle for when it fails
        }
    }
}
