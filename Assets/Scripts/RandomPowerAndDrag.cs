namespace TurnTheGameOn.SimpleTrafficSystem
{
    using UnityEngine;

    public class RandomPowerAndDrag : MonoBehaviour
    {
        public float minPower = 2300f;
        public float maxPower = 2600f;
        public float minDrag = 0.55f;
        public float maxDrag = 0.65f;

        void OnEnable()
        {
            GetComponent<AITrafficCar>().accelerationPower = UnityEngine.Random.Range(minPower, maxPower);
            GetComponent<AITrafficCar>().minDrag = UnityEngine.Random.Range(minDrag, maxDrag);
        }
    }
}