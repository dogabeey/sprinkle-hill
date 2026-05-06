using Sirenix.OdinInspector;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game
{
    [CreateAssetMenu(fileName = "New Tile Data", menuName = "Game/Tile Data", order = 1)]
    public class TileData : ScriptableObject
    {
        public enum DrawStartingCorner
        {
            TopLeft,
            TopRight,
            BottomLeft,
            BottomRight
        }

        [Flags]
        public enum WallSideOverrides
        {
            None = 0,
            Up = 1,
            Down = 2,
            Left = 4,
            Right = 8
        }

        [FoldoutGroup("Tile Types")]
        public GridCellController normalCell;
        [FoldoutGroup("Tile Types")]
        public TileSpriteSet normalTileSprites;
        [FoldoutGroup("Tile Types")]
        public TileSpriteSet unbreakableWallTileSprites;

        public GridCellController breakableWall;
        public GridCellController unbreakableWall;
        [Header("Settings")]
        public Vector3 generalPositionOffset;
        public Vector2 spacing;
        //public LayerMask layerMaskToDetect;
        //public float overlapSphereRadius = 0.1f;

        public Dictionary<Vector2Int, GridCellController> Generate(bool[,] createData, Vector3 startPosition, DrawStartingCorner drawStartingCorner, Transform parent, bool combineMeshes = false, Grid3D.GridCell[,] gridCells = null)
        {
            Dictionary<Vector2Int, GridCellController> generatedTileDict = new Dictionary<Vector2Int, GridCellController>();
            List<GameObject> generatedObjects = new List<GameObject>();

            int rows = createData.GetLength(0);
            int cols = createData.GetLength(1);
            for (int i = 0; i < rows; i++)
            {
                for (int j = 0; j < cols; j++)
                {
                    if (createData[i, j])
                    {
                        Vector3 position = Vector3.zero;
                        switch (drawStartingCorner)
                        {
                            case DrawStartingCorner.TopLeft:
                                position = startPosition + new Vector3(i * spacing.x, -j * spacing.y, 0f);
                                break;
                            case DrawStartingCorner.TopRight:
                                position = startPosition + new Vector3(-i * spacing.x, -j * spacing.y, 0f);
                                break;
                            case DrawStartingCorner.BottomLeft:
                                position = startPosition + new Vector3(i * spacing.x, j * spacing.y, 0f);
                                break;
                            case DrawStartingCorner.BottomRight:
                                position = startPosition + new Vector3(-i * spacing.x, j * spacing.y, 0f);
                                break;
                        }

                        Grid3D.GridCell sourceCell = gridCells != null &&
                                                i < gridCells.GetLength(0) &&
                                                j < gridCells.GetLength(1)
                            ? gridCells[i, j]
                            : null;

                        bool isBreakableWall = sourceCell != null && sourceCell.cellType == Grid3D.CellType.BreakableWall;

                        bool isUnbreakableWall = sourceCell != null && sourceCell.cellType == Grid3D.CellType.UnbreakableWall;

                        GridCellController tilePrefab = isBreakableWall
                            ? (breakableWall != null ? breakableWall : normalCell)
                            : isUnbreakableWall
                                ? (unbreakableWall != null ? unbreakableWall : normalCell)
                                : normalCell;

                        if (tilePrefab != null)
                        {
                            GridCellController cellController = Instantiate(tilePrefab, position, Quaternion.identity, parent);

                            if (!isBreakableWall)
                            {
                                TileSpriteSet selectedSet = isUnbreakableWall && unbreakableWallTileSprites != null
                                    ? unbreakableWallTileSprites
                                    : normalTileSprites;

                                Sprite tileSprite = DetermineTileType(createData, i, j, drawStartingCorner, gridCells);
                                if (cellController.gridSprite != null)
                                {
                                    cellController.gridSprite.sprite = FallbackToErrorTile(tileSprite);
                                    Color currentColor = cellController.gridSprite.color;
                                    cellController.gridSprite.color = selectedSet != null
                                        ? selectedSet.GetColorForTile(new Vector2Int(i, j), currentColor)
                                        : currentColor;
                                }
                            }

                            ApplyFeatureSprite(cellController, gridCells, i, j, drawStartingCorner);

                            generatedObjects.Add(cellController.gameObject);

                            Vector2Int coordinates = new Vector2Int(i, j);
                            cellController.Bind(coordinates);
                            if (sourceCell != null && sourceCell.cellType == Grid3D.CellType.BreakableWall)
                            {
                                string conditionName = sourceCell.breakableWallElementCondition != null ? sourceCell.breakableWallElementCondition.name : "<none>";
                                Debug.Log($"[BreakableWallOverride] Generate() at {coordinates} | condition={conditionName}");
                            }
                            ApplyBreakableWallOverrideSprite(cellController, sourceCell);
                            generatedTileDict.Add(coordinates, cellController);

                            if (isBreakableWall)
                            {
                                EventManager.TriggerEvent(GameEvent.BREAKABLE_WALL_CREATED, new EventParam(
                                    vectorList: new Vector3[] { new Vector3(coordinates.x, coordinates.y, 0f) }
                                ));
                            }

                            if (sourceCell != null && sourceCell.cellFeature is WaferFeature)
                            {
                                EventManager.TriggerEvent(GameEvent.WAFER_CREATED, new EventParam(
                                    vectorList: new Vector3[] { new Vector3(coordinates.x, coordinates.y, 0f) },
                                    paramScriptable: sourceCell.cellFeature
                                ));
                            }

                            if (sourceCell != null && sourceCell.cellFeature is GlassFeature)
                            {
                                EventManager.TriggerEvent(GameEvent.GLASS_CREATED, new EventParam(
                                    vectorList: new Vector3[] { new Vector3(coordinates.x, coordinates.y, 0f) },
                                    paramScriptable: sourceCell.cellFeature
                                ));
                            }
                        }
                    }
                }
            }

            List<Transform> transforms = generatedObjects.Select(g => g.transform).ToList();

            SetParentToAveragePos(transforms);
            parent.position += generalPositionOffset;

            if(combineMeshes)
            {
                CombineMeshes(transforms, parent, "Wall", UnityEngine.Rendering.ShadowCastingMode.Off);
                foreach (GameObject obj in generatedObjects)
                {
                    Destroy(obj);
                }
            }


            return generatedTileDict;
        }

        public void RefreshTileSprites(Dictionary<Vector2Int, GridCellController> generatedTileDict, Grid3D.GridCell[,] gridCells, Vector2Int center, DrawStartingCorner drawStartingCorner = DrawStartingCorner.TopLeft, bool includeNeighbors = true)
        {
            if (generatedTileDict == null || gridCells == null)
                return;

            bool[,] createData = BuildCreateData(gridCells);
            ApplyTileSpritesAt(generatedTileDict, gridCells, createData, center.x, center.y, drawStartingCorner);

            if (!includeNeighbors)
                return;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    if (dx == 0 && dy == 0)
                        continue;

                    ApplyTileSpritesAt(generatedTileDict, gridCells, createData, center.x + dx, center.y + dy, drawStartingCorner);
                }
            }
        }

        private bool[,] BuildCreateData(Grid3D.GridCell[,] gridCells)
        {
            int width = gridCells.GetLength(0);
            int height = gridCells.GetLength(1);
            bool[,] createData = new bool[width, height];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Grid3D.GridCell cell = gridCells[x, y];
                    createData[x, y] = cell != null && cell.cellType != Grid3D.CellType.Empty;
                }
            }

            return createData;
        }

        private void ApplyTileSpritesAt(Dictionary<Vector2Int, GridCellController> generatedTileDict, Grid3D.GridCell[,] gridCells, bool[,] createData, int x, int y, DrawStartingCorner drawStartingCorner)
        {
            if (x < 0 || y < 0 || x >= gridCells.GetLength(0) || y >= gridCells.GetLength(1))
                return;

            Vector2Int coordinates = new Vector2Int(x, y);
            if (!generatedTileDict.TryGetValue(coordinates, out GridCellController cellController) || cellController == null)
                return;

            Grid3D.GridCell sourceCell = gridCells[x, y];
            if (sourceCell == null)
                return;

            bool isBreakableWall = sourceCell.cellType == Grid3D.CellType.BreakableWall;
            bool isUnbreakableWall = sourceCell.cellType == Grid3D.CellType.UnbreakableWall;

            if (!isBreakableWall)
            {
                TileSpriteSet selectedSet = isUnbreakableWall && unbreakableWallTileSprites != null
                    ? unbreakableWallTileSprites
                    : normalTileSprites;

                Sprite tileSprite = DetermineTileType(createData, x, y, drawStartingCorner, gridCells);
                if (cellController.gridSprite != null)
                {
                    cellController.gridSprite.sprite = FallbackToErrorTile(tileSprite);
                    Color currentColor = cellController.gridSprite.color;
                    cellController.gridSprite.color = selectedSet != null
                        ? selectedSet.GetColorForTile(coordinates, currentColor)
                        : currentColor;
                }
            }

            if (isBreakableWall)
            {
                string conditionName = sourceCell.breakableWallElementCondition != null ? sourceCell.breakableWallElementCondition.name : "<none>";
            }

            ApplyBreakableWallOverrideSprite(cellController, sourceCell);

            ApplyFeatureSprite(cellController, gridCells, x, y, drawStartingCorner);
        }

        private static void ApplyBreakableWallOverrideSprite(GridCellController cellController, Grid3D.GridCell sourceCell)
        {
            if (cellController == null || sourceCell == null)
            {
                return;
            }

            if (sourceCell.cellType != Grid3D.CellType.BreakableWall)
            {
                return;
            }

            ElementData conditionedElement = sourceCell.breakableWallElementCondition;
            if (conditionedElement == null || conditionedElement.breakableWallOverride == null)
            {
                string conditionName = conditionedElement != null ? conditionedElement.name : "<none>";
                return;
            }

            SpriteRenderer targetRenderer = cellController.gridSprite != null
                ? cellController.gridSprite
                : cellController.GetComponentInChildren<SpriteRenderer>(true);

            if (targetRenderer == null)
            {
                return;
            }

            targetRenderer.sprite = conditionedElement.breakableWallOverride;
        }

        private Sprite DetermineTileType(bool[,] createData, int x, int y, DrawStartingCorner drawStartingCorner, Grid3D.GridCell[,] gridCells = null, WallSideOverrides wallSideOverrides = WallSideOverrides.None)
        {
            Grid3D.GridCell sourceCell = gridCells != null &&
                                         x < gridCells.GetLength(0) &&
                                         y < gridCells.GetLength(1)
                ? gridCells[x, y]
                : null;

            bool isBreakableWall = sourceCell != null && sourceCell.cellType == Grid3D.CellType.BreakableWall;

            bool isUnbreakableWall = sourceCell != null && sourceCell.cellType == Grid3D.CellType.UnbreakableWall;

            if (isBreakableWall)
            {
                return null;
            }

            bool up = false, down = false, left = false, right = false, upLeft = false, upRight = false, downLeft = false, downRight = false;
            switch (drawStartingCorner)
            {
                case DrawStartingCorner.TopLeft:
                    up = y > 0 && createData[x, y - 1] || (wallSideOverrides & WallSideOverrides.Up) != 0;
                    down = y < createData.GetLength(1) - 1 && createData[x, y + 1] || (wallSideOverrides & WallSideOverrides.Down) != 0;
                    left = x > 0 && createData[x - 1, y] || (wallSideOverrides & WallSideOverrides.Left) != 0;
                    right = x < createData.GetLength(0) - 1 && createData[x + 1, y] || (wallSideOverrides & WallSideOverrides.Right) != 0;
                    upLeft = x > 0 && y > 0 && createData[x - 1, y - 1];
                    upRight = x < createData.GetLength(0) - 1 && y > 0 && createData[x + 1, y - 1];
                    downLeft = x > 0 && y < createData.GetLength(1) - 1 && createData[x - 1, y + 1];
                    downRight = x < createData.GetLength(0) - 1 && y < createData.GetLength(1) - 1 && createData[x + 1, y + 1];
                    break;
                case DrawStartingCorner.TopRight:
                    up = y > 0 && createData[x, y - 1] || (wallSideOverrides & WallSideOverrides.Up) != 0;
                    down = y < createData.GetLength(1) - 1 && createData[x, y + 1] || (wallSideOverrides & WallSideOverrides.Down) != 0;
                    right = x > 0 && createData[x - 1, y] || (wallSideOverrides & WallSideOverrides.Right) != 0;
                    left = x < createData.GetLength(0) - 1 && createData[x + 1, y] || (wallSideOverrides & WallSideOverrides.Left) != 0;
                    upRight = x > 0 && y > 0 && createData[x - 1, y - 1];
                    upLeft = x < createData.GetLength(0) - 1 && y > 0 && createData[x + 1, y - 1];
                    downRight = x > 0 && y < createData.GetLength(1) - 1 && createData[x - 1, y + 1];
                    downLeft = x < createData.GetLength(0) - 1 && y < createData.GetLength(1) - 1 && createData[x + 1, y + 1];
                    break;
                case DrawStartingCorner.BottomLeft:
                    up = y < createData.GetLength(1) - 1 && createData[x, y + 1] || (wallSideOverrides & WallSideOverrides.Up) != 0;
                    down = y > 0 && createData[x, y - 1] || (wallSideOverrides & WallSideOverrides.Down) != 0;
                    left = x > 0 && createData[x - 1, y] || (wallSideOverrides & WallSideOverrides.Left) != 0;
                    right = x < createData.GetLength(0) - 1 && createData[x + 1, y] || (wallSideOverrides & WallSideOverrides.Right) != 0;
                    downLeft = x > 0 && y > 0 && createData[x - 1, y - 1];
                    downRight = x < createData.GetLength(0) - 1 && y > 0 && createData[x + 1, y - 1];
                    upLeft = x > 0 && y < createData.GetLength(1) - 1 && createData[x - 1, y + 1];
                    upRight = x < createData.GetLength(0) - 1 && y < createData.GetLength(1) - 1 && createData[x + 1, y + 1];
                    break;
                case DrawStartingCorner.BottomRight:
                    up = y < createData.GetLength(1) - 1 && createData[x, y + 1] || (wallSideOverrides & WallSideOverrides.Up) != 0;
                    down = y > 0 && createData[x, y - 1] || (wallSideOverrides & WallSideOverrides.Down) != 0;
                    right = x > 0 && createData[x - 1, y] || (wallSideOverrides & WallSideOverrides.Right) != 0;
                    left = x < createData.GetLength(0) - 1 && createData[x + 1, y] || (wallSideOverrides & WallSideOverrides.Left) != 0;
                    downRight = x > 0 && y > 0 && createData[x - 1, y - 1];
                    downLeft = x < createData.GetLength(0) - 1 && y > 0 && createData[x + 1, y - 1];
                    upRight = x > 0 && y < createData.GetLength(1) - 1 && createData[x - 1, y + 1];
                    upLeft = x < createData.GetLength(0) - 1 && y < createData.GetLength(1) - 1 && createData[x + 1, y + 1];
                    break;
            }

            TileSpriteSet selectedSet = isUnbreakableWall && unbreakableWallTileSprites != null
                ? unbreakableWallTileSprites
                : normalTileSprites;

            if (!up && !left && right && down) return ResolveSprite(selectedSet != null ? selectedSet.topLeftCorner : null);
            if (!up && !right && left && down) return ResolveSprite(selectedSet != null ? selectedSet.topRightCorner : null);
            if (!down && !left && right && up) return ResolveSprite(selectedSet != null ? selectedSet.bottomLeftCorner : null);
            if (!down && !right && left && up) return ResolveSprite(selectedSet != null ? selectedSet.bottomRightCorner : null);
            if (!up && down && left && right) return ResolveSprite(selectedSet != null ? selectedSet.topEdge : null);
            if (!down && up && left && right) return ResolveSprite(selectedSet != null ? selectedSet.bottomEdge : null);
            if (!left && right && up && down) return ResolveSprite(selectedSet != null ? selectedSet.leftEdge : null);
            if (!right && left && up && down) return ResolveSprite(selectedSet != null ? selectedSet.rightEdge : null);
            if (!up && down && !left && !right) return ResolveSprite(selectedSet != null ? selectedSet.topTip : null);
            if (!down && up && !left && !right) return ResolveSprite(selectedSet != null ? selectedSet.bottomTip : null);
            if (!left && right && !up && !down) return ResolveSprite(selectedSet != null ? selectedSet.leftTip : null);
            if (!right && left && !up && !down) return ResolveSprite(selectedSet != null ? selectedSet.rightTip : null);
            if (!left && !right && up && down) return ResolveSprite(selectedSet != null ? selectedSet.verticalTile : null);
            if (!up && !down && left && right) return ResolveSprite(selectedSet != null ? selectedSet.horizontalTile : null);
            if (!upLeft && !downLeft && up && down && left) return ResolveSprite(selectedSet != null ? selectedSet.verticalWithLeftConnectionTile : null);
            if (!upRight && !downRight && up && down && right) return ResolveSprite(selectedSet != null ? selectedSet.verticalWithRightConnectionTile : null);
            if (!upLeft && !upRight && left && right && up) return ResolveSprite(selectedSet != null ? selectedSet.horizontalWithUpConnectionTile : null);
            if (!downLeft && !downRight && left && right && down) return ResolveSprite(selectedSet != null ? selectedSet.horizontalWithDownConnectionTile : null);
            if (up && down && left && right && !upLeft && !upRight && !downLeft && !downRight) return ResolveSprite(selectedSet != null ? selectedSet.onlyVerticalAndHorizontalConnectionTile : null);
            if (up && !down && left && !right && !downLeft && !upLeft && !upRight && !downRight) return ResolveSprite(selectedSet != null ? selectedSet.upperAndLeftTile : null);
            if (up && !down && !left && right && !downLeft && !upLeft && !upRight && !downRight) return ResolveSprite(selectedSet != null ? selectedSet.upperAndRightTile : null);
            if (!up && down && left && !right && !downLeft && !upLeft && !upRight && !downRight) return ResolveSprite(selectedSet != null ? selectedSet.lowerAndLeftTile : null);
            if (!up && down && !left && right && !downLeft && !upLeft && !upRight && !downRight) return ResolveSprite(selectedSet != null ? selectedSet.lowerAndRightTile : null);
            if (!up && !down && !left && !right) return ResolveSprite(selectedSet != null ? selectedSet.singleTileStandalone : null);
            if (!upLeft && upRight && downLeft && downRight) return ResolveSprite(selectedSet != null ? selectedSet.topLeftConvexCorner : null);
            if (!upRight && upLeft && downLeft && downRight) return ResolveSprite(selectedSet != null ? selectedSet.topRightConvexCorner : null);
            if (!downLeft && upLeft && upRight && downRight) return ResolveSprite(selectedSet != null ? selectedSet.bottomLeftConvexCorner : null);
            if (!downRight && upLeft && upRight && downLeft) return ResolveSprite(selectedSet != null ? selectedSet.bottomRightConvexCorner : null);
            if (up && down && left && right && upLeft && upRight && downLeft && downRight) return ResolveSprite(selectedSet != null ? selectedSet.singleInnerTile : null);

            return ResolveSprite(selectedSet != null ? selectedSet.errorTile : null);
        }

        public void ApplyFeatureSprite(GridCellController cellController, Grid3D.GridCell[,] gridCells, int x, int y, DrawStartingCorner drawStartingCorner)
        {
            if (cellController == null || cellController.featureSprite == null)
                return;

            Grid3D.GridCell sourceCell = gridCells != null &&
                                         x >= 0 && y >= 0 &&
                                         x < gridCells.GetLength(0) &&
                                         y < gridCells.GetLength(1)
                ? gridCells[x, y]
                : null;

            CellFeature feature = sourceCell != null ? sourceCell.cellFeature : null;
            TileSpriteSet featureSet = feature != null ? feature.GetTileSpriteSet(sourceCell) : null;

            if (feature == null || featureSet == null)
            {
                cellController.featureSprite.sprite = null;
                cellController.featureSprite.enabled = false;
                return;
            }

            int featureGroupIndex = sourceCell != null ? sourceCell.cellFeatureGroupIndex : 0;
            Sprite featureSprite = DetermineFeatureTileType(gridCells, x, y, drawStartingCorner, feature, featureGroupIndex, featureSet);
            cellController.featureSprite.sprite = featureSprite;
            cellController.featureSprite.enabled = featureSprite != null;
            cellController.featureSprite.sortingOrder = feature.spriteLayerIndex;

            Color currentColor = cellController.featureSprite.color;
            cellController.featureSprite.color = featureSet.GetColorForTile(new Vector2Int(x, y), sourceCell, currentColor);

            Material currentMaterial = cellController.featureSprite.sharedMaterial;
            cellController.featureSprite.sharedMaterial = featureSet.GetMaterialForTile(new Vector2Int(x, y), sourceCell, currentMaterial);
        }

        private Sprite DetermineFeatureTileType(Grid3D.GridCell[,] gridCells, int x, int y, DrawStartingCorner drawStartingCorner, CellFeature feature, int featureGroupIndex, TileSpriteSet selectedSet)
        {
            bool up = false, down = false, left = false, right = false, upLeft = false, upRight = false, downLeft = false, downRight = false;

            switch (drawStartingCorner)
            {
                case DrawStartingCorner.TopLeft:
                    up = HasMatchingFeature(gridCells, x, y - 1, feature, featureGroupIndex);
                    down = HasMatchingFeature(gridCells, x, y + 1, feature, featureGroupIndex);
                    left = HasMatchingFeature(gridCells, x - 1, y, feature, featureGroupIndex);
                    right = HasMatchingFeature(gridCells, x + 1, y, feature, featureGroupIndex);
                    upLeft = HasMatchingFeature(gridCells, x - 1, y - 1, feature, featureGroupIndex);
                    upRight = HasMatchingFeature(gridCells, x + 1, y - 1, feature, featureGroupIndex);
                    downLeft = HasMatchingFeature(gridCells, x - 1, y + 1, feature, featureGroupIndex);
                    downRight = HasMatchingFeature(gridCells, x + 1, y + 1, feature, featureGroupIndex);
                    break;
                case DrawStartingCorner.TopRight:
                    up = HasMatchingFeature(gridCells, x, y - 1, feature, featureGroupIndex);
                    down = HasMatchingFeature(gridCells, x, y + 1, feature, featureGroupIndex);
                    right = HasMatchingFeature(gridCells, x - 1, y, feature, featureGroupIndex);
                    left = HasMatchingFeature(gridCells, x + 1, y, feature, featureGroupIndex);
                    upRight = HasMatchingFeature(gridCells, x - 1, y - 1, feature, featureGroupIndex);
                    upLeft = HasMatchingFeature(gridCells, x + 1, y - 1, feature, featureGroupIndex);
                    downRight = HasMatchingFeature(gridCells, x - 1, y + 1, feature, featureGroupIndex);
                    downLeft = HasMatchingFeature(gridCells, x + 1, y + 1, feature, featureGroupIndex);
                    break;
                case DrawStartingCorner.BottomLeft:
                    up = HasMatchingFeature(gridCells, x, y + 1, feature, featureGroupIndex);
                    down = HasMatchingFeature(gridCells, x, y - 1, feature, featureGroupIndex);
                    left = HasMatchingFeature(gridCells, x - 1, y, feature, featureGroupIndex);
                    right = HasMatchingFeature(gridCells, x + 1, y, feature, featureGroupIndex);
                    downLeft = HasMatchingFeature(gridCells, x - 1, y - 1, feature, featureGroupIndex);
                    downRight = HasMatchingFeature(gridCells, x + 1, y - 1, feature, featureGroupIndex);
                    upLeft = HasMatchingFeature(gridCells, x - 1, y + 1, feature, featureGroupIndex);
                    upRight = HasMatchingFeature(gridCells, x + 1, y + 1, feature, featureGroupIndex);
                    break;
                case DrawStartingCorner.BottomRight:
                    up = HasMatchingFeature(gridCells, x, y + 1, feature, featureGroupIndex);
                    down = HasMatchingFeature(gridCells, x, y - 1, feature, featureGroupIndex);
                    right = HasMatchingFeature(gridCells, x - 1, y, feature, featureGroupIndex);
                    left = HasMatchingFeature(gridCells, x + 1, y, feature, featureGroupIndex);
                    downRight = HasMatchingFeature(gridCells, x - 1, y - 1, feature, featureGroupIndex);
                    downLeft = HasMatchingFeature(gridCells, x + 1, y - 1, feature, featureGroupIndex);
                    upRight = HasMatchingFeature(gridCells, x - 1, y + 1, feature, featureGroupIndex);
                    upLeft = HasMatchingFeature(gridCells, x + 1, y + 1, feature, featureGroupIndex);
                    break;
            }

            if (!up && !left && right && down) return ResolveSprite(selectedSet.topLeftCorner);
            if (!up && !right && left && down) return ResolveSprite(selectedSet.topRightCorner);
            if (!down && !left && right && up) return ResolveSprite(selectedSet.bottomLeftCorner);
            if (!down && !right && left && up) return ResolveSprite(selectedSet.bottomRightCorner);
            if (!up && down && left && right) return ResolveSprite(selectedSet.topEdge);
            if (!down && up && left && right) return ResolveSprite(selectedSet.bottomEdge);
            if (!left && right && up && down) return ResolveSprite(selectedSet.leftEdge);
            if (!right && left && up && down) return ResolveSprite(selectedSet.rightEdge);
            if (!up && down && !left && !right) return ResolveSprite(selectedSet.topTip);
            if (!down && up && !left && !right) return ResolveSprite(selectedSet.bottomTip);
            if (!left && right && !up && !down) return ResolveSprite(selectedSet.leftTip);
            if (!right && left && !up && !down) return ResolveSprite(selectedSet.rightTip);
            if (!left && !right && up && down) return ResolveSprite(selectedSet.verticalTile);
            if (!up && !down && left && right) return ResolveSprite(selectedSet.horizontalTile);
            if (!upLeft && !downLeft && up && down && left) return ResolveSprite(selectedSet.verticalWithLeftConnectionTile);
            if (!upRight && !downRight && up && down && right) return ResolveSprite(selectedSet.verticalWithRightConnectionTile);
            if (!upLeft && !upRight && left && right && up) return ResolveSprite(selectedSet.horizontalWithUpConnectionTile);
            if (!downLeft && !downRight && left && right && down) return ResolveSprite(selectedSet.horizontalWithDownConnectionTile);
            if (up && down && left && right && !upLeft && !upRight && !downLeft && !downRight) return ResolveSprite(selectedSet.onlyVerticalAndHorizontalConnectionTile);
            if (up && !down && left && !right && !downLeft && !upLeft && !upRight && !downRight) return ResolveSprite(selectedSet.upperAndLeftTile);
            if (up && !down && !left && right && !downLeft && !upLeft && !upRight && !downRight) return ResolveSprite(selectedSet.upperAndRightTile);
            if (!up && down && left && !right && !downLeft && !upLeft && !upRight && !downRight) return ResolveSprite(selectedSet.lowerAndLeftTile);
            if (!up && down && !left && right && !downLeft && !upLeft && !upRight && !downRight) return ResolveSprite(selectedSet.lowerAndRightTile);
            if (!up && !down && !left && !right) return ResolveSprite(selectedSet.singleTileStandalone);
            if (!upLeft && upRight && downLeft && downRight) return ResolveSprite(selectedSet.topLeftConvexCorner);
            if (!upRight && upLeft && downLeft && downRight) return ResolveSprite(selectedSet.topRightConvexCorner);
            if (!downLeft && upLeft && upRight && downRight) return ResolveSprite(selectedSet.bottomLeftConvexCorner);
            if (!downRight && upLeft && upRight && downLeft) return ResolveSprite(selectedSet.bottomRightConvexCorner);
            if (up && down && left && right && upLeft && upRight && downLeft && downRight) return ResolveSprite(selectedSet.singleInnerTile);

            return ResolveSprite(selectedSet.errorTile);
        }

        private bool HasMatchingFeature(Grid3D.GridCell[,] gridCells, int x, int y, CellFeature feature, int groupIndex)
        {
            if (feature == null || gridCells == null)
                return false;

            if (x < 0 || y < 0 || x >= gridCells.GetLength(0) || y >= gridCells.GetLength(1))
                return false;

            Grid3D.GridCell cell = gridCells[x, y];
            return cell != null && cell.cellFeature == feature && cell.cellFeatureGroupIndex == groupIndex;
        }

        public static GameObject CombineMeshes<T>(List<T> sources, Transform parent, string objectName, ShadowCastingMode shadowCastingMode = ShadowCastingMode.On) where T : Component
        {
            Dictionary<Material, List<CombineInstance>> materialToCombineInstances =
                new Dictionary<Material, List<CombineInstance>>();

            foreach (var t in sources)
            {
                var meshFilters = t.GetComponentsInChildren<MeshFilter>();
                foreach (var mf in meshFilters)
                {
                    var mr = mf.GetComponent<MeshRenderer>();
                    if (!mf.sharedMesh || !mr) continue;

                    var mesh = mf.sharedMesh;
                    var materials = mr.sharedMaterials;

                    for (int sub = 0; sub < mesh.subMeshCount; sub++)
                    {
                        if (sub >= materials.Length) continue;
                        var mat = materials[sub];

                        if (!materialToCombineInstances.TryGetValue(mat, out var list))
                        {
                            list = new List<CombineInstance>();
                            materialToCombineInstances[mat] = list;
                        }

                        CombineInstance ci = new CombineInstance
                        {
                            mesh = mesh,
                            subMeshIndex = sub,
                            transform = mf.transform.localToWorldMatrix
                        };
                        list.Add(ci);
                    }
                }
            }

            List<Material> finalMaterials = new List<Material>();
            List<CombineInstance> finalCombineInstances = new List<CombineInstance>();

            int subMeshOffset = 0;
            Mesh finalMesh = new Mesh();

            foreach (var kvp in materialToCombineInstances)
            {
                var combinedSubMesh = new Mesh();
                combinedSubMesh.CombineMeshes(kvp.Value.ToArray(), true, true);

                for (int i = 0; i < combinedSubMesh.subMeshCount; i++)
                {
                    CombineInstance ci = new CombineInstance
                    {
                        mesh = combinedSubMesh,
                        subMeshIndex = i,
                        transform = Matrix4x4.identity
                    };
                    finalCombineInstances.Add(ci);
                    finalMaterials.Add(kvp.Key);
                }

                subMeshOffset += combinedSubMesh.subMeshCount;
            }

            finalMesh.CombineMeshes(finalCombineInstances.ToArray(), false, false);

            GameObject mergedObject = new GameObject(objectName);
            mergedObject.transform.parent = parent;

            var mfCombined = mergedObject.AddComponent<MeshFilter>();
            var mrCombined = mergedObject.AddComponent<MeshRenderer>();

            mfCombined.sharedMesh = finalMesh;
            mrCombined.sharedMaterials = finalMaterials.ToArray();
            mrCombined.shadowCastingMode = shadowCastingMode;

            return mergedObject;
        }

        // Take the average position of a list of transforms, get the parent of the first transform, unparent them all, set the parent's position to the average, then reparent them all to the parent.
        public static void SetParentToAveragePos(List<Transform> children)
        {
            if (children.Count == 0) return;
            Vector3 averagePos = Vector3.zero;
            foreach (Transform child in children)
            {
                averagePos += child.position;
            }
            averagePos /= children.Count;
            Transform parent = children[0].parent;
            foreach (Transform child in children)
            {
                child.parent = null;
            }
            parent.position = averagePos;
            foreach (Transform child in children)
            {
                child.parent = parent;
            }
        }
        
        public GameObject DetermineTileType(bool[,] createData, int x, int y, DrawStartingCorner drawStartingCorner, WallSideOverrides wallSideOverrides = WallSideOverrides.None)
        {
            return normalCell != null ? normalCell.gameObject : null;
        }

        private Sprite FallbackToErrorTile(Sprite tile)
        {
            if (tile != null)
                return tile;

            if (normalTileSprites != null && normalTileSprites.errorTile != null)
                return normalTileSprites.errorTile;

            if (unbreakableWallTileSprites != null && unbreakableWallTileSprites.errorTile != null)
                return unbreakableWallTileSprites.errorTile;

            return null;
        }

        private Sprite ResolveSprite(Sprite sprite)
        {
            return FallbackToErrorTile(sprite);
        }
        /*
        /// <summary>
        /// Scans a rectangular area in the game world to decide if there can be walls placed based on the presence of colliders of a specific layermask, then returns a boolean matrix representing where walls should be created.
        /// </summary>
        /// <param name="startingPoint">The world position to start scanning from.</param>
        /// <param name="spacing">Spacing between each scan point.</param>
        /// <param name="width">Span in the X direction (number of points).</param>
        /// <param name="height">Span in the Z direction (number of points).</param>
        /// <param name="layerMaskToDetect">The layermask to use when detecting colliders.</param>
        /// <param name="drawStartingCorner">Which corner the scanning starts from.</param>
        /// <returns></returns>
        public bool[,] ScanTheAreaForWalls(Vector3 startingPoint, Vector2 spacing, int width, int height, LayerMask layerMaskToDetect, DrawStartingCorner drawStartingCorner)
        {
            bool[,] createData = new bool[width, height];
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    Vector3 scanPosition = Vector3.zero;
                    switch (drawStartingCorner)
                    {
                        case DrawStartingCorner.TopLeft:
                            scanPosition = startingPoint + new Vector3(i * spacing.x, 0, -j * spacing.y);
                            break;
                        case DrawStartingCorner.TopRight:
                            scanPosition = startingPoint + new Vector3(-i * spacing.x, 0, -j * spacing.y);
                            break;
                        case DrawStartingCorner.BottomLeft:
                            scanPosition = startingPoint + new Vector3(i * spacing.x, 0, j * spacing.y);
                            break;
                        case DrawStartingCorner.BottomRight:
                            scanPosition = startingPoint + new Vector3(-i * spacing.x, 0, j * spacing.y);
                            break;
                    }
                    Collider[] hitColliders = Physics.OverlapSphere(scanPosition, overlapSphereRadius, layerMaskToDetect);
                    createData[i, j] = hitColliders.Length == 0;
                }
            }
            return createData;
        }
        */
    }
}
