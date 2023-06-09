﻿namespace TurnTheGameOn.SimpleTrafficSystem
{
    using UnityEngine;

    [CreateAssetMenu(fileName = "AssetReferences", menuName = "TurnTheGameOn/STS/AssetReferences")]
    public class AssetReferences : ScriptableObject
    {
        public GameObject _AITrafficController;
        public GameObject _AIPeopleController;
        public GameObject _AITrafficController_StylizedVehiclesPack;
        public GameObject _AITrafficLightManager;
        public GameObject _AITrafficSpawnPoint;
        public GameObject _AITrafficStopManager;
        public GameObject _AITrafficWaypoint;
        public GameObject _AITrafficWaypointRoute;
        public GameObject _SplineRouteCreator;
        public GameObject _YieldTrigger;
        public GameObject _StopSign;
        public GameObject _TrafficLight_1;
        public GameObject _TrafficLight_2;
        public GameObject _TrafficLight_3;
        //Rebe:给行人添加一个路线工具
        public GameObject _AIPeopleWaypointRoute;
    }
}