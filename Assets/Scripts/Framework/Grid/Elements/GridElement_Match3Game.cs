using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class GridElement_Match3Game : GridElement
    {
        [FoldoutGroup("Match3")]
        public SpriteRenderer highlightSprite;
        [FoldoutGroup("Match3/Cauldron")]
        public Image cauldronProgressBackground;
        [FoldoutGroup("Match3/Cauldron")]
        public Image cauldronProgressFill;
        [FoldoutGroup("Match3/Cauldron")]
        public SpriteRenderer cauldronReadyIndicator;

        private Vector3 cauldronFillBaseScale;
        private bool cauldronFillCached;

        public override void InitElement(Grid3D ownerGrid, GridElementInfo elementInfo)
        {
            base.InitElement(ownerGrid, elementInfo);
            RefreshCauldronVisual();
        }

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

        private void RefreshCauldronVisual()
        {
            bool isCauldron = elementInfo != null &&
                              elementInfo.powerUpType == ElementPowerUpType.Cauldron &&
                              elementInfo.elementData != null &&
                              elementInfo.elementData.isCauldron;

            if (cauldronProgressBackground != null)
                cauldronProgressBackground.gameObject.SetActive(isCauldron) ;

            if (cauldronProgressFill != null)
            {
                cauldronProgressFill.enabled = isCauldron;
                cauldronProgressFill.fillAmount = isCauldron && elementInfo.elementData.cauldronChargeRequired > 0
                    ? Mathf.Clamp01((float)elementInfo.cauldronProgress / elementInfo.elementData.cauldronChargeRequired)
                    : 0f;
            }

            if (cauldronReadyIndicator != null)
            {
                bool isReady = isCauldron && elementInfo.cauldronProgress >= Mathf.Max(1, elementInfo.elementData.cauldronChargeRequired);
                cauldronReadyIndicator.enabled = isReady;
            }
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

            GridHelper.AnimateEmission(this, 1, 0.2f);

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