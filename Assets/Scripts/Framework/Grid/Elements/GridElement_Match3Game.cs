using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;

namespace Game
{
    public class GridElement_Match3Game : GridElement
    {
        [FoldoutGroup("Match3")]
        public SpriteRenderer highlightSprite;
        public float maxAnimationEmission = 1f;

        public override void PostInit()
        {
        }

        public override void PreInit()
        {
        }

        public void SetSelected(bool isSelected)
        {
            if (highlightSprite != null)
            {
                highlightSprite.enabled = isSelected;
            }
        }

        private void OnDisable()
        {
            SetSelected(false);
        }

        protected override IEnumerator HueAnim()
        {
            elementRenderer.material.SetFloat("_HueShiftSpeed", 5);
            yield break;
        }

        public override IEnumerator DestroyElement()
        {
            transform.DOKill();

            Collider[] colliders = GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            GridHelper.AnimateEmission(this, maxAnimationEmission, 0.2f);

            ConstantManager constantManager = GameManager.Instance != null ? GameManager.Instance.constantManager : null;
            Vector3 punchScale = constantManager != null ? constantManager.elementDestroyPunchScale : new Vector3(0.25f, 0.25f, 0f);
            float punchDuration = constantManager != null ? constantManager.elementDestroyPunchDuration : 0.2f;
            int punchVibrato = constantManager != null ? constantManager.elementDestroyPunchVibrato : 8;
            float punchElasticity = constantManager != null ? constantManager.elementDestroyPunchElasticity : 0.8f;

            Tween destroyTween = transform.DOPunchScale(punchScale, punchDuration, punchVibrato, punchElasticity);

            EventManager.TriggerEvent(GameEvent.ELEMENT_DESTROYED,
                eventParam: new EventParam(paramScriptable: elementInfo != null ? elementInfo.elementData : null));

            yield return destroyTween.WaitForCompletion();

            if (constantManager != null && constantManager.elementDestroyParticlePrefab != null)
            {
                ParticleSystem destroyParticle = Instantiate(constantManager.elementDestroyParticlePrefab, transform.position, Quaternion.identity);
                destroyParticle.Play();
                Destroy(destroyParticle.gameObject, destroyParticle.main.duration + destroyParticle.main.startLifetime.constantMax + 0.2f);
            }

            Destroy(gameObject);
        }
    }
}