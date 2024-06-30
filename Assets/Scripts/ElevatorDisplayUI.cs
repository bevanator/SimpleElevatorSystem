using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
namespace ElevatorSystem
{
    public class ElevatorDisplayUI : MonoBehaviour
    {
        [SerializeField] private Image m_UpArrow;
        [SerializeField] private Image m_DownArrow;
        [SerializeField] private TextMeshProUGUI m_FloorLabel;
        [SerializeField] private TextMeshProUGUI m_VipInfo;
        [SerializeField] private TextMeshProUGUI m_WeightInfo;

        private void Awake()
        {
            DisableBoth();
            SetWeightLabel(0);
        }
        private void OnEnable()
        {
            ElevatorController.OnElevatorStateChanged += OnElevatorStateChanged;
            ElevatorController.OnElevatorFloorChanged += OnElevatorFloorChanged;
            ElevatorController.OnElevatorStopped += OnElevatorStopped;
            ElevatorController.OnElevatorWeightUpdated += OnElevatorWeightUpdated;
            ElevatorController.OnElevatorVipOccupied += OnElevatorVipOccupied;
        }
        private void OnDisable()
        {
            ElevatorController.OnElevatorStateChanged -= OnElevatorStateChanged;
            ElevatorController.OnElevatorFloorChanged -= OnElevatorFloorChanged;
            ElevatorController.OnElevatorStopped -= OnElevatorStopped;
            ElevatorController.OnElevatorWeightUpdated -= OnElevatorWeightUpdated;
            ElevatorController.OnElevatorVipOccupied -= OnElevatorVipOccupied;            
        }
        private void OnElevatorVipOccupied(bool isOccupied)
        {
            SetVipInfo(isOccupied);
        }
        private void OnElevatorStateChanged(ElevatorStates state)
        {
           if(state == ElevatorStates.GoingUp) EnableUpArrow();
           else EnableDownArrow();
        }
        private void OnElevatorWeightUpdated(int weight)
        {
            SetWeightLabel(weight);
        }
        private void OnElevatorFloorChanged(int floorIndex)
        {
            SetFloorLabel(floorIndex);
        }
        private void OnElevatorStopped()
        {
            DisableBoth();
        }

        private void EnableUpArrow()
        {
            m_UpArrow.enabled = true;
            m_DownArrow.enabled = false;
        }
        private void SetVipInfo(bool state)
        {
            m_VipInfo.enabled = state;
        }
        private void DisableBoth()
        {
            m_UpArrow.enabled = false;
            m_DownArrow.enabled = false;
        }

        private void EnableDownArrow()
        {
            m_DownArrow.enabled = true;
            m_UpArrow.enabled = false;
        }

        private void SetFloorLabel(int floorIndex)
        {
            m_FloorLabel.text = floorIndex == 0 ? "G" : floorIndex.ToString();
        }
        private void SetWeightLabel(int weight)
        {
            m_WeightInfo.color = weight >= 300 ? Color.red : Color.green;
            m_WeightInfo.text = weight.ToString() + " KG";
        }

    }
}