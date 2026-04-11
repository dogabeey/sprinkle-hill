using UnityEngine;

namespace MobileHapticsProFreeEdition
{
    public class SelfDestroy : MonoBehaviour
    {
        public float destructionDelay;

        private void OnBecameVisible()
        {
            // Set destructionDelay to a random value between 1 and 5 seconds
            destructionDelay = Random.Range(1f, 5f);

            // Destroy the game object after the random delay
            Destroy(gameObject, destructionDelay);
        }
    }
}