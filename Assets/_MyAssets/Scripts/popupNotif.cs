using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class popupNotif : MonoBehaviour
{
    void Start()
    {
        StartCoroutine(notif());
    }

    IEnumerator notif()
    {


        yield return new WaitForSeconds(4);
        Destroy(this.gameObject);
    }

}
