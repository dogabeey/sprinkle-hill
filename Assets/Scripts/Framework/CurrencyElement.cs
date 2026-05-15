using DG.Tweening;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;

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
                StartCoroutine(UpdateCurrencyUI(refCurrency, param.paramFloat));
            }
        }

        private Tween currencyTextTween;
        public IEnumerator UpdateCurrencyUI(CurrencyModel currency, float amount)
        {
            refCurrency = currency;

            float finalAmount = CurrencyManager.Instance.GetCurrencyAmount(currency);
            if (currencyTextTween != null && currencyTextTween.IsActive())
            {
                currencyTextTween.Kill();
            }
            if(amount < 0)
                currencyText.text = $"<sprite index={currency.spriteIndexForUI}> {(finalAmount + amount).ToLargeNumberString()} (<color=red>-{(-amount).ToLargeNumberString()})";
            else
                currencyText.text = $"<sprite index={currency.spriteIndexForUI}> {(finalAmount + amount).ToLargeNumberString()} (<color=green>+{amount.ToLargeNumberString()})";
            
            yield return new WaitForSeconds(0.5f);
            currencyTextTween = DOVirtual.Float(amount, finalAmount, 0.5f, (value) =>
            {
                string formattedAmount = value.ToLargeNumberString();
                currencyText.text = $"<sprite index={currency.spriteIndexForUI}>" + formattedAmount;
            });
        }

        
    }
    public static class NumberFormatter
    {
        public static string ToLargeNumberString(this float value)
        {
            float absValue = Mathf.Abs(value);

            if (absValue >= 1_000_000_000f)
            {
                return (value / 1_000_000_000f).ToString("0.##", CultureInfo.InvariantCulture) + "B";
            }

            if (absValue >= 1_000_000f)
            {
                return (value / 1_000_000f).ToString("0.##", CultureInfo.InvariantCulture) + "M";
            }

            if (absValue >= 1_000f)
            {
                return (value / 1_000f).ToString("0.##", CultureInfo.InvariantCulture) + "K";
            }

            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }
    }
}
