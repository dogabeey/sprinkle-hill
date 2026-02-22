using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class EdgeLitSprite : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;
    public Transform lightObject;
    // Start is called before the first frame update
    void Start()
    {
    }

    [ExecuteAlways]
    // Update is called once per frame
    void Update()
    {
        spriteRenderer.sharedMaterial.SetVector("_LightPosition", lightObject.position);
    }
}
