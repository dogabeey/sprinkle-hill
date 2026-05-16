using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class BreakableWall : GridCellController
    {
        [System.Serializable]
        public class HealthObjectByHealth
        {
            public GameObject healthObject; // the healthObject will be active while cell health is at or above the specified threshold
            public int healthThreshold; // healthObject will be active while cell health is at or above this threshold
            public ParticleSystem breakParticle;
        }
        private int currentHealth;

        public ParticleSystem breakParticle;
        [SerializeField] private List<HealthObjectByHealth> healthObjectsByHealth;
        [SerializeField] private string breakTriggerName = "Break";
        [SerializeField] private float breakAnimationDuration = 0.25f;

        public void InitializeHealth(int health)
        {
            CurrentHealth = Mathf.Max(0, health);
        }

        public int CurrentHealth
        {
            get => currentHealth;
            set
            {
                currentHealth = Mathf.Max(0, value);
                UpdateHealthObjects();
            }
        }

        private void UpdateHealthObjects()
        {
            foreach (var healthObjectByHealth in healthObjectsByHealth)
            {
                if (healthObjectByHealth.healthObject != null)
                {
                    bool shouldBeActive = currentHealth >= healthObjectByHealth.healthThreshold;
                    if (healthObjectByHealth.healthObject.activeSelf && !shouldBeActive)
                    {
                        ParticleSystem breakParticleInstance = healthObjectByHealth.breakParticle != null ? Instantiate(healthObjectByHealth.breakParticle, healthObjectByHealth.healthObject.transform.position, Quaternion.identity) : null;
                        if (breakParticleInstance != null)
                        {
                            breakParticleInstance.Play();
                        }
                    }
                        healthObjectByHealth.healthObject.SetActive(shouldBeActive);
                }
            }
        }

        private void Awake()
        {
        }

        public void WallBreak()
        {
            if (breakParticle != null)
            {
                ParticleSystem breakParticleInstance = Instantiate(breakParticle, transform.position, Quaternion.identity);
                breakParticleInstance.Play();
            }
        }
    }
}
