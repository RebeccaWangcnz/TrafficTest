//--------------------------------------------------------------
//      Vehicle Physics Pro: advanced vehicle physics kit
//          Copyright © 2011-2023 Angel Garcia "Edy"
//        http://vehiclephysics.com | @VehiclePhysics
//--------------------------------------------------------------

// Using this in VehicleBase requires its center of mass parameter to be null

using UnityEngine;
using EdyCommonTools;


namespace VehiclePhysics.Utility
{

public class VariableVehicleCargo : VehicleBehaviour
	{
	public float unloadedMass = 500.0f;
	public float loadedMass = 2000.0f;
	public Transform unloadedCOM;
	public Transform loadedCOM;
	[Range(0,1)]
	public float load = 0.0f;

	public bool showGizmo = true;


	Transform m_originalCOM;
	float m_originalMass;


	public override void OnEnableVehicle ()
		{
		m_originalCOM = vehicle.centerOfMass;
		m_originalMass = vehicle.cachedRigidbody.mass;
		vehicle.centerOfMass = null;
		}


	public override void OnDisableVehicle ()
		{
		vehicle.cachedRigidbody.mass = m_originalMass;
		vehicle.centerOfMass = m_originalCOM;
		}


	public override void FixedUpdateVehicle ()
		{
		vehicle.cachedRigidbody.mass = Mathf.Lerp(unloadedMass, loadedMass, load);

		if (unloadedCOM != null && loadedCOM != null)
			{
			Vector3 localUnloadedCOM = vehicle.cachedTransform.InverseTransformPoint(unloadedCOM.position);
			Vector3 localLoadedCOM = vehicle.cachedTransform.InverseTransformPoint(loadedCOM.position);

			SetCenterOfMass(Vector3.Lerp(localUnloadedCOM, localLoadedCOM, load));
			}
		else
		if (unloadedCOM != null)
			{
			SetCenterOfMass(vehicle.cachedTransform.InverseTransformPoint(unloadedCOM.position));
			}
		else
		if (loadedCOM != null)
			{
			SetCenterOfMass(vehicle.cachedTransform.InverseTransformPoint(loadedCOM.position));
			}
		}


	void SetCenterOfMass (Vector3 localCenterOfMass)
		{
		// Using a threshold ensures the center of mass to be modified when it has really changed.
		// The threshold is so small because we're comparing with sqrMagnitude (faster), not magnitude.

		if ((vehicle.cachedRigidbody.centerOfMass - localCenterOfMass).sqrMagnitude > 0.0000001f)
			vehicle.cachedRigidbody.centerOfMass = localCenterOfMass;
		}


	void Update ()
		{
		if (showGizmo)
			DebugUtility.DrawCrossMark(vehicle.cachedTransform.TransformPoint(vehicle.cachedRigidbody.centerOfMass), vehicle.cachedTransform, GColor.white);
		}
	}

}