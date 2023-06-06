//--------------------------------------------------------------
//      Vehicle Physics Pro: advanced vehicle physics kit
//          Copyright © 2011-2023 Angel Garcia "Edy"
//        http://vehiclephysics.com | @VehiclePhysics
//--------------------------------------------------------------

using UnityEngine;


namespace VehiclePhysics.Utility
{

public class DifferentialSettings : VehicleBehaviour
	{
	public enum Mode { Default, Locked, Open }

	public Mode differential = Mode.Default;
	public Mode frontDifferential = Mode.Default;
	public Mode rearDifferential = Mode.Default;
	public Mode driveline = Mode.Default;


	public override void OnDisableVehicle ()
		{
		int[] settings = vehicle.data.Get(Channel.Settings);

		settings[SettingsData.DifferentialLock] = 0;
		settings[SettingsData.FrontDifferentialLock] = 0;
		settings[SettingsData.RearDifferentialLock] = 0;
		settings[SettingsData.DrivelineLock] = 0;
		}


	public override void UpdateVehicle ()
		{
		int[] settings = vehicle.data.Get(Channel.Settings);

		settings[SettingsData.DifferentialLock] = (int)differential;
		settings[SettingsData.FrontDifferentialLock] = (int)frontDifferential;
		settings[SettingsData.RearDifferentialLock] = (int)rearDifferential;
		settings[SettingsData.DrivelineLock] = (int)driveline;
		}
	}

}