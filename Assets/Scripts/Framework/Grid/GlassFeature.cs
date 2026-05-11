using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;
using System.Linq;

namespace Game
{
    [System.Serializable]
    public class GlassDamageSpritePair
    {
        [Min(0)] public int missingHealth;
        public Sprite sprite;
    }
    [System.Serializable]
    public class GlassDamageTilingAndOffsetPair
    {
        [Min(0)] public int missingHealth;
        public Vector2 tiling;
        public Vector2 offset;
    }
    /// <summary>
    /// Glass allows elements to fall through, but blocks swaps and matching while active.
    /// It breaks when an element is matched over or adjacent to it.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Cell Feature/Glass...")]
    public class GlassFeature : CellFeature
    {
        [Min(1)] public int defaultGroupHealth = 1;
        public List<GlassDamageSpritePair> damageSprites = new List<GlassDamageSpritePair>();
        public override bool AcceptElements => false;

        public Sprite GetDamageSprite(int missingHealth)
        {
            if (damageSprites == null || damageSprites.Count == 0)
                return null;

            Sprite damageSprite = damageSprites.Where(pair => pair.missingHealth == missingHealth).Select(pair => pair.sprite).FirstOrDefault();
            return damageSprite;
        }

        public override void OnElementMatchedOverTheCell(Grid3D.GridCell cell, GridElement element)
        {
        }

        public override void OnElementMatchedAdjacentToTheCell(Grid3D.GridCell thisCell, Grid3D.GridCell matchedCell, GridElement element)
        {
        }
    }
}
