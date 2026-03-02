using Sirenix.OdinInspector;
using System;
using UnityEngine;
using DG.Tweening;
using System.Collections;

namespace Game
{
    public abstract class GridElement : Grid3D
    {
        [System.Serializable]
        public class MeshData
        {
            public Renderer renderer;
            public int materialIndex;
        }

        [FoldoutGroup("Grid Element")]
        public GridElementInfo elementInfo;
        [FoldoutGroup("Grid Element")]
        public Renderer elementRenderer;
        [FoldoutGroup("Grid Element")]
        [ReadOnly]
        public Grid3D ownerGrid;

        public virtual void InitElement(Grid3D ownerGrid, GridElementInfo elementInfo)
        {
            this.ownerGrid = ownerGrid;
            this.elementInfo = elementInfo;
            SetElement();
        }

        protected virtual void SetElement()
        {
            ElementData visualInfo = elementInfo.elementData;
            if (visualInfo != null)
            {
                if (elementRenderer != null && visualInfo.elementMesh != null)
                {
                    if(elementRenderer is MeshRenderer meshRenderer)
                    {
                        MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
                        if (meshFilter != null)
                        {
                            meshFilter.mesh = visualInfo.elementMesh;
                        }
                    }
                    if(elementRenderer is SpriteRenderer spriteRenderer)
                    {
                        spriteRenderer.sprite = visualInfo.ElementSprite;
                    }
                }
            }
        }

        internal IEnumerator DestroyElement()
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
            yield return destroySequence.WaitForCompletion();
        }
    }

    [System.Serializable]
    public class GridElementInfo
    {
        public ElementData elementData;
    }
}
