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

        public struct GameEvents
        {
            public const string COLLECTIBLE_EARNED = "COLLECTIBLE_EARNED";
            public const string OBJECTIVE_COMPLETED = "OBJECTIVE_COMPLETED";
            public const string OBJECTIVE_FAILED = "OBJECTIVE_FAILED";

            public const string LEVEL_COMPLETED = "LEVEL_COMPLETED";
            public const string LEVEL_FAILED = "LEVEL_FAILED";
            public const string LEVEL_STARTED = "LEVEL_STARTED";

            public const string CURRENT_WORLD_CHANGED = "CURRENT_WORLD_CHANGED";
        }
    }
}