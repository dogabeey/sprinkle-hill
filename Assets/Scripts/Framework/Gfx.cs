using Sirenix.OdinInspector;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class Gfx : MonoBehaviour
    {
        [FoldoutGroup("Sprites")]
        public Sprite addTimeIcon;
        [FoldoutGroup("Sprites")]
        public Sprite shuffleActionIcon;
        [FoldoutGroup("Sprites")]
        public Sprite bombElementIcon;
        [FoldoutGroup("Sprites")]
        public Sprite discoBallElementIcon;
        [FoldoutGroup("Sprites")]
        public Sprite rocketElementIcon;
        [FoldoutGroup("Sprites")]
        public Sprite easyLevelIcon;
        [FoldoutGroup("Sprites")]
        public Sprite mediumLevelIcon;
        [FoldoutGroup("Sprites")]
        public Sprite hardLevelIcon;
        [FoldoutGroup("Sprites")]
        public Sprite hiddenIndicatorIcon;
        [FoldoutGroup("Sprites")]
        public Sprite sparklingIndicatorIcon;
        [FoldoutGroup("Sprites")]
        public Sprite crunchedBackgroundSprite;
        [FoldoutGroup("Particle Systems")]
        public ParticleSystem elementDestroyParticlePrefab;
        [FoldoutGroup("Particle Systems")]
        public ParticleSystem bombImpactParticlePrefab;
        [FoldoutGroup("Particle Systems")]
        public ParticleSystem rocketTrailParticlePrefab;
        [FoldoutGroup("Particle Systems")]
        public ParticleSystem addTimePowerupTrailParticlePrefab;
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
