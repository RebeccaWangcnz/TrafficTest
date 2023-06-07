namespace TurnTheGameOn.SimpleTrafficSystem
{
    using System.Collections;
    using System.Collections.Generic;
    using UnityEngine;

    public class AIWaypointRoute : MonoBehaviour
    {
        public bool isRegistered { get; protected set; }
        public List<CarAIWaypointInfo> waypointDataList;
    }
}
