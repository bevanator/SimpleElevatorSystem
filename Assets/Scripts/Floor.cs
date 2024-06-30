using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using UnityEngine;
namespace ElevatorSystem
{
    public class Floor : MonoBehaviour
    {
        [SerializeField] private Animator m_ElevatorDoorAnimator;
        [SerializeField] private QueuePlacer m_QueuePlacer;
        [ShowInInspector] private List<Passenger> _passengersInQueue = new();
        private int _index;
        [ShowInInspector, ReadOnly] private bool _isOpen;
        public bool IsOpen => _isOpen;
        public int Index => _index;
        public Transform ExitPoint => m_ExitPoint;
        [SerializeField] private Transform m_ElevatorPoint;
        [SerializeField] private Transform m_ExitPoint;
        private FloorManager _floorManager;
        private Tween _closingTween;
        private bool _isBoarding;

        private int _vipIndex = 0;
        private void Awake()
        {
            
        }

        public bool IsQueueEmpty()
        {
            return _passengersInQueue.Count == 0;
        }
        public bool IsQueueAtCapacity()
        {
            return _passengersInQueue.Count >= 5;
        }
        private void OnEnable()
        {
            PassengerController.OnPassengerCreatedEvent += OnPassengerCreatedEvent;
        }
        private void OnDisable()
        {
            PassengerController.OnPassengerCreatedEvent -= OnPassengerCreatedEvent;
        }

        public void Init(int index, FloorManager floorManager)
        {
            _index = index;
            _floorManager = floorManager;
        }
        
        public bool IsVipInQueue()
        {
            foreach (Passenger passenger in _passengersInQueue)
            {
                if (passenger.IsVip) return true;
            }
            return false;
        }

        private int GetMinimumVipWeightInQueue()
        {
            int minWeight = 100;
            foreach (Passenger passenger in _passengersInQueue)
            {
                if (passenger.IsVip && passenger.Weight < minWeight) minWeight = passenger.Weight;
            }
            return minWeight;
        }
        
        private void OnPassengerCreatedEvent(Passenger passenger, int floorIndex)
        {
            if(floorIndex != _index) return;
            PlacePassengerInQueue(passenger);
        }
        private void PlacePassengerInQueue(Passenger passenger, bool isDropped = false)
        {
            if (passenger.IsVip)
            {
                _passengersInQueue.Insert(_vipIndex, passenger);
                m_QueuePlacer.UpdateQueueVipCount();
                m_QueuePlacer.UpdateQueueCount();
                _vipIndex++;
                AdjustPositions();
                return;
            }
            if (isDropped)
            {
                passenger.SetSortingOrder(5);
                passenger.SetInElevatorStatus(false);
                passenger.SetDropPoint(_index);
                _passengersInQueue.Insert(_vipIndex, passenger);
                // passenger.MoveTo(m_QueuePlacer.GetLatestQueueVipPosition());
            }
            else
            {
                _passengersInQueue.Add(passenger);
                // passenger.MoveTo(m_QueuePlacer.GetLatestQueuePosition());
            }
            m_QueuePlacer.UpdateQueueCount();
            AdjustPositions();
        }
        private void AdjustPositions(bool animate = false)
        {
            for (int i = 0; i < _passengersInQueue.Count; i++)
            {
                Passenger _passenger = _passengersInQueue[i];
                _passenger.MoveTo(m_QueuePlacer.GetPositionByIndex(i), animate);
            }
        }

        [Button]
        public void OpenElevatorDoor()
        {
            if(_isOpen) return;
            m_ElevatorDoorAnimator.SetTrigger("open");
            _isOpen = true;
            _closingTween = DOVirtual.DelayedCall(1f, () => {
                _isOpen = false;
            });
            _closingTween = DOVirtual.DelayedCall(1.5f, () => {
                CloseElevatorDoor();
            });
        }
        
        [Button]
        public void CloseElevatorDoor()
        {
            m_ElevatorDoorAnimator.SetTrigger("close");
            // DOVirtual.DelayedCall(1f, () => 
            // {
            //     _isOpen = false;
            // });
        }

