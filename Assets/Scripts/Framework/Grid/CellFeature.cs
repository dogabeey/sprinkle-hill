using UnityEngine; using Game.EventManagement;

namespace Game
{

    /// <summary>
    /// Cell features are special properties that can be assigned to grid cells to create unique gameplay mechanics. They can interact with matched elements in various ways, such as being destroyed 
    /// when an element is matched over them or providing bonuses when adjacent elements are matched. Cell features can be used to add variety and strategic depth to the game by introducing different 
    /// types of cells with distinct behaviors.
    /// </summary>
    public abstract class CellFeature : ScriptableObject
    {
        [System.Flags]
        public enum CellFeatureFlags
        {
            None = 0,
            PreventsElements = 1 << 0,
            NotTargetableByDiscoBall = 1 << 1,
            NotTargetableByPropeller = 1 << 2,
        }

        public TileSpriteSet tileSpriteSet;
        public Sprite featureIcon;
        public int spriteLayerIndex;
        public ParticleSystem idleParticleEffect;
        public ParticleSystem destroyParticleEffect;

        public abstract CellFeatureFlags FeatureFlags { get; }

        public virtual TileSpriteSet GetTileSpriteSet(Grid3D.GridCell cell)
        {
            return tileSpriteSet;
        }

        public abstract void OnElementMatchedOverTheCell(Grid3D.GridCell cell, GridElement element);
        public abstract void OnElementMatchedAdjacentToTheCell(Grid3D.GridCell thisCell, Grid3D.GridCell matchedCell, GridElement element);
    }
}
