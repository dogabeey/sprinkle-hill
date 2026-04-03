using UnityEngine;
using TMPro;

namespace Game
{
    [CreateAssetMenu(fileName = "CurrencyModel", menuName = "Scriptable Objects/Currency Model")]
    public class CurrencyModel : ScriptableObject
    {
        public string currencyID;
        public float startingAmount;
        public Sprite currencyIcon;
        public string showFormat = "0.##";
        [Tooltip("Used for TMP_Text with sprites. Set to the index of the sprite in the TMP Sprite Asset.")]
        public int spriteIndexForUI = 0;
    }
}
