using System;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using Sirenix.OdinInspector;
using Sirenix.Utilities;
using Unity.VisualScripting;
using UnityEngine;
namespace ElevatorSystem
{
    public class ElevatorController : MonoBehaviour
    {
        private static ElevatorController _instance;
        private FloorManager _floorManager;
        [SerializeField] private float m_ElevatorSpeed = 5f;
        private ElevatorStates _currentState;
        [ShowInInspector] private int _currentFloorIndex;
        private int _destinationFloorIndex;
        private Tween _elevatorTween;
        [ShowInInspector] private List<int> _elevatorDestinationQueue = new();
        private static Transform _interior;
        public static Transform Interior => _interior;
        public int PassengerCount => _passengerCount;

        private int _passengerCount;

        [ShowInInspector, ReadOnly] private bool _isMoving;
        private Tween _boardingTween;
        private Tween _resetTween;
        [ShowInInspector] private bool _isIdle;
        [ShowInInspector] private bool _isOpen;
        public static event Action<ElevatorStates> OnElevatorStateChanged;
        public static event Action<int> OnElevatorFloorChanged;
        public static event Action OnElevatorStopped;
        public static event Action<int> OnElevatorWeightUpdated;
        public static event Action<bool> OnElevatorVipOccupied;
        [ShowInInspector] private int _weightInElevator;
        private bool _isAtCapacity;
        public int WeightInElevator => _weightInElevator;
        [ShowInInspector] private List<Passenger> _passengersInElevatorList = new();
        private void Awake()
        {
            _instance = this;
            _interior = GetComponentInChildren<Transform>();
            _floorManager = FindObjectOfType<FloorManager>();
            _currentFloorIndex = 0;
            _elevatorTween = null;
            _isIdle = true;
        }
        private void OnEnable()
        {
            PassengerController.OnPassengerCreatedEvent += OnPassengerCreatedEvent;
            _floorManager.OnPassengerBoardedEvent += OnPassengerBoardedEvent;
            _floorManager.OnPassengerLeftEvent += OnPassengerLeftEvent;
        }

        private void OnDisable()
        {
            PassengerController.OnPassengerCreatedEvent -= OnPassengerCreatedEvent;
            _floorManager.OnPassengerBoardedEvent -= OnPassengerBoardedEvent;
            _floorManager.OnPassengerLeftEvent -= OnPassengerLeftEvent;

        }
        public static int GetPassengerCount()
        {
            return _instance._passengerCount;
        }
        private void OnPassengerLeftEvent(Passenger passenger)
        {
            _passengerCount = PassengerCount - 1;
            _weightInElevator -= passenger.Weight;
            _isAtCapacity = _weightInElevator >= 300;
            _passengersInElevatorList.Remove(passenger);
            OnElevatorWeightUpdated?.Invoke(_weightInElevator);

        }
        private void RemoveFromElevatorInternal(Passenger passenger)
        {
            _passengersInElevatorList.Remove(passenger);
            _passengerCount--;
            _weightInElevator -= passenger.Weight;
            passenger.transform.SetParent(null);
        }
        public static void RemoveFromElevator(Passenger passenger)
        {
            _instance.RemoveFromElevatorInternal(passenger);
        }
        private void OnPassengerBoardedEvent(List<int> destinations, List<Passenger> passengers)
        {
            if (passengers == null)
            {
                // if (_currentFloorIndex != 0 && _passengerCount == 0)
                if (_passengersInElevatorList.Count == 0)
                {
                    if (_elevatorDestinationQueue.IsNullOrEmpty())
                    {
                        _resetTween = DOVirtual.DelayedCall(1.5f, () => {
                            _isOpen = false;
                            GoDown();
                        });
                    }
                }
            }
            else
            {
                _elevatorDestinationQueue.AddRange(destinations);
                _passengerCount = PassengerCount + destinations.Count;
                foreach (Passenger passenger in passengers)
                {
                    _weightInElevator += passenger.Weight;
                    _passengersInElevatorList.Add(passenger);
                }
                _isAtCapacity = _weightInElevator >= 300;
                OnElevatorWeightUpdated?.Invoke(_weightInElevator);
            }
            
            _elevatorDestinationQueue = _elevatorDestinationQueue.Distinct().ToList();
            if (_elevatorDestinationQueue.IsNullOrEmpty()) return;
            HandleElevatorDirection();
        }

