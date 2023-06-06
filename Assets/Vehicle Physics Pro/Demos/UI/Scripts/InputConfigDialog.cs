//--------------------------------------------------------------
//      Vehicle Physics Pro: advanced vehicle physics kit
//          Copyright © 2011-2023 Angel Garcia "Edy"
//        http://vehiclephysics.com | @VehiclePhysics
//--------------------------------------------------------------

// SettingsDialog: a dialog for configuring vehicle settings
//
// Assumes the VPStandardInput component is the only one active in the vehicle.


using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using EdyCommonTools;
using System;


namespace VehiclePhysics.UI
{

public class InputConfigDialog : MonoBehaviour
	{
	public VehicleBase vehicle;

	// Monitor toggles only. Panels are shown/hidden via GenericMenu

	[Header("Menu Toggles")]
	public Toggle keyboard;
	public Toggle xbox;
	public Toggle wheel;

	[Header("Xbox")]
	public Toggle xbox1;
	public Toggle xbox2;
	public Toggle xbox3;
	public Toggle xbox4;
	public Slider xboxNonLinearity;
	public Slider xboxDeadZone;

	[Header("Wheel")]
	public Toggle device1;
	public Toggle device2;
	public Toggle device3;
	public Toggle device4;
	public Dropdown deviceModel;
	public Toggle deviceClutchPresent;
	public Toggle deviceClutchAbsent;
	public Slider deviceThrottle;
	public Slider deviceBrake;
	public Slider deviceClutch;
	public Slider deviceIntensity;
	public Slider deviceLinearity;
	public Slider deviceFriction;
	public Toggle deviceUIShow;
	public Toggle deviceUIHide;

	[Header("UI")]
	public Text messageBox;
	public Color defaultMessageColor = Color.white;
	public Color warningMessageColor = GColor.orangeA100;
	public Button deviceDebugInfo;

	[Header("External")]
	public GameObject deviceDebugInfoPanel;


	void Awake ()
		{
		// First time this GameObject becomes active: clear the method options.
		// They will be configured from the vehicle in OnEnable.

		SetEnabled(keyboard, false);
		SetEnabled(xbox, false);
		SetEnabled(wheel, false);
		}


	void Start ()
		{
		// WATCHDOG

		// In some builds the Keyboard panel remains enabled
		// after resetting the scene with Wheel enabled.
		//
		// Looks like an Unity bug. Tracing the execution reads keyboard.isOn = true
		// right after setting keyboard.isOn = false, in both Awake and OnDisable.
		// Maybe something related with the order of initialization in the Unity UI scripts?
		//
		// Never happens on first run, only after resetting the scene with Esc
		// (SceneManager.LoadScene).

		// So we explicitly disable the Keyboard panel here if it should be disabled.

		if (IsEnabled(xbox) || IsEnabled(wheel))
			SetEnabled(keyboard, false);

		// Additionally, when this happens then "SetEnabled(keyboard, true)" in OnEnable
		// won't trigger the EnableKeyboard call, as it's already on.

		// So we explicitly call EnableKeyboard here if Keyboard should be enabled.

		if (IsEnabled(keyboard))
			EnableKeyboard();
		}


	void OnEnable ()
		{
		// Input methods

		AddListener(keyboard, OnKeyboard);
		AddListener(xbox, OnXbox);
		AddListener(wheel, OnWheel);

		// Initialize the UI with the values from the components.
		// Must be done before adding the remaining listeners, otherwise they would get invoked.
		// Method option listeners are enabled so current method is properly initialized.

		InitializeXboxUI();
		InitializeWheelUI();
		if (!IsEnabled(xbox) && !IsEnabled(wheel))
			SetEnabled(keyboard, true);

		// Changing Xbox device requires restart the input

		AddListener(xbox1, OnXbox);
		AddListener(xbox2, OnXbox);
		AddListener(xbox3, OnXbox);
		AddListener(xbox4, OnXbox);

		// Xbox settings don't require a restart

		AddListener(xboxNonLinearity, UpdateXboxSettings);
		AddListener(xboxDeadZone, UpdateXboxSettings);

		// Changing Wheel device requires restart the input

		AddListener(device1, OnWheel);
		AddListener(device2, OnWheel);
		AddListener(device3, OnWheel);
		AddListener(device4, OnWheel);

		// Changing Wheel parameters don't require a restart

		AddListener(deviceModel, UpdateWheelSettings);
		AddListener(deviceClutchPresent, delegate { UpdateWheelSettings(); });
		AddListener(deviceThrottle, UpdateWheelSettings);
		AddListener(deviceBrake, UpdateWheelSettings);
		AddListener(deviceClutch, UpdateWheelSettings);
		AddListener(deviceIntensity, UpdateWheelSettings);
		AddListener(deviceLinearity, UpdateWheelSettings);
		AddListener(deviceFriction, UpdateWheelSettings);
		AddListener(deviceUIShow, delegate { UpdateWheelSettings(); });

		// Toggle debug info button

		if (deviceDebugInfo != null)
			deviceDebugInfo.onClick.AddListener(OnDeviceDebugInfo);
		}


