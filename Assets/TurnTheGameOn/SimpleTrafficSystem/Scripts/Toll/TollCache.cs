using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TollCache : MonoBehaviour
{
    [HideInInspector]public int linelength=0;
    void OnTriggerEnter(Collider other)
    {
        linelength++;
    }
}
