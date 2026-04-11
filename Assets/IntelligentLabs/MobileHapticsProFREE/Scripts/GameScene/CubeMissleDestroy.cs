using MobileHapticsProFreeEdition;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MobileHapticsProFreeEdition
{
    public class CubeMissileDestroy : MonoBehaviour
    {
        private void OnTriggerEnter(Collider other)
        {
            if (other.gameObject.CompareTag("Obstacle"))
            {
                Destroy(gameObject);
            }

            if (other.gameObject.CompareTag("SingleCube"))
            {
                // Apply a force to the other object (SingleCube)
                Rigidbody otherRb = other.GetComponent<Rigidbody>();
                if (otherRb != null)
                {
                    Vector3 hitDirection = other.transform.position - transform.position; // Direction from missile to SingleCube
                    hitDirection = hitDirection.normalized; // Normalize the direction vector

                    float impactForce = 325f; // Adjust the force value as needed
                    otherRb.AddForce(hitDirection * impactForce);
                }

                //Destroy(gameObject);
            }
        }

        private void OnBecameInvisible()
        {
            Destroy(gameObject);
        }
    }
}