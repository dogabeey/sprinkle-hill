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

            ConstantManager constantManager = GameManager.Instance != null ? GameManager.Instance.constantManager : null;
            float popHeight = constantManager != null ? constantManager.elementDestroyPopHeight : 0.2f;
            float popDuration = constantManager != null ? constantManager.elementDestroyPopDuration : 0.08f;
            float fallDistance = constantManager != null ? constantManager.elementDestroyFallDistance : 0.25f;
            float fallDuration = constantManager != null ? constantManager.elementDestroyFallDuration : 0.12f;
            float targetScaleMultiplier = constantManager != null ? constantManager.elementDestroyTargetScaleMultiplier : 0.1f;
            float scaleDuration = constantManager != null ? constantManager.elementDestroyScaleDuration : 0.2f;
            float rotateZ = constantManager != null ? constantManager.elementDestroyRotateZ : 120f;
            float rotateDuration = constantManager != null ? constantManager.elementDestroyRotateDuration : 0.2f;

            Vector3 initialScale = transform.localScale;
            Sequence destroySequence = DOTween.Sequence();
            destroySequence.Append(transform.DOLocalMoveY(popHeight, popDuration).SetRelative().SetEase(Ease.OutQuad));
            destroySequence.Append(transform.DOLocalMoveY(-fallDistance, fallDuration).SetRelative().SetEase(Ease.InQuad));
            destroySequence.Join(transform.DOScale(initialScale * targetScaleMultiplier, scaleDuration).SetEase(Ease.InBack));
            destroySequence.Join(transform.DOLocalRotate(new Vector3(0f, 0f, rotateZ), rotateDuration, RotateMode.LocalAxisAdd).SetEase(Ease.InQuad));
            destroySequence.OnComplete(() => Destroy(gameObject));

            EventManager.TriggerEvent(GameEvent.ELEMENT_DESTROYED, 
                eventParam: new EventParam(paramScriptable: elementInfo.elementData));

            yield return destroySequence.WaitForCompletion();
        }

    }

}