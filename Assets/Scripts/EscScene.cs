using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;


public class EscScene : MonoBehaviour
{
    void OnTriggerEnter(Collider other)
    {
        if (other.tag == "DrivingCar")
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
    Application.Quit();
#endif
            Debug.Log("¼ì²âµ½car£¬ÓÎÏ·ÍË³ö");
        }
    }
}
