using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class Gfx : MonoBehaviour
    {
        [FoldoutGroup("Icons")]
        public Sprite addTimeIcon;
        [FoldoutGroup("Icons")]
        public Sprite addMovesIcon;
        [FoldoutGroup("Icons")]
        public Sprite shuffleActionIcon;
        [FoldoutGroup("Icons")]
        public Sprite bombElementIcon;
        [FoldoutGroup("Icons")]
        public Sprite discoBallElementIcon;
        [FoldoutGroup("Icons")]
        public Sprite rocketElementIcon;
        [FoldoutGroup("Icons")]
        public Sprite easyLevelIcon;
        [FoldoutGroup("Icons")]
        public Sprite mediumLevelIcon;
        [FoldoutGroup("Icons")]
        public Sprite hardLevelIcon;
        [FoldoutGroup("Icons")]
        public Sprite hiddenIndicatorIcon;
        [FoldoutGroup("Icons")]
        public Sprite sparklingIndicatorIcon;
        [FoldoutGroup("Icons")]
        public Sprite crunchedBackgroundSprite;
        [FoldoutGroup("Buy Icons")]
        public Sprite cashSmallAmount, cashMediumAmount, cashLargeAmount;
        [FoldoutGroup("Buy Icons")]
        public Sprite premiumCurrencySmallAmount, premiumCurrencyMediumAmount, premiumCurrencyLargeAmount;
        [FoldoutGroup("Particle Systems")]
        public ParticleSystem elementDestroyParticlePrefab;
        [FoldoutGroup("Particle Systems")]
        public ParticleSystem bombImpactParticlePrefab;
        [FoldoutGroup("Particle Systems")]
        public ParticleSystem rocketTrailParticlePrefab;
        [FoldoutGroup("Particle Systems")]
        public ParticleSystem addTimePowerupTrailParticlePrefab;
        [FoldoutGroup("Particle Systems")]
        public ParticleSystem addMovesPowerupTrailParticlePrefab;
        [FoldoutGroup("Particle Systems")]
        public ParticleSystem shufflePowerupTrailParticlePrefab;
        [FoldoutGroup("Particle Systems")]
        public ParticleSystem bombPowerupTrailParticlePrefab;
        [FoldoutGroup("Particle Systems")]
        public ParticleSystem discoBallPowerupTrailParticlePrefab;
        [FoldoutGroup("Particle Systems")]
        public ParticleSystem rocketPowerupTrailParticlePrefab;

    }
}
