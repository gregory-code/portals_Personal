using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Unity.Rendering.Universal;
using UnityEngine.Rendering;
using RenderPipeline = UnityEngine.Rendering.RenderPipelineManager;

public class PortalCam : MonoBehaviour
{
    private Portal[] portals = new Portal[2];
    private RenderTexture[] portalTextures = new RenderTexture[2];

    private void Awake()
    {
        portalTextures[0] = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
        portalTextures[1] = new RenderTexture(Screen.width, Screen.height, 24, RenderTextureFormat.ARGB32);
    }

    void Start()
    {
        //portals[0].Renderer.material.mainTexture = portalTextures[0];
        //portals[1].Renderer.material.mainTexture = portalTextures[1];
    }

}
