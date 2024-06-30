using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
namespace ElevatorSystem
{
    public class FloorManager : MonoBehaviour
    {
        private static FloorManager _instance;
        [SerializeField] private List<Floor> m_FloorList = new();
        private Floor _currentFloor;
        public Floor CurrentFloor => _currentFloor;
        private bool _isGround;
        public bool IsGround => _isGround;
        public event Action<List<int>, List<Passenger>> OnPassengerBoardedEvent;
        public event Action<Passenger> OnPassengerLeftEvent;
        public event Action<int> OnReachedFloorEvent;
        
        private void Awake()
        {
            _instance = this;
            InitFloors();
            _currentFloor = m_FloorList[0];
            _isGround = true;
        }
        
        private void InitFloors()
        {
            int i = 0;
            foreach (Floor floor in m_FloorList) floor.Init(i++, this);
        }
        public void OnReachedAtFloor(int index)
        {
            _isGround = index == 0;
            _currentFloor = m_FloorList[index];
            // _currentFloor.OpenElevatorDoor();
            // DOVirtual.DelayedCall(1.3f, () => 
            // {
            //     _currentFloor.LoadPassengers();
            // });
        }
        
        public void OnStoppedAtFloor(int index)
        {
            OnReachedFloorEvent?.Invoke(_currentFloor.Index);
            if (ElevatorController.GetPassengerCount() == 0 && _currentFloor.IsQueueEmpty()) return;
            _currentFloor.OpenElevatorDoor();
        }
        
        public static Floor GetFloorByIndex(int index)
        {
            return _instance.m_FloorList[index];
        }

        public void OnPassengerLoadComplete(List<int> destList = null, List<Passenger> passList = null)
        {
            if (passList == null)
            {
                OnPassengerBoardedEvent?.Invoke(null, null);
                return;
            }
            OnPassengerBoardedEvent?.Invoke(destList, passList);
        }
        public void OnPassengerLeft(Passenger passenger)
        {
            OnPassengerLeftEvent?.Invoke(passenger);
        }
    }
}