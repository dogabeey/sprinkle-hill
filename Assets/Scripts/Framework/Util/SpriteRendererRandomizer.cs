using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using UnityEngine;

public class SpriteRendererRandomizer : MonoBehaviour
{
    public Sprite[] sprites;

    private SpriteRenderer spriteRenderer;
    // Start is called before the first frame update
    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (sprites.Length > 0)
        {
            spriteRenderer.sprite = sprites[Random.Range(0, sprites.Length)];
        }
    }

    private void OnValidate()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (sprites.Length > 0)
        {
            spriteRenderer.sprite = sprites[Random.Range(0, sprites.Length)];
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
