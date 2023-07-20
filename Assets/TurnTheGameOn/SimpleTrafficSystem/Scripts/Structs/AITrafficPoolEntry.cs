namespace TurnTheGameOn.SimpleTrafficSystem
{
    [System.Serializable]
    public struct AITrafficPoolEntry
    {
        public string name;
        public int assignedIndex;
        public AITrafficCar trafficPrefab;
    }
    [System.Serializable]//Rebe
    public struct AIPeoplePoolEntry
    {
        public string name;
        public int assignedIndex;
        public AIPeople peoplePrefab;
    }
}