	void OnDisable ()
		{
		RemoveListener(keyboard, OnKeyboard);
		RemoveListener(xbox, OnXbox);
		RemoveListener(wheel, OnWheel);

		RemoveListener(xbox1, OnXbox);
		RemoveListener(xbox2, OnXbox);
		RemoveListener(xbox3, OnXbox);
		RemoveListener(xbox4, OnXbox);

		RemoveListener(xboxNonLinearity, UpdateXboxSettings);
		RemoveListener(xboxDeadZone, UpdateXboxSettings);

		RemoveListener(device1, OnWheel);
		RemoveListener(device2, OnWheel);
		RemoveListener(device3, OnWheel);
		RemoveListener(device4, OnWheel);

		RemoveListener(deviceModel, UpdateWheelSettings);
		RemoveListener(deviceClutchPresent, delegate { UpdateWheelSettings(); });
		RemoveListener(deviceThrottle, UpdateWheelSettings);
		RemoveListener(deviceBrake, UpdateWheelSettings);
		RemoveListener(deviceClutch, UpdateWheelSettings);
		RemoveListener(deviceIntensity, UpdateWheelSettings);
		RemoveListener(deviceLinearity, UpdateWheelSettings);
		RemoveListener(deviceFriction, UpdateWheelSettings);
		RemoveListener(deviceUIShow, delegate { UpdateWheelSettings(); });

		if (deviceDebugInfo != null)
			deviceDebugInfo.onClick.RemoveListener(OnDeviceDebugInfo);
		}


	// Listeners


	void OnKeyboard (bool value)
		{
		if (value)
			EnableKeyboard();
		}


	void OnXbox (bool value)
		{
		if (value)
			EnableXbox();
		}


	void OnWheel (bool value)
		{
		if (value)
			EnableWheel();
		}


	void OnDeviceDebugInfo ()
		{
		ToggleDebugInfoPanel();
		}


	// Keyboard


	void EnableKeyboard ()
		{
		#if !VPP_ESSENTIAL
		SetEnabled(typeof(VPDeviceInput), false);
		SetEnabled(typeof(VPXboxInput), false);
		#endif
		SetEnabled(typeof(VPStandardInput), true);
		HideDebugInfoPanel();

		SetMessage("");
		}


	// Xbox


	void EnableXbox ()
		{
		if (vehicle == null) return;

		#if !VPP_ESSENTIAL

		// Check for XBox input availability

		VPXboxInput xboxInput = vehicle.GetComponentInChildren<VPXboxInput>();
		if (xboxInput == null)
			{
			EnableKeyboard();
			SetMessage("This vehicle doesn't support Xbox input.\nUsing keyboard.", warningMessageColor);
			return;
			}

		// Disable other input methods

		SetEnabled(typeof(VPDeviceInput), false);
		SetEnabled(typeof(VPStandardInput), false);
		HideDebugInfoPanel();

		// Try initialize with the given device

		int device = 0;

		if (xbox2 != null && xbox2.isOn) device = 1;
		else if (xbox3 != null && xbox3.isOn) device = 2;
		else if (xbox4 != null && xbox4.isOn) device = 3;

		xboxInput.device = (XboxDevice)device;
		xboxInput.enabled = false;
		xboxInput.enabled = true;

		// Check for error

		if (xboxInput.enabled)
			{
			SetMessage("Using Xbox Gamepad #" + (device+1));
			}
		else
			{
			EnableKeyboard();
			SetMessage("Xbox Gamepad not found in this slot.\nUsing keyboard.", warningMessageColor);
			}

		#else
		EnableKeyboard();
		SetMessage("Xbox Gamepad support not available.\nUsing keyboard.", warningMessageColor);
		#endif
		}


