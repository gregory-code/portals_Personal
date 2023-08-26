using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using static UnityEditor.SceneView;

public class PortalController : MonoBehaviour
{
    [Header("Camera")]
    public Camera playerCam;
    public Quaternion TargetRotation;

    [Header("Portals")]
    [SerializeField] private LayerMask portalLayer;
    [SerializeField] private Portal[] portals = new Portal[2];

    PlayerControls PC;

    private void Awake()
    {
        PC = GetComponent<PlayerControls>();
    }

    private void Update()
    {
        var inputVector = PC.pInputActions.Player.Movement.ReadValue<Vector2>();
        var rotation = new Vector2(-Input.GetAxis("Mouse Y"), Input.GetAxis("Mouse X"));
        var targetEuler = TargetRotation.eulerAngles + (Vector3)rotation * 3;
        if (targetEuler.x > 180.0f)
        {
            targetEuler.x -= 360.0f;
        }
        targetEuler.x = Mathf.Clamp(targetEuler.x, -75.0f, 75.0f);
        TargetRotation = Quaternion.Euler(targetEuler);
    }

    public void FirePortal(int portalID, float distance)
    {
        RaycastHit hit;
        Ray raycast = Camera.main.ScreenPointToRay(Input.mousePosition);
        Physics.Raycast(raycast, out hit, distance, portalLayer);
        Debug.DrawLine(Camera.main.transform.position, hit.point, Color.red);

        if (hit.collider != null)
        {
            Debug.DrawRay(hit.point, hit.normal, Color.green);

            var portalForward = -hit.normal;
            var portalRight = Vector3.Cross(Vector3.up, portalForward).normalized;
            var portalUp = Vector3.Cross(portalForward, portalRight).normalized;

            var portalRotation = Quaternion.LookRotation(portalForward, portalUp);

            // Attempt to place the portal.
            bool wasPlaced = portals[portalID].PlacePortal(hit.collider, hit.point, portalRotation);
        }
    }
}
