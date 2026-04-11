using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace MobileHapticsProFreeEdition
{
    [CustomEditor(typeof(GameHaptics))]
    public class HapticsEngineEditor : Editor
    {
        // Reference to the Ultimate Edition Store page (free edition only)
        private const string ultimateEditionUrl = "https://assetstore.unity.com/packages/tools/game-toolkits/mobile-haptics-pro-ultimate-edition-299877";

        // Reference to the Ultimate Edition image (free edition only)
        private Texture2D ultimatePreviewImage;

        void OnEnable()
        {
            // Load the image from the specific folder
            ultimatePreviewImage = (Texture2D)AssetDatabase.LoadAssetAtPath("Assets/IntelligentLabs/MobileHapticsProFreeEdition/Sprites/UltimatePreview.png", typeof(Texture2D));
        }

        public override void OnInspectorGUI()
        {
            DrawDefaultInspector();

            GUILayout.Space(15);
            GUILayout.Label("  * Unlock The Full Haptics Engine - Click Below *", EditorStyles.boldLabel);
            GUILayout.Space(5);

            if (ultimatePreviewImage != null)
            {
                if (GUILayout.Button(ultimatePreviewImage, GUILayout.Width(300), GUILayout.Height(205)))
                {
                    Application.OpenURL(ultimateEditionUrl);
                }
            }
        }
    }
}
