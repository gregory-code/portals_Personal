using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using RenderPipeline = UnityEngine.Rendering.RenderPipelineManager;

[RequireComponent(typeof(BoxCollider))]
public class Portal : MonoBehaviour
{
    [SerializeField] private LayerMask placementMask;
    public Portal OtherPortal;
    public Transform placement;
    public bool bPlaced = false;
    public float hugFactor = 0.5f;

    public Camera portalCam;
    public Camera otherPortalCam;
    private Camera playerCam;

    public Material portalMat;

    public float xCamOffset;
    public float yCamOffset;
    public float zCamOffset;

    private List<portalObject> portalObjects = new List<portalObject>();
    private Collider wallCollider;

    //Components attached to the portal
    private new BoxCollider collider;

    private void Awake()
    {
        collider = GetComponent<BoxCollider>();
    }

    private void Start()
    {
        gameObject.SetActive(false);
        
        playerCam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

        if(otherPortalCam.targetTexture != null)
        {
            otherPortalCam.targetTexture.Release();
        }

        otherPortalCam.targetTexture = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        portalMat.mainTexture = otherPortalCam.targetTexture;
    }

    private void Update()
    {
        if (!bPlaced || !OtherPortal.bPlaced) return;

        for (int i = 0; i < portalObjects.Count; ++i)
        {
            Vector3 portalObj = transform.InverseTransformPoint(portalObjects[i].transform.position);

            Debug.Log("Portal Object Z is: " + portalObj.z);

            if(portalObj.z > 0)
            {
                portalObjects[i].Warp();
            }
        }
    }

    private void LateUpdate()
    {
        //movePortalCam();
        rotPortalCam();
    }

    private void movePortalCam()
    {
        Vector3 playerOffset = playerCam.transform.position - OtherPortal.transform.position;

        // Transform the player offset to be relative to the portal's local space
        Vector3 portalSpaceOffset = placement.InverseTransformDirection(playerOffset);

        portalCam.transform.position = placement.position + portalSpaceOffset;
    }

    private void rotPortalCam()
    {
        Transform pc = playerCam.transform;
        portalCam.transform.localRotation = Quaternion.Euler(-pc.localRotation.eulerAngles.x, pc.localRotation.eulerAngles.y, pc.localRotation.eulerAngles.z);
        portalCam.transform.Rotate(xCamOffset, yCamOffset, zCamOffset);
    }

    private void OnTriggerEnter(Collider other)
    {
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
        var obj = other.GetComponent<portalObject>();

        if (portalObjects.Contains(obj))
        {
            Debug.Log("Object went out");
            portalObjects.Remove(obj);
            obj.ExitPortal(wallCollider);
        }
    }

    public bool PlacePortal(Collider wallCollider, Vector3 pos, Quaternion rot)
    {
        placement.position = pos;
        placement.rotation = rot;
        placement.position -= placement.forward * 0.001f;

        HugCorners();
        this.wallCollider = wallCollider;

        //ResetPortalCam();

        transform.position = placement.position;
        transform.rotation = placement.rotation;

        transform.Rotate(-90f, 0f, 0f); //fixes final rotate

        Debug.Log("Placed portal set to true");
        gameObject.SetActive(true);
        bPlaced = true;
        return true;
    }

    // Ensure the portal cannot extend past the edge of a surface.
    private void HugCorners()
    {
        var testPoints = new List<Vector3>
        {
            new Vector3(-1.1f,  0.0f, 0.1f),
            new Vector3( 1.1f,  0.0f, 0.1f),
            new Vector3( 0.0f, -2.1f, 0.1f),
            new Vector3( 0.0f,  2.1f, 0.1f) 
        };

        var testDirs = new List<Vector3> { Vector3.right, -Vector3.right, Vector3.up, -Vector3.up };

        Vector3 portalPosition = placement.position;
        Quaternion portalRotation = placement.rotation;

        for (int i = 0; i < 4; ++i)
        {
            RaycastHit hit;
            Vector3 raycastPos = portalPosition + portalRotation * testPoints[i];
            Vector3 raycastDir = portalRotation * testDirs[i];

            if (Physics.Raycast(raycastPos, raycastDir, out hit, 2.1f, placementMask))
            {
                var offset = (hit.point - raycastPos) * hugFactor;
                placement.Translate(offset, Space.World);
            }
        }
    }

    public void RemovePortal()
    {
        gameObject.SetActive(false);
        bPlaced = false;
    }
}
