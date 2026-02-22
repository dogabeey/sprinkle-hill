using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteAlways]
public class MultiTextureSprite : MonoBehaviour
{
    public enum Axis
    {
        X,
        Y
    }

    public SpriteRenderer spriteRenderer;
    public Axis axis;
    // Start is called before the first frame update
    void Start()
    {
    }

    [ExecuteAlways]
    // Update is called once per frame
    void Update()
    {
        spriteRenderer.sharedMaterial.SetFloat("_SpriteTileSize", axis == Axis.X ? Mathf.Floor(spriteRenderer.size.x) : Mathf.Floor(spriteRenderer.size.y));
    }
}
