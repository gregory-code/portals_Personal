using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class popupNotif : MonoBehaviour
{

    private bool _hide;

    void Start()
    {
        transform.localPosition = new Vector2(-700, 645);
        _hide = false;
        StartCoroutine(notif());
    }

    private void Update()
    {

        Vector3 target = (_hide) ? new Vector3(-700, 645, 0) : new Vector3(-700, 400, 0);
        transform.localPosition = Vector3.Lerp(transform.localPosition, target, 3 * Time.deltaTime);
    }

    IEnumerator notif()
    {

        yield return new WaitForSeconds(4);
        _hide = true;
        yield return new WaitForSeconds(1);
        Destroy(this.gameObject);

    }

}
