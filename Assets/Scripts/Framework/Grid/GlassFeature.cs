using UnityEngine;
using System.Collections.Generic;
using Sirenix.OdinInspector;

namespace Game
{
    public enum DamageVisualType
    {
        Sprite,
        TilingAndOffset
    }
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
        public DamageVisualType damageVisualType = DamageVisualType.Sprite;
        [Min(1)] public int defaultGroupHealth = 1;
        [ShowIf(nameof(IsDamageVisualTypeSprite))]
        public List<GlassDamageSpritePair> damageSprites = new List<GlassDamageSpritePair>();
        [HideIf(nameof(IsDamageVisualTypeSprite))]
        public Texture2D damageIndicatorTextureSheet;
        [HideIf(nameof(IsDamageVisualTypeSprite))]
        public List<GlassDamageTilingAndOffsetPair> damageTilingAndOffsets = new List<GlassDamageTilingAndOffsetPair>();
        public override bool AcceptElements => true;

        public bool IsDamageVisualTypeSprite => damageVisualType == DamageVisualType.Sprite;

        public Sprite GetDamageSprite(int missingHealth)
        {
            Sprite bestSprite = null;
            int bestMissingHealth = int.MinValue;

            for (int i = 0; i < damageSprites.Count; i++)
            {
                GlassDamageSpritePair pair = damageSprites[i];
                if (pair == null || pair.sprite == null)
                    continue;

                if (pair.missingHealth > missingHealth)
                    continue;

                if (pair.missingHealth > bestMissingHealth)
                {
                    bestMissingHealth = pair.missingHealth;
                    bestSprite = pair.sprite;
                }
            }

            return bestSprite;
        }
        public (Vector2 tiling, Vector2 offset) GetDamageTilingAndOffset(int missingHealth)
        {
            Vector2 bestTiling = Vector2.one;
            Vector2 bestOffset = Vector2.zero;
            int bestMissingHealth = int.MinValue;
            for (int i = 0; i < damageTilingAndOffsets.Count; i++)
            {
                GlassDamageTilingAndOffsetPair pair = damageTilingAndOffsets[i];
                if (pair == null)
                    continue;
                if (pair.missingHealth > missingHealth)
                    continue;
                if (pair.missingHealth > bestMissingHealth)
                {
                    bestMissingHealth = pair.missingHealth;
                    bestTiling = pair.tiling;
                    bestOffset = pair.offset;
                }
            }
            return (bestTiling, bestOffset);
        }

        public override void OnElementMatchedOverTheCell(Grid3D.GridCell cell, GridElement element)
        {
        }

        public override void OnElementMatchedAdjacentToTheCell(Grid3D.GridCell thisCell, Grid3D.GridCell matchedCell, GridElement element)
        {
        }
    }
}
