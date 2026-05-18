using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class Gfx : MonoBehaviour
    {
        [FoldoutGroup("Editor Icons")]
        public Sprite breakableWallIcon, breakableWallIcon2, breakableWallIcon3;
        [FoldoutGroup("Editor Icons")]
        public Sprite hiddenIndicatorIcon;
        [FoldoutGroup("Editor Icons")]
        public Sprite sparklingIndicatorIcon;
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
        [FoldoutGroup("Particle Systems")]
        public ParticleSystem hiddenElementRevealParticlePrefab;
    }
}
