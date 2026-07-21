using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>Visual representation of one selectable bonus power-up.</summary>
    public class BoosterUINode : MonoBehaviour
    {
        public Button selectButton;
        public Image boosterIcon;
        public TMP_Text boosterNameText;

        public ElementPowerUpType PowerUpType { get; private set; }

        public void Initialize(ElementPowerUpType powerUpType, Action<ElementPowerUpType> onSelected)
        {
            PowerUpType = powerUpType;

            ElementData powerUpData = GetPowerUpData(powerUpType);
            if (boosterIcon != null)
                boosterIcon.sprite = powerUpData != null ? powerUpData.displayIcon : null;
            if (boosterNameText != null)
                boosterNameText.text = powerUpType == ElementPowerUpType.Rocket ? "Rocket" : powerUpData != null ? powerUpData.displayName : powerUpType.ToString();

            if (selectButton == null)
                return;

            selectButton.onClick.RemoveAllListeners();
            selectButton.onClick.AddListener(() => onSelected?.Invoke(PowerUpType));
        }

        private static ElementData GetPowerUpData(ElementPowerUpType powerUpType)
        {
            switch (powerUpType)
            {
                case ElementPowerUpType.Bomb:
                    return EditorAddressables.bombData;
                case ElementPowerUpType.Rocket:
                case ElementPowerUpType.HorizontalRocket:
                    return EditorAddressables.horizontalRocketData;
                case ElementPowerUpType.VerticalRocket:
                    return EditorAddressables.verticalRocketData;
                case ElementPowerUpType.Propeller:
                    return EditorAddressables.propellerData;
                case ElementPowerUpType.DiscoBall:
                    return EditorAddressables.discoBallData;
                default:
                    return null;
            }
        }
    }
}
