using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class GetLicense : MonoBehaviour
{
    public LicenseType licensetype;
    public GameObject licenseF;
    public GameObject licenseB;
    [HideInInspector]
    public Material getmaterial;
    void Start()
    {
        licenseF.GetComponent<MeshRenderer>().material = getmaterial;
        licenseB.GetComponent<MeshRenderer>().material = getmaterial;
    }
}

