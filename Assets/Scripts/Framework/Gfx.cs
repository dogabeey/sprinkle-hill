using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class Gfx : MonoBehaviour
    {
        [Header("Currencies")]
        public CurrencyModel cashCurrency;
        public CurrencyModel premiumCurrency;
        [Header("Sprites")]
        public Sprite addTimeIcon;
        public Sprite shuffleActionIcon;
        public Sprite bombElementIcon;
        public Sprite discoBallElementIcon;
        public Sprite rocketElementIcon;
        public Sprite easyLevelIcon;
        public Sprite mediumLevelIcon;
        public Sprite hardLevelIcon;
        [Header("Particle Systems")]
        public ParticleSystem elementDestroyParticlePrefab;
        public ParticleSystem bombImpactParticlePrefab;
        public ParticleSystem rocketTrailParticlePrefab;
        public ParticleSystem addTimePowerupTrailParticlePrefab;
        public ParticleSystem shufflePowerupTrailParticlePrefab;
        public ParticleSystem bombPowerupTrailParticlePrefab;
        public ParticleSystem discoBallPowerupTrailParticlePrefab;
        public ParticleSystem rocketPowerupTrailParticlePrefab;

    }
}
