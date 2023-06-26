﻿namespace TurnTheGameOn.SimpleTrafficSystem
{
    using UnityEngine;
    using System.Collections;

    public class AITrafficSpawnPoint : MonoBehaviour
    {
        public bool isRegistered { get; private set; }
        public bool isTrigger { get; private set; }
        public bool isVisible { get; private set; }
        public Transform transformCached { get; private set; }
        public AITrafficWaypoint waypoint;
        public Material runtimeMaterial;
        //Rebe：判断是否位于人行道上的出生点
        private bool isPeopleRoute;

        private void OnEnable()
        {
            GetComponent<MeshRenderer>().sharedMaterial = runtimeMaterial;
        }

        private void Awake()
        {
            transformCached = transform;
            isVisible = true;
            isPeopleRoute = GetComponentInParent<AITrafficWaypointRoute>().isPeopleRoute;
            StartCoroutine(RegisterSpawnPointCoroutine());
        }

        IEnumerator RegisterSpawnPointCoroutine()
        {
            if (isRegistered == false)
            {
                if (!isPeopleRoute)
                {
                    while (AITrafficController.Instance == null)
                    {
                        yield return new WaitForEndOfFrame();
                    }

                    AITrafficController.Instance.RegisterSpawnPoint(this);
                }
                else
                {
                    while (AIPeopleController.Instance == null)
                    {
                        yield return new WaitForEndOfFrame();
                    }
                    AIPeopleController.Instance.RegisterSpawnPoint(this);
                }
                isRegistered = true;
            }
        }

        void OnBecameInvisible()
        {
#if UNITY_EDITOR
            if (Camera.current != null)
            {
                if (Camera.current.name == "SceneCamera")
                    return;
            }
#endif
            isVisible = false;
        }

        void OnBecameVisible()
        {
#if UNITY_EDITOR
            if (Camera.current != null)
            {
                if (Camera.current.name == "SceneCamera")
                    return;
            }
#endif
            isVisible = true;
        }

        private void OnTriggerEnter(Collider other)
        {
            isTrigger = true;
        }

        private void OnTriggerStay(Collider other)
        {
            isTrigger = true;
        }

        private void OnTriggerExit(Collider other)
        {
            isTrigger = false;
        }

        public bool CanSpawn()
        {
            if (!isVisible && !isTrigger)
                return true;
            else
                return false;
        }

        public void RegisterSpawnPoint()
        {
            if (isRegistered == false)
            {
                AITrafficController.Instance.RegisterSpawnPoint(this);
                isRegistered = true;
                isTrigger = false;
            }
        }

        public void RemoveSpawnPoint()
        {
            if (isRegistered)
            {
                AITrafficController.Instance.RemoveSpawnPoint(this);
                isRegistered = false;
            }
        }
    }
}