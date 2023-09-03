using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using static UnityEditor.PlayerSettings;
using static UnityEngine.Rendering.DebugUI.Table;

public class portalProj : MonoBehaviour
{
    public RaycastHit hit;
    public int portalID;
    public float initialSpeed = 12f;
    public float acceleration = 60f;

    private bool bStop;

    private float currentSpeed = 12f;

    private void Start()
    {

        // Set the initial velocity based on the initial speed
        currentSpeed = initialSpeed;

    }

    private void Update()
    {
        if (bStop) return;

        if (Time.timeScale == 0) return;

        // Gradually increase the speed over time
        currentSpeed += acceleration * Time.deltaTime;

        // Calculate the direction towards the target
        Vector3 direction = (hit.point - transform.position).normalized;

        // Update the velocity with the new speed and direction
        GetComponent<Rigidbody>().velocity = direction * currentSpeed;

        if(Vector3.Distance(transform.position, hit.point) < 1)
        {
            GameObject.FindGameObjectWithTag("Player").GetComponent<PortalController>().PlacePortal(hit, portalID);
            StartCoroutine(placePortal());
        }
    }

    private IEnumerator placePortal()
    {
        bStop = true;

        PlayerControls PC = GameObject.FindGameObjectWithTag("Player").GetComponent<PlayerControls>();
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

        transform.position = hit.point;
        transform.rotation = portalRotation;
        transform.position -= transform.forward * 0.001f;


        yield return new WaitForSeconds(0.3f);
        Destroy(this.gameObject);
    }
}