        private bool IsPassengerGoingToExit(int floorIndex)
        {
            foreach (Passenger passenger in _passengersInElevatorList)
            {
                if (passenger.Destination == floorIndex) return true;
            }
            return false;
        }

        private bool IsVipOnBoard()
        {
            foreach (Passenger passenger in _passengersInElevatorList)
            {
                if (passenger.IsVip) return true;
            }
            return false;
        }
        
        private void HandleElevatorDirection(float delay = 1.5f, bool skip = false)
        {
            if(_boardingTween.IsActive()) _boardingTween.Kill();
            if(_elevatorDestinationQueue.IsNullOrEmpty()) return;
            int index = skip ? 1 : 0;
            int targetFloor = _elevatorDestinationQueue[index];
            _boardingTween = DOVirtual.DelayedCall(delay, () => {
                if (targetFloor > _currentFloorIndex) GoUp();
                else if (targetFloor < _currentFloorIndex) GoDown();
                else
                {
                    if(!IsPassengerGoingToExit(_currentFloorIndex)) return;
                    RemoveFromElevatorQueue(targetFloor);
                    _isOpen = true;
                    _floorManager.CurrentFloor.OpenElevatorDoor();
                    _floorManager.CurrentFloor.LoadPassengers();
                }
            });
        }

        public static int GetTotalWeight()
        {
            return _instance._weightInElevator;
        }


        private void OnPassengerCreatedEvent(Passenger passenger, int passengerFloor)
        {
            if (passengerFloor == _currentFloorIndex && !_floorManager.CurrentFloor.IsOpen && !_isMoving && !_isIdle)
            {
                DOVirtual.DelayedCall(0.5f,() => 
                {
                    AddToElevatorQueue(passengerFloor);
                    _elevatorDestinationQueue = _elevatorDestinationQueue.Distinct().ToList();
                    HandleElevatorDirection();
                });
                return;
            }
            AddToElevatorQueue(passengerFloor);
            _elevatorDestinationQueue = _elevatorDestinationQueue.Distinct().ToList();
            if (_isMoving) return;
            if (_isIdle)
            {
                if(passengerFloor != _currentFloorIndex) HandleElevatorDirection(0);
                else
                {
                    RemoveFromElevatorQueue(passengerFloor);
                    _isIdle = false;
                    _isOpen = true;
                    _floorManager.CurrentFloor.OpenElevatorDoor();
                    _floorManager.CurrentFloor.LoadPassengers();
                }
                return;
            }
            if (passengerFloor != _currentFloorIndex) HandleElevatorDirection();

            if (passengerFloor != _currentFloorIndex && !_isMoving) return;
            if (passengerFloor == _currentFloorIndex && _floorManager.CurrentFloor.IsOpen)
            {
                RemoveFromElevatorQueue(passengerFloor);
            }
        }
        private void GoUp()
        {
            if (_currentFloorIndex >= 2) return;
            SetElevatorState(ElevatorStates.GoingUp);
            MoveToFloor(_currentFloorIndex+1);
        }
        private void GoDown()
        {
            if (_currentFloorIndex <= 0)
            {
                _isMoving = false;
                _isIdle = true;
                OnElevatorStopped?.Invoke();
                return;
            }
            SetElevatorState(ElevatorStates.GoingDown);
            MoveToFloor(_currentFloorIndex-1);
        }
        

