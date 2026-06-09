using DG.Tweening;
using MobileHapticsProFreeEdition;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Diagnostics;
using UnityEngine; using Game.EventManagement;

namespace Game
{
    public abstract class GridElement : Grid3D, IPoolable<GridElement>
    {
        private static Material _defaultSpriteMaterial;
        private bool _cachedInitialScale;
        private Vector3 _initialLocalScale;

        [System.Serializable]
        public class MeshData
        {
            public Renderer renderer;
            public int materialIndex;
        }

        [FoldoutGroup("Grid Element")]
        public GridElementInfo elementInfo;
        [FoldoutGroup("Grid Element")]
        public Animator elementAnimator;
        [FoldoutGroup("Grid Element")]
        public Renderer elementRenderer;
        [FoldoutGroup("Grid Element")]
        public string powerUpActivationString;
        [FoldoutGroup("Grid Element")]
        public Sprite hiddenSprite;
        [FoldoutGroup("Grid Element")]
        [ReadOnly]
        public Grid3D ownerGrid;

        internal int currentAnimationLayerIndex = -1;

        public virtual void InitElement(Grid3D ownerGrid, GridElementInfo elementInfo)
        {
            this.ownerGrid = ownerGrid;
            this.elementInfo = elementInfo;

            if (!_cachedInitialScale)
            {
                _initialLocalScale = transform.localScale;
                _cachedInitialScale = true;
            }

            SetElement();
            SetElementAnimation();
            ApplyCoverageTransform();
            
            if (elementInfo.isSparkling && !elementInfo.isHidden)
            {
                StartCoroutine(HueAnim());
            }
        }

        public virtual void PlayRevealEffect()
        {
            var gfxManager = GameManager.Instance != null ? Gfx.Instance : null;
            if (gfxManager == null)
                return;

            SpawnParticleEffect(gfxManager.hiddenElementRevealParticlePrefab);
        }

        protected void SpawnParticleEffect(ParticleSystem particlePrefab)
        {
            if (particlePrefab == null || transform == null)
                return;

            Vector3 spawnPosition = elementRenderer != null ? elementRenderer.bounds.center : transform.position;
            ParticleSystem particle = Instantiate(particlePrefab, spawnPosition, Quaternion.identity);
            particle.Play();
            Destroy(particle.gameObject, particle.main.duration + particle.main.startLifetime.constantMax + 0.2f);
        }

        private static Vector2Int GetGridCoverage(ElementData data)
        {
            if (data == null)
                return Vector2Int.one;

            return new Vector2Int(Mathf.Max(1, data.gridCoverage.x), Mathf.Max(1, data.gridCoverage.y));
        }

        private void ApplyCoverageTransform()
        {
            Vector2Int coverage = GetGridCoverage(elementInfo != null ? elementInfo.elementData : null);

            transform.localScale = new Vector3(
                _initialLocalScale.x * coverage.x,
                _initialLocalScale.y * coverage.y,
                _initialLocalScale.z);

            if (coverage == Vector2Int.one)
            {
                return;
            }

            if (!(ownerGrid is Match3Grid match3Grid) || !match3Grid.TryGetElementPosition(this, out Vector2Int anchorPos))
            {
                transform.localPosition = Vector3.zero;
                return;
            }

            Vector2Int endPos = anchorPos + new Vector2Int(coverage.x - 1, coverage.y - 1);
            if (!match3Grid.IsValidPosition(endPos))
            {
                transform.localPosition = Vector3.zero;
                return;
            }

            Vector3 startWorld = match3Grid.GetCellPositionInGrid(anchorPos);
            Vector3 endWorld = match3Grid.GetCellPositionInGrid(endPos);
            transform.position = (startWorld + endWorld) * 0.5f;
        }

        protected virtual void SetElement()
        {
            ElementData visualInfo = elementInfo.elementData;
            if (visualInfo != null)
            {
                if (elementRenderer is MeshRenderer meshRenderer)
                {
                    MeshFilter meshFilter = meshRenderer.GetComponent<MeshFilter>();
                    if (meshFilter != null && visualInfo.elementMesh != null)
                    {
                        meshFilter.mesh = visualInfo.elementMesh;
                    }

                    if (visualInfo.elementMaterial != null)
                    {
                        meshRenderer.sharedMaterial = visualInfo.elementMaterial;
                    }
                }

                if (elementRenderer is SpriteRenderer spriteRenderer)
                {
                    if (elementInfo != null && elementInfo.isHidden && hiddenSprite != null)
                    {
                        spriteRenderer.sprite = hiddenSprite;
                    }
                    else
                    {
                        spriteRenderer.sprite = visualInfo.displayIcon;
                    }

                    if (elementInfo != null && elementInfo.powerUpType != ElementPowerUpType.None)
                    {
                        if (visualInfo.elementMaterial != null)
                        {
                            spriteRenderer.sharedMaterial = visualInfo.elementMaterial;
                        }
                    }
                }
            }
        }

