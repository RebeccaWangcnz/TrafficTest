//--------------------------------------------------------------
//      Vehicle Physics Pro: advanced vehicle physics kit
//          Copyright © 2011-2023 Angel Garcia "Edy"
//        http://vehiclephysics.com | @VehiclePhysics
//--------------------------------------------------------------

using UnityEngine;


namespace VehiclePhysics.Demos
{

public class SaveVehicleConfig : MonoBehaviour
	{
	public string prefix = "Vehicle Physics Pro.Demo";

	[Space(5)]
	#if !VPP_ESSENTIAL
	public VPHeadMotion headMotion;
	#endif
	public VPCameraController cameraController;

	VehicleBase m_vehicle;

	// Load once the first time this GameObject becomes active

	void Awake ()
		{
		m_vehicle = GetComponentInParent<VehicleBase>();

		if (isActiveAndEnabled)
			{
			RestoreInput();
			RestoreCamera();
			}
		}

	// Store each time the GameObject is disabled

	void OnDisable ()
		{
		SaveInput();
		SaveCamera();
		}


	void OnApplicationQuit ()
		{
		// This seems to prevent an issue when camera component gets destroyed before
		// SaveVehicleConfig.OnDisable is called.

		SaveCamera();
		}


	public void SaveInput ()
		{
		#if !VPP_ESSENTIAL
		VPXboxInput xboxInput = m_vehicle.GetComponentInChildren<VPXboxInput>();
		if (xboxInput != null)
			{
			SetSection("xbox");
			SetBool("xboxInput", xboxInput.enabled);
			SetInt("device", (int)xboxInput.device);
			SetFloat("steeringNonlinearity", xboxInput.steeringNonlinearity);
			SetFloat("steeringDeadZone", xboxInput.steeringDeadZone);
			}

		VPDeviceInput deviceInput = m_vehicle.GetComponentInChildren<VPDeviceInput>();
		if (deviceInput != null)
			{
			SetSection("wheel");
			SetBool("deviceInput", deviceInput.enabled);
			SetInt("selectedDevice", deviceInput.selectedDevice);
			SetInt("deviceModel", (int)deviceInput.deviceModel);
			SetBool("disableClutchInput", deviceInput.disableClutchInput);
			SetFloat("throttleRangeMax", deviceInput.throttleRangeMax);
			SetFloat("brakeRangeMax", deviceInput.brakeRangeMax);
			SetFloat("clutchRangeMax", deviceInput.clutchRangeMax);
			SetFloat("forceIntensity", deviceInput.forceIntensity);
			SetFloat("nonLinearBias", deviceInput.nonLinearBias);
			SetFloat("damperCoefficient", deviceInput.damperCoefficient);
			SetBool("showForceFeedbackUI", deviceInput.showForceFeedbackUI);
			}

		PlayerPrefs.Save();
		#endif
		}


	public void RestoreInput ()
		{
		#if !VPP_ESSENTIAL

		// Restore VPDeviceInput first.
		// If both gets enabled, VPXboxInput will disable VPDeviceInput.

		VPDeviceInput deviceInput = m_vehicle.GetComponentInChildren<VPDeviceInput>();
		if (deviceInput != null)
			{
			SetSection("wheel");

			GetInt("selectedDevice", ref deviceInput.selectedDevice);

			int deviceModel = (int)deviceInput.deviceModel;
			GetInt("deviceModel", ref deviceModel);
			deviceInput.deviceModel = (VPDeviceInput.DeviceModel)deviceModel;

			deviceInput.disableClutchInput = GetBool("disableClutchInput", deviceInput.disableClutchInput);

			GetFloat("throttleRangeMax", ref deviceInput.throttleRangeMax);
			GetFloat("brakeRangeMax", ref deviceInput.brakeRangeMax);
			GetFloat("clutchRangeMax", ref deviceInput.clutchRangeMax);
			GetFloat("forceIntensity", ref deviceInput.forceIntensity);
			GetFloat("nonLinearBias", ref deviceInput.nonLinearBias);
			GetFloat("damperCoefficient", ref deviceInput.damperCoefficient);
			deviceInput.showForceFeedbackUI = GetBool("showForceFeedbackUI", deviceInput.showForceFeedbackUI);

			deviceInput.enabled = GetBool("deviceInput", deviceInput.enabled);
			}

		VPXboxInput xboxInput = m_vehicle.GetComponentInChildren<VPXboxInput>();
		if (xboxInput != null)
			{
			SetSection("xbox");

			int device = (int)xboxInput.device;
			GetInt("device", ref device);
			xboxInput.device = (XboxDevice)device;

			GetFloat("steeringNonlinearity", ref xboxInput.steeringNonlinearity);
			GetFloat("steeringDeadZone", ref xboxInput.steeringDeadZone);

			xboxInput.enabled = GetBool("xboxInput", xboxInput.enabled);
			}

		// If no other input is available, ensure Keyboard is.

		if ((xboxInput == null || !xboxInput.enabled)
			&& (deviceInput == null || !deviceInput.enabled))
			{
			VPStandardInput standardInput = m_vehicle.GetComponentInChildren<VPStandardInput>();
			if (standardInput != null) standardInput.enabled = true;
			}
		#endif
		}


	public void SaveCamera ()
		{
		SetSection("camera");
		if (cameraController != null) SetFloat("cameraFov", cameraController.driverCameraFov);
		#if !VPP_ESSENTIAL
		if (headMotion != null) SetBool("headMotion", headMotion.enabled);
		#endif
		}


	public void RestoreCamera ()
		{
		SetSection("camera");
		if (cameraController != null) cameraController.driverCameraFov = GetFloat("cameraFov", cameraController.driverCameraFov);
		#if !VPP_ESSENTIAL
		if (headMotion != null) headMotion.enabled = GetBool("headMotion", headMotion.enabled);
		#endif
		}


	// Utility: using prefix + section and getting values with reference


	string m_section = "";


	void SetSection (string section)
		{
		m_section = section;
		}


	string GetFullKey (string key)
		{
		return string.IsNullOrEmpty(m_section)? prefix + "." + key
			: prefix + "." + m_section + "." + key;
		}


	void SetFloat (string key, float value)
		{
		PlayerPrefs.SetFloat(GetFullKey(key), value);
		}


	void GetFloat (string key, ref float value)
		{
		value = PlayerPrefs.GetFloat(GetFullKey(key), value);
		}


	float GetFloat (string key, float defaultValue)
		{
		return PlayerPrefs.GetFloat(GetFullKey(key), defaultValue);
		}


	void SetInt (string key, int value)
		{
		PlayerPrefs.SetInt(GetFullKey(key), value);
		}


	void GetInt (string key, ref int value)
		{
		value = PlayerPrefs.GetInt(GetFullKey(key), value);
		}


	void SetBool (string key, bool value)
		{
		PlayerPrefs.SetInt(GetFullKey(key), value? 1 : 0);
		}


	bool GetBool (string key, bool defaultValue)
		{
		return PlayerPrefs.GetInt(GetFullKey(key), defaultValue? 1 : 0) != 0;
		}


	void SetString (string key, string value)
		{
		PlayerPrefs.SetString(GetFullKey(key), value);
		}


	string GetString (string key, string defaultValue)
		{
		return PlayerPrefs.GetString(GetFullKey(key), defaultValue);
		}


	bool GetString (string key, out string value)
		{
		value = PlayerPrefs.GetString(GetFullKey(key), "");
		return value != "";
		}

	}

}