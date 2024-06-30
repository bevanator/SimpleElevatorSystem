using System;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
namespace ElevatorSystem
{
    public class QueuePlacer : MonoBehaviour
    {
        [SerializeField] private List<Transform> m_PositionList = new();
        [ShowInInspector] private int _occupiedPeopleCount;
        [ShowInInspector] private int _occupiedVipCount;
        private void Awake()
        {
            _occupiedPeopleCount = 0;
            _occupiedVipCount = 0;
        }
        public Vector3 GetPositionByIndex(int index)
        {
            return m_PositionList[index].position;
        }
        public Vector3 GetLatestQueuePosition()
        {
            return m_PositionList[_occupiedPeopleCount].position;
        }
        public Vector3 GetLatestQueueVipPosition()
        {
            return m_PositionList[_occupiedVipCount].position;
        }
        
        public void UpdateQueueCount()
        {
            _occupiedPeopleCount++;
        }
        public void UpdateQueueVipCount()
        {
            _occupiedVipCount++;
        }
        
        public void ReduceQueuePlacerCount()
        {
            _occupiedPeopleCount--;
        }
        
        public void ResetQueuePlacer()
        {
            _occupiedPeopleCount = 0;
        }
    }
}