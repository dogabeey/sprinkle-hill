using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using Game.SimpleJSON;

namespace Game
{
    public partial class CurrencyManager : SingletonComponent<CurrencyManager>, ISaveable
    {
        [Serializable]
        [InlineEditor]
        public class CurrencyInfo
        {
            public CurrencyModel currencyModel;
            public float amount;
        }

        [Header("References")]
        public Transform currencyContainer;
        public CanvasGroup currencyCanvasGroup;
        public CurrencyElement currencyElementPrefab;
        public List<CurrencyInfo> currencyInfos;

        [Header("Animation Settings")]
        public Transform currencyAnimationContainer;
        public Image currencySpritePrefab;
        public float flightDuration = 0.1f;
        public float currencySpriteMultiplier = 10f;

        private readonly List<CurrencyElement> currencyElements = new List<CurrencyElement>();

        public string SaveId => "currency_management";

        public SaveDataType SaveDataType => SaveDataType.WorldProgression;

        private void OnEnable()
        {
            EventManager.StartListening(GameEvent.LEVEL_STARTED, OnLevelStarted);
            EventManager.StartListening(GameEvent.LEVEL_COMPLETED, OnLevelCompleted);
            EventManager.StartListening(GameEvent.LEVEL_FAILED, OnLevelCompleted);
            EventManager.StartListening(GameEvent.SCREEN_OPENED, OnScreenOpened);
            EventManager.StartListening(GameEvent.SCREEN_CLOSED, OnScreenClosed);
        }
        private void OnDisable()
        {
            EventManager.StopListening(GameEvent.LEVEL_STARTED, OnLevelStarted);
            EventManager.StopListening(GameEvent.LEVEL_COMPLETED, OnLevelCompleted);
            EventManager.StopListening(GameEvent.LEVEL_FAILED, OnLevelCompleted);
            EventManager.StopListening(GameEvent.SCREEN_OPENED, OnScreenOpened);
            EventManager.StopListening(GameEvent.SCREEN_CLOSED, OnScreenClosed);
        }
        private void OnLevelStarted(EventParam e)
        {
            currencyCanvasGroup.alpha = 0;
        }
        private void OnLevelCompleted(EventParam e)
        {
            currencyCanvasGroup.alpha = 1;
        }

        private void OnScreenOpened(EventParam e)
        {
            currencyCanvasGroup.alpha = 1;
        }

        private void OnScreenClosed(EventParam e)
        {
            currencyCanvasGroup.alpha = 0;
        }

        private void Start()
        {
            SaveManager.Instance.Register(this);
            currencyElements.Clear();

            foreach (var currencyInfo in currencyInfos)
            {
                CurrencyElement instantiatedElement = Instantiate(currencyElementPrefab, currencyContainer);
                instantiatedElement.currencyTransform = instantiatedElement.transform;
                instantiatedElement.currencyText = instantiatedElement.GetComponentInChildren<TMP_Text>();
                StartCoroutine(instantiatedElement.UpdateCurrencyUI(currencyInfo.currencyModel, currencyInfo.amount));
                currencyElements.Add(instantiatedElement);
            }
        }

        public void AddCurrency(CurrencyModel currencyModel, float amount, GameObject source = null)
        {
            StartCoroutine(AddCurrencyCoroutine(currencyModel, amount, source));
        }

        public IEnumerator AddCurrencyCoroutine(CurrencyModel currencyModel, float amount, GameObject source = null)
        {
            CurrencyInfo currencyInfo = currencyInfos.Find(x => x.currencyModel != null && x.currencyModel == currencyModel);

            CurrencyElement element = currencyElements.Find(x => x.refCurrency != null && x.refCurrency == currencyModel);

            if (source != null && element != null)  
            {
                Vector3 sourceScreenPos = source.transform.position;
                yield return StartCoroutine(AddCurrencyAnimationCoroutine(currencyInfo, sourceScreenPos, element.currencyTransform.position, amount));
            }

            currencyInfo.amount += amount;
            NotifyCurrencyChanged(currencyInfo.currencyModel, amount);
        }


        private IEnumerator AddCurrencyAnimationCoroutine(CurrencyInfo currencyInfo, Vector3 sourcePosition, Vector3 targetPosition, float amount)
        {
            int spriteAmount = Mathf.Max(1, Mathf.CeilToInt(Mathf.Abs(amount) * currencySpriteMultiplier));
            spriteAmount = Mathf.Min(spriteAmount, 30);
            float startAmount = currencyInfo.amount;

            if (currencySpritePrefab != null && currencyAnimationContainer != null)
            {
                float spawnStep = 0.02f;
                float totalAnimDuration = flightDuration;

                for (int i = 0; i < spriteAmount; i++)
                {
                    Image spriteInstance = Instantiate(currencySpritePrefab, currencyAnimationContainer);
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
                        .SetEase(Ease.Linear)
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

            currencyInfo.amount = startAmount + amount;
            NotifyCurrencyChanged(currencyInfo.currencyModel, amount);

            yield break;
        }

        private static void NotifyCurrencyChanged(CurrencyModel currencyModel, float amount)
        {
            EventManager.TriggerEvent(GameEvent.CURRENCY_CHANGED, new EventParam
            {
                paramScriptable = currencyModel,
                paramFloat = amount
            });
        }

        internal float GetCurrencyAmount(CurrencyModel costCurrency)
        {
            CurrencyInfo info = currencyInfos.Find(x => x.currencyModel == costCurrency);
            return info != null ? info.amount : 0f;
        }

        internal Sprite GetCurrencySprite(CurrencyModel costCurrency)
        {
            return costCurrency.currencyIcon;
        }

        public Dictionary<string, object> Save()
        {
            var saveData = new Dictionary<string, object>();
            foreach (var currencyInfo in currencyInfos)
            {
                if (currencyInfo.currencyModel != null)
                {
                    saveData[currencyInfo.currencyModel.currencyID] = currencyInfo.amount;
                }
            }

            return saveData;
        }

        public bool Load(Action onLoadSuccess, Action onLoadFail)
        {
            JSONNode saveData = GameManager.Instance.saveManager.LoadSave(this);

            if (saveData == null)
            {
                onLoadFail?.Invoke();
                return false;
            }

            foreach (var currencyInfo in currencyInfos)
            {
                if (currencyInfo.currencyModel != null)
                {
                    string currencyID = currencyInfo.currencyModel.currencyID;
                    if (saveData[currencyID] != null)
                    {
                        currencyInfo.amount = saveData[currencyID].AsFloat;
                    }
                }
            }
            return true;
        }
    }
}
