using DG.Tweening;
using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshRendererRandomizer : MonoBehaviour
{
    public float offsetMultiplier = 0.25f;
    [MinMaxSlider(0, 3, true)]
    public Vector2Int randomRange = new Vector2Int(0, 3);

    private MeshRenderer meshRenderer;
    // Start is called before the first frame update
    void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            Material[] materials = meshRenderer.materials;
            for (int i = 0; i < materials.Length; i++)
            {
                materials[i].mainTextureOffset = new Vector2(Random.Range(randomRange.x, randomRange.y) * offsetMultiplier, Random.Range(randomRange.x, randomRange.y) * offsetMultiplier);
            }
        }
    }

    private void OnValidate()
    {
        if (meshRenderer != null)
        {
            Material[] materials = meshRenderer.materials;
            meshRenderer.material.DOTiling(Vector2.one * offsetMultiplier, 0);
            meshRenderer.material.DOOffset(new Vector2(Random.Range(randomRange.x, randomRange.y) * offsetMultiplier, Random.Range(randomRange.x, randomRange.y) * offsetMultiplier), 0);
        }
    }

    // Update is called once per frame
    void Update()
    {

    }
}
