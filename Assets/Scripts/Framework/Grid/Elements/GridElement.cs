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
                if (elementRenderer != null || visualInfo.elementMesh != null)
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

        public abstract IEnumerator DestroyElement();
    }

    [System.Serializable]
    public class GridElementInfo
    {
        public ElementData elementData;
    }
}
