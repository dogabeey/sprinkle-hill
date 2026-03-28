using UnityEngine;

namespace Game
{
    public class TestCreature : MonoBehaviour
    {
        [SerializeField] private float maxHp = 100f;
        [SerializeField] private float attackDamage = 15f;
        [SerializeField] private float attackCooldown = 1.5f;

        private float currentHp;
        private float attackTimer;
        private bool isDead;

        private void Start()
        {
            currentHp = maxHp;
            attackTimer = 0f;
        }

        private void Update()
        {
            if (isDead) return;

            attackTimer += Time.deltaTime;
        }

        public void TakeDamage(float amount)
        {
            if (isDead) return;

            currentHp -= amount;
            currentHp = Mathf.Max(currentHp, 0f);

            if (currentHp == 0f)
                Die();
        }

        /// <summary>
        /// Attempts an attack against a target creature. Returns true if the attack was performed.
        /// </summary>
        public bool TryAttack(TestCreature target)
        {
            if (isDead || target == null) return false;
            if (attackTimer < attackCooldown) return false;

            attackTimer = 0f;
            target.TakeDamage(attackDamage);
            return true;
        }

        public void Heal(float amount)
        {
            if (isDead) return;

            currentHp = Mathf.Min(currentHp + amount, maxHp);
        }

        public float GetHpRatio() => currentHp / maxHp;
        public bool IsDead => isDead;

        private void Die()
        {
            isDead = true;
            Debug.Log($"{gameObject.name} has died.");
        }
    }
}
