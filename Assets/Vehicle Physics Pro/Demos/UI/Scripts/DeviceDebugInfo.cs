//--------------------------------------------------------------
//      Vehicle Physics Pro: advanced vehicle physics kit
//          Copyright © 2011-2023 Angel Garcia "Edy"
//        http://vehiclephysics.com | @VehiclePhysics
//--------------------------------------------------------------

// DevideDebugInfo: displays the raw values received from the device in runtime


using UnityEngine;
using UnityEngine.UI;
using EdyCommonTools;


namespace VehiclePhysics.UI
{

public class DeviceDebugInfo : MonoBehaviour
	{
	public VehicleBase vehicle;

	public Text axis1;
	public Text axis2;
	public Text pov1;
	public Text pov2;
	public Text buttons;


	#if !VPP_ESSENTIAL
	VPDeviceInput m_deviceInput;


	void OnEnable ()
		{
		m_deviceInput = vehicle != null? vehicle.GetComponentInChildren<VPDeviceInput>() : null;
		}


	void OnDisable ()
		{
		if (m_deviceInput != null) m_deviceInput.debugInfo = false;
		}


	void Update ()
		{
		if (m_deviceInput != null && m_deviceInput.enabled)
			{
			m_deviceInput.debugInfo = true;

			if (axis1 != null)
				{
				axis1.text = string.Format("A0\t{0}\nA1\t{1}\nA2\t{2}\nA3\t{3}",
					m_deviceInput.lX, m_deviceInput.lY, m_deviceInput.lZ, m_deviceInput.lRx);
				}

			if (axis2 != null)
				{
				axis2.text = string.Format("A4\t{0}\nA5\t{1}\nA6\t{2}\nA7\t{3}",
					m_deviceInput.lRy, m_deviceInput.lRz, m_deviceInput.rglSlider0, m_deviceInput.rglSlider1);
				}

			if (pov1 != null)
				pov1.text = string.Format("P0\t{0}\nP1\t{1}", m_deviceInput.rgdwPOV0, m_deviceInput.rgdwPOV1);

			if (pov2 != null)
				pov2.text = string.Format("P2\t{0}\nP3\t{1}", m_deviceInput.rgdwPOV2, m_deviceInput.rgdwPOV3);

			if (buttons != null)
				buttons.text = string.Format("BT\t{0}", m_deviceInput.rgbButtons);
			}
		else
			{
			if (axis1 != null) axis1.text = "A0\t-\nA1\t-\nA2\t-\nA3\t-";
			if (axis2 != null) axis2.text = "A4\t-\nA5\t-\nA6\t-\nA7\t-";
			if (pov1 != null) pov1.text = "P0\t-\nP1\t-";
			if (pov2 != null) pov2.text = "P2\t-\nP3\t-";
			if (buttons != null) buttons.text = "BT\t-";
			}
		}

	#endif
	}
}