        public void LoadPassengers()
        {
            //todo: change logic later

            // if(_isBoarding) return;
            _isBoarding = true;
            DOVirtual.DelayedCall(0.5f, () => {
                ProcessPassengers();
            });

            void ProcessPassengers()
            {
                if (_passengersInQueue.IsNullOrEmpty())
                {
                    SendNullPassenger();
                    return;
                }
                if (ElevatorController.IsAtCapacity() && !IsVipInQueue() && !ElevatorController.IsDpInside())
                {
                    SendNullPassenger();
                    return;
                }

                if (ElevatorController.IsAtCapacity() && IsVipInQueue())
                {
                    if ((ElevatorController.GetTotalWeight() - ElevatorController.GetTotalDroppableWeight() + _passengersInQueue[0].Weight) >= 300)
                    {
                        DOVirtual.DelayedCall(0.5f, () => _floorManager.OnPassengerLoadComplete(null));
                        return;
                    }
                }

                _isOpen = false;
                List<int> destinationList = new();
                List<Passenger> passengerList = new();
                List<Passenger> droppableDpList = new();
                if (IsVipInQueue()) droppableDpList = ElevatorController.GetLatestDps();
                int totalWeight = ElevatorController.GetTotalWeight();


                DOVirtual.DelayedCall(0.25f, () => {
                    // CloseElevatorDoor();
                    for (int i = 0; i < _passengersInQueue.Count; i++)
                    {
                        Passenger passenger = _passengersInQueue[i];
                        if (passenger.IsDropped) break;
                        if (passenger.IsVip)
                        {
                            int replacingWeight = 0;
                            List<Passenger> droppedList = new();
                            foreach (Passenger dPassenger in droppableDpList)
                            {
                                if (passenger.Weight + totalWeight < 300) break;
                                ElevatorController.RemoveFromElevator(dPassenger);
                                totalWeight -= dPassenger.Weight;
                                PlacePassengerInQueue(dPassenger, true);
                                dPassenger.SetDropStatus(true);
                                droppedList.Add(dPassenger);
                                replacingWeight += dPassenger.Weight;
                                if (replacingWeight > passenger.Weight) break;
                            }
                            droppableDpList = droppableDpList.Except(droppedList).ToList();
                            DOVirtual.DelayedCall(0.5f, () => {
                                AdjustPositions();
                                foreach (Passenger qpassenger in droppedList)
                                {
                                    ElevatorController.AddToElevatorQueue(qpassenger.DropPoint);
                                    qpassenger.SetDropStatus(false);
                                }
                            });
                        }
                        passenger.MoveTo(m_ElevatorPoint.position, true, true, 0.5f, true, false, true);
                        passenger.transform.SetParent(ElevatorController.Interior);
                        destinationList.Add(passenger.Destination);
                        m_QueuePlacer.ReduceQueuePlacerCount();
                        passenger.SetInElevatorStatus(true);
                        passengerList.Add(passenger);
                        totalWeight += passenger.Weight;
                        if (totalWeight >= 300) break;
                    }

                    List<int> dList = destinationList.Distinct().ToList();
                    foreach (Passenger passenger in passengerList)
                    {
                        _passengersInQueue.Remove(passenger);
                        if (passenger.IsVip) _vipIndex--;
                    }

                    DOVirtual.DelayedCall(0.25f, () => {
                        _isBoarding = false;
                        _floorManager.OnPassengerLoadComplete(dList, passengerList);
                    });

                    if (!_passengersInQueue.IsNullOrEmpty())
                    {
                        AdjustPositions(true);
                        DOVirtual.DelayedCall(0.5f, () => ElevatorController.AddToElevatorQueue(_index));
                    }

                });
                void SendNullPassenger()
                {
                    DOVirtual.DelayedCall(0.5f, () => _floorManager.OnPassengerLoadComplete(null));
                }
            }
        }
    }
}