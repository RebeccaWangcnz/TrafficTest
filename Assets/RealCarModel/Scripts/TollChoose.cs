using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TurnTheGameOn.SimpleTrafficSystem;

public class TollChoose : MonoBehaviour
{
    [SerializeField] private AITrafficController m_AITrafficController;
    private GameObject car;
    public GameObject[] tollpoints;
    public GameObject BigVehiclePoint;
    int x = 0;
    int[] counts = new int[4];
    void OnTriggerEnter(Collider other)
    {
        car = other.transform.gameObject;
        if (car.tag == "AITrafficCar")
        {
            if(car.GetComponent<AITrafficCar>().vehicleType!= AITrafficVehicleType.BigVehicle)
            {
                int j = 99;
                for (int i = 0; i < tollpoints.Length; i++)
                {
                    counts[i] = tollpoints[i].GetComponent<TollCache>().linelength;
                    //Debug.Log(i+"+"+counts[i]);
                    if (counts[i] < j)
                    {
                        j = counts[i];
                        x = i;
                    }
                }
                if (m_AITrafficController.EnabledNewPoint(car, tollpoints[x].transform))
                {
                    //this.GetComponent<AITrafficWaypoint>().onReachWaypointSettings.newRoutePoints[0]=tollpoints[x].GetComponent<AITrafficWaypoint>();//不能用这个方法，会导致route丢失
                    car.GetComponent<AITrafficCar>().ChangeToRouteWaypoint(tollpoints[x].GetComponent<AITrafficWaypoint>().onReachWaypointSettings);
                }
            }
            if (car.GetComponent<AITrafficCar>().vehicleType == AITrafficVehicleType.BigVehicle)
            {
                car.GetComponent<AITrafficCar>().ChangeToRouteWaypoint(BigVehiclePoint.GetComponent<AITrafficWaypoint>().onReachWaypointSettings);
            }
        }
    }
}
