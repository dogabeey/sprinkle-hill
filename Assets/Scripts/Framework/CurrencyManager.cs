using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;
using DG.Tweening;
using TMPro;
using Sirenix.OdinInspector;
using UnityEngine.UI;
using Game.SimpleJSON;
using Game.Singleton;
using Game.EventManagement;

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
        public List<CurrencyInfo> currencyInfos;

        [Header("Animation Settings")]
        public Transform currencyAnimationContainer;
        public Image currencySpritePrefab;
        public float flightDuration = 0.1f;
        public float currencySpriteMultiplier = 10f;

        private readonly List<CurrencyElement> currencyElements = new List<CurrencyElement>();
        private readonly Dictionary<CurrencyModel, DateTime> currencyTimerStartUtc = new Dictionary<CurrencyModel, DateTime>();

        public string SaveId => "currency_management";

        public SaveDataType SaveDataType => SaveDataType.WorldProgression;

        protected override void Awake()
        {
            base.Awake();
            SaveManager.Instance.Register(this);

            if (!Load(null, null))
            {
                ApplyDefaultCurrencyAmounts();
            }

            InitializeCurrencyTimers();
            ProcessCurrencyTimers();
        }

        private void OnEnable()
        {
            EventManager.StartListening(GameEvent.LEVEL_STARTED, OnLevelStarted);
            EventManager.StartListening(GameEvent.LEVEL_COMPLETED, OnLevelCompleted);
            EventManager.StartListening(GameEvent.LEVEL_FAILED, OnLevelCompleted);
            EventManager.StartListening(GameEvent.SCREEN_OPENED, OnScreenOpened);
            EventManager.StartListening(GameEvent.SCREEN_CLOSED, OnScreenClosed);
            EventManager.StartListening(GameEvent.GAME_STATE_CHANGED, OnGameStateChanged);
        }
        private void OnDisable()
        {
            EventManager.StopListening(GameEvent.LEVEL_STARTED, OnLevelStarted);
            EventManager.StopListening(GameEvent.LEVEL_COMPLETED, OnLevelCompleted);
            EventManager.StopListening(GameEvent.LEVEL_FAILED, OnLevelCompleted);
            EventManager.StopListening(GameEvent.SCREEN_OPENED, OnScreenOpened);
            EventManager.StopListening(GameEvent.SCREEN_CLOSED, OnScreenClosed);
            EventManager.StopListening(GameEvent.GAME_STATE_CHANGED, OnGameStateChanged);
        }
        private void OnLevelStarted(EventParam e)
        {
            UpdateCurrencyCanvasVisibility();
        }
        private void OnLevelCompleted(EventParam e)
        {
            UpdateCurrencyCanvasVisibility();
        }

        private void OnScreenOpened(EventParam e)
        {
            UpdateCurrencyCanvasVisibility();
        }

        private void OnScreenClosed(EventParam e)
        {
            UpdateCurrencyCanvasVisibility();
        }

        private void OnGameStateChanged(EventParam e)
        {
            UpdateCurrencyCanvasVisibility();
        }

        private void UpdateCurrencyCanvasVisibility()
        {
            if (currencyCanvasGroup == null)
                return;

            GameManager gameManager = GameManager.Instance;
            bool isOnMainMenu = gameManager != null && gameManager.CurrentGameState == GameState.Overworld;
            bool isOnLevelEndPanel = gameManager != null && gameManager.CurrentWorld != null && gameManager.CurrentLevel != null && gameManager.CurrentLevel.isEnded;
            currencyCanvasGroup.alpha = isOnMainMenu || isOnLevelEndPanel ? 1f : 0f;
        }

        private void Start()
        {
            UpdateCurrencyCanvasVisibility();
            currencyElements.Clear();

            foreach (CurrencyInfo currencyInfo in currencyInfos)
            {
                CurrencyElement instantiatedElement = Instantiate(currencyInfo.currencyModel.currencyElementPrefab, currencyContainer);
                instantiatedElement.currencyTransform = instantiatedElement.transform;
                if (instantiatedElement.currencyText == null)
                    instantiatedElement.currencyText = instantiatedElement.GetComponentInChildren<TMP_Text>();
                StartCoroutine(instantiatedElement.UpdateCurrencyUI(currencyInfo.currencyModel, currencyInfo.amount));
                currencyElements.Add(instantiatedElement);
            }
        }

        private void Update()
        {
            ProcessCurrencyTimers();
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
                ProcessCurrencyTimers();
        }

        private void ApplyDefaultCurrencyAmounts()
        {
            if (currencyInfos == null)
                return;

            for (int i = 0; i < currencyInfos.Count; i++)
            {
                CurrencyInfo currencyInfo = currencyInfos[i];
                if (currencyInfo?.currencyModel == null)
                    continue;

                currencyInfo.amount = currencyInfo.currencyModel.startingAmount;
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

            bool animatedReward = source != null && element != null;
            if (animatedReward)
            {
                Vector3 sourceScreenPos = source.transform.position;
                yield return StartCoroutine(AddCurrencyAnimationCoroutine(currencyInfo, sourceScreenPos, element.currencyTransform.position, amount));
            }

            if (animatedReward)
                yield break;


            float previousAmount = currencyInfo.amount;
            currencyInfo.amount += amount;
            UpdateCurrencyTimerAfterAmountChanged(currencyInfo, previousAmount);
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

            float previousAmount = currencyInfo.amount;
            currencyInfo.amount = startAmount + amount;
            UpdateCurrencyTimerAfterAmountChanged(currencyInfo, previousAmount);
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

        internal bool TryGetRemainingCurrencyCooldown(CurrencyModel currencyModel, out TimeSpan remainingCooldown)
        {
            remainingCooldown = TimeSpan.Zero;
            CurrencyInfo currencyInfo = currencyInfos.Find(info => info.currencyModel == currencyModel);
            if (currencyInfo == null || !IsCurrencyTimerActive(currencyInfo))
                return false;

            DateTime timerStart = GetOrCreateCurrencyTimerStart(currencyModel, DateTime.UtcNow);
            double cooldownSeconds = GetCurrencyCooldownSeconds(currencyModel);
            double elapsedSeconds = Math.Max(0d, (DateTime.UtcNow - timerStart).TotalSeconds);
            remainingCooldown = TimeSpan.FromSeconds(Math.Max(0d, cooldownSeconds - elapsedSeconds));
            return true;
        }

        public Dictionary<string, object> Save()
        {
            var saveData = new Dictionary<string, object>();
            foreach (var currencyInfo in currencyInfos)
            {
                if (currencyInfo.currencyModel != null)
                {
                    saveData[currencyInfo.currencyModel.currencyID] = currencyInfo.amount;
                    if (currencyTimerStartUtc.TryGetValue(currencyInfo.currencyModel, out DateTime timerStart))
                        saveData[GetCurrencyTimerSaveKey(currencyInfo.currencyModel)] = timerStart.Ticks.ToString(CultureInfo.InvariantCulture);
                }
            }

            return saveData;
        }

        public bool Load(Action onLoadSuccess, Action onLoadFail)
        {
            JSONNode saveData = SaveManager.Instance.LoadSave(this);

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

                    string timerSaveKey = GetCurrencyTimerSaveKey(currencyInfo.currencyModel);
                    if (saveData[timerSaveKey] != null && long.TryParse(saveData[timerSaveKey].Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out long timerTicks))
                    {
                        try
                        {
                            currencyTimerStartUtc[currencyInfo.currencyModel] = new DateTime(timerTicks, DateTimeKind.Utc);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            // Invalid local timestamp; a fresh timer is created during initialization.
                        }
                    }
                }
            }

            onLoadSuccess?.Invoke();
            return true;
        }

        private void InitializeCurrencyTimers()
        {
            if (currencyInfos == null)
                return;

            DateTime now = DateTime.UtcNow;
            foreach (CurrencyInfo currencyInfo in currencyInfos)
            {
                if (currencyInfo?.currencyModel != null && currencyInfo.currencyModel.isNewCurrencyTimerEnabled)
                    GetOrCreateCurrencyTimerStart(currencyInfo.currencyModel, now);
            }
        }

        private void ProcessCurrencyTimers()
        {
            if (currencyInfos == null)
                return;

            DateTime now = DateTime.UtcNow;
            foreach (CurrencyInfo currencyInfo in currencyInfos)
            {
                if (!IsCurrencyTimerActive(currencyInfo))
                    continue;

                CurrencyModel currencyModel = currencyInfo.currencyModel;
                DateTime timerStart = GetOrCreateCurrencyTimerStart(currencyModel, now);
                double cooldownSeconds = GetCurrencyCooldownSeconds(currencyModel);
                double elapsedSeconds = Math.Max(0d, (now - timerStart).TotalSeconds);
                int earnedAmount = Mathf.FloorToInt((float)(elapsedSeconds / cooldownSeconds));
                if (earnedAmount <= 0)
                    continue;

                int availableAmount = Mathf.FloorToInt(currencyModel.maxAmount - currencyInfo.amount);
                int grantedAmount = Mathf.Min(earnedAmount, availableAmount);
                if (grantedAmount <= 0)
                {
                    currencyTimerStartUtc[currencyModel] = now;
                    continue;
                }

                currencyInfo.amount += grantedAmount;
                currencyTimerStartUtc[currencyModel] = currencyInfo.amount >= currencyModel.maxAmount
                    ? now
                    : timerStart.AddSeconds(grantedAmount * cooldownSeconds);
                NotifyCurrencyChanged(currencyModel, grantedAmount);
            }
        }

        private void UpdateCurrencyTimerAfterAmountChanged(CurrencyInfo currencyInfo, float previousAmount)
        {
            if (currencyInfo?.currencyModel == null || !currencyInfo.currencyModel.isNewCurrencyTimerEnabled)
                return;

            DateTime now = DateTime.UtcNow;
            CurrencyModel currencyModel = currencyInfo.currencyModel;
            if (currencyInfo.amount >= currencyModel.maxAmount || previousAmount >= currencyModel.maxAmount)
                currencyTimerStartUtc[currencyModel] = now;
            else
                GetOrCreateCurrencyTimerStart(currencyModel, now);
        }

        private static bool IsCurrencyTimerActive(CurrencyInfo currencyInfo)
        {
            return currencyInfo?.currencyModel != null &&
                   currencyInfo.currencyModel.isNewCurrencyTimerEnabled &&
                   currencyInfo.currencyModel.newCurrencyTimer > 0f &&
                   currencyInfo.currencyModel.maxAmount > currencyInfo.amount;
        }

        private static double GetCurrencyCooldownSeconds(CurrencyModel currencyModel)
        {
            return Math.Max(0.01d, currencyModel.newCurrencyTimer * 60d);
        }

        private DateTime GetOrCreateCurrencyTimerStart(CurrencyModel currencyModel, DateTime now)
        {
            if (!currencyTimerStartUtc.TryGetValue(currencyModel, out DateTime timerStart))
            {
                timerStart = now;
                currencyTimerStartUtc[currencyModel] = timerStart;
            }

            if (timerStart > now)
            {
                // The device clock moved backwards; restart the local cooldown from the current time.
                timerStart = now;
                currencyTimerStartUtc[currencyModel] = timerStart;
            }

            return timerStart;
        }

        private static string GetCurrencyTimerSaveKey(CurrencyModel currencyModel)
        {
            return $"{currencyModel.currencyID}_currencyTimerStartUtcTicks";
        }
    }
}
