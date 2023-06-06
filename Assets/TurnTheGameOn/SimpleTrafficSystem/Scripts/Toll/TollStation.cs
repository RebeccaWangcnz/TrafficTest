using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TurnTheGameOn.SimpleTrafficSystem;

public class TollStation : MonoBehaviour
{
    public TollCache tollcache;
    void OnTriggerExit(Collider other)
    {
        tollcache.linelength--;
    }
}
