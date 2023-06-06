//--------------------------------------------------------------
//      Vehicle Physics Pro: advanced vehicle physics kit
//          Copyright © 2011-2023 Angel Garcia "Edy"
//        http://vehiclephysics.com | @VehiclePhysics
//--------------------------------------------------------------

// ForceFeedbackMonitor: displays live force feedback values


using UnityEngine;
using UnityEngine.UI;
using EdyCommonTools;


namespace VehiclePhysics.UI
{

public class ForceFeedbackMonitor : MonoBehaviour
	{
	public VehicleBase vehicle;

	// These bars will be controlled via Image.fillAmount

	public Image steeringForceBar;
	public Image steeringFrictionBar;

	public Color saturationColor = GColor.accentRed;


	#if !VPP_ESSENTIAL
	VPDeviceInput m_deviceInput;
	Color m_forceColor = GColor.cyan;
	Color m_frictionColor = GColor.cyan;


	void OnEnable ()
		{
		if (steeringForceBar != null) m_forceColor = steeringForceBar.color;
		if (steeringFrictionBar != null) m_frictionColor = steeringFrictionBar.color;

		m_deviceInput = vehicle != null? vehicle.GetComponentInChildren<VPDeviceInput>() : null;
		}


	void OnDisable ()
		{
		if (steeringForceBar != null) steeringForceBar.color = m_forceColor;
		if (steeringFrictionBar != null) steeringFrictionBar.color = m_frictionColor;
		}


	void LateUpdate ()
		{
		if (m_deviceInput != null && m_deviceInput.enabled)
			{
			SetBarAndColor(steeringForceBar, m_deviceInput.currentForceFactor, m_forceColor);
			SetBarAndColor(steeringFrictionBar, m_deviceInput.currentDamperFactor, m_frictionColor);
			}
		else
			{
			SetBarAndColor(steeringForceBar, 0.0f, m_forceColor);
			SetBarAndColor(steeringFrictionBar, 0.0f, m_frictionColor);
			}
		}


	void SetBarAndColor (Image image, float value, Color normalColor)
		{
		if (image != null)
			{
			value = MathUtility.FastAbs(value);
			image.fillAmount = value;
			image.color = value >= 1.0f? saturationColor : normalColor;
			}
		}

	#endif
	}
}