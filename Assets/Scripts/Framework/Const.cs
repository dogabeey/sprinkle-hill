using UnityEngine; using Game.EventManagement;

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
        FeatureProgress,
        BuyMenu,
        ConsentPopup,
    }

    public class ConstantManager : MonoBehaviour
    {
        [Header("General")]
        public float loadingScreenDuration = 1f;
        [Header("Element Animation")]
        [RemoteConfig("element_swap_move_duration", 0.3f)]
        public float elementSwapMoveDuration = 0.3f;
        [RemoteConfig("element_fall_speed", 3.3f)]
        public float elementFallSpeed = 3.3f;
        [RemoteConfig("match_clear_delay", 0.3f)]
        public float matchClearDelay = 0.3f;
        [RemoteConfig("element_destroy_punch_scale", 0.25f)]
        public float elementDestroyPunchScale = 0.25f;
        [RemoteConfig("element_destroy_punch_duration", 0.2f)]
        public float elementDestroyPunchDuration = 0.2f;
        [RemoteConfig("element_destroy_punch_vibrato", 8)]
        public int elementDestroyPunchVibrato = 8;
        [RemoteConfig("element_destroy_punch_elasticity", 0.8f)]
        public float elementDestroyPunchElasticity = 0.8f;
        public ParticleSystem elementDestroyParticlePrefab;

        [Header("Disco Ball")]
        [RemoteConfig("disco_ball_trail_duration", 0.12f)]
        public float discoBallTrailDuration = 0.12f;
        [RemoteConfig("disco_ball_trail_spawn_delay", 0.02f)]
        public float discoBallTrailSpawnDelay = 0.02f;
        [RemoteConfig("disco_ball_emission_peak", 3f)]
        public float discoBallEmissionPeak = 3f;
        [RemoteConfig("disco_ball_emission_reset_delay", 0.08f)]
        public float discoBallEmissionResetDelay = 0.08f;
        [RemoteConfig("disco_ball_spin_loop_duration", 0.12f)]
        public float discoBallSpinLoopDuration = 0.12f;
        [RemoteConfig("disco_ball_spin_degrees_per_loop", 540f)]
        public float discoBallSpinDegreesPerLoop = 540f;

        [Header("Sparkling Trail Animation")]
        public GameObject sparklingTrailPrefab;
        [RemoteConfig("sparkling_trail_duration", 0.5f)]
        public float sparklingTrailDuration = 0.5f;
        [RemoteConfig("sparkling_trail_spawn_delay", 0.05f)]
        public float sparklingTrailSpawnDelay = 0.05f;
        [RemoteConfig("sparkling_trail_fade_delay", 0.5f)]
        public float sparklingTrailFadeDelay = 0.5f;

        [Header("Bomb Impact")]
        public ParticleSystem bombImpactParticlePrefab;
        [RemoteConfig("bomb_impact_shake_duration", 0.25f)]
        public float bombImpactShakeDuration = 0.25f;
        [RemoteConfig("bomb_impact_shake_magnitude", 0.35f)]
        public float bombImpactShakeMagnitude = 0.35f;
        [RemoteConfig("bomb_impact_shake_vibrato", 12)]
        public int bombImpactShakeVibrato = 12;
        [RemoteConfig("bomb_impact_shake_randomness", 90f)]
        public float bombImpactShakeRandomness = 90f;

        [Header("Rocket")]
        public ParticleSystem rocketTrailParticlePrefab;
        [RemoteConfig("rocket_travel_speed", 10f)]
        public float rocketTravelSpeed = 10f;

        [Header("Cauldron")]
        [RemoteConfig("element_to_cauldron_max_height", 0.75f)]
        public float elementToCauldronMaxHeight = 0.75f;
        [RemoteConfig("element_to_cauldron_height_multiplier", 0.5f)]
        public float elementToCauldronHeightMultiplier = 0.5f;

        [Header("Power-Up Spawn Limits")]
        [RemoteConfig("max_bomb_count", 3)]
        [Min(0)] public int maxBombCount = 3;
        [RemoteConfig("max_bomb_indirect_count", 6)]
        [Min(0)] public int maxBombIndirectCount = 6;
        [RemoteConfig("max_rocket_count", 3)]
        [Min(0)] public int maxRocketCount = 3;
        [RemoteConfig("max_rocket_indirect_count", 6)]
        [Min(0)] public int maxRocketIndirectCount = 6;
        [RemoteConfig("max_propeller_count", 3)]
        [Min(0)] public int maxPropellerCount = 3;
        [RemoteConfig("max_propeller_indirect_count", 6)]
        [Min(0)] public int maxPropellerIndirectCount = 6;
        [RemoteConfig("max_disco_ball_count", 1)]
        [Min(0)] public int maxDiscoBallCount = 1;
        [RemoteConfig("max_disco_ball_indirect_count", 2)]
        [Min(0)] public int maxDiscoBallIndirectCount = 2;
        [RemoteConfig("max_cauldron_count", 1)]
        [Min(0)] public int maxCauldronCount = 1;
        [RemoteConfig("max_cauldron_indirect_count", 1)]
        [Min(0)] public int maxCauldronIndirectCount = 1;

        public struct SOUNDS
        {
            public struct MUSICS
            {
                public const string MAIN_MENU = "MainMenu";
                public const string GAMEPLAY = "Gameplay";
            }
            public struct EFFECTS
            {
                public const string LEVEL_COMPLETE = "LevelComplete";
                public const string LEVEL_FAILED = "LevelFailed";
                public const string ELEMENT_SWAP = "ElementSwap";
                public const string MATCH = "Match";
                public const string BOMB = "Bomb";
                public const string ROCKET = "Rocket";
                public const string DISCO_BALL_ACTIVATE = "DiscoBallActivate";
                public const string DISCO_BALL_TRAIL = "DiscoBallTrail";
                public const string BUTTON_CLICK_FAIL = "ButtonClickFail";
                public const string BUTTON_CLICK_SUCCESS = "ButtonClickSuccess";
            }
        }

        public struct RESOURCES
        {
            public struct CURRENCY
            {
                public const string CASH = "Cash";
                public const string GEM = "Gem";
                public const string TOKEN = "Token";
            }
        }
    }
}