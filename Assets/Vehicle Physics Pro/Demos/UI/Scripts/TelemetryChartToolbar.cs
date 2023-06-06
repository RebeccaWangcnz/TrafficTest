//--------------------------------------------------------------
//      Vehicle Physics Pro: advanced vehicle physics kit
//          Copyright © 2011-2023 Angel Garcia "Edy"
//        http://vehiclephysics.com | @VehiclePhysics
//--------------------------------------------------------------

// TelemetryChartToolbar: reads toolbar buttons and applies the functions


using UnityEngine;
using UnityEngine.UI	;
using UnityEngine.EventSystems;
using EdyCommonTools;


namespace VehiclePhysics.UI
{

public class TelemetryChartToolbar : MonoBehaviour
		#if !VPP_ESSENTIAL
		, IPointerDownHandler, IPointerUpHandler
		#endif
	{
	#if !VPP_ESSENTIAL
	public VPPerformanceDisplay telemetryChart;
	#endif

	public Color onPressColor = GColor.ParseColorHex("#838383");

	[Header("Toolbar elements")]
	public GameObject program;
	public GameObject startStop;
	public GameObject panLeft;
	public GameObject panRight;
	public GameObject panUp;
	public GameObject panDown;
	public GameObject zoomHorizIn;
	public GameObject zoomHorizOut;
	public GameObject zoomVertIn;
	public GameObject zoomVertOut;
	public GameObject reset;
	public GameObject windowSize;
	public GameObject close;

	[Header("UI Texts")]
	public Text startStopText;
	public Text windowSizeText;
	public string startText = "START";
	public string stopText = "STOP";
	public string maximizeText = "\uF065";	// FontAwesome glyphs
	public string minimizeText = "\uF066";

	#if !VPP_ESSENTIAL

	Graphic m_lastGraphic = null;
	Color m_lastColor;


	float m_horizontalZoom = 0.0f;
	float m_verticalZoom = 0.0f;
	float m_horizontalPan = 0.0f;
	float m_verticalPan = 0.0f;


	void OnEnable ()
		{
		StopActions();
		}


	void Update ()
		{
		if (telemetryChart != null)
			{
			telemetryChart.HorizontalZoom(m_horizontalZoom);
			telemetryChart.VerticalZoom(m_verticalZoom);
			telemetryChart.HorizontalPan(m_horizontalPan);
			telemetryChart.VerticalPan(m_verticalPan);

			// Show START-STOP based on recording state

			if (startStopText != null)
				{
				startStopText.text = telemetryChart.IsRecording()? stopText : startText;
				}

			// Switch maximize-minimize icon based on visual state

			if (windowSizeText != null)
				{
				windowSizeText.text = telemetryChart.viewMode == VPPerformanceDisplay.ViewportMode.Small ? maximizeText : minimizeText;
				}
			}
		}


	public void OnPointerDown (PointerEventData eventData)
		{
		// Ensure to cancel any other press or action

		OnPointerUp(eventData);

		if (eventData.button != PointerEventData.InputButton.Left) return;

		// Handle close button separately

		GameObject pressed = eventData.pointerCurrentRaycast.gameObject;

		if (pressed == close)
			{
			if (telemetryChart != null) telemetryChart.visible = false;
			this.gameObject.SetActive(false);
			return;
			}

		// Handle other buttons

		m_lastGraphic = pressed.GetComponentInChildren<Graphic>();

		if (m_lastGraphic != null)
			{
			m_lastColor = m_lastGraphic.color;
			m_lastGraphic.color = onPressColor;
			}

		PerformAction(pressed);
		}


	public void OnPointerUp (PointerEventData eventData)
		{
		StopActions();

		if (m_lastGraphic != null)
			{
			m_lastGraphic.color = m_lastColor;
			m_lastGraphic = null;
			}
		}


	public void PerformAction (GameObject go)
		{
		// Cancel any ongoing action

		StopActions();

		if (telemetryChart == null || go == null) return;

		// Perform actions based on the invoked object

        if (go == program)
			{
			telemetryChart.NextChart();
			}
		else
		if (go == startStop)
			{
			telemetryChart.ToggleRecord();
			}
		else
		if (go == panLeft)
			{
			m_horizontalPan = -1.0f;
			}
		else
		if (go == panRight)
			{
			m_horizontalPan = 1.0f;
			}
		else
		if (go == panUp)
			{
			m_verticalPan = 1.0f;
			}
		else
		if (go == panDown)
			{
			m_verticalPan = -1.0f;
			}
		else
		if (go == zoomHorizIn)
			{
			m_horizontalZoom = 1.0f;
			}
		else
		if (go == zoomHorizOut)
			{
			m_horizontalZoom = -1.0f;
			}
		else
		if (go == zoomVertIn)
			{
			m_verticalZoom = 1.0f;
			}
		else
		if (go == zoomVertOut)
			{
			m_verticalZoom = -1.0f;
			}
		else
		if (go == reset)
			{
			telemetryChart.ResetView();
			}
		else
		if (go == windowSize)
			{
			telemetryChart.ToggleViewMode();
			}
		}


	public void StopActions ()
		{
		m_horizontalZoom = 0.0f;
		m_verticalZoom = 0.0f;
		m_horizontalPan = 0.0f;
		m_verticalPan = 0.0f;
		}

	#endif
	}

}