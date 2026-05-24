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
        /// <summary>
        /// Visual representation of the cell feature in the cell.
        /// </summary>
        public TileSpriteSet tileSpriteSet;
        public Sprite featureIcon;
        public int spriteLayerIndex;
        public ParticleSystem idleParticleEffect;
        public ParticleSystem destroyParticleEffect;

        /// <summary>
        /// Indicates whether this cell feature can accept elements to fall into it. If false, the cel this feature is assigned to will act as empty.
        /// </summary>
        public abstract bool AcceptElements { get; }

        public virtual TileSpriteSet GetTileSpriteSet(Grid3D.GridCell cell)
        {
            return tileSpriteSet;
        }

        public abstract void OnElementMatchedOverTheCell(Grid3D.GridCell cell, GridElement element);
        public abstract void OnElementMatchedAdjacentToTheCell(Grid3D.GridCell thisCell, Grid3D.GridCell matchedCell, GridElement element);
    }
}
