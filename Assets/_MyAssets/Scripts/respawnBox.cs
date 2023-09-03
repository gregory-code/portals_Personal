using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class respawnBox : MonoBehaviour
{
    [SerializeField] private Transform respawnPoint;

    private void Awake()
    {
        respawnPoint = transform;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "Player")
        {
            other.GetComponent<PlayerControls>().respawnPoint = respawnPoint;

        }
    }
}
