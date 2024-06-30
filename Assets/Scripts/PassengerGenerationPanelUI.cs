using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ElevatorSystem
{
    public class PassengerGenerationPanelUI : MonoBehaviour
    {
        [SerializeField] private TMP_Dropdown m_DestinationInput; 
        [SerializeField] private TMP_InputField m_WeightInput;
        private int _destination;
        private int _weight = 50;
        [SerializeField] private Button m_VipButton;
        [SerializeField] private Button m_DpButton;
        [SerializeField] private Floor m_Floor;
        

        private void Start()
        {
            m_VipButton.onClick.AddListener(() => 
            {
                _destination = m_DestinationInput.value;
                if (string.IsNullOrEmpty(m_WeightInput.text) || m_DestinationInput.value == m_Floor.Index) return;
                _weight =  Int32.Parse(m_WeightInput.text);
                PassengerController.CreateVip(m_Floor.Index, _destination, _weight);
            });
            
            m_DpButton.onClick.AddListener(() => 
            {
                _destination = m_DestinationInput.value;
                if (string.IsNullOrEmpty(m_WeightInput.text) ||  m_DestinationInput.value == m_Floor.Index) return;
                _weight =  Int32.Parse(m_WeightInput.text);
                PassengerController.CreateDp(m_Floor.Index, _destination, _weight);
            });
        }

    }
}
