using System;
using DG.Tweening;
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;
namespace ElevatorSystem
{
    public class Passenger : MonoBehaviour
    {
        private bool _isDropped;
        private int _destination;
        private int _dropPoint;
        [ShowInInspector] private int _weight;
        private SpriteRenderer _sprite;
        private bool _isInElevator;
        [SerializeField] private TextMeshProUGUI m_InfoText;
        public int Destination => _destination;
        private FloorManager _floorManager;
        private bool _isVip;
        public bool IsVip => _isVip;
        public int Weight => _weight;
        public bool IsDropped => _isDropped;
        public int DropPoint => _dropPoint;
        private void Awake()
        {
            _floorManager = FindObjectOfType<FloorManager>();
            _sprite = GetComponent<SpriteRenderer>();
            _sprite.sortingOrder = 5;
        }
        private void OnEnable()
        {
            _floorManager.OnReachedFloorEvent += OnReachedFloorEvent;
        }
        private void OnDisable()
        {
            _floorManager.OnReachedFloorEvent -= OnReachedFloorEvent;
        }
        private void OnReachedFloorEvent(int floorIndex)
        {
            if (floorIndex != _destination || !_isInElevator) return;
            transform.SetParent(null);
            DOVirtual.DelayedCall(0.4f, () => {
                _floorManager.OnPassengerLeft(this);
                SetSortingOrder(5);
                MoveTo(_floorManager.CurrentFloor.ExitPoint.position, true, true, 0.8f, false, true);
            });
        }
        public void Init(int destination, int weight, bool isVip = false)
        {
            _destination = destination;
            _weight = weight;
            _isVip = isVip;
            string dest = (destination == 0) ? "G" : destination.ToString();
            m_InfoText.text = weight.ToString() + "(" + dest + ")";
        }
        public void MoveTo(Vector3 pos, bool animate = false, bool randomOffset = false, float duration = 0.5f, bool randomDelay = false, bool fadeOnComplete = false, bool changeSortingLayer = false)
        {
            if (!animate) transform.position = pos;
            else
            {
                if (randomOffset) pos = new Vector3(pos.x + Random.Range(-0.2f, 0.2f), pos.y, pos.z);
                float delay = randomDelay ? Random.Range(0.1f,0.4f) : 0f;
                transform.DOMove(pos, duration).SetDelay(delay).OnComplete(() => {
                    if (fadeOnComplete) _sprite.DOFade(0, 0.3f).OnComplete(() => Destroy(gameObject));
                    if (changeSortingLayer) SetSortingOrder(2);
                });
            }
        }

        public void SetInElevatorStatus(bool state)
        {
            _isInElevator = state;
        }
        public void SetDropStatus(bool state)
        {
            _isDropped = state;
        }
        public void SetDropPoint(int floor)
        {
            _dropPoint = floor;
        }
        
        public void SetSortingOrder(int order)
        {
            _sprite.sortingOrder = order;
        }
    }
}