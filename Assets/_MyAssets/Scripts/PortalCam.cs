using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Rendering.Universal;
using UnityEngine.Rendering;
using RenderPipeline = UnityEngine.Rendering.RenderPipelineManager;
using UnityEngine.Rendering.Universal;

public class PortalCam : MonoBehaviour
{
    [Header("Cam Settings")]
    [SerializeField] private int iterations = 7;

    private Camera portalCamera;

    private Portal[] portals = new Portal[2];
    private RenderTexture[] portalTextures = new RenderTexture[2];

    private Camera mainCam;

    private void Awake()
    {
        mainCam = GameObject.FindGameObjectWithTag("MainCamera").GetComponent<Camera>();

        portalTextures[0] = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        portalTextures[1] = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
    }

    void Start()
    {
        portals[0].Renderer.material.mainTexture = portalTextures[0];
        portals[1].Renderer.material.mainTexture = portalTextures[1];
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

        if (portals[0].Renderer.isVisible)
        {
            portalCamera.targetTexture = portalTextures[0];
            for(int i = iterations - 1; i >= 0; --i)
            {
                RenderCamera(portals[0], portals[1], i, SRC);
            }
        }

        if (portals[1].Renderer.isVisible)
        {
            portalCamera.targetTexture = portalTextures[1];
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

        Transform cameraTransform = portalCamera.transform;
        cameraTransform.position = transform.position;
        cameraTransform.rotation = transform.rotation;

        for (int i = 0; i <= iterationID; i++)
        {
            // Position the camera behind the other portal.
            Vector3 relativePos = inTransform.InverseTransformPoint(cameraTransform.position);
            relativePos = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativePos;
            cameraTransform.position = outTransform.TransformPoint(relativePos);

            // Rotate the camera to look through the other portal.
            Quaternion relativeRot = Quaternion.Inverse(inTransform.rotation) * cameraTransform.rotation;
            relativeRot = Quaternion.Euler(0.0f, 180.0f, 0.0f) * relativeRot;
            cameraTransform.rotation = outTransform.rotation * relativeRot;
        }

        // Set the camera's oblique view frustum.
        Plane p = new Plane(-outTransform.forward, outTransform.position);
        Vector4 clipPlaneWorldSpace = new Vector4(p.normal.x, p.normal.y, p.normal.z, p.distance);
        Vector4 clipPlaneCameraSpace =
            Matrix4x4.Transpose(Matrix4x4.Inverse(portalCamera.worldToCameraMatrix)) * clipPlaneWorldSpace;

        var newMatrix = mainCam.CalculateObliqueMatrix(clipPlaneCameraSpace);
        portalCamera.projectionMatrix = newMatrix;

        // Render the camera to its render target.
        UniversalRenderPipeline.RenderSingleCamera(SRC, portalCamera);
    }
}
