//--------------------------------------------------------------
//      Vehicle Physics Pro: advanced vehicle physics kit
//          Copyright © 2011-2023 Angel Garcia "Edy"
//        http://vehiclephysics.com | @VehiclePhysics
//--------------------------------------------------------------

// Advanced: This example hooks up the onBeforeIntegrationStep for ensuring its modifications
// reach the integration step bypassing all other components and scripts.
//
// It overrides the Engine's tcs variables for limiting the engine rpms to an arbitrary value.
// Optionally applies full load for forcing those rpms regardless of the input

using UnityEngine;


namespace VehiclePhysics.Examples
{

public class SetEngineRpm : VehicleBehaviour
	{
	public float rpm = 1000.0f;


	Engine m_engine;


	public override void OnEnableVehicle ()
		{
		m_engine = vehicle.GetInternalObject(typeof(Engine)) as Engine;
		if (m_engine == null)
			{
			enabled = false;
			return;
			}

		vehicle.onBeforeIntegrationStep += ApplyForcedRpms;
		}


	public override void OnDisableVehicle ()
		{
		vehicle.onBeforeIntegrationStep -= ApplyForcedRpms;
		m_engine.autoRpms = false;
		}


	void ApplyForcedRpms ()
		{
		m_engine.autoRpms = true;
		m_engine.targetRpms = rpm;
		}
	}
}