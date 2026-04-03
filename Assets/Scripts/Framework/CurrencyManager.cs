using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;
using Sirenix.OdinInspector;

namespace Game
{
    public partial class CurrencyManager : SingletonComponent<CurrencyManager>
    {
        [Serializable]
        [InlineEditor]
        public class CurrencyInfo
        {
            public CurrencyModel currencyModel;
            public float Amount
            {
                get => PlayerPrefs.GetFloat("Currency_" + currencyModel.currencyID, currencyModel.startingAmount);
                set => PlayerPrefs.SetFloat("Currency_" + currencyModel.currencyID, value);
            }
        }

        [Header("References")]
        public Transform currencyContainer;
        public CurrencyElement currencyElementPrefab;
        public List<CurrencyInfo> currencyInfos;

        [Header("Animation Settings")]
        public Transform currencyAnimationContainer;
        public SpriteRenderer currencySpritePrefab;
        public float flightDuration = 0.1f;
        public float currencySpriteMultiplier = 10f;

        private readonly List<CurrencyElement> currencyElements = new List<CurrencyElement>();

        private void Start()
        {
            currencyElements.Clear();

            foreach (var currencyInfo in currencyInfos)
            {
                CurrencyElement instantiatedElement = Instantiate(currencyElementPrefab, currencyContainer);
                instantiatedElement.currencyTransform = instantiatedElement.transform;
                instantiatedElement.currencyText = instantiatedElement.GetComponentInChildren<TMP_Text>();
                instantiatedElement.UpdateCurrencyUI(currencyInfo.currencyModel);
                currencyElements.Add(instantiatedElement);
            }
        }

        public IEnumerator AddCurrency(string currencyID, float amount, GameObject source = null)
        {
            CurrencyInfo currencyInfo = currencyInfos.Find(x => x.currencyModel != null && x.currencyModel.currencyID == currencyID);
            if (currencyInfo == null)
            {
                Debug.LogWarning($"Currency with id '{currencyID}' not found.");
                yield break;
            }

            CurrencyElement element = currencyElements.Find(x => x.refCurrency != null && x.refCurrency.currencyID == currencyID);

            if (source != null && element != null)
            {
                yield return StartCoroutine(AddCurrencyAnimationCoroutine(currencyInfo, source.transform.position, element.currencyTransform.position, amount));
            }

            currencyInfo.Amount += amount;
            NotifyCurrencyChanged(currencyInfo.currencyModel);
        }

        private void AddCurrencyAnimation(CurrencyInfo currencyInfo, Vector3 sourcePosition, Vector3 targetPosition, float amount)
        {
            StartCoroutine(AddCurrencyAnimationCoroutine(currencyInfo, sourcePosition, targetPosition, amount));
        }

        private IEnumerator AddCurrencyAnimationCoroutine(CurrencyInfo currencyInfo, Vector3 sourcePosition, Vector3 targetPosition, float amount)
        {
            int spriteAmount = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(amount) * currencySpriteMultiplier));
            spriteAmount = Mathf.Min(spriteAmount, 30);
            float startAmount = currencyInfo.Amount;

            if (currencySpritePrefab != null && currencyAnimationContainer != null)
            {
                float spawnStep = 0.02f;
                float totalAnimDuration = flightDuration;

                for (int i = 0; i < spriteAmount; i++)
                {
                    SpriteRenderer spriteInstance = Instantiate(currencySpritePrefab, currencyAnimationContainer);
                    if (currencyInfo.currencyModel != null && currencyInfo.currencyModel.currencyIcon != null)
                    {
                        spriteInstance.sprite = currencyInfo.currencyModel.currencyIcon;
                    }

                    Vector3 randomOffset = new Vector3(UnityEngine.Random.Range(-0.2f, 0.2f), UnityEngine.Random.Range(-0.2f, 0.2f), 0f);
                    spriteInstance.transform.position = sourcePosition + randomOffset;

                    float delay = i * spawnStep;
                    float duration = Mathf.Max(0.05f, flightDuration);
                    totalAnimDuration = Mathf.Max(totalAnimDuration, delay + duration);

                    spriteInstance.transform
                        .DOMove(targetPosition, duration)
                        .SetDelay(delay)
                        .SetEase(Ease.InOutQuad)
                        .OnComplete(() =>
                        {
                            if (spriteInstance != null)
                            {
                                Destroy(spriteInstance.gameObject);
                            }
                        });
                }

                yield return new WaitForSeconds(totalAnimDuration);
            }

            currencyInfo.Amount = startAmount + amount;
            NotifyCurrencyChanged(currencyInfo.currencyModel);

            yield break;
        }

        private static void NotifyCurrencyChanged(CurrencyModel currencyModel)
        {
            EventManager.TriggerEvent(GameEvent.CURRENCY_CHANGED, new EventParam
            {
                paramScriptable = currencyModel
            });
        }

        internal float GetCurrencyAmount(CurrencyModel costCurrency)
        {
            CurrencyInfo info = currencyInfos.Find(x => x.currencyModel == costCurrency);
            return info != null ? info.Amount : 0f;
        }

        internal Sprite GetCurrencySprite(CurrencyModel costCurrency)
        {
            return costCurrency.currencyIcon;
        }
    }
}
