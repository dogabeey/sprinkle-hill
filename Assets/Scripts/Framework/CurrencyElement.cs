using DG.Tweening;
using System;
using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine; 
using UnityEngine.UI;
using Game.EventManagement;

namespace Game
{
    public class CurrencyElement : MonoBehaviour
    {
        public Transform currencyTransform;
        public Image currencyImage;
        public TMP_Text currencyText;
        public TMP_Text remainingCooldownText;
        public GameObject currencyTimerBG;

        internal CurrencyModel refCurrency;

        private void OnEnable()
        {
            EventManager.StartListening(GameEvent.CURRENCY_CHANGED, OnCurrencyChanged);
            SetCooldownUIVisible(false);
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

        private void Update()
        {
            if (refCurrency == null || CurrencyManager.Instance == null)
            {
                SetCooldownUIVisible(false);
                return;
            }

            if (CurrencyManager.Instance.TryGetRemainingCurrencyCooldown(refCurrency, out TimeSpan remainingCooldown))
            {
                SetCooldownUIVisible(true);
                if (remainingCooldownText != null)
                    remainingCooldownText.text = FormatRemainingCooldown(remainingCooldown);
            }
            else
            {
                SetCooldownUIVisible(false);
            }
        }

        public IEnumerator UpdateCurrencyUI(CurrencyModel currency, float amount)
        {
            refCurrency = currency;

            float finalAmount = CurrencyManager.Instance.GetCurrencyAmount(currency);
            if (currencyTextTween != null && currencyTextTween.IsActive())
            {
                currencyTextTween.Kill();
            }
            if(amount < 0)
                currencyText.text = $"{(finalAmount - amount).ToLargeNumberString()} <color=red>\n-{(-amount).ToLargeNumberString()}";
            else
                currencyText.text = $"{(finalAmount - amount).ToLargeNumberString()} <color=green>\n+{amount.ToLargeNumberString()}";
            
            yield return new WaitForSeconds(0.1f);
            currencyTextTween = DOVirtual.Float(finalAmount - amount, finalAmount, 0.1f, (value) =>
            {
                string formattedAmount = value.ToLargeNumberString();
                currencyText.text = formattedAmount;
            });

            if(currencyImage != null)
            {
                currencyImage.sprite = currency.currencyIcon;
            }
            yield return new WaitForSeconds(0.5f);
        }

        private string FormatRemainingCooldown(TimeSpan remainingCooldown)
        {
            int totalSeconds = Mathf.Max(0, Mathf.CeilToInt((float)remainingCooldown.TotalSeconds));
            int hours = totalSeconds / 3600;
            int minutes = (totalSeconds % 3600) / 60;
            int seconds = totalSeconds % 60;
            return hours > 0 ? $"+1 <sprite index={refCurrency.spriteIndexForUI}> in {hours}:{minutes:00}:{seconds:00}" : $"{minutes:00}:{seconds:00}";
        }

        private void SetCooldownUIVisible(bool isVisible)
        {
            if (remainingCooldownText != null)
                remainingCooldownText.gameObject.SetActive(isVisible);
            if (currencyTimerBG != null)
                currencyTimerBG.SetActive(isVisible);
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
