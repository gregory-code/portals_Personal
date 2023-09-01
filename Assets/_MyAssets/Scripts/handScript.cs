using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class handScript : MonoBehaviour
{
    public void fireProj()
    {
        Debug.Log("fired off proj!");
        GameObject.FindGameObjectWithTag("Player").GetComponent<PortalController>().firePortalProj();
    }
}
