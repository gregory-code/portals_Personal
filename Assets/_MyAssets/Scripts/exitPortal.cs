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

            myPortal.GetComponent<Portal>().SetCollisionEnabled(true);
            otherPortal.GetComponent<Portal>().SetCollisionEnabled(true);

            other.GetComponent<PortalController>().ExitPortal();
        }
    }
}
