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
        public Sprite topLeftCorner;
        [FoldoutGroup("Tile Types")]
        public Sprite topRightCorner;
        [FoldoutGroup("Tile Types")]
        public Sprite bottomLeftCorner;
        [FoldoutGroup("Tile Types")]
        public Sprite bottomRightCorner;
        [FoldoutGroup("Tile Types")]
        public Sprite topEdge;
        [FoldoutGroup("Tile Types")]
        public Sprite bottomEdge;
        [FoldoutGroup("Tile Types")]
        public Sprite leftEdge;
        [FoldoutGroup("Tile Types")]
        public Sprite rightEdge;
        [FoldoutGroup("Tile Types")]
        public Sprite topTip;
        [FoldoutGroup("Tile Types")]
        public Sprite bottomTip;
        [FoldoutGroup("Tile Types")]
        public Sprite leftTip;
        [FoldoutGroup("Tile Types")]
        public Sprite rightTip;
        [FoldoutGroup("Tile Types")]
        public Sprite verticalTile;
        [FoldoutGroup("Tile Types")]
        public Sprite horizontalTile;
        [FoldoutGroup("Tile Types")]
        public Sprite verticalWithLeftConnectionTile;
        [FoldoutGroup("Tile Types")]
        public Sprite verticalWithRightConnectionTile;
        [FoldoutGroup("Tile Types")]
        public Sprite horizontalWithUpConnectionTile;
        [FoldoutGroup("Tile Types")]
        public Sprite horizontalWithDownConnectionTile;
        [FoldoutGroup("Tile Types")]
        public Sprite onlyVerticalAndHorizontalConnectionTile;
        [FoldoutGroup("Tile Types")]
        public Sprite upperAndLeftTile;
        [FoldoutGroup("Tile Types")]
        public Sprite upperAndRightTile;
        [FoldoutGroup("Tile Types")]
        public Sprite lowerAndLeftTile;
        [FoldoutGroup("Tile Types")]
        public Sprite lowerAndRightTile;
        [FoldoutGroup("Tile Types")]
        public Sprite singleTileStandalone;
        [FoldoutGroup("Tile Types")]
        public Sprite topLeftConvexCorner;
        [FoldoutGroup("Tile Types")]
        public Sprite topRightConvexCorner;
        [FoldoutGroup("Tile Types")]
        public Sprite bottomLeftConvexCorner;
        [FoldoutGroup("Tile Types")]
        public Sprite bottomRightConvexCorner;
        [FoldoutGroup("Tile Types")]
        public Sprite singleInnerTile;
        [FoldoutGroup("Tile Types")]
        public Sprite errorTile;

        public GridCellController breakableWall;
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

                        bool isBreakableWall = gridCells != null &&
                                               i < gridCells.GetLength(0) &&
                                               j < gridCells.GetLength(1) &&
                                               gridCells[i, j] != null &&
                                               gridCells[i, j].cellType == Grid3D.CellType.BreakableWall;

                        GridCellController tilePrefab = isBreakableWall
                            ? (breakableWall != null ? breakableWall : normalCell)
                            : normalCell;

                        if (tilePrefab != null)
                        {
                            GridCellController cellController = Instantiate(tilePrefab, position, Quaternion.identity, parent);

                            if (!isBreakableWall)
                            {
                                Sprite tileSprite = DetermineTileType(createData, i, j, drawStartingCorner, gridCells);
                                if (cellController.gridSprite != null)
                                {
                                    cellController.gridSprite.sprite = FallbackToErrorTile(tileSprite);
                                }
                            }

                            generatedObjects.Add(cellController.gameObject);

                            Vector2Int coordinates = new Vector2Int(i, j);
                            cellController.Bind(coordinates);
                            generatedTileDict.Add(coordinates, cellController);
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

        private Sprite DetermineTileType(bool[,] createData, int x, int y, DrawStartingCorner drawStartingCorner, Grid3D.GridCell[,] gridCells = null, WallSideOverrides wallSideOverrides = WallSideOverrides.None)
        {
            bool isBreakableWall = gridCells != null &&
                                   x < gridCells.GetLength(0) &&
                                   y < gridCells.GetLength(1) &&
                                   gridCells[x, y] != null &&
                                   gridCells[x, y].cellType == Grid3D.CellType.BreakableWall;

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

            if (!up && !left && right && down) return topLeftCorner;
            if (!up && !right && left && down) return topRightCorner;
            if (!down && !left && right && up) return bottomLeftCorner;
            if (!down && !right && left && up) return bottomRightCorner;
            if (!up && down && left && right) return topEdge;
            if (!down && up && left && right) return bottomEdge;
            if (!left && right && up && down) return leftEdge;
            if (!right && left && up && down) return rightEdge;
            if (!up && down && !left && !right) return topTip;
            if (!down && up && !left && !right) return bottomTip;
            if (!left && right && !up && !down) return leftTip;
            if (!right && left && !up && !down) return rightTip;
            if (!left && !right && up && down) return verticalTile;
            if (!up && !down && left && right) return horizontalTile;
            if (!upLeft && !downLeft && up && down && left) return verticalWithLeftConnectionTile;
            if (!upRight && !downRight && up && down && right) return verticalWithRightConnectionTile;
            if (!upLeft && !upRight && left && right && up) return horizontalWithUpConnectionTile;
            if (!downLeft && !downRight && left && right && down) return horizontalWithDownConnectionTile;
            if (up && down && left && right && !upLeft && !upRight && !downLeft && !downRight) return onlyVerticalAndHorizontalConnectionTile;
            if (up && !down && left && !right && !downLeft && !upLeft && !upRight && !downRight) return upperAndLeftTile;
            if (up && !down && !left && right && !downLeft && !upLeft && !upRight && !downRight) return upperAndRightTile;
            if (!up && down && left && !right && !downLeft && !upLeft && !upRight && !downRight) return lowerAndLeftTile;
            if (!up && down && !left && right && !downLeft && !upLeft && !upRight && !downRight) return lowerAndRightTile;
            if (!up && !down && !left && !right) return singleTileStandalone;
            if (!upLeft && upRight && downLeft && downRight) return topLeftConvexCorner;
            if (!upRight && upLeft && downLeft && downRight) return topRightConvexCorner;
            if (!downLeft && upLeft && upRight && downRight) return bottomLeftConvexCorner;
            if (!downRight && upLeft && upRight && downLeft) return bottomRightConvexCorner;
            if (up && down && left && right && upLeft && upRight && downLeft && downRight) return singleInnerTile;

            return FallbackToErrorTile(errorTile);
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
            return tile != null ? tile : errorTile;
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
