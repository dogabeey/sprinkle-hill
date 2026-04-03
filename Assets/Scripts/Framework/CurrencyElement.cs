using UnityEngine;
using TMPro;

namespace Game
{
    public class CurrencyElement : MonoBehaviour
    {
        public Transform currencyTransform;
        public TMP_Text currencyText;

        internal CurrencyModel refCurrency;

        private void OnEnable()
        {
            EventManager.StartListening(GameEvent.CURRENCY_CHANGED, OnCurrencyChanged);
        }

        private void OnDisable()
        {
            EventManager.StopListening(GameEvent.CURRENCY_CHANGED, OnCurrencyChanged);
        }

        public void OnCurrencyChanged(EventParam param)
        {
            if (param.paramScriptable == refCurrency)
            {
                UpdateCurrencyUI(refCurrency);
            }
        }

        public void UpdateCurrencyUI(CurrencyModel currency)
        {
            refCurrency = currency;

            float amount = CurrencyManager.Instance.GetCurrencyAmount(currency);
            float shortAmount = FormatCurrencyAmount(amount);
            string abbreviation = GetCurrencyAbbreviation(amount);

            currencyText.text = $"<sprite index={currency.spriteIndexForUI}>" + shortAmount.ToString(currency.showFormat) + abbreviation;
        }

        public float FormatCurrencyAmount(float amount)
        {
            if (float.IsNaN(amount) || float.IsInfinity(amount))
            {
                return 0f;
            }

            float absAmount = Mathf.Abs(amount);

            if (absAmount >= 1_000_000_000_000_000f) return amount / 1_000_000_000_000_000f; // Qa
            if (absAmount >= 1_000_000_000_000f) return amount / 1_000_000_000_000f; // T
            if (absAmount >= 1_000_000_000f) return amount / 1_000_000_000f; // B
            if (absAmount >= 1_000_000f) return amount / 1_000_000f; // M
            if (absAmount >= 1_000f) return amount / 1_000f; // K

            return amount;
        }

        private static string GetCurrencyAbbreviation(float amount)
        {
            float absAmount = Mathf.Abs(amount);

            if (absAmount >= 1_000_000_000_000_000f) return "Qa";
            if (absAmount >= 1_000_000_000_000f) return "T";
            if (absAmount >= 1_000_000_000f) return "B";
            if (absAmount >= 1_000_000f) return "M";
            if (absAmount >= 1_000f) return "K";

            return string.Empty;
        }
    }
}
