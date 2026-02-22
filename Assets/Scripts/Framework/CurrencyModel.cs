using UnityEngine;
using TMPro;

namespace Game
{
    [CreateAssetMenu(fileName = "CurrencyModel", menuName = "Scriptable Objects/Currency Model")]
    public class CurrencyModel : ScriptableObject
    {
        public string currencyID;
        public float startingAmount;
        public SpriteRenderer currencySpritePrefab;
    }
}
