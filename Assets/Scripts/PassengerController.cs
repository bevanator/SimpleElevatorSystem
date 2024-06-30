using System;
using UnityEngine;
namespace ElevatorSystem
{
    public class PassengerController : MonoBehaviour
    {
        private static PassengerController _instance;
        public static event Action<Passenger, int> OnPassengerCreatedEvent;
        [SerializeField] private Passenger m_DefaultPassengerPrefab;
        [SerializeField] private Passenger m_VipPassengerPrefab;
        
        private void Awake()
        {
            _instance = this;
        }
        public static void CreateVip(int floorIndex, int destination, int weight)
        {
            if(FloorManager.GetFloorByIndex(floorIndex).IsQueueAtCapacity()) return;
            Passenger passenger = Instantiate(_instance.m_VipPassengerPrefab, null);
            passenger.Init(destination, weight, true);
            passenger.gameObject.name = "Vip_" + weight + "(" + destination + ")";
            passenger.SetInElevatorStatus(false);
            OnPassengerCreatedEvent?.Invoke(passenger, floorIndex);
        }

        public static void CreateDp(int floorIndex, int destination, int weight)
        {
            if(FloorManager.GetFloorByIndex(floorIndex).IsQueueAtCapacity()) return;
            Passenger passenger = Instantiate(_instance.m_DefaultPassengerPrefab, null);
            passenger.Init(destination, weight);
            passenger.gameObject.name = "Dp_" + weight + "(" + destination + ")";
            passenger.SetInElevatorStatus(false);
            OnPassengerCreatedEvent?.Invoke(passenger, floorIndex);
        }
    }
}