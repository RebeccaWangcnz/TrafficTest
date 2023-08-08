namespace TurnTheGameOn.SimpleTrafficSystem
{
    [System.Serializable]
    public struct AITrafficPoolEntry
    {
        public string name;
        public int assignedIndex;
        public AITrafficCar trafficPrefab;
    }
    [System.Serializable]
    public struct AIPeoplePoolEntry//Rebe：用来登记在pool里行人的信息
    {
        public string name;
        public int assignedIndex;
        public AIPeople peoplePrefab;
    }
}