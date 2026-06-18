using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using UnityEngine; using Game.EventManagement;
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
            // TODO: This is a bit of a hack, we should ideally have a separate element class for cauldron and move this logic there.
        }

        protected override IEnumerator HueAnim()
        {
            elementRenderer.material.SetFloat("_HueShiftSpeed", 5);
            yield break;
        }

        public override IEnumerator DestroyElement()
        {
            if (this == null || transform == null)
                yield break;

            transform.DOKill();

            Collider[] colliders = GetComponentsInChildren<Collider>();
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            GridHelper.AnimateEmission(this, 1.5f, 0.2f);

            ConstantManager constantManager = ConstantManager.Instance;
            Vector3 punchScale = constantManager.elementDestroyPunchScale * Vector3.one;
            float punchDuration = constantManager.elementDestroyPunchDuration;
            int punchVibrato = constantManager.elementDestroyPunchVibrato;
            float punchElasticity = constantManager.elementDestroyPunchElasticity;

            Tween destroyTween = transform.DOPunchScale(punchScale, punchDuration, punchVibrato, punchElasticity);

            EventManager.TriggerEvent(GameEvent.ELEMENT_DESTROYED,
                eventParam: new EventParam(paramScriptable: elementInfo != null ? elementInfo.elementData : null));

            if (destroyTween != null && destroyTween.active)
                yield return destroyTween.WaitForCompletion();

            if (this != null && constantManager != null && transform != null)
            {
                SpawnParticleEffect(constantManager.elementDestroyParticlePrefab);
            }

            if (this != null)
                Destroy(gameObject);
        }
    }
}