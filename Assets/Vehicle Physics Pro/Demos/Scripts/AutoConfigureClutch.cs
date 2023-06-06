//--------------------------------------------------------------
//      Vehicle Physics Pro: advanced vehicle physics kit
//          Copyright © 2011-2023 Angel Garcia "Edy"
//        http://vehiclephysics.com | @VehiclePhysics
//--------------------------------------------------------------

using UnityEngine;


namespace VehiclePhysics.Demos
{

public class AutoConfigureClutch : VehicleBehaviour
	{
	public Engine.ClutchType defaultClutch = Engine.ClutchType.TorqueConverterLimited;
	public Engine.ClutchType deviceInputClutch = Engine.ClutchType.FrictionDisc;


	#if !VPP_ESSENTIAL

	Engine m_engine;
	VPDeviceInput m_deviceInput;


	public override void OnEnableVehicle ()
		{
		// Get the internal Engine block.
		// If no engine is present this component does nothing, so it disables itself.

		m_engine = vehicle.GetInternalObject(typeof(Engine)) as Engine;
		if (m_engine == null)
			{
			enabled = false;
			return;
			}

		// Get the device input component and the actual clutch type

		m_deviceInput = vehicle.GetComponentInChildren<VPDeviceInput>();
		}


	public override void UpdateVehicle ()
		{
		if (m_deviceInput != null && m_deviceInput.enabled)
			m_engine.clutchSettings.type = deviceInputClutch;
		else
			m_engine.clutchSettings.type = defaultClutch;
		}

	#endif
    }

}
