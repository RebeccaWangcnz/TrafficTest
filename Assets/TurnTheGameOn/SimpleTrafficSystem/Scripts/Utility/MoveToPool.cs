using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TurnTheGameOn.SimpleTrafficSystem;

public class MoveToPool : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if(other.transform.gameObject.tag=="AITrafficCar")
        {
            other.transform.gameObject.GetComponent<AITrafficCar>().MoveCarToPool();
        }
    }
}
