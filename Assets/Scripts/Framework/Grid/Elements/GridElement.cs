using Sirenix.OdinInspector;
using UnityEngine;

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

        [Header("Grid Element")]
        public GridElementInfo elementInfo;
        public MeshData[] meshDataArray;
        public MeshFilter meshFilter;
        [ReadOnly]
        public Grid3D ownerGrid;

        public virtual void InitElement(Grid3D ownerGrid, GridElementInfo elementInfo)
        {
            this.ownerGrid = ownerGrid;
            this.elementInfo = elementInfo;
            SetElementVisual();
        }

        protected virtual void SetElementVisual()
        {
            ElementVisualInfo visualInfo = GameManager.Instance.elementVisualDataManager.GetElementVisualInfo(elementInfo.elementVisual);
            if (visualInfo != null)
            {
                if (meshFilter != null && visualInfo.elementMesh != null)
                {
                    meshFilter.mesh = visualInfo.elementMesh;
                }
                if (meshDataArray != null && meshDataArray.Length > 0)
                {
                    foreach (var meshData in meshDataArray)
                    {
                        if (meshData.renderer != null && visualInfo.elementMaterial != null)
                        {
                            Material[] materials = meshData.renderer.materials;
                            if (meshData.materialIndex < materials.Length)
                            {
                                materials[meshData.materialIndex] = visualInfo.elementMaterial;
                                meshData.renderer.materials = materials;
                            }
                        }
                    }
                }
            }
        }
    }

    [System.Serializable]
    public class GridElementInfo
    {
        public ElementVisualType elementVisual;

        public GridElementInfo(ElementVisualType elementVisual)
        {
            this.elementVisual = elementVisual;
        }
    }
}
