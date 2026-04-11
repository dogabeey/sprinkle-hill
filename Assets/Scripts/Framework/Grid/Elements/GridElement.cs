using Sirenix.OdinInspector;
using System;
using UnityEngine;
using DG.Tweening;
using System.Collections;
using MobileHapticsProFreeEdition;

namespace Game
{
    public abstract class GridElement : Grid3D
    {
        private static Material _defaultSpriteMaterial;

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
        public Sprite hiddenSprite;
        [FoldoutGroup("Grid Element")]
        [ReadOnly]
        public Grid3D ownerGrid;

        public virtual void InitElement(Grid3D ownerGrid, GridElementInfo elementInfo)
        {
            this.ownerGrid = ownerGrid;
            this.elementInfo = elementInfo;
            SetElement();
            
            if (elementInfo.isSparkling && !elementInfo.isHidden)
            {
                StartCoroutine(HueAnim());
            }
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
                        Material defaultSpriteMat = GetDefaultSpriteMaterial();
                        if (defaultSpriteMat != null)
                            spriteRenderer.sharedMaterial = defaultSpriteMat;
                    }
                    else if (visualInfo.elementMaterial != null)
                    {
                        spriteRenderer.sharedMaterial = visualInfo.elementMaterial;
                    }
                }
            }

            if (elementRenderer != null)
            {
                elementRenderer.transform.localRotation = elementInfo.powerUpType == ElementPowerUpType.HorizontalRocket
                    ? Quaternion.Euler(0f, 0f, 90f)
                    : Quaternion.identity;
            }
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

        public abstract IEnumerator DestroyElement();
    }

    [System.Serializable]
    public class GridElementInfo
    {
        public ElementData elementData;
        public bool isSparkling;
        public bool isHidden;
        public ElementPowerUpType powerUpType;
    }

    public enum ElementPowerUpType
    {
        None,
        Bomb,
        VerticalRocket,
        HorizontalRocket,
        BigBomb,
        Scatter,
        DiscoBall
    }

    /// <summary>
    /// Shared utility methods for grid elements. Eliminates duplicated emission / renderer helpers.
    /// </summary>
    public static class GridHelper
    {
        private const string EmissionProperty = "_Emission";

        public static void TriggerHaptic(HapticModes mode)
        {
            if (GameManager.Instance == null || GameManager.Instance.soundManager == null)
                return;

            if (!GameManager.Instance.soundManager.IsVibrationOn)
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

        public static void ShakeCamera(float duration, float magnitude, int vibrato, float randomness)
        {
            return;
        }
    }
}
