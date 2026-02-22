using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using TMPro;

namespace Game
{
    public partial class CurrencyManager : SingletonComponent<CurrencyManager>
    {
        [Serializable]
        public class CurrencyInfo
        {
            public CurrencyModel currencyModel;
            public Transform currencyTransform;
            public TMP_Text currencyText;
            public float Amount { get => PlayerPrefs.GetFloat("Currency_" + currencyModel.currencyID, currencyModel.startingAmount); set => PlayerPrefs.SetFloat("Currency_" + currencyModel.currencyID, value); }
        }

        [Header("Coin")]
        public TMP_Text coinText;
        public Transform coinTransform;
        public SpriteRenderer coinSpritePrefab;
        public List<CurrencyInfo> currencyInfos;
        [Header("Animation Settings")]
        public float flightDuration = 0.1f;
        public float coinSpriteMultiplier = 10;

        public float Coin { 
            get => PlayerPrefs.GetFloat("Coin", 0);
            set
            {
                PlayerPrefs.SetFloat("Coin", value);
                UpdateCoinText();
            }
        }

        private void Start()
        {
            UpdateCoinText();
            UpdateCurrencyText();
        }
        private void Update()
        {
            UpdateCurrencyText();
        }


        public void AddCoin(float coinAmount, GameObject source = null)
        {
            if(source != null)
            {
                AddCoinAnimation(source.transform.position, coinTransform.position, coinAmount);
            }
        }
        public void AddCurrency(string currencyID, float premiumCurrencyAmount, GameObject source = null)
        {
            CurrencyInfo currencyModel = currencyInfos.Find(x => x.currencyModel.currencyID == currencyID);
            if (source != null)
            {
                AddCoinAnimation(source.transform.position, currencyModel.currencyTransform.position, premiumCurrencyAmount);
            }

            currencyModel.Amount += premiumCurrencyAmount;
        }
        
        private void AddCoinAnimation(Vector3 sourcePosition, Vector3  targetPosition, float coinAmount)
        {
            StartCoroutine(AddCoinAnimationCoroutine(sourcePosition, targetPosition, coinAmount));
        }
        private IEnumerator AddCoinAnimationCoroutine(Vector3 sourcePosition, Vector3 targetPosition, float coinAmount)
        {
            int coinSpriteAmount = Coin == 0 ? Mathf.CeilToInt((Coin + coinAmount * coinSpriteMultiplier) / 100) : Mathf.CeilToInt((Coin + coinAmount * coinSpriteMultiplier) / Coin);
            for (int i = 0; i < coinSpriteAmount; i++)
            {
                SpriteRenderer coinSprite = Instantiate(coinSpritePrefab, sourcePosition, Quaternion.identity);
                coinSprite.transform.SetParent(coinTransform);
                yield return coinSprite.transform.DOMove(targetPosition, flightDuration).SetEase(Ease.InOutQuad).WaitForCompletion();
                Coin += coinAmount / (float) coinSpriteAmount;
                Destroy(coinSprite.gameObject);
            }

            yield break;
        }

        void UpdateCoinText()
        {
            coinText.text = Mathf.FloorToInt(Coin).ToString();

        }
        private void UpdateCurrencyText()
        {
            foreach (var currencyModel in currencyInfos)
            {
                currencyModel.currencyText.text = Mathf.FloorToInt(currencyModel.Amount).ToString();
            }
        }

        internal float GetCurrencyAmount(CurrencyModel costCurrency)
        {
            return currencyInfos.Find(x => x.currencyModel == costCurrency).Amount;
        }
    }
}
