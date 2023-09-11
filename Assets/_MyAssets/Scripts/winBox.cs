using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider))]
public class winBox : MonoBehaviour
{
    [SerializeField] private GameObject[] _winningPlatforms;
    [SerializeField] private Vector3[] _Originalpos;
    private bool _bWon;

    public void Start()
    {
        for(int i = 0; i < _winningPlatforms.Length; i++)
        {
            _Originalpos[i] = _winningPlatforms[i].transform.position;
            _Originalpos[i].y += 100;
        }
    }

    public void Update()
    {
        if (_bWon == false) return;

        for (int i = 0; i < _winningPlatforms.Length; i++)
        {
            _winningPlatforms[i].transform.position = Vector3.Lerp(_winningPlatforms[i].transform.position, _Originalpos[i], 3 * Time.deltaTime);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.tag == "ball")
        {
            _bWon = true;
            Debug.Log("You win!");
        }
    }
}
