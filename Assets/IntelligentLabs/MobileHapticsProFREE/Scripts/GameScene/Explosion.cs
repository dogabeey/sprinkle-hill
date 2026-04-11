using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace MobileHapticsProFreeEdition
{
    public class Explosion : MonoBehaviour
    {
        Rigidbody rb;

        public float explosionForce = 3500f; // Adjust the force magnitude
        public float explosionRadius = 5f; // Adjust the radius of the explosion
        public float upwardsModifier = 0f; // Adjust the vertical push

        private void OnBecameVisible()
        {
            rb = GetComponent<Rigidbody>();
            rb.AddExplosionForce(explosionForce, transform.position - new Vector3(0, 0, 1), explosionRadius, upwardsModifier, ForceMode.Acceleration);
        }

        private void OnBecameInvisible()
        {
            // When gameObject gets out of the screen -> Destroy it
            Destroy(gameObject);
        }
    }
}