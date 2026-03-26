using UnityEngine;

namespace Game
{
    public enum Screens
    {
        MainMenu,
        LevelList,
        WorldList,
        WinScreen,
        LoseScreen,
        SettingScreen,
    }

    [CreateAssetMenu(fileName = "ConstantManager", menuName = "Game/Constant Manager...", order = 1)]
    public class ConstantManager : ScriptableObject
    {
        public float elementSwapMoveDuration = 0.3f;
        public float elementFallSpeed = 3.3f;
        public float matchClearDelay = 0.3f;
        [Header("Element Destroy Animation")]
        public float elementDestroyPopHeight = 0.2f;
        public float elementDestroyPopDuration = 0.08f;
        public float elementDestroyFallDistance = 0.25f;
        public float elementDestroyFallDuration = 0.12f;
        public float elementDestroyTargetScaleMultiplier = 0.1f;
        public float elementDestroyScaleDuration = 0.2f;
        public float elementDestroyRotateZ = 120f;
        public float elementDestroyRotateDuration = 0.2f;
        
        [Header("Sparkling Trail Animation")]
        public GameObject sparklingTrailPrefab;
        public float sparklingTrailDuration = 0.5f;
        public float sparklingTrailSpawnDelay = 0.05f;
        public float sparklingTrailFadeDelay = 0.5f;
        
        [Header("Sparkling Camera Shake")]
        public float sparklingShakeBaseMagnitude = 0.1f;
        public float sparklingShakeMagnitudeIncrement = 0.05f;
        public float sparklingShakeDuration = 0.2f;
        public int sparklingShakeVibrato = 10;
        public float sparklingShakeRandomness = 90f;
        
        [Header("Match Camera Shake")]
        public float matchShakeBaseMagnitude = 0.15f;
        public float matchShakeComboMultiplier = 0.1f;
        public float matchShakeDuration = 0.3f;
        public int matchShakeVibrato = 10;
        public float matchShakeRandomness = 90f;

        [Header("Bomb Impact")]
        public ParticleSystem bombImpactParticlePrefab;
        public float bombImpactShakeDuration = 0.25f;
        public float bombImpactShakeMagnitude = 0.35f;
        public int bombImpactShakeVibrato = 12;
        public float bombImpactShakeRandomness = 90f;

        public struct TAGS
        {
            public const string PLAYER = "Player";
            public const string ENEMY = "Enemy";
            public const string COLLECTIBLE = "Collectible";
            public const string GROUND = "Ground";
        }

        public struct BindingNames
        {
            public const string KEYBOARD = "Keyboard";
            public const string GAMEPAD = "Gamepad";
        }
        public struct SOUNDS
        {
            public struct MUSICS
            {
                public const string MAIN_MENU = "MainMenu";
                public const string GAMEPLAY = "Gameplay";
            }
            public struct EFFECTS
            {
                public const string TYPEWRITER = "Typewriter";
                public const string JUMP = "Jump";
                public const string DEATH = "Death";
                public const string PICKUP = "Pickup";
                public const string LEVEL_COMPLETE = "LevelComplete";
                public const string LEVEL_FAILED = "LevelFailed";
            }
        }
    }
}