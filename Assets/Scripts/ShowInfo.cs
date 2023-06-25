namespace TurnTheGameOn.SimpleTrafficSystem
{
    using UnityEngine;
    [RequireComponent(typeof(AITrafficCar))]
    public class ShowInfo : MonoBehaviour
    {
        
        private AITrafficCar car;
        public float Speed;
        public float Power;
        public float Drag;
        // Start is called before the first frame update
        private void Awake()
        {
            car = GetComponent<AITrafficCar>();

        }

        // Update is called once per frame
        void Update()
        {
            Speed = car.CurrentSpeed();
            Power = car.AccelerationInput();
            Drag = this.GetComponent<Rigidbody>().drag;
        }
    }
}
