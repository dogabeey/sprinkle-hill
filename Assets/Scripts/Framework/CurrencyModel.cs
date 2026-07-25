using UnityEngine; using Game.EventManagement;
using TMPro;

namespace Game
{
    [CreateAssetMenu(fileName = "CurrencyModel", menuName = "Game/Currency Model")]
    public class CurrencyModel : ScriptableObject
    {
        public CurrencyElement currencyElementPrefab;
        public string currencyID;
        public float startingAmount;
        public Sprite currencyIcon;
        public string showFormat = "0.##";
        [Tooltip("Used for TMP_Text with sprites. Set to the index of the sprite in the TMP Sprite Asset.")]
        public int spriteIndexForUI = 0;
        [Header("Regeneration")]
        [Tooltip("Enables offline regeneration for this currency, such as a heart/life currency.")]
        public bool isNewCurrencyTimerEnabled;
        [Min(0.01f), Tooltip("Minutes required to regenerate one currency.")]
        public float newCurrencyTimer = 30f;
        [Min(1f), Tooltip("Maximum amount regeneration can restore this currency to.")]
        public float maxAmount = 5f;
        public string regenTextFormat = "{0:00}:{1:00}";
    }
}
