using Sirenix.OdinInspector;
using UnityEngine;

public class WarpableSpriteModifier : MonoBehaviour
{
    private const string mainTextureId = "_MainTex";
    private const string twirlId = "_Twirl";
    private const string spherizeId = "_Spherize";
    private const string scaleId = "_Scale";
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
    public Vector2 scale;
    [OnValueChanged(nameof(SetMaterialValues))]
    public Vector2 spherize;
    [OnValueChanged(nameof(SetMaterialValues))]
    public Texture2D distortionTexture;
    [OnValueChanged(nameof(SetMaterialValues))]
    public float distortionStrength;
    [OnValueChanged(nameof(SetMaterialValues))]
    public float distortionSpeed;
    [OnValueChanged(nameof(SetMaterialValues))]
    public Vector2 distortionDirection;

    private bool hasInitialValues;
    private Texture2D initialMainTexture;
    private Vector2 initialTwirl;
    private Vector2 initialScale;
    private Vector2 initialSpherize;
    private Texture2D initialDistortionTexture;
    private float initialDistortionStrength;
    private float initialDistortionSpeed;
    private Vector2 initialDistortionDirection;

    private void Awake()
    {
        CacheInitialValues();
    }

    private void OnEnable()
    {
        ResetToInitialValues();
    }

    private void OnDisable()
    {
        ResetToInitialValues();
    }

    private void Update()
    {
        SetMaterialValues();
    }

    /// <summary>
    /// Restores values that may have been modified by an animation before this
    /// visual is returned to, or retrieved from, the element pool.
    /// </summary>
    public void ResetToInitialValues()
    {
        CacheInitialValues();

        mainTexture = initialMainTexture;
        twirl = initialTwirl;
        scale = initialScale;
        spherize = initialSpherize;
        distortionTexture = initialDistortionTexture;
        distortionStrength = initialDistortionStrength;
        distortionSpeed = initialDistortionSpeed;
        distortionDirection = initialDistortionDirection;

        SetMaterialValues();
    }

    private void CacheInitialValues()
    {
        if (hasInitialValues)
            return;

        hasInitialValues = true;
        initialMainTexture = mainTexture;
        initialTwirl = twirl;
        initialScale = scale;
        initialSpherize = spherize;
        initialDistortionTexture = distortionTexture;
        initialDistortionStrength = distortionStrength;
        initialDistortionSpeed = distortionSpeed;
        initialDistortionDirection = distortionDirection;
    }

    private void SetMaterialValues()
    {
        if (rendererReference == null || rendererReference.sharedMaterial == null)
            return;

        if(Application.isPlaying)
        {
            if (mainTexture)
                rendererReference.sharedMaterial.SetTexture(mainTextureId, mainTexture);
            if (distortionTexture)
                rendererReference.sharedMaterial.SetTexture(distortionTextureId, distortionTexture);
            rendererReference.sharedMaterial.SetFloat(distortionStrengthId, distortionStrength);
            rendererReference.sharedMaterial.SetFloat(distortionSpeedId, distortionSpeed);
            rendererReference.sharedMaterial.SetVector(twirlId, twirl);
            rendererReference.sharedMaterial.SetVector(distortionDirectionId, distortionDirection);
            rendererReference.sharedMaterial.SetVector(spherizeId, spherize);
            rendererReference.sharedMaterial.SetVector(scaleId, scale);
        }
        else
        {
            if (mainTexture)
                rendererReference.material.SetTexture(mainTextureId, mainTexture);
            if (distortionTexture)
                rendererReference.material.SetTexture(distortionTextureId, distortionTexture);
            rendererReference.material.SetFloat(distortionStrengthId, distortionStrength);
            rendererReference.material.SetFloat(distortionSpeedId, distortionSpeed);
            rendererReference.material.SetVector(twirlId, twirl);
            rendererReference.material.SetVector(distortionDirectionId, distortionDirection);
            rendererReference.material.SetVector(spherizeId, spherize);
            rendererReference.material.SetVector(scaleId, scale);
        }
    }
}
