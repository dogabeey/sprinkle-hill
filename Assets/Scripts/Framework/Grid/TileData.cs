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

        public GameObject topLeftCorner, topRightCorner, bottomLeftCorner, bottomRightCorner;
        public GameObject topEdge, bottomEdge, leftEdge, rightEdge;
        public GameObject topLeftConvexCorner, topRightConvexCorner, bottomLeftConvexCorner, bottomRightConvexCorner;
        public GameObject topTip, bottomTip, leftTip, rightTip;
        public GameObject singleTileStandalone, singleInnerTile;
        public GameObject verticalTile, horizontalTile;
        public GameObject verticalWithLeftConnectionTile, verticalWithRightConnectionTile, horizontalWithUpConnectionTile, horizontalWithDownConnectionTile;
        public GameObject onlyVerticalAndHorizontalConnectionTile;
        public GameObject upperAndLeftTile, upperAndRightTile, lowerAndLeftTile, lowerAndRightTile;
        public GameObject errorTile;
        [Header("Settings")]
        public Vector3 generalPositionOffset;
        public Vector2 spacing;
        //public LayerMask layerMaskToDetect;
        //public float overlapSphereRadius = 0.1f;

        public Dictionary<Vector3Int, Transform> Generate(bool[,] createData, Vector3 startPosition, DrawStartingCorner drawStartingCorner, Transform parent, bool combineMeshes = false)
        {
            Dictionary<Vector3Int, Transform> generatedTileDict = new Dictionary<Vector3Int, Transform>();
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
                                position = startPosition + new Vector3(i * spacing.x, 0, -j * spacing.y);
                                break;
                            case DrawStartingCorner.TopRight:
                                position = startPosition + new Vector3(-i * spacing.x, 0, -j * spacing.y);
                                break;
                            case DrawStartingCorner.BottomLeft:
                                position = startPosition + new Vector3(i * spacing.x, 0, j * spacing.y);
                                break;
                            case DrawStartingCorner.BottomRight:
                                position = startPosition + new Vector3(-i * spacing.x, 0, j * spacing.y);
                                break;
                        }
                        GameObject tileToInstantiate = DetermineTileType(createData, i, j, drawStartingCorner);
                        if (tileToInstantiate != null)
                        {
                            GameObject obj = Instantiate(tileToInstantiate, position, Quaternion.identity, parent);
                            generatedObjects.Add(obj);
                            generatedTileDict.Add(new Vector3Int(i, 0, j), obj.transform);
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

        /// <summary>
        /// Determines the type of tile to use based on the surrounding tiles.
        /// </summary>
        /// <param name="createData">Boolean matrix representing where tiles should be created.</param>
        /// <param name="x">X coordinates of the index you are trying to determine.</param>
        /// <param name="y">Y coordinates of the index you are trying to determine.</param>
        /// <param name="drawStartingCorner">Which corner the drawing starts from.</param>
        /// <param name="wallSideOverrides">Optional overrides to force certain sides to be treated as walls.</param></param>
        /// <returns></returns>
        public GameObject DetermineTileType(bool[,] createData, int x, int y, DrawStartingCorner drawStartingCorner, WallSideOverrides wallSideOverrides = WallSideOverrides.None)
        {
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
                default:
                    break;
            }
            // Check for corners
            if (!up && !left && right && down) return topLeftCorner;
            if (!up && !right && left && down) return topRightCorner;
            if (!down && !left && right && up) return bottomLeftCorner;
            if (!down && !right && left && up) return bottomRightCorner;
            // Check for edges
            if (!up && down && left && right) return topEdge;
            if (!down && up && left && right) return bottomEdge;
            if (!left && right && up && down) return leftEdge;
            if (!right && left && up && down) return rightEdge;
            // Check for tips
            if (!up && down && !left && !right) return topTip;
            if (!down && up && !left && !right) return bottomTip;
            if (!left && right && !up && !down) return leftTip;
            if (!right && left && !up && !down) return rightTip;
            // Horizontal and vertical tiles
            if (!left && !right && up && down) return verticalTile;
            if (!up && !down && left && right) return horizontalTile;
            // Horizontal and vertical tiles with one connection
            if (!upLeft && !downLeft && up && down && left) return verticalWithLeftConnectionTile;
            if (!upRight && !downRight && up && down && right) return verticalWithRightConnectionTile;
            if (!upLeft && !upRight && left && right && up) return horizontalWithUpConnectionTile;
            if (!downLeft && !downRight && left && right && down) return horizontalWithDownConnectionTile;
            // Horizontal and vertical tile with four connection
            if (up && down && left && right && !upLeft && !upRight && !downLeft && !downRight) return onlyVerticalAndHorizontalConnectionTile;
            // Two connection tiles
            if (up && !down && left && !right && !downLeft && !upLeft && !upRight && !downRight) return upperAndLeftTile;
            if (up && !down && !left && right && !downLeft && !upLeft && !upRight && !downRight) return upperAndRightTile;
            if (!up && down && left && !right && !downLeft && !upLeft && !upRight && !downRight) return lowerAndLeftTile;
            if (!up && down && !left && right && !downLeft && !upLeft && !upRight && !downRight) return lowerAndRightTile;
            // Single tile
            if (!up && !down && !left && !right) return singleTileStandalone;
            // If surrounded on all sides
            // Check for convex corners
            if (!upLeft && upRight && downLeft && downRight) return topLeftConvexCorner;
            if (!upRight && upLeft && downLeft && downRight) return topRightConvexCorner;
            if (!downLeft && upLeft && upRight && downRight) return bottomLeftConvexCorner;
            if (!downRight && upLeft && upRight && downLeft) return bottomRightConvexCorner;
            // If ALL true
            if (up && down && left && right && upLeft && upRight && downLeft && downRight) return singleInnerTile;
            return errorTile;
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
