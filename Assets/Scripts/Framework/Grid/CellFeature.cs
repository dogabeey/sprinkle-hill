using UnityEngine;

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

        /// <summary>
        /// Indicates whether this cell feature can accept elements to fall into it. If false, the cel this feature is assigned to will act as empty.
        /// </summary>
        public abstract bool AcceptElements { get; }

        public abstract void OnElementMatchedOverTheCell(Grid3D.GridCell cell, GridElement element);
        public abstract void OnElementMatchedAdjacentToTheCell(Grid3D.GridCell thisCell, Grid3D.GridCell matchedCell, GridElement element);
    }

    /// <summary>
    /// Wafer is background feature that breaks and destroyed when elements are matched over it.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Cell Feature/Wafer...")]
    public class WaferFeature : CellFeature
    {
        public override bool AcceptElements => true;

        public override void OnElementMatchedOverTheCell(Grid3D.GridCell cell, GridElement element)
        {
            if (cell == null)
                return;

            EventManager.TriggerEvent(GameEvent.WAFER_CLEARED, new EventParam(
                paramScriptable: this,
                vectorList: cell != null
                    ? new Vector3[] { new Vector3(cell.coordinates.x, cell.coordinates.y, 0f) }
                    : null
            ));

            cell.cellFeature = null;
        }
        public override void OnElementMatchedAdjacentToTheCell(Grid3D.GridCell thisCell, Grid3D.GridCell matchedCell, GridElement element)
        {
            // Wafer is only affected by matches directly over its own cell.
        }
    }

    /// <summary>
    /// Glass allows elements to fall through, but blocks swaps and matching while active.
    /// It breaks when an element is matched over or adjacent to it.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Cell Feature/Glass...")]
    public class GlassFeature : CellFeature
    {
        public override bool AcceptElements => true;

        public override void OnElementMatchedOverTheCell(Grid3D.GridCell cell, GridElement element)
        {
            if (cell == null)
                return;

            cell.cellFeature = null;
        }

        public override void OnElementMatchedAdjacentToTheCell(Grid3D.GridCell thisCell, Grid3D.GridCell matchedCell, GridElement element)
        {
            if (thisCell == null)
                return;

            thisCell.cellFeature = null;
        }
    }
}
