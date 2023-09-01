using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.XR;
using static UnityEngine.ParticleSystem;
using RenderPipeline = UnityEngine.Rendering.RenderPipelineManager;

[RequireComponent(typeof(BoxCollider))]
public class Portal : MonoBehaviour
{
    [SerializeField] private LayerMask placementMask;
    public Portal OtherPortal;
    public Transform placement;
    public bool bPlaced = false;

    private List<portalObject> portalObjects = new List<portalObject>();
    private Collider wallCollider;

    private bool bPlayerTele;

    private new BoxCollider collider;

    private ParticleSystem cloudParticles;
    private Transform pulseEffect;

    private PlayerControls PC;
    private Animator portalAnim_Controller;

    public Renderer Renderer { get; private set; }

    private void Awake()
    {
        collider = GetComponent<BoxCollider>();
        Renderer = GetComponent<Renderer>();
        portalAnim_Controller = GetComponent<Animator>();

        cloudParticles = transform.Find("Smoke").GetComponent<ParticleSystem>();
        pulseEffect = transform.Find("pulse").GetComponent<Transform>();
    }

    private void Start()
    {
        PC = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerControls>();
        portalAnim_Controller.SetTrigger("Place");
        gameObject.SetActive(false);
    }

    private void Update()
    {
        Renderer.enabled = OtherPortal.bPlaced;

        if (!bPlaced || !OtherPortal.bPlaced) return;

        ItemTeleport();
        IsViewable();
        PlayerTeleport();
    }

    private void IsViewable()
    {
        // Check if Portal A is in view
        Vector3 viewportPosA = Camera.main.WorldToViewportPoint(transform.position);
        bool isPortalAInView = viewportPosA.x >= 0 && viewportPosA.x <= 1 && viewportPosA.y >= 0 && viewportPosA.y <= 1 && viewportPosA.z > 0;

        // Check if Portal B is in view
        Vector3 viewportPosB = Camera.main.WorldToViewportPoint(OtherPortal.transform.position);
        bool isPortalBInView = viewportPosB.x >= 0 && viewportPosB.x <= 1 && viewportPosB.y >= 0 && viewportPosB.y <= 1 && viewportPosB.z > 0;

        if (isPortalAInView && !isPortalBInView) // my portal is visable, other is not
        {
            pulseEffect.transform.localPosition = new Vector3(0, 0, -0.001f); //pushes pulse back so it cannot be seen through portal
            
            return;
        }
        else if (!isPortalAInView && isPortalBInView) // other portal is visable, mine is not
        {
            pulseEffect.transform.localPosition = new Vector3(0, 0, 0.001f); //pushes pulse forward so it's seen

            return;
        }
    }

    private void ItemTeleport()
    {
        for (int i = 0; i < portalObjects.Count; ++i)
        {
            Vector3 portalObj = transform.InverseTransformPoint(portalObjects[i].transform.position);

            if (portalObj.z > 0)
            {
                portalObjects[i].Teleport();
            }
        }
    }

    private void PlayerTeleport()
    {
        if (!bPlayerTele) return;

        PortalController playerCon = GameObject.FindGameObjectWithTag("Player").GetComponent<PortalController>();

        Vector3 player = transform.InverseTransformPoint(playerCon.transform.position);

        if (player.z > -0.5)
        {
            bPlayerTele = false;
            playerCon.GetComponent<PortalController>().Teleport();
        }
    }