        private void MoveToFloor(int floor)
        {
            Floor targetFloor = FloorManager.GetFloorByIndex(floor);
            float distance = Vector3.Distance(transform.position, targetFloor.transform.position);
            _isMoving = true;
            _isOpen = false;
            _isIdle = false;
            if(_currentState == ElevatorStates.GoingUp) _currentFloorIndex++;
            else _currentFloorIndex--;
            OnElevatorVipOccupied?.Invoke(IsVipInside());
            _elevatorTween = transform.DOMoveY(targetFloor.transform.position.y, distance / m_ElevatorSpeed).OnComplete(() => {
                OnElevatorFloorChanged?.Invoke(_currentFloorIndex);
                _floorManager.OnReachedAtFloor(_currentFloorIndex);
                // _currentFloorIndex = _currentState == ElevatorStates.GoingUp ? _currentFloorIndex++ : _currentFloorIndex--;
                if (_isAtCapacity 
                    && !IsPassengerGoingToExit(floor) && !FloorManager.GetFloorByIndex(floor).IsVipInQueue()) 
                {
                    if (_elevatorDestinationQueue.Contains(floor))
                    {
                        RemoveFromElevatorQueue(_currentFloorIndex);
                        HandleElevatorDirection(0);
                        AddToElevatorQueue(_currentFloorIndex);
                        return;
                    }
                    else
                    {
                        HandleElevatorDirection(0);
                        return;
                    }

                }
                if (_elevatorDestinationQueue.Contains(floor))
                {
                    _isMoving = false;
                    _floorManager.OnStoppedAtFloor(_currentFloorIndex);
                    _isOpen = true;
                    OnElevatorStopped?.Invoke();
                    _floorManager.CurrentFloor.LoadPassengers();
                    RemoveFromElevatorQueue(floor);
                    return;
                }
                if (_elevatorDestinationQueue.IsNullOrEmpty())
                {
                    if (_currentState == ElevatorStates.GoingUp) GoUp();
                    else GoDown();
                }
                else HandleElevatorDirection(0f);

            });
        }
        private void RemoveFromElevatorQueue(int floor)
        {
            _elevatorDestinationQueue.Remove(floor);
        }

        private void SetElevatorState(ElevatorStates state)
        {
            _currentState = state;
            OnElevatorStateChanged?.Invoke(state);
        }
        public static bool IsAtCapacity()
        {
            return _instance._isAtCapacity;
        }
        public static void AddToElevatorQueue(int index)
        {
            _instance._elevatorDestinationQueue.Add(index);
        }
        public static bool IsVipInside()
        {
            return _instance.IsVipInsideInternal();
        }
        public static bool IsDpInside()
        {
            return _instance.IsDpInsideInternal();
        }
        private bool IsVipInsideInternal()
        {
            foreach (Passenger passenger in _passengersInElevatorList)
            {
                if (passenger.IsVip) return true;
            }
            return false;
        }
        private bool IsDpInsideInternal()
        {
            foreach (Passenger passenger in _passengersInElevatorList)
            {
                if (!passenger.IsVip) return true;
            }
            return false;
        }

        public static List<Passenger> GetLatestDps()
        {
            return _instance.GetLatestDpsInternal();
        }
        private List<Passenger> GetLatestDpsInternal()
        {
            List<Passenger> dpList = new();
            for (int i = 0; i < _passengersInElevatorList.Count; i++)
            {
                Passenger passenger = _passengersInElevatorList[_passengersInElevatorList.Count-1-i];
                if (!passenger.IsVip) dpList.Add(passenger);
            }
            return dpList;
        }
        public static int GetTotalDroppableWeight()
        {
            return _instance.GetTotalDroppableWeightInternal();
        }
        private int GetTotalDroppableWeightInternal()
        {
            int totalWeight = 0;
            foreach (Passenger passenger in GetLatestDpsInternal())
            {
                totalWeight += passenger.Weight;
            }
            return totalWeight;
        }
    }
    public enum ElevatorStates
    {
        GoingUp,
        GoingDown
    }
}