	void UpdateXboxSettings ()
		{
		if (vehicle == null) return;

		#if !VPP_ESSENTIAL

		// Check for XboxInput availability

		VPXboxInput xboxInput = vehicle.GetComponentInChildren<VPXboxInput>();
		if (xboxInput == null) return;

		// Steering

		if (xboxNonLinearity != null) xboxInput.steeringNonlinearity = xboxNonLinearity.value;
		if (xboxDeadZone != null) xboxInput.steeringDeadZone = xboxDeadZone.value;

		#endif
		}


	void InitializeXboxUI ()
		{
		if (vehicle == null) return;

		#if !VPP_ESSENTIAL

		VPXboxInput xboxInput = vehicle.GetComponentInChildren<VPXboxInput>();
		if (xboxInput == null) return;

		// Retrieve current device slot

		if (xbox1 != null) xbox1.isOn = xboxInput.device == XboxDevice.XboxController1;
		if (xbox2 != null) xbox2.isOn = xboxInput.device == XboxDevice.XboxController2;
		if (xbox3 != null) xbox3.isOn = xboxInput.device == XboxDevice.XboxController3;
		if (xbox4 != null) xbox4.isOn = xboxInput.device == XboxDevice.XboxController4;

		// Steering

		if (xboxNonLinearity != null) xboxNonLinearity.value = xboxInput.steeringNonlinearity;
		if (xboxDeadZone != null) xboxDeadZone.value = xboxInput.steeringDeadZone;

		// Enable the xbox panel if this input is enabled.
		// This triggers the initialization method if the listener has been set.

		if (xboxInput.enabled && xbox != null)
			xbox.isOn = true;

		#endif
		}


	// Wheel


	void EnableWheel ()
		{
		if (vehicle == null) return;

		#if !VPP_ESSENTIAL

		// Check for DirectInput availability

		VPDeviceInput deviceInput = vehicle.GetComponentInChildren<VPDeviceInput>();
		if (deviceInput == null)
			{
			EnableKeyboard();
			SetMessage("This vehicle doesn't support wheel input.\nUsing keyboard.", warningMessageColor);
			return;
			}

		// Disable other input methods

		SetEnabled(typeof(VPXboxInput), false);
		SetEnabled(typeof(VPStandardInput), false);

		// Try initializing with the given device

		int device = 0;

		if (device2 != null && device2.isOn) device = 1;
		else if (device3 != null && device3.isOn) device = 2;
		else if (device4 != null && device4.isOn) device = 3;

		deviceInput.selectedDevice = device;
		deviceInput.enabled = false;
		deviceInput.enabled = true;

		// Check for error

		if (deviceInput.enabled)
			{
			// Show: device name, device capabilities and recommended degrees of rotation

			string message = deviceInput.deviceName + "\n" + deviceInput.deviceCaps;
			message += "\nConfigure the wheel to <b>" + deviceInput.configuredWheelRange.ToString("0.") + "</b> degrees of rotation";
			message += "\nEnsure the <i>Combined Pedals</i> mode is disabled";

			SetMessage(message);
			}
		else
			{
			EnableKeyboard();
			SetMessage("Wheel device not found in this slot or unavailable.\nUsing keyboard.", warningMessageColor);
			}

		#else
		EnableKeyboard();
		SetMessage("Wheel device support not available.\nUsing keyboard.", warningMessageColor);
		#endif
		}


	void UpdateWheelSettings ()
		{
		if (vehicle == null) return;

		#if !VPP_ESSENTIAL

		// Check for DirectInput availability

		VPDeviceInput deviceInput = vehicle.GetComponentInChildren<VPDeviceInput>();
		if (deviceInput == null) return;

		// Device Model

		if (deviceModel != null)
			deviceInput.deviceModel = (VPDeviceInput.DeviceModel)deviceModel.value;

		if (deviceClutchPresent != null)
			deviceInput.disableClutchInput = !deviceClutchPresent.isOn;

		// Pedals

		if (deviceThrottle != null) deviceInput.throttleRangeMax = deviceThrottle.value;
		if (deviceBrake != null) deviceInput.brakeRangeMax = deviceBrake.value;
		if (deviceClutch != null) deviceInput.clutchRangeMax = deviceClutch.value;

		// Force feedback

		if (deviceIntensity != null) deviceInput.forceIntensity = deviceIntensity.value;
		if (deviceLinearity != null) deviceInput.nonLinearBias = deviceLinearity.value;
		if (deviceFriction != null) deviceInput.damperCoefficient = deviceFriction.value;
		if (deviceUIShow != null) deviceInput.showForceFeedbackUI = deviceUIShow.isOn;

		#endif
		}


