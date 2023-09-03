using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class exitPortal : MonoBehaviour
{
    private void OnTriggerExit(Collider other)
    {
        if (other.tag == "Player")
        {

            Debug.Log("Player exitted");
            Portal myPortal = transform.root.GetComponent<Portal>();
            Portal otherPortal = myPortal.OtherPortal;

            myPortal.bPlayerTele = false;

            myPortal.GetComponent<Portal>().collider1.enabled = true;
            myPortal.GetComponent<Portal>().collider2.enabled = true;
            otherPortal.GetComponent<Portal>().collider1.enabled = true;
            otherPortal.GetComponent<Portal>().collider2.enabled = true;

            other.GetComponent<PortalController>().ExitPortal();
        }
    }
}
