using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Game
{
    /// <summary>
    /// Represents a grid specialized for Match-3 gameplay.
    /// </summary>
    public class Match3Grid : Grid3D
    {
        private int currentComboCount = 0;
        private const int BombSortingOrderBoost = 200;
        
        public override void PreInit()
        {
            // Custom pre-initialization logic for Match-3 grid
        }
        public override void PostInit()
        {
            // Custom post-initialization logic for Match-3 grid
        }

        public bool TryGetElementPosition(GridElement element, out Vector2Int position)
        {
            if (element != null)
            {
                Transform elementParent = element.transform.parent;
                foreach (var tileKvp in generatedTiles)
                {
                    if (tileKvp.Value != null && tileKvp.Value.transform == elementParent)
                    {
                        position = new Vector2Int(tileKvp.Key.x, tileKvp.Key.y);
                        return true;
                    }
                }
            }

            position = default;
            return false;
        }

        public static bool AreAdjacent(Vector2Int first, Vector2Int second)
        {
            return Mathf.Abs(first.x - second.x) + Mathf.Abs(first.y - second.y) == 1;
        }

        public bool IsValidPosition(Vector2Int pos)
        {
            return GetCell(pos) != null;
        }

        public IEnumerator SwapAndMatch(Vector2Int first, Vector2Int second)
        {
            // Check if either element is sparkling
            GridCell firstCell = GetCell(first);
            GridCell secondCell = GetCell(second);

            if (firstCell?.elementInfo?.isBomb == true)
            {
                yield return StartCoroutine(ActivateBombAt(first));
                yield break;
            }
            else if (secondCell?.elementInfo?.isBomb == true)
            {
                yield return StartCoroutine(ActivateBombAt(second));
                yield break;
            }
            
            if (firstCell?.elementInfo?.isSparkling == true)
            {
                yield return StartCoroutine(HandleSparklingSwap(first, second, firstCell, secondCell));
                yield break;
            }
            else if (secondCell?.elementInfo?.isSparkling == true)
            {
                yield return StartCoroutine(HandleSparklingSwap(second, first, secondCell, firstCell));
                yield break;
            }
            
            yield return StartCoroutine(SwapElements(first, second));
            
            EventManager.TriggerEvent(GameEvent.ELEMENTS_SWAPPED, new EventParam(
                vectorList: new Vector3[] { new Vector3(first.x, first.y, 0), new Vector3(second.x, second.y, 0) }
            ));
            
            yield return StartCoroutine(MatchProcess(first, second));
        }

        public IEnumerator MatchProcess(Vector2Int initialElement1, Vector2Int initialElement2)
        {
            List<List<Vector2Int>> matchedGroups;
            currentComboCount = 0;
            // 1. Check for matches
            while ((matchedGroups = CheckMatchOf(3)).Count > 0)
            {
                currentComboCount++;

                List<BombSpawnRequest> bombSpawns = FindBombSpawnsFromSquareMatches(matchedGroups, initialElement1, initialElement2);
                HashSet<Vector2Int> protectedPositions = new HashSet<Vector2Int>();
                for (int i = 0; i < bombSpawns.Count; i++)
                {
                    protectedPositions.Add(bombSpawns[i].position);
                }
                
                // Trigger match detected event
                EventManager.TriggerEvent(GameEvent.MATCH_DETECTED, new EventParam(
                    paramInt: matchedGroups.Count
                ));
                
                // Trigger combo event if this is a chain match
                if (currentComboCount > 1)
                {
                    EventManager.TriggerEvent(GameEvent.COMBO_TRIGGERED, new EventParam(
                        paramInt: currentComboCount
                    ));
                }
                
                // Trigger element matched event for each group
                foreach (var group in matchedGroups)
                {
                    EventManager.TriggerEvent(GameEvent.ELEMENT_MATCHED, new EventParam(
                        paramScriptable: GetCell(group[0])?.elementInfo?.elementData,
                        paramInt: group.Count
                    ));
                }
                
                // 2. Clear matched elements with animations
                yield return StartCoroutine(ClearMatches(matchedGroups, protectedPositions));

                // 2.5 Create bombs after merge/clear animations complete
                for (int i = 0; i < bombSpawns.Count; i++)
                {
                    CreateBombAt(bombSpawns[i].position, bombSpawns[i].sourceData);
                }

                // 3. Apply gravity and refill
                yield return StartCoroutine(ApplyGravity());
            }

            if(currentComboCount == 0)
            {
                // If no matches were found, swap the elements back to their original positions
                EventManager.TriggerEvent(GameEvent.SWAP_FAILED, new EventParam(
                    vectorList: new Vector3[] { new Vector3(initialElement1.x, initialElement1.y, 0), new Vector3(initialElement2.x, initialElement2.y, 0) }
                ));
                yield return StartCoroutine(SwapElements(initialElement1, initialElement2));
            }
            else
            {
                // Grid is now stable after all matches
                EventManager.TriggerEvent(GameEvent.GRID_STABLE);
            }

            yield break;
        }

        /// <summary>
        /// Handles the special swap behavior when a sparkling element is involved.
        /// Converts all elements matching the target type to the sparkling element's type.
        /// Trails are spawned sequentially and convert elements as they arrive.
        /// </summary>
        private IEnumerator HandleSparklingSwap(Vector2Int sparklingPos, Vector2Int targetPos, GridCell sparklingCell, GridCell targetCell)
        {
            if (targetCell?.elementInfo?.elementData == null)
            {
                yield break;
            }
            
            ElementData targetElementData = targetCell.elementInfo.elementData;
            ElementData sparklingElementData = sparklingCell.elementInfo.elementData;
            
            // Move sparkling element to target position
            if (generatedTiles.TryGetValue(sparklingPos, out GridCellController sparklingTile) && 
                generatedTiles.TryGetValue(targetPos, out GridCellController targetTile))
            {
                GridElement sparklingElement = sparklingTile.GetComponentInChildren<GridElement>();
                if (sparklingElement != null)
                {
                    yield return sparklingElement.transform.DOLocalMove(targetTile.transform.position - sparklingTile.transform.position, 
                        GameManager.Instance.constantManager.elementSwapMoveDuration).SetEase(Ease.OutBack).WaitForCompletion();
                }
            }
            
            // Collect all positions that need to be converted
            List<Vector2Int> convertedPositions = new List<Vector2Int>();
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    GridCell cell = GetCell(pos);
                    
                    if (cell?.elementInfo?.elementData == targetElementData)
                    {
                        if (cell.elementInfo.isBomb)
                        {
                            continue;
                        }

                        convertedPositions.Add(pos);
                    }
                }
            }
            
            // Create and animate trail renderers flying to each converted element
            // Elements will be converted as each trail arrives
            if (convertedPositions.Count > 0)
            {
                yield return StartCoroutine(AnimateSparklingTrails(sparklingPos, convertedPositions, sparklingElementData));
            }
            
            // Destroy the sparkling element
            sparklingCell.elementInfo = null;
            if (generatedTiles.TryGetValue(sparklingPos, out GridCellController sparklingElementTile))
            {
                GridElement elementToDestroy = sparklingElementTile.GetComponentInChildren<GridElement>();
                if (elementToDestroy != null)
                {
                    StartCoroutine(elementToDestroy.DestroyElement());
                }
            }
            
            yield return new WaitForSeconds(GameManager.Instance.constantManager.matchClearDelay);
            
            // Check for matches
            yield return StartCoroutine(MatchProcess(targetPos, sparklingPos));
        }
        
        private IEnumerator AnimateSparklingTrails(Vector2Int sourcePos, List<Vector2Int> targetPositions, ElementData sparklingElementData)
        {
            ConstantManager constantManager = GameManager.Instance.constantManager;
            
            if (!generatedTiles.TryGetValue(sourcePos, out GridCellController sourceTile))
            {
                yield break;
            }
            
            Vector3 sourceWorldPos = sourceTile.transform.position;
            List<Coroutine> trailCoroutines = new List<Coroutine>();
            
            // Create and animate trails sequentially with delay
            int trailIndex = 0;
            foreach (var targetPos in targetPositions)
            {
                // Start a coroutine for each individual trail, passing the trail index for shake magnitude
                Coroutine trailCoroutine = StartCoroutine(AnimateSingleTrail(sourceWorldPos, targetPos, sparklingElementData, constantManager, trailIndex));
                trailCoroutines.Add(trailCoroutine);
                
                trailIndex++;
                
                // Wait for the spawn delay before creating the next trail
                yield return new WaitForSeconds(constantManager.sparklingTrailSpawnDelay);
            }
            
            // Wait for all trails to complete (they're already running in parallel)
            // The longest trail will take: sparklingTrailDuration (no fade delay anymore)
            float maxWaitTime = constantManager.sparklingTrailDuration;
            yield return new WaitForSeconds(maxWaitTime);
        }
        
        private IEnumerator AnimateSingleTrail(Vector3 sourcePos, Vector2Int targetPos, ElementData sparklingElementData, ConstantManager constantManager, int trailIndex)
        {
            if (!generatedTiles.TryGetValue(targetPos, out GridCellController targetTile))
            {
                yield break;
            }
            
            Vector3 targetWorldPos = targetTile.transform.position;
            GameObject trailObj = null;
            
            // Instantiate trail prefab or create a default trail object
            if (constantManager.sparklingTrailPrefab != null)
            {
                trailObj = Instantiate(constantManager.sparklingTrailPrefab, sourcePos, Quaternion.identity);
            }
            else
            {
                // Create default trail object if no prefab is assigned
                trailObj = new GameObject($"SparklingTrail_{targetPos.x}_{targetPos.y}");
                trailObj.transform.position = sourcePos;
                
                // Add and configure default trail renderer
                TrailRenderer trail = trailObj.AddComponent<TrailRenderer>();
                trail.time = 0.5f;
                trail.startWidth = 0.1f;
                trail.endWidth = 0f;
                trail.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
                trail.numCapVertices = 5;
                trail.numCornerVertices = 5;
                
                // Create a default gradient
                Gradient gradient = new Gradient();
                GradientColorKey[] colorKeys = new GradientColorKey[2];
                colorKeys[0] = new GradientColorKey(Color.white, 0f);
                colorKeys[1] = new GradientColorKey(Color.white, 1f);
                GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];
                alphaKeys[0] = new GradientAlphaKey(1f, 0f);
                alphaKeys[1] = new GradientAlphaKey(0f, 1f);
                gradient.SetKeys(colorKeys, alphaKeys);
                trail.colorGradient = gradient;
                trail.material = new Material(Shader.Find("Sprites/Default"));
            }
            
            // Animate trail to target position
            yield return trailObj.transform.DOMove(targetWorldPos, constantManager.sparklingTrailDuration).SetEase(Ease.InOutQuad).WaitForCompletion();
            
            // Shake camera with increasing magnitude based on trail index
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                float shakeMagnitude = constantManager.sparklingShakeBaseMagnitude + (trailIndex * constantManager.sparklingShakeMagnitudeIncrement);
                mainCamera.transform.DOShakePosition(
                    constantManager.sparklingShakeDuration,
                    shakeMagnitude,
                    constantManager.sparklingShakeVibrato,
                    constantManager.sparklingShakeRandomness
                );
            }
            
            // Convert the element at this position NOW (after trail arrives)
            GridCell cell = GetCell(targetPos);
            if (cell?.elementInfo != null)
            {
                cell.elementInfo.elementData = sparklingElementData;
                cell.elementInfo.isSparkling = false;
                cell.elementInfo.isBomb = false;
                
                // Update visual representation
                if (generatedTiles.TryGetValue(targetPos, out GridCellController tile))
                {
                    GridElement element = tile.GetComponentInChildren<GridElement>();
                    if (element != null)
                    {
                        element.StopAllCoroutines();
                        element.elementInfo.elementData = sparklingElementData;
                        element.elementInfo.isSparkling = false;
                        element.elementInfo.isBomb = false;
                        element.InitElement(this, element.elementInfo);
                    }
                }
            }
            
            // Destroy trail immediately after reaching destination
            if (trailObj != null)
            {
                Destroy(trailObj);
            }
        }

        private struct BombSpawnRequest
        {
            public Vector2Int position;
            public ElementData sourceData;
        }

        public bool IsBombAt(Vector2Int pos)
        {
            GridCell cell = GetCell(pos);
            return cell?.elementInfo?.isBomb == true;
        }

        public IEnumerator ActivateBombAt(Vector2Int bombPos)
        {
            GridCell bombCell = GetCell(bombPos);
            if (bombCell?.elementInfo == null || !bombCell.elementInfo.isBomb)
            {
                yield break;
            }

            Vector2Int targetPos = GetBombTargetPositionWithObstaclePriority();
            EventManager.TriggerEvent(GameEvent.SPECIAL_ELEMENT_ACTIVATED);
            Vector3 impactWorldPos = generatedTiles.TryGetValue(targetPos, out GridCellController impactTile) && impactTile != null
                ? impactTile.transform.position
                : Vector3.zero;

            if (generatedTiles.TryGetValue(bombPos, out GridCellController bombTile) && bombTile != null)
            {
                GridElement bombElement = bombTile.GetComponentInChildren<GridElement>();
                if (bombElement != null)
                {
                    Transform tempParent = parent != null ? parent : transform;
                    bombElement.transform.SetParent(tempParent, true);

                    if (generatedTiles.TryGetValue(targetPos, out GridCellController targetTile) && targetTile != null)
                    {
                        impactWorldPos = targetTile.transform.position;
                        yield return StartCoroutine(AnimateBombFlight(bombElement, impactWorldPos));
                    }

                    PlayBombImpactEffects(impactWorldPos);
                    StartCoroutine(bombElement.DestroyElement());
                }
            }

            bombCell.elementInfo = null;

            yield return StartCoroutine(ClearAreaAt(targetPos, 1));
            yield return StartCoroutine(ResolveBoardAfterSpecialClear());
        }

        private void PlayBombImpactEffects(Vector3 impactWorldPos)
        {
            ConstantManager constantManager = GameManager.Instance != null ? GameManager.Instance.constantManager : null;
            if (constantManager == null)
            {
                return;
            }

            if (constantManager.bombImpactParticlePrefab != null)
            {
                ParticleSystem impactParticle = Instantiate(constantManager.bombImpactParticlePrefab, impactWorldPos, Quaternion.identity);
                impactParticle.Play();
                Destroy(impactParticle.gameObject, impactParticle.main.duration + impactParticle.main.startLifetime.constantMax + 0.2f);
            }

            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.transform.DOShakePosition(
                    constantManager.bombImpactShakeDuration,
                    constantManager.bombImpactShakeMagnitude,
                    constantManager.bombImpactShakeVibrato,
                    constantManager.bombImpactShakeRandomness
                );
            }
        }

        private IEnumerator AnimateBombFlight(GridElement bombElement, Vector3 targetWorldPosition)
        {
            if (bombElement == null)
            {
                yield break;
            }

            Transform bombTransform = bombElement.transform;
            Vector3 startPos = bombTransform.position;
            float duration = Mathf.Max(0.25f, GameManager.Instance.constantManager.elementSwapMoveDuration * 2f);

            Vector3 arcOffset = Vector3.up * 1.1f;
            Vector3 midPoint = Vector3.Lerp(startPos, targetWorldPosition, 0.5f) + arcOffset;
            Vector3[] path = new Vector3[] { startPos, midPoint, targetWorldPosition };

            Sequence flightSequence = DOTween.Sequence();
            flightSequence.Join(bombTransform.DOPath(path, duration, PathType.CatmullRom)
                .SetEase(Ease.OutQuad)
                .SetOptions(false));
            flightSequence.Join(bombTransform.DORotate(new Vector3(0f, 0f, 540f), duration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetRelative());
            flightSequence.Join(bombTransform.DOScale(1.18f, duration * 0.45f)
                .SetLoops(2, LoopType.Yoyo)
                .SetEase(Ease.InOutSine));

            yield return flightSequence.WaitForCompletion();
        }

        private IEnumerator ResolveBoardAfterSpecialClear()
        {
            currentComboCount = 0;
            yield return StartCoroutine(ApplyGravity());

            List<List<Vector2Int>> matchedGroups;
            while ((matchedGroups = CheckMatchOf(3)).Count > 0)
            {
                currentComboCount++;

                EventManager.TriggerEvent(GameEvent.MATCH_DETECTED, new EventParam(paramInt: matchedGroups.Count));
                if (currentComboCount > 1)
                {
                    EventManager.TriggerEvent(GameEvent.COMBO_TRIGGERED, new EventParam(paramInt: currentComboCount));
                }

                foreach (var group in matchedGroups)
                {
                    EventManager.TriggerEvent(GameEvent.ELEMENT_MATCHED, new EventParam(
                        paramScriptable: GetCell(group[0])?.elementInfo?.elementData,
                        paramInt: group.Count
                    ));
                }

                yield return StartCoroutine(ClearMatches(matchedGroups));
                yield return StartCoroutine(ApplyGravity());
            }

            EventManager.TriggerEvent(GameEvent.GRID_STABLE);
        }

        private IEnumerator ClearAreaAt(Vector2Int center, int radius)
        {
            HashSet<Vector2Int> wallsToBreak = new HashSet<Vector2Int>();

            for (int x = center.x - radius; x <= center.x + radius; x++)
            {
                for (int y = center.y - radius; y <= center.y + radius; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    GridCell cell = GetCell(pos);
                    if (cell == null)
                    {
                        continue;
                    }

                    if (cell.cellType == CellType.BreakableWall)
                    {
                        wallsToBreak.Add(pos);
                        continue;
                    }

                    if (cell.cellType != CellType.Normal || cell.elementInfo == null)
                    {
                        continue;
                    }

                    cell.elementInfo = null;
                    if (generatedTiles.TryGetValue(pos, out GridCellController tile) && tile != null)
                    {
                        GridElement element = tile.GetComponentInChildren<GridElement>();
                        if (element != null)
                        {
                            StartCoroutine(element.DestroyElement());
                        }
                    }
                }
            }

            yield return new WaitForSeconds(GameManager.Instance.constantManager.matchClearDelay);

            foreach (Vector2Int wallPos in wallsToBreak)
            {
                yield return StartCoroutine(BreakWall(wallPos));
            }
        }

        private Vector2Int GetRandomNormalCellPosition()
        {
            List<Vector2Int> candidates = new List<Vector2Int>();
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    GridCell cell = GetCell(pos);
                    if (cell != null && cell.cellType == CellType.Normal)
                    {
                        candidates.Add(pos);
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return Vector2Int.zero;
            }

            return candidates[Random.Range(0, candidates.Count)];
        }

        private Vector2Int GetBombTargetPositionWithObstaclePriority()
        {
            List<Vector2Int> obstaclePriorityCandidates = new List<Vector2Int>();
            List<Vector2Int> fallbackCandidates = new List<Vector2Int>();

            Vector2Int[] neighborOffsets = new Vector2Int[]
            {
                new Vector2Int(0, 1),
                new Vector2Int(0, -1),
                new Vector2Int(-1, 0),
                new Vector2Int(1, 0)
            };

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    GridCell cell = GetCell(pos);
                    if (cell == null || cell.cellType != CellType.Normal)
                    {
                        continue;
                    }

                    fallbackCandidates.Add(pos);

                    bool hasObstacleNeighbor = false;
                    for (int i = 0; i < neighborOffsets.Length; i++)
                    {
                        GridCell neighbor = GetCell(pos + neighborOffsets[i]);
                        if (neighbor != null && neighbor.cellType == CellType.BreakableWall)
                        {
                            hasObstacleNeighbor = true;
                            break;
                        }
                    }

                    if (hasObstacleNeighbor)
                    {
                        obstaclePriorityCandidates.Add(pos);
                    }
                }
            }

            if (obstaclePriorityCandidates.Count > 0)
            {
                return obstaclePriorityCandidates[Random.Range(0, obstaclePriorityCandidates.Count)];
            }

            if (fallbackCandidates.Count > 0)
            {
                return fallbackCandidates[Random.Range(0, fallbackCandidates.Count)];
            }

            return GetRandomNormalCellPosition();
        }

        private List<BombSpawnRequest> FindBombSpawnsFromSquareMatches(List<List<Vector2Int>> matchedGroups, Vector2Int initialElement1, Vector2Int initialElement2)
        {
            List<BombSpawnRequest> spawns = new List<BombSpawnRequest>();
            HashSet<Vector2Int> usedPositions = new HashSet<Vector2Int>();

            for (int groupIndex = 0; groupIndex < matchedGroups.Count; groupIndex++)
            {
                List<Vector2Int> group = matchedGroups[groupIndex];
                HashSet<Vector2Int> groupSet = new HashSet<Vector2Int>(group);
                bool foundForGroup = false;

                for (int x = 0; x < gridSize.x - 1 && !foundForGroup; x++)
                {
                    for (int y = 0; y < gridSize.y - 1 && !foundForGroup; y++)
                    {
                        Vector2Int p1 = new Vector2Int(x, y);
                        Vector2Int p2 = new Vector2Int(x + 1, y);
                        Vector2Int p3 = new Vector2Int(x, y + 1);
                        Vector2Int p4 = new Vector2Int(x + 1, y + 1);

                        if (!groupSet.Contains(p1) || !groupSet.Contains(p2) || !groupSet.Contains(p3) || !groupSet.Contains(p4))
                        {
                            continue;
                        }

                        ElementData sourceData = GetCell(p1)?.elementInfo?.elementData;
                        if (sourceData == null)
                        {
                            continue;
                        }

                        GridCell p2Cell = GetCell(p2);
                        GridCell p3Cell = GetCell(p3);
                        GridCell p4Cell = GetCell(p4);
                        if (p2Cell?.elementInfo?.elementData != sourceData ||
                            p3Cell?.elementInfo?.elementData != sourceData ||
                            p4Cell?.elementInfo?.elementData != sourceData)
                        {
                            continue;
                        }

                        Vector2Int spawnPos = p1;
                        if (groupSet.Contains(initialElement2) && (initialElement2 == p1 || initialElement2 == p2 || initialElement2 == p3 || initialElement2 == p4))
                        {
                            spawnPos = initialElement2;
                        }
                        else if (groupSet.Contains(initialElement1) && (initialElement1 == p1 || initialElement1 == p2 || initialElement1 == p3 || initialElement1 == p4))
                        {
                            spawnPos = initialElement1;
                        }

                        if (usedPositions.Add(spawnPos))
                        {
                            spawns.Add(new BombSpawnRequest
                            {
                                position = spawnPos,
                                sourceData = sourceData
                            });
                        }

                        foundForGroup = true;
                    }
                }
            }

            return spawns;
        }

        private void CreateBombAt(Vector2Int pos, ElementData sourceData)
        {
            GridCell cell = GetCell(pos);
            if (cell == null || cell.cellType != CellType.Normal)
            {
                return;
            }

            ElementData bombData = sourceData;
            if (GameManager.Instance.CurrentLevel is LevelScene_Match3Game match3Level && match3Level.bombElementData != null)
            {
                bombData = match3Level.bombElementData;
            }

            if (cell.elementInfo == null)
            {
                cell.elementInfo = new GridElementInfo();
            }

            cell.elementInfo.elementData = bombData;
            cell.elementInfo.isBomb = true;
            cell.elementInfo.isSparkling = false;
            cell.elementInfo.isHidden = false;

            if (generatedTiles.TryGetValue(pos, out GridCellController tile) && tile != null)
            {
                GridElement element = tile.GetComponentInChildren<GridElement>();
                if (element != null)
                {
                    element.elementInfo = cell.elementInfo;
                    element.InitElement(this, cell.elementInfo);
                    ApplyBombSortingPriority(element, true);
                }
            }
        }

        private void ApplyBombSortingPriority(GridElement element, bool isBomb)
        {
            if (element == null)
            {
                return;
            }

            Renderer[] renderers = element.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (isBomb)
                {
                    renderers[i].sortingOrder += BombSortingOrderBoost;
                }
                else if (renderers[i].sortingOrder >= BombSortingOrderBoost)
                {
                    renderers[i].sortingOrder -= BombSortingOrderBoost;
                }
            }
        }

        public IEnumerator SwapElements(Vector2Int first, Vector2Int second)
        {
            Vector2Int firstPos = new Vector2Int(first.x, first.y);
            Vector2Int secondPos = new Vector2Int(second.x, second.y);

            GridCell firstCell = GetCell(firstPos);
            GridCell secondCell = GetCell(secondPos);

            if (firstCell == null || secondCell == null)
            {
                yield break;
            }

            GridElementInfo firstInfo = firstCell.elementInfo;
            GridElementInfo secondInfo = secondCell.elementInfo;

            if (firstInfo == null || secondInfo == null)
            {
                yield break;
            }

            firstCell.elementInfo = secondInfo;
            secondCell.elementInfo = firstInfo;

            if (!generatedTiles.TryGetValue(firstPos, out GridCellController firstTile) ||
                !generatedTiles.TryGetValue(secondPos, out GridCellController secondTile))
            {
                yield break;
            }

            GridElement firstElement = firstTile.GetComponentInChildren<GridElement>();
            GridElement secondElement = secondTile.GetComponentInChildren<GridElement>();

            if (firstElement != null && secondElement != null)
            {
                Transform firstParent = firstElement.transform.parent;
                Transform secondParent = secondElement.transform.parent;

                firstElement.transform.SetParent(secondParent, true);
                secondElement.transform.SetParent(firstParent, true);

                firstElement.transform.DOLocalMove(Vector3.zero, GameManager.Instance.constantManager.elementSwapMoveDuration).SetEase(Ease.OutBack);
                yield return secondElement.transform.DOLocalMove(Vector3.zero, GameManager.Instance.constantManager.elementSwapMoveDuration).SetEase(Ease.OutBack).WaitForCompletion();
            }
            else if (firstElement != null)
            {
                firstElement.transform.SetParent(secondTile.transform, true);
                yield return firstElement.transform.DOLocalMove(Vector3.zero, GameManager.Instance.constantManager.elementSwapMoveDuration).SetEase(Ease.OutBack).WaitForCompletion();
            }
            else if (secondElement != null)
            {
                secondElement.transform.SetParent(firstTile.transform, true);
                yield return secondElement.transform.DOLocalMove(Vector3.zero, GameManager.Instance.constantManager.elementSwapMoveDuration).SetEase(Ease.OutBack).WaitForCompletion();
            }
        }

        protected override void GenerateElements()
        {
            EnsureGridCells();
            List<ElementData> elementPool = new List<ElementData>();
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    GridCell cell = gridCells[x, y];
                    ElementData data = cell != null ? cell.elementInfo?.elementData : null;
                    if (data != null && (cell.elementInfo == null || !cell.elementInfo.isBomb) && !elementPool.Contains(data))
                    {
                        elementPool.Add(data);
                    }
                }
            }

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    GridCell cell = gridCells[x, y];
                    if (cell == null || cell.cellType != CellType.Normal || cell.elementInfo == null || cell.elementInfo.elementData == null)
                    {
                        continue;
                    }

                    if (!UseLevelEditor && !cell.elementInfo.isHidden && elementPool.Count > 1)
                    {
                        List<ElementData> candidates = new List<ElementData>();
                        foreach (ElementData data in elementPool)
                        {
                            if (!WouldCreateMatch(x, y, data))
                            {
                                candidates.Add(data);
                            }
                        }

                        if (candidates.Count > 0)
                        {
                            cell.elementInfo.elementData = candidates[Random.Range(0, candidates.Count)];
                        }
                    }

                    if (!generatedTiles.TryGetValue(cell.coordinates, out GridCellController tile))
                    {
                        continue;
                    }

                    GridElement element = Instantiate(gridElementPrefab, tile.transform.position, Quaternion.identity, tile.transform);
                    element.elementInfo = cell.elementInfo;
                    generatedElements.Add(element);
                    element.InitElement(this, element.elementInfo);
                    ApplyBombSortingPriority(element, cell.elementInfo != null && cell.elementInfo.isBomb);
                }
            }
        }

        private bool WouldCreateMatch(int x, int y, ElementData data)
        {
            return IsSameElement(x - 1, y, data) && IsSameElement(x - 2, y, data)
                || IsSameElement(x, y - 1, data) && IsSameElement(x, y - 2, data);
        }

        private bool IsSameElement(int x, int y, ElementData data)
        {
            GridCell cell = GetCell(new Vector2Int(x, y));
            return cell != null &&
                   cell.cellType == CellType.Normal &&
                   cell.elementInfo != null &&
                   !cell.elementInfo.isHidden &&
                   !cell.elementInfo.isBomb &&
                   cell.elementInfo.elementData == data;
        }

        private List<List<Vector2Int>> CheckMatchOf(int elementCount = 3)
        {
            Dictionary<Vector2Int, ElementData> matchedElements = new Dictionary<Vector2Int, ElementData>();

            ElementData GetElementData(int x, int y)
            {
                GridCell cell = GetCell(new Vector2Int(x, y));
                if (cell == null ||
                    cell.cellType != CellType.Normal ||
                    cell.elementInfo == null ||
                    cell.elementInfo.isHidden ||
                    cell.elementInfo.isBomb)
                {
                    return null;
                }

                return cell.elementInfo.elementData;
            }

            void AddMatched(int x, int y, ElementData data)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (!matchedElements.ContainsKey(pos))
                {
                    matchedElements.Add(pos, data);
                }
            }

            for (int y = 0; y < gridSize.y; y++)
            {
                ElementData currentData = null;
                int runLength = 0;

                for (int x = 0; x < gridSize.x; x++)
                {
                    ElementData data = GetElementData(x, y);

                    if (data != null && data == currentData)
                    {
                        runLength++;
                        continue;
                    }

                    if (currentData != null && runLength >= elementCount)
                    {
                        for (int matchX = x - runLength; matchX < x; matchX++)
                        {
                            AddMatched(matchX, y, currentData);
                        }
                    }

                    currentData = data;
                    runLength = data != null ? 1 : 0;
                }

                if (currentData != null && runLength >= elementCount)
                {
                    for (int matchX = gridSize.x - runLength; matchX < gridSize.x; matchX++)
                    {
                        AddMatched(matchX, y, currentData);
                    }
                }
            }

            for (int x = 0; x < gridSize.x; x++)
            {
                ElementData currentData = null;
                int runLength = 0;

                for (int y = 0; y < gridSize.y; y++)
                {
                    ElementData data = GetElementData(x, y);

                    if (data != null && data == currentData)
                    {
                        runLength++;
                        continue;
                    }

                    if (currentData != null && runLength >= elementCount)
                    {
                        for (int matchY = y - runLength; matchY < y; matchY++)
                        {
                            AddMatched(x, matchY, currentData);
                        }
                    }

                    currentData = data;
                    runLength = data != null ? 1 : 0;
                }

                if (currentData != null && runLength >= elementCount)
                {
                    for (int matchY = gridSize.y - runLength; matchY < gridSize.y; matchY++)
                    {
                        AddMatched(x, matchY, currentData);
                    }
                }
            }

            for (int x = 0; x < gridSize.x - 1; x++)
            {
                for (int y = 0; y < gridSize.y - 1; y++)
                {
                    ElementData bottomLeft = GetElementData(x, y);
                    if (bottomLeft == null)
                    {
                        continue;
                    }

                    ElementData bottomRight = GetElementData(x + 1, y);
                    ElementData topLeft = GetElementData(x, y + 1);
                    ElementData topRight = GetElementData(x + 1, y + 1);

                    if (bottomRight == bottomLeft && topLeft == bottomLeft && topRight == bottomLeft)
                    {
                        AddMatched(x, y, bottomLeft);
                        AddMatched(x + 1, y, bottomLeft);
                        AddMatched(x, y + 1, bottomLeft);
                        AddMatched(x + 1, y + 1, bottomLeft);
                    }
                }
            }

            List<List<Vector2Int>> groups = new List<List<Vector2Int>>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();

            foreach (var kvp in matchedElements)
            {
                if (visited.Contains(kvp.Key))
                {
                    continue;
                }

                List<Vector2Int> group = new List<Vector2Int>();
                Queue<Vector2Int> queue = new Queue<Vector2Int>();
                queue.Enqueue(kvp.Key);
                visited.Add(kvp.Key);

                while (queue.Count > 0)
                {
                    Vector2Int pos = queue.Dequeue();
                    group.Add(pos);
                    ElementData data = matchedElements[pos];

                    Vector2Int[] neighbors =
                    {
                        new Vector2Int(pos.x + 1, pos.y),
                        new Vector2Int(pos.x - 1, pos.y),
                        new Vector2Int(pos.x, pos.y + 1),
                        new Vector2Int(pos.x, pos.y - 1)
                    };

                    foreach (Vector2Int neighbor in neighbors)
                    {
                        if (matchedElements.TryGetValue(neighbor, out ElementData neighborData) &&
                            neighborData == data &&
                            !visited.Contains(neighbor))
                        {
                            visited.Add(neighbor);
                            queue.Enqueue(neighbor);
                        }
                    }
                }

                if (group.Count >= elementCount)
                {
                    groups.Add(group);
                }
            }

            return groups;
        }
        // Clears matched elements from the grid and plays their destruction animations.
        private IEnumerator ClearMatches(List<List<Vector2Int>> matchedPositions, HashSet<Vector2Int> protectedPositions = null)
        {
            // Shake camera based on current combo count
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                ConstantManager constantManager = GameManager.Instance.constantManager;
                float shakeMagnitude = constantManager.matchShakeBaseMagnitude + ((currentComboCount - 1) * constantManager.matchShakeComboMultiplier);
                mainCamera.transform.DOShakePosition(
                    constantManager.matchShakeDuration,
                    shakeMagnitude,
                    constantManager.matchShakeVibrato,
                    constantManager.matchShakeRandomness
                );
            }
            
            HashSet<Vector2Int> clearedPositions = new HashSet<Vector2Int>();
            HashSet<Vector2Int> wallsToBreak = new HashSet<Vector2Int>();
            HashSet<Vector2Int> hiddenToReveal = new HashSet<Vector2Int>();
            Vector2Int[] adjacentOffsets =
            {
                new Vector2Int(0, 1),
                new Vector2Int(0, -1),
                new Vector2Int(-1, 0),
                new Vector2Int(1, 0)
            };

            foreach (var group in matchedPositions)
            {
                Vector2Int? mergeTarget = GetMergeTargetForGroup(group, protectedPositions);

                foreach (var pos in group)
                {
                    if (protectedPositions != null && protectedPositions.Contains(pos))
                    {
                        continue;
                    }

                    if (clearedPositions.Contains(pos))
                    {
                        continue;
                    }

                    Vector2Int gridPos = new Vector2Int(pos.x, pos.y);
                    GridCell cell = GetCell(gridPos);
                    if (cell != null && cell.elementInfo != null)
                    {
                        cell.elementInfo = null;
                        if (generatedTiles.TryGetValue(gridPos, out GridCellController tile))
                        {
                            GridElement element = tile.GetComponentInChildren<GridElement>();
                            if (element != null)
                            {
                                if (mergeTarget.HasValue)
                                {
                                    StartCoroutine(MergeElementIntoTarget(element, mergeTarget.Value));
                                }
                                else
                                {
                                    StartCoroutine(element.DestroyElement());
                                }
                            }
                        }

                        foreach (Vector2Int offset in adjacentOffsets)
                        {
                            Vector2Int adjacentPos = gridPos + offset;
                            GridCell adjacentCell = GetCell(adjacentPos);
                            if (adjacentCell == null)
                            {
                                continue;
                            }

                            if (adjacentCell.cellType == CellType.BreakableWall)
                            {
                                wallsToBreak.Add(adjacentPos);
                            }

                            if (adjacentCell.cellType == CellType.Normal &&
                                adjacentCell.elementInfo != null &&
                                adjacentCell.elementInfo.isHidden)
                            {
                                hiddenToReveal.Add(adjacentPos);
                            }
                        }
                    }

                    clearedPositions.Add(pos);
                }

                yield return new WaitForSeconds(GameManager.Instance.constantManager.matchClearDelay);
            }

            foreach (Vector2Int revealPos in hiddenToReveal)
            {
                RevealHiddenElement(revealPos);
            }

            foreach (Vector2Int wallPos in wallsToBreak)
            {
                yield return StartCoroutine(BreakWall(wallPos));
            }
        }

        private Vector2Int? GetMergeTargetForGroup(List<Vector2Int> group, HashSet<Vector2Int> protectedPositions)
        {
            if (protectedPositions == null || group == null)
            {
                return null;
            }

            for (int i = 0; i < group.Count; i++)
            {
                if (protectedPositions.Contains(group[i]))
                {
                    return group[i];
                }
            }

            return null;
        }

        private IEnumerator MergeElementIntoTarget(GridElement element, Vector2Int targetPos)
        {
            if (element == null)
            {
                yield break;
            }

            if (!generatedTiles.TryGetValue(targetPos, out GridCellController targetTile) || targetTile == null)
            {
                yield return StartCoroutine(element.DestroyElement());
                yield break;
            }

            Transform t = element.transform;
            t.DOKill();

            Collider[] colliders = element.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                colliders[i].enabled = false;
            }

            SetElementEmission(element, 5f);

            float duration = Mathf.Max(0.08f, GameManager.Instance.constantManager.elementSwapMoveDuration * 0.6f);
            Sequence mergeSequence = DOTween.Sequence();
            mergeSequence.Join(t.DOMove(targetTile.transform.position, duration).SetEase(Ease.InBack));
            mergeSequence.Join(t.DOScale(Vector3.zero, duration).SetEase(Ease.InBack));
            mergeSequence.Join(t.DORotate(new Vector3(0f, 0f, 180f), duration, RotateMode.LocalAxisAdd).SetEase(Ease.InQuad));

            EventManager.TriggerEvent(GameEvent.ELEMENT_DESTROYED,
                eventParam: new EventParam(paramScriptable: element.elementInfo != null ? element.elementInfo.elementData : null));

            yield return mergeSequence.WaitForCompletion();

            if (element != null)
            {
                Destroy(element.gameObject);
            }
        }

        private void SetElementEmission(GridElement element, float emissionValue)
        {
            if (element == null)
            {
                return;
            }

            Renderer[] renderers = element.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                Material mat = renderers[i] != null ? renderers[i].material : null;
                if (mat != null && mat.HasProperty("_Emission"))
                {
                    mat.SetFloat("_Emission", emissionValue);
                }
            }
        }

        private void RevealHiddenElement(Vector2Int pos)
        {
            GridCell cell = GetCell(pos);
            if (cell == null || cell.cellType != CellType.Normal || cell.elementInfo == null || !cell.elementInfo.isHidden)
            {
                return;
            }

            cell.elementInfo.isHidden = false;

            if (generatedTiles.TryGetValue(pos, out GridCellController tile) && tile != null)
            {
                GridElement element = tile.GetComponentInChildren<GridElement>();
                if (element != null)
                {
                    element.InitElement(this, cell.elementInfo);
                }
            }
        }

        private IEnumerator BreakWall(Vector2Int wallPos)
        {
            GridCell cell = GetCell(wallPos);
            if (cell == null || cell.cellType != CellType.BreakableWall)
            {
                yield break;
            }

            if (generatedTiles.TryGetValue(wallPos, out GridCellController wallTile) && wallTile != null)
            {
                Transform wallParent = wallTile.transform.parent;
                Vector3 wallPosition = wallTile.transform.position;
                Quaternion wallRotation = wallTile.transform.rotation;
                Vector3 wallScale = wallTile.transform.localScale;

                BreakableWall breakableWall = wallTile.GetComponent<BreakableWall>();
                if (breakableWall != null)
                {
                    yield return StartCoroutine(breakableWall.WallBreak());
                }

                if (tileGenerationData != null && tileGenerationData.normalCell != null)
                {
                    GridCellController normalTile = Instantiate(tileGenerationData.normalCell, wallPosition, wallRotation, wallParent);
                    normalTile.transform.localScale = wallScale;
                    normalTile.Bind(wallPos);
                    generatedTiles[wallPos] = normalTile;
                }

                if (wallTile != null)
                {
                    Destroy(wallTile.gameObject);
                }
            }

            cell.cellType = CellType.Normal;
        }

        private IEnumerator ApplyGravity()
        {
            EventManager.TriggerEvent(GameEvent.GRAVITY_STARTED);
            
            ConstantManager constantManager = GameManager.Instance != null ? GameManager.Instance.constantManager : null;
            float fallSpeed = constantManager != null ? constantManager.elementFallSpeed : 3.3f;

            EnsureGridCells();
            List<ElementData> elementPool = new List<ElementData>();
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    GridCell cell = GetCell(new Vector2Int(x, y));
                    ElementData data = cell != null ? cell.elementInfo?.elementData : null;
                    if (data != null && (cell.elementInfo == null || !cell.elementInfo.isBomb) && !elementPool.Contains(data))
                    {
                        elementPool.Add(data);
                    }
                }
            }

            if (elementPool.Count == 0)
            {
                foreach (GridElement element in generatedElements)
                {
                    if (element != null && element.elementInfo != null && element.elementInfo.elementData != null && !element.elementInfo.isBomb && !elementPool.Contains(element.elementInfo.elementData))
                    {
                        elementPool.Add(element.elementInfo.elementData);
                    }
                }
            }

            if (elementPool.Count == 0)
            {
                yield break;
            }
            
            bool shouldApplySparkling = false;
            float sparklingChance = 0f;
            
            if (GameManager.Instance.CurrentLevel is LevelScene_Match3Game match3Level)
            {
                shouldApplySparkling = currentComboCount >= match3Level.sparklingPowerAfterXCombo;
                sparklingChance = match3Level.sparklingAppearChance;
            }

            Sequence gravitySequence = DOTween.Sequence();
            bool hasTween = false;

            for (int x = 0; x < gridSize.x; x++)
            {
                List<List<int>> sections = new List<List<int>>();
                List<int> currentSection = new List<int>();

                for (int y = 0; y < gridSize.y; y++)
                {
                    Vector2Int cellPos = new Vector2Int(x, y);
                    GridCell cell = GetCell(cellPos);

                    if (cell != null && cell.cellType == CellType.Normal)
                    {
                        currentSection.Add(y);
                    }
                    else
                    {
                        if (currentSection.Count > 0)
                        {
                            sections.Add(new List<int>(currentSection));
                            currentSection.Clear();
                        }
                    }
                }

                if (currentSection.Count > 0)
                {
                    sections.Add(currentSection);
                }

                foreach (List<int> playableRows in sections)
                {
                    if (playableRows.Count == 0)
                    {
                        continue;
                    }

                    int writeIndex = playableRows.Count - 1;
                    int highestExistingElementIndex = -1;

                    for (int readIndex = playableRows.Count - 1; readIndex >= 0; readIndex--)
                    {
                        int readY = playableRows[readIndex];
                        Vector2Int readPos = new Vector2Int(x, readY);

                        GridCell readCell = GetCell(readPos);
                        GridElementInfo movingInfo = readCell != null ? readCell.elementInfo : null;
                        if (movingInfo == null || movingInfo.elementData == null)
                        {
                            continue;
                        }

                        if (highestExistingElementIndex == -1)
                        {
                            highestExistingElementIndex = readIndex;
                        }

                        int targetY = playableRows[writeIndex];
                        Vector2Int targetPos = new Vector2Int(x, targetY);
                        GridCell targetCell = GetCell(targetPos);

                        if (targetCell == null)
                        {
                            continue;
                        }

                        if (targetPos != readPos)
                        {
                            readCell.elementInfo = null;
                            targetCell.elementInfo = movingInfo;

                            if (generatedTiles.TryGetValue(readPos, out GridCellController fromTile) &&
                                generatedTiles.TryGetValue(targetPos, out GridCellController toTile) &&
                                fromTile != null && toTile != null)
                            {
                                GridElement movingElement = fromTile.GetComponentInChildren<GridElement>();
                                if (movingElement != null)
                                {
                                    movingElement.transform.SetParent(toTile.transform, true);
                                    float moveDistance = movingElement.transform.localPosition.magnitude;
                                    float moveDuration = fallSpeed > 0f ? moveDistance / fallSpeed : 0f;
                                    gravitySequence.Join(movingElement.transform.DOLocalMove(Vector3.zero, moveDuration).SetEase(Ease.OutQuad));
                                    hasTween = true;
                                }
                            }
                        }

                        writeIndex--;
                    }

                    int spawnBaseOffset = highestExistingElementIndex != -1 ? (playableRows.Count - highestExistingElementIndex) : 0;

                    for (int emptyIndex = writeIndex; emptyIndex >= 0; emptyIndex--)
                    {
                        int targetY = playableRows[emptyIndex];
                        Vector2Int targetPos = new Vector2Int(x, targetY);
                        GridCell targetCell = GetCell(targetPos);

                        if (targetCell == null)
                        {
                            continue;
                        }

                        ElementData randomData = elementPool[Random.Range(0, elementPool.Count)];
                        bool isSparkling = shouldApplySparkling && Random.value < sparklingChance;
                        GridElementInfo newInfo = new GridElementInfo
                        {
                            elementData = randomData,
                            isSparkling = isSparkling,
                            isBomb = false
                        };
                        targetCell.elementInfo = newInfo;

                        if (generatedTiles.TryGetValue(targetPos, out GridCellController targetTile) && targetTile != null)
                        {
                            int stackOffset = spawnBaseOffset + (writeIndex - emptyIndex + 1);
                            Vector3 spawnWorldPos = targetTile.transform.position + Vector3.up * stackOffset;
                            GridElement newElement = Instantiate(gridElementPrefab, spawnWorldPos, Quaternion.identity);
                            newElement.transform.SetParent(targetTile.transform, true);
                            newElement.elementInfo = newInfo;
                            generatedElements.Add(newElement);
                            newElement.InitElement(this, newInfo);
                            ApplyBombSortingPriority(newElement, false);

                            float spawnDistance = newElement.transform.localPosition.magnitude;
                            float spawnDuration = fallSpeed > 0f ? spawnDistance / fallSpeed : 0f;
                            gravitySequence.Join(newElement.transform.DOLocalMove(Vector3.zero, spawnDuration).SetEase(Ease.Linear));
                            hasTween = true;
                        }
                    }
                }
            }

            generatedElements.RemoveAll(element => element == null);

            if (hasTween)
            {
                yield return gravitySequence.WaitForCompletion();
            }
            
            EventManager.TriggerEvent(GameEvent.GRAVITY_COMPLETED);

            int refilledCount = 0;
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    GridCell cell = GetCell(new Vector2Int(x, y));
                    if (cell != null && cell.elementInfo != null && cell.elementInfo.elementData != null)
                    {
                        refilledCount++;
                    }
                }
            }
            
            if (refilledCount > 0)
            {
                EventManager.TriggerEvent(GameEvent.ELEMENTS_REFILLED, new EventParam(paramInt: refilledCount));
            }
        }
    }
}