    private void OnTriggerEnter(Collider other)
    {

        if (other.tag == "Player")
        {
            PortalController PC = other.GetComponent<PortalController>();
            Debug.Log("Player went in");
            bPlayerTele = true;
            PC.SetIsInPortal(this, OtherPortal, wallCollider);
        }

        var obj = other.GetComponent<portalObject>();
        if(obj != null)
        {
            Debug.Log("Object went in");
            portalObjects.Add(obj);
            obj.SetIsInPortal(this, OtherPortal, wallCollider);
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {
            PortalController PC = other.GetComponent<PortalController>();
            Debug.Log("Player went out");
            bPlayerTele = false;
            PC.ExitPortal(wallCollider);
        }

        var obj = other.GetComponent<portalObject>();

        if (portalObjects.Contains(obj))
        {
            Debug.Log("Object went out");
            portalObjects.Remove(obj);
            obj.ExitPortal(wallCollider);
        }
    }

    public void disapateSmoke()
    {
        ParticleSystem.MainModule cloud = cloudParticles.main;
        cloud.startSize = new ParticleSystem.MinMaxCurve(min: 0f, max: 0f);
    }

    public bool PlacePortal(Collider wallCollider, Vector3 pos, Quaternion rot)
    {

        placement.position = pos;
        placement.rotation = rot;
        placement.position -= placement.forward * 0.001f;

        HugCorner();
        FixIntersects();

        if (CheckOverlap())
        {

            this.wallCollider = wallCollider;
            transform.position = placement.position;
            transform.rotation = placement.rotation;

            gameObject.SetActive(true);
            bPlaced = true;

            ParticleSystem.MainModule cloud = cloudParticles.main;
            cloud.startSize = new ParticleSystem.MinMaxCurve(min: 0.1f, max: 0.5f);
            portalAnim_Controller.speed = (PC.currentSpeed / 4);
            portalAnim_Controller.ResetTrigger("Place");
            portalAnim_Controller.SetTrigger("Place");

            return true;
        }

        return false;
    }

    // Ensure the portal cannot extend past the edge of a surface.
    private void HugCorner()
    {
        var testPoints = new List<Vector3>
        {
            new Vector3(-1.1f,  0.0f, 0.1f),
            new Vector3( 1.1f,  0.0f, 0.1f),
            new Vector3( 0.0f, -2.1f, 0.1f),
            new Vector3( 0.0f,  2.1f, 0.1f)
        };

        var testDirs = new List<Vector3> { Vector3.right, -Vector3.right, Vector3.up, -Vector3.up };

        for (int i = 0; i < 4; ++i)
        {
            RaycastHit hit;
            Vector3 raycastPos = placement.TransformPoint(testPoints[i]);
            Vector3 raycastDir = placement.TransformDirection(testDirs[i]);

            if (Physics.CheckSphere(raycastPos, 0.05f, placementMask))
            {
                break;
            }
            else if (Physics.Raycast(raycastPos, raycastDir, out hit, 2, placementMask))
            {
                var offset = hit.point - raycastPos;
                placement.Translate(offset, Space.World);
            }
        }
    }

    // Ensure the portal cannot intersect a section of wall.
    private void FixIntersects()
    {
        var testDirs = new List<Vector3> { Vector3.right, -Vector3.right, Vector3.up, -Vector3.up };

        var testDists = new List<float> { 1.1f, 1.1f, 2.1f, 2.1f };

        for (int i = 0; i < 4; ++i)
        {
            RaycastHit hit;
            Vector3 raycastPos = placement.TransformPoint(0.0f, 0.0f, -0.1f);
            Vector3 raycastDir = placement.TransformDirection(testDirs[i]);

            if (Physics.Raycast(raycastPos, raycastDir, out hit, testDists[i], placementMask))
            {
                var offset = (hit.point - raycastPos);
                var newOffset = -raycastDir * (testDists[i] - offset.magnitude);
                placement.Translate(newOffset, Space.World);
            }
        }
    }

    // Once positioning has taken place, ensure the portal isn't intersecting anything.
    private bool CheckOverlap()
    {
        var checkExtents = new Vector3(0.9f, 1.9f, 0.05f);

        var checkPositions = new Vector3[]
        {
            placement.position + placement.TransformVector(new Vector3( 0.0f,  0.0f, -0.1f)),

            placement.position + placement.TransformVector(new Vector3(-1.0f, -2.0f, -0.1f)),
            placement.position + placement.TransformVector(new Vector3(-1.0f,  2.0f, -0.1f)),
            placement.position + placement.TransformVector(new Vector3( 1.0f, -2.0f, -0.1f)),
            placement.position + placement.TransformVector(new Vector3( 1.0f,  2.0f, -0.1f)),

            placement.TransformVector(new Vector3(0.0f, 0.0f, 0.2f))
        };

        // Ensure the portal does not intersect walls.
        var intersections = Physics.OverlapBox(checkPositions[0], checkExtents, placement.rotation, placementMask);

        if (intersections.Length > 1)
        {
            return false;
        }
        else if (intersections.Length == 1)
        {
            // We are allowed to intersect the old portal position.
            if (intersections[0] != collider)
            {
                return false;
            }
        }

        // Ensure the portal corners overlap a surface.
        bool bOverlap = true;

        for (int i = 1; i < checkPositions.Length - 1; ++i)
        {
            bOverlap &= Physics.Linecast(checkPositions[i], checkPositions[i] + checkPositions[checkPositions.Length - 1], placementMask);
        }

        return bOverlap;
    }

    public void RemovePortal()
    {
        gameObject.SetActive(false);
        bPlaced = false;
    }
}
