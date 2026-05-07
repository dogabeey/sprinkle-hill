using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class BreakableWall : GridCellController
    {
        public class HealthObjectByHealth
        {
            public GameObject healthObject; // the healthObject will be active while cell health is at or below the specified threshold
            public int healthThreshold; // healthObject will be active while cell health is at or below this threshold
            public ParticleSystem breakParticle;
        }
        private int currentHealth;

        [SerializeField] private Animator wallAnimator;
        [SerializeField] private List<HealthObjectByHealth> healthObjectsByHealth;
        [SerializeField] private string breakTriggerName = "Break";
        [SerializeField] private float breakAnimationDuration = 0.25f;

        public int CurrentHealth
        {
            get => currentHealth;
            set
            {
                currentHealth = value;
                UpdateHealthObjects();
            }
        }

        private void UpdateHealthObjects()
        {
            foreach (var healthObjectByHealth in healthObjectsByHealth)
            {
                if (healthObjectByHealth.healthObject != null)
                {
                    bool shouldBeActive = currentHealth <= healthObjectByHealth.healthThreshold;
                    if (healthObjectByHealth.healthObject.activeSelf != shouldBeActive)
                    {
                        healthObjectByHealth.healthObject.SetActive(shouldBeActive);
                        if (shouldBeActive && healthObjectByHealth.breakParticle != null)
                        {
                            healthObjectByHealth.breakParticle.Play();
                        }
                    }
                }
            }
        }

        private void Awake()
        {
            if (wallAnimator == null)
            {
                wallAnimator = GetComponent<Animator>();
            }
        }

        public IEnumerator WallBreak()
        {
            CurrentHealth--;
            if (CurrentHealth <= 0)
            {
                if (wallAnimator != null && !string.IsNullOrEmpty(breakTriggerName))
                {
                    wallAnimator.SetTrigger(breakTriggerName);

                    if (breakAnimationDuration > 0f)
                    {
                        yield return new WaitForSeconds(breakAnimationDuration);
                    }
                }
            }

        }
    }
}
