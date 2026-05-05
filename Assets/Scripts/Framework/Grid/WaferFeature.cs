using UnityEngine;

namespace Game
{
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

            CellFeature destroyedFeature = cell.cellFeature;
            Match3Grid grid = element != null ? element.ownerGrid as Match3Grid : null;

            EventManager.TriggerEvent(GameEvent.WAFER_CLEARED, new EventParam(
                paramScriptable: this,
                vectorList: cell != null
                    ? new Vector3[] { new Vector3(cell.coordinates.x, cell.coordinates.y, 0f) }
                    : null
            ));

            cell.cellFeature = null;
            cell.cellFeatureGroupIndex = 0;
            cell.cellFeatureGroupHealth = 0;
            cell.cellFeatureGroupMaxHealth = 0;

            if (grid != null)
                grid.PlayCellFeatureDestroyEffectAt(destroyedFeature, cell.coordinates);
        }
        public override void OnElementMatchedAdjacentToTheCell(Grid3D.GridCell thisCell, Grid3D.GridCell matchedCell, GridElement element)
        {
            // Wafer is only affected by matches directly over its own cell.
        }
    }
}
