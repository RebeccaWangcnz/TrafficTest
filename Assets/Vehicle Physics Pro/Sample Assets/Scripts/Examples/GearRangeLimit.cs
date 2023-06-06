//--------------------------------------------------------------
//      Vehicle Physics Pro: advanced vehicle physics kit
//          Copyright © 2011-2023 Angel Garcia "Edy"
//        http://vehiclephysics.com | @VehiclePhysics
//--------------------------------------------------------------

using UnityEngine;


namespace VehiclePhysics.Examples
{

public class GearRangeLimit : VehicleBehaviour
	{
	public int minAllowedGear = 0;
	public int maxAllowedGear = 0;


	public override void FixedUpdateVehicle ()
		{
		vehicle.data.Set(Channel.Settings, SettingsData.MinGearOverride, minAllowedGear);
		vehicle.data.Set(Channel.Settings, SettingsData.MaxGearOverride, maxAllowedGear);
		}
	}

}