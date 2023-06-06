using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TurnTheGameOn.SimpleTrafficSystem;

public class ForceToChange : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        other.transform.gameObject.GetComponent<AITrafficCar>().SetForceLaneChange(true);
    }
}
