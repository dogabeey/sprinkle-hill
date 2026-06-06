using Sirenix.OdinInspector;
using UnityEngine;

public class WarpableSpriteModifier : MonoBehaviour
{
    private const string mainTextureId = "_MainTex";
    private const string twirlId = "_Twirl";
    private const string distortionTextureId = "_Distortion_Texture";
    private const string distortionStrengthId = "_Distortion_Strength";
    private const string distortionSpeedId = "_Distortion_Speed";
    private const string distortionDirectionId = "_Distortion_Direction";

    [OnValueChanged(nameof(SetMaterialValues))]
    public Renderer rendererReference;
    [OnValueChanged(nameof(SetMaterialValues))]
    public Texture2D mainTexture;
    [OnValueChanged(nameof(SetMaterialValues))]
    public Vector2 twirl;
    [OnValueChanged(nameof(SetMaterialValues))]
    public Texture2D distortionTexture;
    [OnValueChanged(nameof(SetMaterialValues))]
    public float distortionStrength;
    [OnValueChanged(nameof(SetMaterialValues))]
    public float distortionSpeed;
    [OnValueChanged(nameof(SetMaterialValues))]
    public Vector2 distortionDirection;


    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        SetMaterialValues();
    }

    private void SetMaterialValues()
    {
        rendererReference.material.SetTexture(mainTextureId, mainTexture);
        rendererReference.material.SetVector(twirlId, twirl);
        rendererReference.material.SetTexture(distortionTextureId, distortionTexture);
        rendererReference.material.SetFloat(distortionStrengthId, distortionStrength);
        rendererReference.material.SetFloat(distortionSpeedId, distortionSpeed);
        rendererReference.material.SetVector(distortionDirectionId, distortionDirection);
    }
}
