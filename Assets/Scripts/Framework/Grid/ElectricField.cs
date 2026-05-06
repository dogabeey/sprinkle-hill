using UnityEngine;

namespace Game
{
    [CreateAssetMenu(menuName = "Game/Cell Feature/Electric Field...")]
    public class ElectricField : CellFeature
    {
        [SerializeField] private ParticleSystem powerOutletActivationParticle;

        private const int PoweredOffState = 0;
        private const int PoweredOnState = 1;

        public override bool AcceptElements => true;

        public override TileSpriteSet GetTileSpriteSet(Grid3D.GridCell cell)
        {
            if (IsPowerGeneratorCell(cell) || IsActivatedPowerOutletCell(cell))
                SetPoweredOn(cell);

            return tileSpriteSet;
        }

        public override void OnElementMatchedOverTheCell(Grid3D.GridCell cell, GridElement element)
        {
            TryPowerOn(cell, element);
            TryActivateAdjacentPowerOutlet(cell, element);
        }

        public override void OnElementMatchedAdjacentToTheCell(Grid3D.GridCell thisCell, Grid3D.GridCell matchedCell, GridElement element)
        {
            // Electric field should only react to events on its own cell.
        }

        private static bool IsPoweredOn(Grid3D.GridCell cell)
        {
            return cell != null && (cell.cellFeatureGroupHealth == PoweredOnState || IsPowerGeneratorCell(cell));
        }

        private static void SetPoweredOn(Grid3D.GridCell cell)
        {
            if (cell == null)
                return;

            cell.cellFeatureGroupHealth = PoweredOnState;
            if (cell.cellFeatureGroupMaxHealth < PoweredOnState)
                cell.cellFeatureGroupMaxHealth = PoweredOnState;
        }

        private void TryPowerOn(Grid3D.GridCell electricFieldCell, GridElement sourceElement)
        {
            if (electricFieldCell == null || IsPoweredOn(electricFieldCell))
                return;

            if (HasAdjacentPowerSource(electricFieldCell, sourceElement) || IsActivatedPowerOutletCell(electricFieldCell))
                SetPoweredOn(electricFieldCell);
            else if (electricFieldCell.cellFeatureGroupHealth != PoweredOffState)
                electricFieldCell.cellFeatureGroupHealth = PoweredOffState;
        }

        private void TryActivateAdjacentPowerOutlet(Grid3D.GridCell electricFieldCell, GridElement sourceElement)
        {
            if (!IsPoweredOn(electricFieldCell))
                return;

            Match3Grid grid = ResolveGrid(sourceElement);
            if (grid == null)
                return;

            Vector2Int center = electricFieldCell.coordinates;
            Vector2Int[] adjacentOffsets = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            for (int i = 0; i < adjacentOffsets.Length; i++)
            {
                Grid3D.GridCell adjacentCell = grid.GetCellPublic(center + adjacentOffsets[i]);
                if (!IsPowerOutletCell(adjacentCell))
                    continue;

                if (IsPoweredOn(adjacentCell))
                    continue;

                SetPoweredOn(adjacentCell);
                EmitPowerOutletActivation(grid, adjacentCell.coordinates);

                EventManager.TriggerEvent(GameEvent.OUTLET_ACTIVATED, new EventParam(
                    paramScriptable: adjacentCell.elementInfo != null ? adjacentCell.elementInfo.elementData : null,
                    vectorList: new Vector3[] { new Vector3(adjacentCell.coordinates.x, adjacentCell.coordinates.y, 0f) }
                ));
            }
        }

        private static Match3Grid ResolveGrid(GridElement sourceElement)
        {
            Match3Grid gridFromElement = sourceElement != null ? sourceElement.ownerGrid as Match3Grid : null;
            if (gridFromElement != null)
                return gridFromElement;

            LevelScene_Match3Game currentLevel = GameManager.Instance != null ? GameManager.Instance.CurrentLevel as LevelScene_Match3Game : null;
            return currentLevel != null ? currentLevel.grid as Match3Grid : null;
        }

        private bool HasAdjacentPowerSource(Grid3D.GridCell electricFieldCell, GridElement sourceElement)
        {
            Match3Grid grid = ResolveGrid(sourceElement);
            if (grid == null)
                return false;

            Vector2Int center = electricFieldCell.coordinates;
            Vector2Int[] adjacentOffsets = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            for (int i = 0; i < adjacentOffsets.Length; i++)
            {
                Grid3D.GridCell adjacentCell = grid.GetCellPublic(center + adjacentOffsets[i]);
                if (adjacentCell == null)
                    continue;

                if (IsAdjacentPowerGenerator(adjacentCell) || IsAdjacentPoweredElectricField(adjacentCell))
                    return true;
            }

            return false;
        }

        private static bool IsAdjacentPowerGenerator(Grid3D.GridCell cell)
        {
            return IsPowerGeneratorCell(cell);
        }

        private static bool IsPowerGeneratorCell(Grid3D.GridCell cell)
        {
            if (cell?.elementInfo?.elementData == null || GameManager.Instance == null)
                return false;

            return GameManager.Instance.powerGeneratorElementData == cell.elementInfo.elementData;
        }

        private static bool IsPowerOutletCell(Grid3D.GridCell cell)
        {
            if (cell?.elementInfo?.elementData == null || GameManager.Instance == null)
                return false;

            return GameManager.Instance.powerOutletElementData == cell.elementInfo.elementData;
        }

        private static bool IsActivatedPowerOutletCell(Grid3D.GridCell cell)
        {
            return IsPowerOutletCell(cell) && cell.cellFeatureGroupHealth == PoweredOnState;
        }

        private void EmitPowerOutletActivation(Match3Grid grid, Vector2Int coordinates)
        {
            if (powerOutletActivationParticle == null || grid == null)
                return;

            Vector3 spawnPosition = grid.GetWorldPosition(coordinates);
            ParticleSystem instance = Instantiate(powerOutletActivationParticle, spawnPosition, Quaternion.identity);
            instance.Play();

            ParticleSystem.MainModule main = instance.main;
            float lifeTime = main.duration + main.startLifetime.constantMax + 0.2f;
            Destroy(instance.gameObject, lifeTime);
        }

        private bool IsAdjacentPoweredElectricField(Grid3D.GridCell cell)
        {
            return cell != null && cell.cellFeature == this && IsPoweredOn(cell);
        }
    }
}