	void InitializeWheelUI ()
		{
		if (vehicle == null) return;

		#if !VPP_ESSENTIAL

		VPDeviceInput deviceInput = vehicle.GetComponentInChildren<VPDeviceInput>();
		if (deviceInput == null) return;

		// Device slot

		if (device1 != null) device1.isOn = deviceInput.selectedDevice <= 0 || deviceInput.selectedDevice > 3;
		if (device2 != null) device2.isOn = deviceInput.selectedDevice == 1;
		if (device3 != null) device3.isOn = deviceInput.selectedDevice == 2;
		if (device4 != null) device4.isOn = deviceInput.selectedDevice == 3;

		// Device model

		if (deviceModel != null) deviceModel.value = (int)deviceInput.deviceModel;
		if (deviceClutchPresent != null) deviceClutchPresent.isOn = !deviceInput.disableClutchInput;
		if (deviceClutchAbsent != null) deviceClutchAbsent.isOn = deviceInput.disableClutchInput;

		// Pedals

		if (deviceThrottle != null) deviceThrottle.value = deviceInput.throttleRangeMax;
		if (deviceBrake != null) deviceBrake.value = deviceInput.brakeRangeMax;
		if (deviceClutch != null) deviceClutch.value = deviceInput.clutchRangeMax;

		// Force feedback

		if (deviceIntensity != null) deviceIntensity.value = deviceInput.forceIntensity;
		if (deviceLinearity != null) deviceLinearity.value = deviceInput.nonLinearBias;
		if (deviceFriction != null) deviceFriction.value = deviceInput.damperCoefficient;
		if (deviceUIShow != null) deviceUIShow.isOn = deviceInput.showForceFeedbackUI;
		if (deviceUIHide != null) deviceUIHide.isOn = !deviceInput.showForceFeedbackUI;

		// Enable the wheel panel if this input is enabled.
		// This triggers the initialization method if the listener has been set.

		if (deviceInput.enabled && wheel != null)
			wheel.isOn = true;

		#endif
		}


	void ToggleDebugInfoPanel ()
		{
		if (vehicle == null) return;

		#if !VPP_ESSENTIAL
		VPDeviceInput deviceInput = vehicle.GetComponentInChildren<VPDeviceInput>();
		if (deviceInput == null) return;

		if (deviceDebugInfoPanel != null)
			deviceDebugInfoPanel.SetActive(!deviceDebugInfoPanel.activeSelf && deviceInput.enabled);
		#endif
		}


	void HideDebugInfoPanel ()
		{
		if (deviceDebugInfoPanel != null)
			deviceDebugInfoPanel.SetActive(false);
		}


	// Helper methods


	void SetMessage (string text)
		{
		SetMessage(text, defaultMessageColor);
		}


	void SetMessage (string text, Color color)
		{
		if (messageBox != null)
			{
			messageBox.text = text;
			messageBox.color = color;
			}
		}


	void AddListener (Toggle toggle, UnityAction<bool> call)
		{
		if (toggle != null) toggle.onValueChanged.AddListener(call);
		}


	void RemoveListener (Toggle toggle, UnityAction<bool> call)
		{
		if (toggle != null) toggle.onValueChanged.RemoveListener(call);
		}

	void AddListener (Dropdown dropdown, UnityAction call)
		{
		if (dropdown != null) dropdown.onValueChanged.AddListener(delegate { call(); });
		}


	void RemoveListener (Dropdown dropdown, UnityAction call)
		{
		if (dropdown != null) dropdown.onValueChanged.RemoveListener(delegate { call(); });
		}


	void AddListener (Slider slider, UnityAction call)
		{
		if (slider != null) slider.onValueChanged.AddListener(delegate { call(); });
		}


	void RemoveListener (Slider slider, UnityAction call)
		{
		if (slider != null) slider.onValueChanged.RemoveListener(delegate { call(); });
		}


	void SetEnabled (Toggle toggle, bool enabled)
		{
		if (toggle != null) toggle.isOn = enabled;
		}


	bool IsEnabled (Toggle toggle)
		{
		return toggle != null? toggle.isOn : false;
		}


	void SetEnabled (Type type, bool enabled)
		{
		if (vehicle == null) return;

		MonoBehaviour comp = vehicle.GetComponentInChildren(type) as MonoBehaviour;
		if (comp != null) comp.enabled = enabled;
		}


	bool IsAvailable (Type type)
		{
		if (vehicle == null) return false;

		MonoBehaviour comp = vehicle.GetComponentInChildren(type) as MonoBehaviour;
		return comp != null;
		}
	}
}