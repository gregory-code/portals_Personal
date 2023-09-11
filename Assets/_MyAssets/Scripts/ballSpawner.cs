using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ballSpawner : MonoBehaviour
{
    private GameObject _ball;
    [SerializeField] private GameObject _ballPrefab;

    void Update()
    {
        if(_ball == null)
        {
            _ball = Instantiate(_ballPrefab, transform.position, transform.rotation);
        }
    }
}
