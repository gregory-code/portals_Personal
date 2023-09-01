using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using static UnityEditor.SceneView;
using RenderPipeline = UnityEngine.Rendering.RenderPipelineManager;

public class PortalController : MonoBehaviour
{
    [Header("Camera")]
    public Camera playerCam;
    public Camera portalCam;
    //public Quaternion TargetRotation;

    [Header("Portals")]
    [SerializeField] private LayerMask portalLayer;
    [SerializeField] private Portal[] portals = new Portal[2];

    [SerializeField]
    private int iterations = 7;

    public float sphereRadius = 1.5f;

    private RenderTexture texture1;
    private RenderTexture texture2;

    private Animator hand_Anim;
    [SerializeField] private Transform spawnTransform;
    private RaycastHit portalHit;
    private int portalID;
    [SerializeField] private GameObject[] portalProj;

    public float camY;

    PlayerControls PC;

    private static readonly Quaternion halfTurn = Quaternion.Euler(0.0f, 180.0f, 0.0f);

    private int inPortalCount = 0;

    private Portal inPortal;
    private Portal outPortal;

    private void Awake()
    {
        PC = GetComponent<PlayerControls>();

        hand_Anim = playerCam.transform.Find("WhiteHand").GetComponent<Animator>();

        texture1 = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        texture2 = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
    }

    private void Start()
    {
        portals[0].Renderer.material.mainTexture = texture1;
        portals[1].Renderer.material.mainTexture = texture2;
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
        if (!portals[0].bPlaced || !portals[1].bPlaced)
        {
            return;
        }

        if(portals[0].Renderer.isVisible)
        {
            portalCam.targetTexture = texture1;
            for (int i = iterations - 1; i >= 0; --i)
            {
                RenderCamera(portals[0], portals[1], i, SRC);
            }
        }

        if (portals[1].Renderer.isVisible)
        {
            portalCam.targetTexture = texture2;
            for (int i = iterations - 1; i >= 0; --i)
            {
                RenderCamera(portals[1], portals[0], i, SRC);
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
        this.inPortal = inPortal;
        this.outPortal = outPortal;

        Physics.IgnoreCollision(GetComponent<CharacterController>(), wallCollider);

        ++inPortalCount;
    }

    public void ExitPortal(Collider wallCollider)
    {
        Physics.IgnoreCollision(GetComponent<CharacterController>(), wallCollider, false);
        --inPortalCount;
    }

    public virtual void Teleport()
    {
        Transform inTransform = inPortal.transform;
        Transform outTransform = outPortal.transform;

        Debug.Log("Tried teleporting the player");


        

        // teleport relative Position
        /*Vector3 relativePos = inTransform.InverseTransformPoint(transform.position);
        relativePos = halfTurn * relativePos;
        transform.position = outTransform.TransformPoint(relativePos);
        transform.position += -transform.forward * 1f;

        // change camera rotation
        transform.rotation = outTransform.rotation;
        PC.TargetRotation = transform.rotation;*/



        // Swap portal references.
        var tmp = inPortal;
        inPortal = outPortal;
        outPortal = tmp;
    }

    public void firePortalProj()
    {
        GameObject proj = Instantiate(portalProj[portalID], spawnTransform.position, spawnTransform.rotation);
        proj.GetComponent<portalProj>().hit = portalHit;
        proj.GetComponent<portalProj>().portalID = portalID;
    }

    public void FirePortal(int portalID, float distance)
    {
        RaycastHit hit;
        Ray raycast = Camera.main.ScreenPointToRay(Input.mousePosition);
        Physics.Raycast(raycast, out hit, distance, portalLayer);
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
            portalHit = hit;
            this.portalID = portalID;
            hand_Anim.ResetTrigger("fire");
            hand_Anim.SetTrigger("fire");
        }
    }

    public void PlacePortal(RaycastHit hit, int portalID)
    {
        // Orient the portal according to camera look direction and surface direction.
        var cameraRotation = PC.TargetRotation;
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


        // This places the portal and returns a bool if it worked or not
        bool bPlaced = portals[portalID].PlacePortal(hit.collider, hit.point, portalRotation);

        if (!bPlaced)
        {
            //Do a particle for when it fails
        }
    }
}
