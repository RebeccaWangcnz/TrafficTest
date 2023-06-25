namespace TurnTheGameOn.SimpleTrafficSystem
{
    using UnityEngine;
    using System.IO;
    using UnityEditor;

    [RequireComponent(typeof(AITrafficCar))]
    public class BrakeLight2 : MonoBehaviour
    {
        public Light[] brakeLights;
        private AITrafficCar car;
        bool isBraking;
        public float timeset;
        public MeshRenderer brakeRenderer;
        public float Drag;

        /*
        public GameObject cubeLight;
        public Material brakematerials;
        public Color color = new Color(1, 0, 0, 1);
        private readonly string _keyword = "_EMISSION";
        private readonly string _colorName = "_EmissionColor";

        */

        private void Awake()
        {
            timeset = 0f;
            car = GetComponent<AITrafficCar>();
            for (int i = 0; i < brakeLights.Length; i++)
            {
                brakeLights[i].enabled = false;
            }
            brakeRenderer.enabled = false;
            //brakematerials.DisableKeyword(_keyword);
            //cubeLight.GetComponent<Renderer>().material = brakematerials;
        }

        void FixedUpdate()
        {
            Drag = this.GetComponent<Rigidbody>().drag;
            if (car.IsBraking())
            {
                timeset =timeset+ Time.deltaTime;
                //if (timeset > 0.01f)
                {
                    if (Drag > 0.55f)
                    {
                        if (isBraking == false)
                        {
                            isBraking = true;
                            for (int i = 0; i < brakeLights.Length; i++)
                            {
                                brakeLights[i].enabled = true;
                            }
                            brakeRenderer.enabled = true;
                            //brakematerials.EnableKeyword(_keyword);
                            //brakematerials.SetColor(_colorName, Color.HSVToRGB(0, 100, 100));
                        }
                    }
                    
                }
           
            }
            else
            {
                timeset = 0f;
                if (isBraking)
                {
                    isBraking = false;
                    for (int i = 0; i < brakeLights.Length; i++)
                    {
                        brakeLights[i].enabled = false;
                    }
                    brakeRenderer.enabled = false;
                    //brakematerials.DisableKeyword(_keyword);
                }
            }
        }
    }
}