        private void SetElementAnimation()
        {
            elementAnimator.enabled = false;

            if (elementInfo == null || elementInfo.elementData == null || elementInfo.elementData.animationController == null)
                return;

            if (elementAnimator == null)
                elementAnimator = GetComponent<Animator>();

            if (elementAnimator == null)
                elementAnimator = GetComponentInChildren<Animator>(true);

            if (elementAnimator == null)
                return;

            elementAnimator.runtimeAnimatorController = elementInfo.elementData.animationController;

            if (elementAnimator.runtimeAnimatorController == null)
            {
                currentAnimationLayerIndex = -1;
                return;
            }

            elementAnimator.enabled = true;
            elementAnimator.Rebind();
            elementAnimator.Update(0f);

            int targetLayer = ResolveAnimationLayer();
            int maxLayer = Mathf.Max(0, elementAnimator.layerCount - 1);
            targetLayer = Mathf.Clamp(targetLayer, 0, maxLayer);

            elementAnimator.SetLayerWeight(0, 1f);
            for (int i = 1; i < elementAnimator.layerCount; i++)
            {
                elementAnimator.SetLayerWeight(i, i == targetLayer ? 1f : 0f);
            }

            currentAnimationLayerIndex = targetLayer;

            string animationName = elementInfo.elementData.defaultIdleAnimation;
            if (string.IsNullOrWhiteSpace(animationName))
                return;

            int stateHash = Animator.StringToHash(animationName);
            if (elementAnimator.HasState(targetLayer, stateHash))
            {
                elementAnimator.Play(stateHash, targetLayer, 0f);
            }
            else if (targetLayer != 0 && elementAnimator.HasState(0, stateHash))
            {
                elementAnimator.Play(stateHash, 0, 0f);
                currentAnimationLayerIndex = 0;
            }

            // Re-apply visual setup after animator has been bound and play called.
            // Some animator controllers may reset SpriteRenderer properties on rebind; reapply to ensure the element sprite/material is visible.
            SetElement();
        }

        private int ResolveAnimationLayer()
        {
            ElementData data = elementInfo.elementData;
            GameManager gm = GameManager.Instance;
            bool isCauldronElement = gm != null && gm.cauldronElementData == data;
            if (data == null || !isCauldronElement || data.elementAnimationsByProgress == null || data.elementAnimationsByProgress.Length == 0)
                return 0;

            int required = Mathf.Max(1, data.cauldronChargeRequired);
            float progress01 = Mathf.Clamp01((float)elementInfo.cauldronProgress / required);

            for (int i = 0; i < data.elementAnimationsByProgress.Length; i++)
            {
                ElementData.ElementAnimationByProgress animationByProgress = data.elementAnimationsByProgress[i];
                float min = Mathf.Min(animationByProgress.progressRange.x, animationByProgress.progressRange.y);
                float max = Mathf.Max(animationByProgress.progressRange.x, animationByProgress.progressRange.y);
                if (progress01 >= min && progress01 <= max)
                    return Mathf.Max(0, animationByProgress.animationLayer);
            }

            return 0;
        }

        protected virtual IEnumerator HueAnim()
        {
            yield break;
        }

        private static Material GetDefaultSpriteMaterial()
        {
            if (_defaultSpriteMaterial != null)
                return _defaultSpriteMaterial;

            Shader spriteShader = Shader.Find("Sprites/Default");
            if (spriteShader == null)
                return null;

            _defaultSpriteMaterial = new Material(spriteShader)
            {
                name = "Runtime_DefaultSpriteMaterial"
            };

            return _defaultSpriteMaterial;
        }

        private void Awake()
        {
            UnityEngine.Debug.Log(
                $"CREATED: {GetType().Name}\n{new StackTrace()}");
        }
        private void OnDestroy()
        {
            if (transform != null)
                transform.DOKill();

            UnityEngine.Debug.Log(
                $"DESTROYED: {GetType().Name}\n{new StackTrace()}");
        }

        public abstract IEnumerator DestroyElement();
    }

    [System.Serializable]
    public class GridElementInfo
    {
        public ElementData elementData;
        public bool randomElement;
        public bool isSparkling;
        public bool isHidden;
        public ElementPowerUpType powerUpType;
        public int cauldronProgress;
    }

    public enum ElementPowerUpType
    {
        None,
        Bomb,
        Rocket,
        Propeller,
        VerticalRocket,
        HorizontalRocket,
        BigBomb,
        Scatter,
        DiscoBall,
        Cauldron
    }

    /// <summary>
    /// Shared utility methods for grid elements. Eliminates duplicated emission / renderer helpers.
    /// </summary>
    public static class GridHelper
    {
        private const string EmissionProperty = "_Emission";

        public static void TriggerHaptic(HapticModes mode)
        {
            if (GameManager.Instance == null || SoundManager.Instance == null)
                return;

            if (!SoundManager.Instance.IsVibrationOn)
                return;

            TapticWave.TriggerHaptic(mode);
        }

        public static void SetEmission(GridElement element, float value)
        {
            if (element == null) return;
            Renderer[] renderers = element.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material mat = renderers[i] != null ? renderers[i].material : null;
                if (mat != null && mat.HasProperty(EmissionProperty))
                    mat.SetFloat(EmissionProperty, value);
            }
        }

        public static void AnimateEmission(GridElement element, float value, float duration)
        {
            if (element == null) return;
            Renderer[] renderers = element.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material mat = renderers[i] != null ? renderers[i].material : null;
                if (mat != null && mat.HasProperty(EmissionProperty))
                    mat.DOFloat(value, EmissionProperty, duration);
            }
        }
    }
}
