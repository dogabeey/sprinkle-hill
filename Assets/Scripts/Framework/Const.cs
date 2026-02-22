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
    public struct Const
    {

        public struct Values
        {
            public const float PICKUP_DROP_HEIGHT_TRY_STEP = 1f;
            public const float MOVEMENT_OVERLAP_SPHERE_SENSITIVITY = 0.2f;
            public const float MOVEMENT_DURATION = 0.2f;
            internal const float MOVEMENT_STEP = 1;
        }

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