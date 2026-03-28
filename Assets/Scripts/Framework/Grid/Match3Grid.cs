using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Game
{
    /// <summary>
    /// Represents a grid specialized for Match-3 gameplay.
    /// Power-up detection / creation / activation is handled by <see cref="PowerUpHandler"/>.
    /// Shared renderer helpers live in <see cref="GridHelper"/>.
    /// </summary>
    public class Match3Grid : Grid3D
    {
        private int currentComboCount = 0;
        private PowerUpHandler powerUpHandler;

        // ------------------------------------------------------------------
        //  Public accessors used by PowerUpHandler & helpers
        // ------------------------------------------------------------------
        public Vector2Int GridSize => gridSize;
        public Transform GridParent => parent;

        public GridCell GetCellPublic(Vector2Int pos) => GetCell(pos);

        public GridElement GetElementAt(Vector2Int pos)
        {
            if (generatedTiles.TryGetValue(pos, out GridCellController tile) && tile != null)
                return tile.GetComponentInChildren<GridElement>();
            return null;
        }

        public Vector3 GetWorldPosition(Vector2Int pos)
        {
            if (generatedTiles.TryGetValue(pos, out GridCellController tile) && tile != null)
                return tile.transform.position;
            return Vector3.zero;
        }

        // ------------------------------------------------------------------
        //  Lifecycle
        // ------------------------------------------------------------------
        public override void PreInit()
        {
            powerUpHandler = new PowerUpHandler(this);
        }

        public override void PostInit() { }

        // ------------------------------------------------------------------
        //  Position helpers
        // ------------------------------------------------------------------
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

        public static bool AreAdjacent(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
        }

        public bool IsValidPosition(Vector2Int pos) => GetCell(pos) != null;

        // ------------------------------------------------------------------
        //  Swap & Match entry point
        // ------------------------------------------------------------------
        public IEnumerator SwapAndMatch(Vector2Int first, Vector2Int second)
        {
            GridCell firstCell = GetCell(first);
            GridCell secondCell = GetCell(second);

            // Activate power-ups on swap
            ElementPowerUpType firstType = firstCell?.elementInfo?.powerUpType ?? ElementPowerUpType.None;
            ElementPowerUpType secondType = secondCell?.elementInfo?.powerUpType ?? ElementPowerUpType.None;

            // Click activation (no drag, same position)
            if (first == second)
            {
                if (PowerUpHandler.IsSpecialPowerUp(firstType))
                {
                    yield return StartCoroutine(powerUpHandler.ActivateAt(first, null));
                    yield break;
                }
                yield break;
            }

            // Swap-based power-up activation: swap first, then activate at new position
            if (PowerUpHandler.IsSpecialPowerUp(firstType) || PowerUpHandler.IsSpecialPowerUp(secondType))
            {
                // Capture the non-power-up element's data before the swap for disco ball
                ElementData firstSwapData = firstCell?.elementInfo?.elementData;
                ElementData secondSwapData = secondCell?.elementInfo?.elementData;

                yield return StartCoroutine(SwapElements(first, second));
                EventManager.TriggerEvent(GameEvent.ELEMENTS_SWAPPED, new EventParam(
                    vectorList: new Vector3[] { new Vector3(first.x, first.y, 0), new Vector3(second.x, second.y, 0) }
                ));

                // After swap, the power-up that was at 'first' is now at 'second' and vice versa
                if (PowerUpHandler.IsSpecialPowerUp(firstType))
                {
                    // power-up moved to 'second', the swapped element was at 'second' (now at 'first')
                    yield return StartCoroutine(powerUpHandler.ActivateAt(second, secondSwapData));
                }
                else if (PowerUpHandler.IsSpecialPowerUp(secondType))
                {
                    // power-up moved to 'first', the swapped element was at 'first' (now at 'second')
                    yield return StartCoroutine(powerUpHandler.ActivateAt(first, firstSwapData));
                }
                yield break;
            }

            // Sparkling swap
            if (firstCell?.elementInfo?.isSparkling == true)
            {
                yield return StartCoroutine(HandleSparklingSwap(first, second, firstCell, secondCell));
                yield break;
            }
            if (secondCell?.elementInfo?.isSparkling == true)
            {
                yield return StartCoroutine(HandleSparklingSwap(second, first, secondCell, firstCell));
                yield break;
            }

            // Normal swap
            yield return StartCoroutine(SwapElements(first, second));
            EventManager.TriggerEvent(GameEvent.ELEMENTS_SWAPPED, new EventParam(
                vectorList: new Vector3[] { new Vector3(first.x, first.y, 0), new Vector3(second.x, second.y, 0) }
            ));
            yield return StartCoroutine(MatchProcess(first, second));
        }

        // ------------------------------------------------------------------
        //  Core match loop
        // ------------------------------------------------------------------
        public IEnumerator MatchProcess(Vector2Int init1, Vector2Int init2)
        {
            List<List<Vector2Int>> matchedGroups;
            currentComboCount = 0;

            while ((matchedGroups = CheckMatchOf(3)).Count > 0)
            {
                currentComboCount++;

                // Detect power-up spawns
                List<PowerUpHandler.SpawnRequest> discoBallSpawns = powerUpHandler.FindDiscoBallSpawns(matchedGroups, init1, init2);
                List<PowerUpHandler.SpawnRequest> bombSpawns = powerUpHandler.FindBombSpawns(matchedGroups, init1, init2);

                HashSet<Vector2Int> discoBallPositions = new HashSet<Vector2Int>();
                for (int i = 0; i < discoBallSpawns.Count; i++) discoBallPositions.Add(discoBallSpawns[i].position);

                List<PowerUpHandler.SpawnRequest> rocketSpawns = powerUpHandler.FindRocketSpawns(matchedGroups, init1, init2, discoBallPositions);

                HashSet<Vector2Int> protectedPositions = new HashSet<Vector2Int>(discoBallPositions);
                for (int i = 0; i < bombSpawns.Count; i++) protectedPositions.Add(bombSpawns[i].position);
                for (int i = 0; i < rocketSpawns.Count; i++) protectedPositions.Add(rocketSpawns[i].position);

                // Events
                EventManager.TriggerEvent(GameEvent.MATCH_DETECTED, new EventParam(paramInt: matchedGroups.Count));
                if (currentComboCount > 1)
                    EventManager.TriggerEvent(GameEvent.COMBO_TRIGGERED, new EventParam(paramInt: currentComboCount));
                foreach (var group in matchedGroups)
                    EventManager.TriggerEvent(GameEvent.ELEMENT_MATCHED, new EventParam(
                        paramScriptable: GetCell(group[0])?.elementInfo?.elementData, paramInt: group.Count));

                // Clear
                yield return StartCoroutine(ClearMatches(matchedGroups, protectedPositions));

                // Create power-ups (disco ball has highest priority)
                for (int i = 0; i < discoBallSpawns.Count; i++)
                    powerUpHandler.CreatePowerUpAt(discoBallSpawns[i].position, discoBallSpawns[i].sourceData, discoBallSpawns[i].powerUpType);
                for (int i = 0; i < bombSpawns.Count; i++)
                    powerUpHandler.CreatePowerUpAt(bombSpawns[i].position, bombSpawns[i].sourceData, bombSpawns[i].powerUpType);
                for (int i = 0; i < rocketSpawns.Count; i++)
                    powerUpHandler.CreatePowerUpAt(rocketSpawns[i].position, rocketSpawns[i].sourceData, rocketSpawns[i].powerUpType);

                // Gravity
                yield return StartCoroutine(ApplyGravity());
            }

            if (currentComboCount == 0)
            {
                EventManager.TriggerEvent(GameEvent.SWAP_FAILED, new EventParam(
                    vectorList: new Vector3[] { new Vector3(init1.x, init1.y, 0), new Vector3(init2.x, init2.y, 0) }
                ));
                yield return StartCoroutine(SwapElements(init1, init2));
            }
            else
            {
                EventManager.TriggerEvent(GameEvent.GRID_STABLE);
            }
        }

        // ------------------------------------------------------------------
        //  Board resolution (after special clears)
        // ------------------------------------------------------------------
        public IEnumerator ResolveBoardAfterSpecialClear()
        {
            currentComboCount = 0;
            yield return StartCoroutine(ApplyGravity());

            List<List<Vector2Int>> matchedGroups;
            while ((matchedGroups = CheckMatchOf(3)).Count > 0)
            {
                currentComboCount++;
                EventManager.TriggerEvent(GameEvent.MATCH_DETECTED, new EventParam(paramInt: matchedGroups.Count));
                if (currentComboCount > 1)
                    EventManager.TriggerEvent(GameEvent.COMBO_TRIGGERED, new EventParam(paramInt: currentComboCount));
                foreach (var group in matchedGroups)
                    EventManager.TriggerEvent(GameEvent.ELEMENT_MATCHED, new EventParam(
                        paramScriptable: GetCell(group[0])?.elementInfo?.elementData, paramInt: group.Count));

                yield return StartCoroutine(ClearMatches(matchedGroups));
                yield return StartCoroutine(ApplyGravity());
            }

            EventManager.TriggerEvent(GameEvent.GRID_STABLE);
        }

        // ------------------------------------------------------------------
        //  Area clear (used by bomb)
        // ------------------------------------------------------------------
        public IEnumerator ClearAreaAt(Vector2Int center, int radius)
        {
            HashSet<Vector2Int> wallsToBreak = new HashSet<Vector2Int>();

            for (int x = center.x - radius; x <= center.x + radius; x++)
            {
                for (int y = center.y - radius; y <= center.y + radius; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    GridCell cell = GetCell(pos);
                    if (cell == null) continue;
                    if (cell.cellType == CellType.BreakableWall) { wallsToBreak.Add(pos); continue; }
                    if (cell.cellType != CellType.Normal || cell.elementInfo == null) continue;

                    cell.elementInfo = null;
                    GridElement element = GetElementAt(pos);
                    if (element != null) StartCoroutine(element.DestroyElement());
                }
            }

            yield return new WaitForSeconds(GameManager.Instance.constantManager.matchClearDelay);

            yield return StartCoroutine(BreakWallsSimultaneous(wallsToBreak));
        }

        // ------------------------------------------------------------------
        //  Bomb target selection
        // ------------------------------------------------------------------
        public Vector2Int GetBombTargetPosition()
        {
            List<Vector2Int> obstacleCandidates = new List<Vector2Int>();
            List<Vector2Int> fallback = new List<Vector2Int>();
            Vector2Int[] offsets = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    GridCell cell = GetCell(pos);
                    if (cell == null || cell.cellType != CellType.Normal) continue;
                    fallback.Add(pos);

                    for (int i = 0; i < offsets.Length; i++)
                    {
                        GridCell neighbor = GetCell(pos + offsets[i]);
                        if (neighbor != null && neighbor.cellType == CellType.BreakableWall)
                        {
                            obstacleCandidates.Add(pos);
                            break;
                        }
                    }
                }
            }

            if (obstacleCandidates.Count > 0) return obstacleCandidates[Random.Range(0, obstacleCandidates.Count)];
            if (fallback.Count > 0) return fallback[Random.Range(0, fallback.Count)];
            return Vector2Int.zero;
        }

        // ------------------------------------------------------------------
        //  Sparkling swap
        // ------------------------------------------------------------------
        private IEnumerator HandleSparklingSwap(Vector2Int sparklingPos, Vector2Int targetPos, GridCell sparklingCell, GridCell targetCell)
        {
            if (targetCell?.elementInfo?.elementData == null) yield break;

            ElementData targetElementData = targetCell.elementInfo.elementData;
            ElementData sparklingElementData = sparklingCell.elementInfo.elementData;

            // Animate sparkling element move
            if (generatedTiles.TryGetValue(sparklingPos, out GridCellController sTile) &&
                generatedTiles.TryGetValue(targetPos, out GridCellController tTile))
            {
                GridElement sparklingElement = sTile.GetComponentInChildren<GridElement>();
                if (sparklingElement != null)
                    yield return sparklingElement.transform.DOLocalMove(tTile.transform.position - sTile.transform.position,
                        GameManager.Instance.constantManager.elementSwapMoveDuration).SetEase(Ease.OutBack).WaitForCompletion();
            }

            // Collect convertible positions
            List<Vector2Int> convertedPositions = new List<Vector2Int>();
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    GridCell cell = GetCell(pos);
                    if (cell?.elementInfo?.elementData == targetElementData &&
                        cell.elementInfo.powerUpType == ElementPowerUpType.None)
                        convertedPositions.Add(pos);
                }
            }

            if (convertedPositions.Count > 0)
                yield return StartCoroutine(AnimateSparklingTrails(sparklingPos, convertedPositions, sparklingElementData));

            // Destroy sparkling element
            sparklingCell.elementInfo = null;
            GridElement toDestroy = GetElementAt(sparklingPos);
            if (toDestroy != null) StartCoroutine(toDestroy.DestroyElement());

            yield return new WaitForSeconds(GameManager.Instance.constantManager.matchClearDelay);
            yield return StartCoroutine(MatchProcess(targetPos, sparklingPos));
        }

        private IEnumerator AnimateSparklingTrails(Vector2Int sourcePos, List<Vector2Int> targets, ElementData sparklingData)
        {
            ConstantManager cm = GameManager.Instance.constantManager;
            if (!generatedTiles.TryGetValue(sourcePos, out GridCellController sourceTile)) yield break;

            Vector3 sourceWorldPos = sourceTile.transform.position;
            int trailIndex = 0;
            foreach (var targetPos in targets)
            {
                StartCoroutine(AnimateSingleTrail(sourceWorldPos, targetPos, sparklingData, cm, trailIndex));
                trailIndex++;
                yield return new WaitForSeconds(cm.sparklingTrailSpawnDelay);
            }
            yield return new WaitForSeconds(cm.sparklingTrailDuration);
        }

        private IEnumerator AnimateSingleTrail(Vector3 sourcePos, Vector2Int targetPos, ElementData sparklingData, ConstantManager cm, int trailIndex)
        {
            if (!generatedTiles.TryGetValue(targetPos, out GridCellController targetTile)) yield break;

            Vector3 targetWorldPos = targetTile.transform.position;
            GameObject trailObj = cm.sparklingTrailPrefab != null
                ? Instantiate(cm.sparklingTrailPrefab, sourcePos, Quaternion.identity)
                : CreateDefaultTrail(sourcePos, targetPos);

            yield return trailObj.transform.DOMove(targetWorldPos, cm.sparklingTrailDuration).SetEase(Ease.InOutQuad).WaitForCompletion();

            float shakeMag = cm.sparklingShakeBaseMagnitude + (trailIndex * cm.sparklingShakeMagnitudeIncrement);
            GridHelper.ShakeCamera(cm.sparklingShakeDuration, shakeMag, cm.sparklingShakeVibrato, cm.sparklingShakeRandomness);

            // Convert element
            GridCell cell = GetCell(targetPos);
            if (cell?.elementInfo != null)
            {
                cell.elementInfo.elementData = sparklingData;
                cell.elementInfo.isSparkling = false;
                cell.elementInfo.powerUpType = ElementPowerUpType.None;

                GridElement element = GetElementAt(targetPos);
                if (element != null)
                {
                    element.StopAllCoroutines();
                    element.elementInfo.elementData = sparklingData;
                    element.elementInfo.isSparkling = false;
                    element.elementInfo.powerUpType = ElementPowerUpType.None;
                    element.InitElement(this, element.elementInfo);
                }
            }

            if (trailObj != null) Destroy(trailObj);
        }

        private GameObject CreateDefaultTrail(Vector3 pos, Vector2Int targetPos)
        {
            GameObject obj = new GameObject($"SparklingTrail_{targetPos.x}_{targetPos.y}");
            obj.transform.position = pos;
            TrailRenderer trail = obj.AddComponent<TrailRenderer>();
            trail.time = 0.5f;
            trail.startWidth = 0.1f;
            trail.endWidth = 0f;
            trail.widthCurve = AnimationCurve.Linear(0, 1, 1, 0);
            trail.numCapVertices = 5;
            trail.numCornerVertices = 5;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            return obj;
        }

        // ------------------------------------------------------------------
        //  Swap animation
        // ------------------------------------------------------------------
        public IEnumerator SwapElements(Vector2Int first, Vector2Int second)
        {
            GridCell firstCell = GetCell(first);
            GridCell secondCell = GetCell(second);
            if (firstCell == null || secondCell == null) yield break;

            GridElementInfo firstInfo = firstCell.elementInfo;
            GridElementInfo secondInfo = secondCell.elementInfo;
            if (firstInfo == null || secondInfo == null) yield break;

            firstCell.elementInfo = secondInfo;
            secondCell.elementInfo = firstInfo;

            if (!generatedTiles.TryGetValue(first, out GridCellController firstTile) ||
                !generatedTiles.TryGetValue(second, out GridCellController secondTile))
                yield break;

            GridElement firstEl = firstTile.GetComponentInChildren<GridElement>();
            GridElement secondEl = secondTile.GetComponentInChildren<GridElement>();
            float dur = GameManager.Instance.constantManager.elementSwapMoveDuration;

            if (firstEl != null && secondEl != null)
            {
                Transform fp = firstEl.transform.parent;
                Transform sp = secondEl.transform.parent;
                firstEl.transform.SetParent(sp, true);
                secondEl.transform.SetParent(fp, true);
                firstEl.transform.DOLocalMove(Vector3.zero, dur).SetEase(Ease.OutBack);
                yield return secondEl.transform.DOLocalMove(Vector3.zero, dur).SetEase(Ease.OutBack).WaitForCompletion();
            }
            else if (firstEl != null)
            {
                firstEl.transform.SetParent(secondTile.transform, true);
                yield return firstEl.transform.DOLocalMove(Vector3.zero, dur).SetEase(Ease.OutBack).WaitForCompletion();
            }
            else if (secondEl != null)
            {
                secondEl.transform.SetParent(firstTile.transform, true);
                yield return secondEl.transform.DOLocalMove(Vector3.zero, dur).SetEase(Ease.OutBack).WaitForCompletion();
            }
        }

        // ------------------------------------------------------------------
        //  Element generation
        // ------------------------------------------------------------------
        protected override void GenerateElements()
        {
            EnsureGridCells();
            List<ElementData> elementPool = BuildElementPool();

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    GridCell cell = gridCells[x, y];
                    if (cell == null || cell.cellType != CellType.Normal || cell.elementInfo?.elementData == null)
                        continue;

                    if (!UseLevelEditor && !cell.elementInfo.isHidden && elementPool.Count > 1)
                    {
                        List<ElementData> candidates = new List<ElementData>();
                        foreach (ElementData data in elementPool)
                            if (!WouldCreateMatch(x, y, data)) candidates.Add(data);
                        if (candidates.Count > 0)
                            cell.elementInfo.elementData = candidates[Random.Range(0, candidates.Count)];
                    }

                    if (!generatedTiles.TryGetValue(cell.coordinates, out GridCellController tile)) continue;

                    GridElement element = Instantiate(gridElementPrefab, tile.transform.position, Quaternion.identity, tile.transform);
                    element.elementInfo = cell.elementInfo;
                    generatedElements.Add(element);
                    element.InitElement(this, element.elementInfo);
                    powerUpHandler.ApplySortingBoost(element, cell.elementInfo.powerUpType == ElementPowerUpType.Bomb);
                }
            }
        }

        // ------------------------------------------------------------------
        //  Match detection
        // ------------------------------------------------------------------
        private bool WouldCreateMatch(int x, int y, ElementData data)
        {
            return (IsSameElement(x - 1, y, data) && IsSameElement(x - 2, y, data))
                || (IsSameElement(x, y - 1, data) && IsSameElement(x, y - 2, data));
        }

        private bool IsSameElement(int x, int y, ElementData data)
        {
            GridCell cell = GetCell(new Vector2Int(x, y));
            return cell != null &&
                   cell.cellType == CellType.Normal &&
                   cell.elementInfo != null &&
                   !cell.elementInfo.isHidden &&
                   cell.elementInfo.powerUpType == ElementPowerUpType.None &&
                   cell.elementInfo.elementData == data;
        }

        private List<List<Vector2Int>> CheckMatchOf(int minCount = 3)
        {
            Dictionary<Vector2Int, ElementData> matched = new Dictionary<Vector2Int, ElementData>();

            ElementData GetData(int x, int y)
            {
                GridCell cell = GetCell(new Vector2Int(x, y));
                if (cell == null || cell.cellType != CellType.Normal || cell.elementInfo == null ||
                    cell.elementInfo.isHidden || PowerUpHandler.IsSpecialPowerUp(cell.elementInfo.powerUpType))
                    return null;
                return cell.elementInfo.elementData;
            }

            void Add(int x, int y, ElementData d) { Vector2Int p = new Vector2Int(x, y); if (!matched.ContainsKey(p)) matched.Add(p, d); }

            // Horizontal
            for (int y = 0; y < gridSize.y; y++)
            {
                ElementData cur = null; int run = 0;
                for (int x = 0; x <= gridSize.x; x++)
                {
                    ElementData d = x < gridSize.x ? GetData(x, y) : null;
                    if (d != null && d == cur) { run++; continue; }
                    if (cur != null && run >= minCount)
                        for (int mx = x - run; mx < x; mx++) Add(mx, y, cur);
                    cur = d; run = d != null ? 1 : 0;
                }
            }

            // Vertical
            for (int x = 0; x < gridSize.x; x++)
            {
                ElementData cur = null; int run = 0;
                for (int y = 0; y <= gridSize.y; y++)
                {
                    ElementData d = y < gridSize.y ? GetData(x, y) : null;
                    if (d != null && d == cur) { run++; continue; }
                    if (cur != null && run >= minCount)
                        for (int my = y - run; my < y; my++) Add(x, my, cur);
                    cur = d; run = d != null ? 1 : 0;
                }
            }

            // 2x2 squares
            for (int x = 0; x < gridSize.x - 1; x++)
            {
                for (int y = 0; y < gridSize.y - 1; y++)
                {
                    ElementData bl = GetData(x, y);
                    if (bl != null && GetData(x + 1, y) == bl && GetData(x, y + 1) == bl && GetData(x + 1, y + 1) == bl)
                    {
                        Add(x, y, bl); Add(x + 1, y, bl); Add(x, y + 1, bl); Add(x + 1, y + 1, bl);
                    }
                }
            }

            // Flood-fill into groups
            List<List<Vector2Int>> groups = new List<List<Vector2Int>>();
            HashSet<Vector2Int> visited = new HashSet<Vector2Int>();
            Vector2Int[] dirs = { Vector2Int.right, Vector2Int.left, Vector2Int.up, Vector2Int.down };

            foreach (var kvp in matched)
            {
                if (visited.Contains(kvp.Key)) continue;
                List<Vector2Int> group = new List<Vector2Int>();
                Queue<Vector2Int> queue = new Queue<Vector2Int>();
                queue.Enqueue(kvp.Key); visited.Add(kvp.Key);
                while (queue.Count > 0)
                {
                    Vector2Int pos = queue.Dequeue();
                    group.Add(pos);
                    ElementData data = matched[pos];
                    for (int i = 0; i < dirs.Length; i++)
                    {
                        Vector2Int n = pos + dirs[i];
                        if (matched.TryGetValue(n, out ElementData nd) && nd == data && !visited.Contains(n))
                        { visited.Add(n); queue.Enqueue(n); }
                    }
                }
                if (group.Count >= minCount) groups.Add(group);
            }
            return groups;
        }

        // ------------------------------------------------------------------
        //  Clear matches
        // ------------------------------------------------------------------
        private IEnumerator ClearMatches(List<List<Vector2Int>> matchedPositions, HashSet<Vector2Int> protectedPositions = null)
        {
            ConstantManager cm = GameManager.Instance.constantManager;
            float shakeMag = cm.matchShakeBaseMagnitude + ((currentComboCount - 1) * cm.matchShakeComboMultiplier);
            GridHelper.ShakeCamera(cm.matchShakeDuration, shakeMag, cm.matchShakeVibrato, cm.matchShakeRandomness);

            HashSet<Vector2Int> cleared = new HashSet<Vector2Int>();
            HashSet<Vector2Int> wallsToBreak = new HashSet<Vector2Int>();
            HashSet<Vector2Int> hiddenToReveal = new HashSet<Vector2Int>();
            Vector2Int[] adjacentOffsets = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            foreach (var group in matchedPositions)
            {
                Vector2Int? mergeTarget = FindMergeTarget(group, protectedPositions);

                if (mergeTarget.HasValue)
                {
                    GridElement mergeEl = GetElementAt(mergeTarget.Value);
                    if (mergeEl != null) GridHelper.AnimateEmission(mergeEl, 5f, 0.2f);
                }

                foreach (var pos in group)
                {
                    if (protectedPositions != null && protectedPositions.Contains(pos)) continue;
                    if (cleared.Contains(pos)) continue;

                    GridCell cell = GetCell(pos);
                    if (cell?.elementInfo == null) continue;

                    cell.elementInfo = null;
                    GridElement element = GetElementAt(pos);
                    if (element != null)
                    {
                        if (mergeTarget.HasValue) StartCoroutine(MergeElementIntoTarget(element, mergeTarget.Value));
                        else StartCoroutine(element.DestroyElement());
                    }

                    foreach (Vector2Int offset in adjacentOffsets)
                    {
                        GridCell adj = GetCell(pos + offset);
                        if (adj == null) continue;
                        if (adj.cellType == CellType.BreakableWall) wallsToBreak.Add(pos + offset);
                        if (adj.cellType == CellType.Normal && adj.elementInfo != null && adj.elementInfo.isHidden) hiddenToReveal.Add(pos + offset);
                    }

                    cleared.Add(pos);
                }

                yield return new WaitForSeconds(cm.matchClearDelay);
            }

            foreach (Vector2Int rp in hiddenToReveal) RevealHiddenElement(rp);
            yield return StartCoroutine(BreakWallsSimultaneous(wallsToBreak));
        }

        private Vector2Int? FindMergeTarget(List<Vector2Int> group, HashSet<Vector2Int> protectedPositions)
        {
            if (protectedPositions == null || group == null) return null;
            for (int i = 0; i < group.Count; i++)
                if (protectedPositions.Contains(group[i])) return group[i];
            return null;
        }

        private IEnumerator MergeElementIntoTarget(GridElement element, Vector2Int targetPos)
        {
            if (element == null) yield break;
            GridCellController targetTile = null;
            if (!generatedTiles.TryGetValue(targetPos, out targetTile) || targetTile == null)
            { yield return StartCoroutine(element.DestroyElement()); yield break; }

            Transform t = element.transform;
            t.DOKill();
            Collider[] colliders = element.GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++) colliders[i].enabled = false;

            GridHelper.SetEmission(element, 5f);

            float dur = Mathf.Max(0.08f, GameManager.Instance.constantManager.elementSwapMoveDuration * 0.6f);
            Tween move = t.DOMove(targetTile.transform.position, dur).SetEase(Ease.InBack);

            EventManager.TriggerEvent(GameEvent.ELEMENT_DESTROYED,
                eventParam: new EventParam(paramScriptable: element.elementInfo?.elementData));

            yield return move.WaitForCompletion();
            if (element != null) Destroy(element.gameObject);
        }

        // ------------------------------------------------------------------
        //  Hidden elements & wall breaking
        // ------------------------------------------------------------------
        private void RevealHiddenElement(Vector2Int pos)
        {
            GridCell cell = GetCell(pos);
            if (cell == null || cell.cellType != CellType.Normal || cell.elementInfo == null || !cell.elementInfo.isHidden) return;
            cell.elementInfo.isHidden = false;
            GridElement element = GetElementAt(pos);
            if (element != null) element.InitElement(this, cell.elementInfo);
        }

        public IEnumerator BreakWallAt(Vector2Int wallPos)
        {
            GridCell cell = GetCell(wallPos);
            if (cell == null || cell.cellType != CellType.BreakableWall) yield break;

            if (generatedTiles.TryGetValue(wallPos, out GridCellController wallTile) && wallTile != null)
            {
                Transform wallParent = wallTile.transform.parent;
                Vector3 wallPosition = wallTile.transform.position;
                Quaternion wallRotation = wallTile.transform.rotation;
                Vector3 wallScale = wallTile.transform.localScale;

                BreakableWall breakable = wallTile.GetComponent<BreakableWall>();
                if (breakable != null) yield return StartCoroutine(breakable.WallBreak());

                if (tileGenerationData != null && tileGenerationData.normalCell != null)
                {
                    GridCellController normalTile = Instantiate(tileGenerationData.normalCell, wallPosition, wallRotation, wallParent);
                    normalTile.transform.localScale = wallScale;
                    normalTile.Bind(wallPos);
                    generatedTiles[wallPos] = normalTile;
                }

                if (wallTile != null) Destroy(wallTile.gameObject);
            }

            cell.cellType = CellType.Normal;
        }

        public IEnumerator BreakWallsSimultaneous(HashSet<Vector2Int> wallPositions)
        {
            if (wallPositions == null || wallPositions.Count == 0) yield break;

            List<Coroutine> breakCoroutines = new List<Coroutine>();
            foreach (Vector2Int wallPos in wallPositions)
            {
                breakCoroutines.Add(StartCoroutine(BreakWallAt(wallPos)));
            }

            for (int i = 0; i < breakCoroutines.Count; i++)
            {
                yield return breakCoroutines[i];
            }
        }

        // ------------------------------------------------------------------
        //  Gravity
        // ------------------------------------------------------------------
        private IEnumerator ApplyGravity()
        {
            EventManager.TriggerEvent(GameEvent.GRAVITY_STARTED);

            ConstantManager cm = GameManager.Instance != null ? GameManager.Instance.constantManager : null;
            float fallSpeed = cm != null ? cm.elementFallSpeed : 3.3f;

            EnsureGridCells();
            List<ElementData> elementPool = BuildElementPool();
            if (elementPool.Count == 0)
            {
                // Fallback: scan instantiated elements
                foreach (GridElement el in generatedElements)
                    if (el != null && el.elementInfo != null && el.elementInfo.elementData != null &&
                        el.elementInfo.powerUpType == ElementPowerUpType.None && !elementPool.Contains(el.elementInfo.elementData))
                        elementPool.Add(el.elementInfo.elementData);
            }
            if (elementPool.Count == 0) yield break;

            bool shouldApplySparkling = false;
            float sparklingChance = 0f;
            if (GameManager.Instance.CurrentLevel is LevelScene_Match3Game match3Level)
            {
                shouldApplySparkling = currentComboCount >= match3Level.sparklingPowerAfterXCombo;
                sparklingChance = match3Level.sparklingAppearChance;
            }

            Sequence gravitySeq = DOTween.Sequence();
            bool hasTween = false;

            for (int x = 0; x < gridSize.x; x++)
            {
                List<List<int>> sections = BuildColumnSections(x);

                foreach (List<int> playableRows in sections)
                {
                    if (playableRows.Count == 0) continue;
                    int writeIndex = playableRows.Count - 1;
                    int highestExisting = -1;

                    // Compact existing elements downward
                    for (int readIndex = playableRows.Count - 1; readIndex >= 0; readIndex--)
                    {
                        int readY = playableRows[readIndex];
                        Vector2Int readPos = new Vector2Int(x, readY);
                        GridCell readCell = GetCell(readPos);
                        GridElementInfo movingInfo = readCell?.elementInfo;
                        if (movingInfo?.elementData == null) continue;
                        if (highestExisting == -1) highestExisting = readIndex;

                        int targetY = playableRows[writeIndex];
                        Vector2Int targetPos = new Vector2Int(x, targetY);
                        GridCell targetCell = GetCell(targetPos);
                        if (targetCell == null) continue;

                        if (targetPos != readPos)
                        {
                            readCell.elementInfo = null;
                            targetCell.elementInfo = movingInfo;

                            if (generatedTiles.TryGetValue(readPos, out GridCellController fromTile) &&
                                generatedTiles.TryGetValue(targetPos, out GridCellController toTile) &&
                                fromTile != null && toTile != null)
                            {
                                GridElement movingEl = fromTile.GetComponentInChildren<GridElement>();
                                if (movingEl != null)
                                {
                                    movingEl.transform.SetParent(toTile.transform, true);
                                    float dist = movingEl.transform.localPosition.magnitude;
                                    float dur = fallSpeed > 0f ? dist / fallSpeed : 0f;
                                    gravitySeq.Join(movingEl.transform.DOLocalMove(Vector3.zero, dur).SetEase(Ease.OutQuad));
                                    hasTween = true;
                                }
                            }
                        }
                        writeIndex--;
                    }

                    // Spawn new elements
                    int spawnBase = highestExisting != -1 ? (playableRows.Count - highestExisting) : 0;
                    for (int emptyIdx = writeIndex; emptyIdx >= 0; emptyIdx--)
                    {
                        int targetY = playableRows[emptyIdx];
                        Vector2Int targetPos = new Vector2Int(x, targetY);
                        GridCell targetCell = GetCell(targetPos);
                        if (targetCell == null) continue;

                        ElementData randomData = elementPool[Random.Range(0, elementPool.Count)];
                        bool isSparkling = shouldApplySparkling && Random.value < sparklingChance;
                        GridElementInfo newInfo = new GridElementInfo
                        {
                            elementData = randomData,
                            isSparkling = isSparkling,
                            powerUpType = ElementPowerUpType.None
                        };
                        targetCell.elementInfo = newInfo;

                        if (generatedTiles.TryGetValue(targetPos, out GridCellController targetTile) && targetTile != null)
                        {
                            int stackOffset = spawnBase + (writeIndex - emptyIdx + 1);
                            Vector3 spawnWorldPos = targetTile.transform.position + Vector3.up * stackOffset;
                            GridElement newEl = Instantiate(gridElementPrefab, spawnWorldPos, Quaternion.identity);
                            newEl.transform.SetParent(targetTile.transform, true);
                            newEl.elementInfo = newInfo;
                            generatedElements.Add(newEl);
                            newEl.InitElement(this, newInfo);
                            powerUpHandler.ApplySortingBoost(newEl, false);

                            float dist = newEl.transform.localPosition.magnitude;
                            float dur = fallSpeed > 0f ? dist / fallSpeed : 0f;
                            gravitySeq.Join(newEl.transform.DOLocalMove(Vector3.zero, dur).SetEase(Ease.Linear));
                            hasTween = true;
                        }
                    }
                }
            }

            generatedElements.RemoveAll(el => el == null);
            if (hasTween) yield return gravitySeq.WaitForCompletion();

            EventManager.TriggerEvent(GameEvent.GRAVITY_COMPLETED);

            int refilledCount = 0;
            for (int x = 0; x < gridSize.x; x++)
                for (int y = 0; y < gridSize.y; y++)
                {
                    GridCell cell = GetCell(new Vector2Int(x, y));
                    if (cell?.elementInfo?.elementData != null) refilledCount++;
                }
            if (refilledCount > 0)
                EventManager.TriggerEvent(GameEvent.ELEMENTS_REFILLED, new EventParam(paramInt: refilledCount));
        }

        // ------------------------------------------------------------------
        //  Shared internal helpers
        // ------------------------------------------------------------------
        private List<ElementData> BuildElementPool()
        {
            List<ElementData> pool = new List<ElementData>();
            for (int x = 0; x < gridSize.x; x++)
                for (int y = 0; y < gridSize.y; y++)
                {
                    GridCell cell = GetCell(new Vector2Int(x, y));
                    ElementData data = cell?.elementInfo?.elementData;
                    if (data != null && cell.elementInfo.powerUpType == ElementPowerUpType.None && !pool.Contains(data))
                        pool.Add(data);
                }
            return pool;
        }

        private List<List<int>> BuildColumnSections(int x)
        {
            List<List<int>> sections = new List<List<int>>();
            List<int> current = new List<int>();
            for (int y = 0; y < gridSize.y; y++)
            {
                GridCell cell = GetCell(new Vector2Int(x, y));
                if (cell != null && cell.cellType == CellType.Normal) current.Add(y);
                else
                {
                    if (current.Count > 0) { sections.Add(new List<int>(current)); current.Clear(); }
                }
            }
            if (current.Count > 0) sections.Add(current);
            return sections;
        }
    }

    /// <summary>
    /// Centralizes power-up detection, creation, and activation for Match3Grid.
    /// Keeps Match3Grid focused on core match/swap/gravity logic.
    /// </summary>
    public class PowerUpHandler
    {
        private readonly Match3Grid grid;
        private const int SortingOrderBoost = 200;

        public PowerUpHandler(Match3Grid grid)
        {
            this.grid = grid;
        }

        public static bool IsSpecialPowerUp(ElementPowerUpType type)
        {
            return type != ElementPowerUpType.None;
        }

        public static bool IsRocket(ElementPowerUpType type)
        {
            return type == ElementPowerUpType.HorizontalRocket || type == ElementPowerUpType.VerticalRocket;
        }

        public static bool IsDiscoBall(ElementPowerUpType type)
        {
            return type == ElementPowerUpType.DiscoBall;
        }

        public struct SpawnRequest
        {
            public Vector2Int position;
            public ElementData sourceData;
            public ElementPowerUpType powerUpType;
        }

        // ------------------------------------------------------------------
        //  Spawn detection
        // ------------------------------------------------------------------

        public List<SpawnRequest> FindBombSpawns(List<List<Vector2Int>> matchedGroups, Vector2Int init1, Vector2Int init2)
        {
            List<SpawnRequest> spawns = new List<SpawnRequest>();
            HashSet<Vector2Int> used = new HashSet<Vector2Int>();

            for (int g = 0; g < matchedGroups.Count; g++)
            {
                List<Vector2Int> group = matchedGroups[g];
                HashSet<Vector2Int> groupSet = new HashSet<Vector2Int>(group);
                bool found = false;

                for (int x = 0; x < grid.GridSize.x - 1 && !found; x++)
                {
                    for (int y = 0; y < grid.GridSize.y - 1 && !found; y++)
                    {
                        Vector2Int p1 = new Vector2Int(x, y);
                        Vector2Int p2 = new Vector2Int(x + 1, y);
                        Vector2Int p3 = new Vector2Int(x, y + 1);
                        Vector2Int p4 = new Vector2Int(x + 1, y + 1);

                        if (!groupSet.Contains(p1) || !groupSet.Contains(p2) ||
                            !groupSet.Contains(p3) || !groupSet.Contains(p4))
                            continue;

                        ElementData src = grid.GetCellPublic(p1)?.elementInfo?.elementData;
                        if (src == null) continue;
                        if (grid.GetCellPublic(p2)?.elementInfo?.elementData != src ||
                            grid.GetCellPublic(p3)?.elementInfo?.elementData != src ||
                            grid.GetCellPublic(p4)?.elementInfo?.elementData != src)
                            continue;

                        Vector2Int spawnPos = PickPreferred(groupSet, new[] { p1, p2, p3, p4 }, init1, init2);
                        if (used.Add(spawnPos))
                            spawns.Add(new SpawnRequest { position = spawnPos, sourceData = src, powerUpType = ElementPowerUpType.Bomb });
                        found = true;
                    }
                }
            }
            return spawns;
        }

        public List<SpawnRequest> FindRocketSpawns(List<List<Vector2Int>> matchedGroups, Vector2Int init1, Vector2Int init2, HashSet<Vector2Int> claimedPositions = null)
        {
            List<SpawnRequest> spawns = new List<SpawnRequest>();
            HashSet<Vector2Int> used = new HashSet<Vector2Int>();

            for (int g = 0; g < matchedGroups.Count; g++)
            {
                List<Vector2Int> group = matchedGroups[g];
                if (group == null || group.Count < 4) continue;

                // Groups of 5+ are reserved for Disco Ball — skip entirely
                if (group.Count >= 5) continue;

                HashSet<Vector2Int> groupSet = new HashSet<Vector2Int>(group);
                ElementData src = grid.GetCellPublic(group[0])?.elementInfo?.elementData;
                if (src == null) continue;
                bool found = false;

                // Horizontal runs
                for (int y = 0; y < grid.GridSize.y && !found; y++)
                {
                    int run = 0;
                    HashSet<Vector2Int> runPos = new HashSet<Vector2Int>();
                    for (int x = 0; x <= grid.GridSize.x; x++)
                    {
                        Vector2Int pos = new Vector2Int(x, y);
                        if (x < grid.GridSize.x && groupSet.Contains(pos)) { run++; runPos.Add(pos); }
                        else
                        {
                            if (run >= 4)
                            {
                                Vector2Int sp = PickPreferredFromSet(runPos, init1, init2);
                                // Skip if this position was already claimed by a higher-priority power-up
                                if (claimedPositions != null && claimedPositions.Contains(sp)) { found = true; break; }
                                if (used.Add(sp))
                                    spawns.Add(new SpawnRequest { position = sp, sourceData = src, powerUpType = ElementPowerUpType.HorizontalRocket });
                                found = true; break;
                            }
                            run = 0; runPos.Clear();
                        }
                    }
                }

                // Vertical runs
                for (int x = 0; x < grid.GridSize.x && !found; x++)
                {
                    int run = 0;
                    HashSet<Vector2Int> runPos = new HashSet<Vector2Int>();
                    for (int y = 0; y <= grid.GridSize.y; y++)
                    {
                        Vector2Int pos = new Vector2Int(x, y);
                        if (y < grid.GridSize.y && groupSet.Contains(pos)) { run++; runPos.Add(pos); }
                        else
                        {
                            if (run >= 4)
                            {
                                Vector2Int sp = PickPreferredFromSet(runPos, init1, init2);
                                // Skip if this position was already claimed by a higher-priority power-up
                                if (claimedPositions != null && claimedPositions.Contains(sp)) { found = true; break; }
                                if (used.Add(sp))
                                    spawns.Add(new SpawnRequest { position = sp, sourceData = src, powerUpType = ElementPowerUpType.VerticalRocket });
                                found = true; break;
                            }
                            run = 0; runPos.Clear();
                        }
                    }
                }
            }
            return spawns;
        }

        public List<SpawnRequest> FindDiscoBallSpawns(List<List<Vector2Int>> matchedGroups, Vector2Int init1, Vector2Int init2)
        {
            List<SpawnRequest> spawns = new List<SpawnRequest>();
            HashSet<Vector2Int> used = new HashSet<Vector2Int>();

            for (int g = 0; g < matchedGroups.Count; g++)
            {
                List<Vector2Int> group = matchedGroups[g];
                if (group == null || group.Count < 5) continue;

                ElementData src = grid.GetCellPublic(group[0])?.elementInfo?.elementData;
                if (src == null) continue;

                HashSet<Vector2Int> groupSet = new HashSet<Vector2Int>(group);
                Vector2Int sp = PickPreferredFromSet(groupSet, init1, init2);
                if (used.Add(sp))
                    spawns.Add(new SpawnRequest { position = sp, sourceData = src, powerUpType = ElementPowerUpType.DiscoBall });
            }
            return spawns;
        }

        // ------------------------------------------------------------------
        //  Creation
        // ------------------------------------------------------------------

        public void CreatePowerUpAt(Vector2Int pos, ElementData sourceData, ElementPowerUpType type)
        {
            Grid3D.GridCell cell = grid.GetCellPublic(pos);
            if (cell == null || cell.cellType != Grid3D.CellType.Normal) return;

            ElementData visualData = ResolveVisualData(sourceData, type);

            if (cell.elementInfo == null)
                cell.elementInfo = new GridElementInfo();

            cell.elementInfo.elementData = visualData;
            cell.elementInfo.powerUpType = type;
            cell.elementInfo.isSparkling = false;
            cell.elementInfo.isHidden = false;

            GridElement element = grid.GetElementAt(pos);
            if (element != null)
            {
                element.elementInfo = cell.elementInfo;
                element.InitElement(grid, cell.elementInfo);
                GridHelper.SetEmission(element, 0f);
                ApplySortingBoost(element, type == ElementPowerUpType.Bomb);
            }
        }

        // ------------------------------------------------------------------
        //  Activation
        // ------------------------------------------------------------------

        public IEnumerator ActivateAt(Vector2Int pos, ElementData swappedElementData)
        {
            Grid3D.GridCell cell = grid.GetCellPublic(pos);
            ElementPowerUpType type = cell?.elementInfo?.powerUpType ?? ElementPowerUpType.None;
            if (type == ElementPowerUpType.Bomb)
                yield return grid.StartCoroutine(ActivateBomb(pos));
            else if (IsRocket(type))
                yield return grid.StartCoroutine(ActivateRocket(pos, type));
            else if (IsDiscoBall(type))
            {
                // Disco Ball requires swap context to know target element type.
                if (swappedElementData == null)
                    yield break;

                yield return grid.StartCoroutine(ActivateDiscoBall(pos, swappedElementData));
            }
        }

        private IEnumerator ActivateDiscoBall(Vector2Int discoBallPos, ElementData targetElementData)
        {
            Grid3D.GridCell discoBallCell = grid.GetCellPublic(discoBallPos);
            if (discoBallCell?.elementInfo == null || discoBallCell.elementInfo.powerUpType != ElementPowerUpType.DiscoBall)
                yield break;

            // Fall back to any normal element on the grid if no swapped element data was provided
            if (targetElementData == null)
            {
                for (int x = 0; x < grid.GridSize.x; x++)
                {
                    for (int y = 0; y < grid.GridSize.y; y++)
                    {
                        Grid3D.GridCell c = grid.GetCellPublic(new Vector2Int(x, y));
                        if (c != null && c.cellType == Grid3D.CellType.Normal &&
                            c.elementInfo != null && c.elementInfo.powerUpType == ElementPowerUpType.None &&
                            c.elementInfo.elementData != null)
                        {
                            targetElementData = c.elementInfo.elementData;
                            break;
                        }
                    }
                    if (targetElementData != null) break;
                }
            }

            if (targetElementData == null) yield break;

            EventManager.TriggerEvent(GameEvent.SPECIAL_ELEMENT_ACTIVATED);

            // Destroy disco ball visual
            discoBallCell.elementInfo = null;
            GridElement discoBallElement = grid.GetElementAt(discoBallPos);
            if (discoBallElement != null)
                grid.StartCoroutine(discoBallElement.DestroyElement());

            // Collect all eligible normal cells (excluding disco ball's old position)
            List<Vector2Int> candidates = new List<Vector2Int>();
            for (int x = 0; x < grid.GridSize.x; x++)
            {
                for (int y = 0; y < grid.GridSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (pos == discoBallPos) continue;
                    Grid3D.GridCell cell = grid.GetCellPublic(pos);
                    if (cell == null || cell.cellType != Grid3D.CellType.Normal || cell.elementInfo == null) continue;
                    if (IsSpecialPowerUp(cell.elementInfo.powerUpType)) continue;
                    candidates.Add(pos);
                }
            }

            // Pick up to 10 random unique cells
            int replaceCount = Mathf.Min(10, candidates.Count);
            List<Vector2Int> selectedCells = new List<Vector2Int>(replaceCount);
            for (int i = 0; i < replaceCount; i++)
            {
                int randIdx = Random.Range(i, candidates.Count);
                Vector2Int tmp = candidates[randIdx];
                candidates[randIdx] = candidates[i];
                candidates[i] = tmp;
                selectedCells.Add(candidates[i]);
            }

            if (selectedCells.Count > 0)
                yield return grid.StartCoroutine(AnimateDiscoBallTrails(discoBallPos, selectedCells, targetElementData));

            GridHelper.ShakeCamera(
                GameManager.Instance.constantManager.matchShakeDuration,
                GameManager.Instance.constantManager.matchShakeBaseMagnitude * 1.5f,
                GameManager.Instance.constantManager.matchShakeVibrato,
                GameManager.Instance.constantManager.matchShakeRandomness);

            yield return grid.StartCoroutine(grid.ResolveBoardAfterSpecialClear());
        }

        private IEnumerator AnimateDiscoBallTrails(Vector2Int sourcePos, List<Vector2Int> targets, ElementData targetElementData)
        {
            ConstantManager cm = GameManager.Instance.constantManager;
            Vector3 sourceWorldPos = grid.GetWorldPosition(sourcePos);

            int trailIndex = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                grid.StartCoroutine(AnimateSingleDiscoTrail(sourceWorldPos, targets[i], targetElementData, trailIndex));
                trailIndex++;
                yield return new WaitForSeconds(cm.discoBallTrailSpawnDelay);
            }

            float totalWait = cm.discoBallTrailDuration + cm.discoBallTrailSpawnDelay * Mathf.Max(0, targets.Count - 1);
            yield return new WaitForSeconds(totalWait);
        }

        private IEnumerator AnimateSingleDiscoTrail(Vector3 sourcePos, Vector2Int targetPos, ElementData targetElementData, int trailIndex)
        {
            ConstantManager cm = GameManager.Instance.constantManager;
            Vector3 targetWorldPos = grid.GetWorldPosition(targetPos);

            GameObject trailObj = cm.sparklingTrailPrefab != null
                ? Object.Instantiate(cm.sparklingTrailPrefab, sourcePos, Quaternion.identity)
                : CreateDefaultDiscoTrail(sourcePos, targetPos, cm.discoBallTrailDuration);

            yield return trailObj.transform.DOMove(targetWorldPos, cm.discoBallTrailDuration).SetEase(Ease.OutQuad).WaitForCompletion();

            Grid3D.GridCell cell = grid.GetCellPublic(targetPos);
            if (cell?.elementInfo != null)
            {
                cell.elementInfo.elementData = targetElementData;
                cell.elementInfo.powerUpType = ElementPowerUpType.None;
                cell.elementInfo.isSparkling = false;

                GridElement element = grid.GetElementAt(targetPos);
                if (element != null)
                {
                    element.elementInfo = cell.elementInfo;
                    element.InitElement(grid, cell.elementInfo);
                    GridHelper.SetEmission(element, cm.discoBallEmissionPeak);
                    grid.StartCoroutine(ResetElementEmission(element, cm.discoBallEmissionResetDelay));
                }
            }

            float shakeMag = GameManager.Instance.constantManager.sparklingShakeBaseMagnitude + (trailIndex * 0.01f);
            GridHelper.ShakeCamera(
                GameManager.Instance.constantManager.sparklingShakeDuration,
                shakeMag,
                GameManager.Instance.constantManager.sparklingShakeVibrato,
                GameManager.Instance.constantManager.sparklingShakeRandomness);

            if (trailObj != null) Object.Destroy(trailObj);
        }

        private IEnumerator ResetElementEmission(GridElement element, float delay)
        {
            if (element == null) yield break;
            yield return new WaitForSeconds(delay);
            if (element != null)
                GridHelper.SetEmission(element, 0f);
        }

        private GameObject CreateDefaultDiscoTrail(Vector3 pos, Vector2Int targetPos, float lifeTime)
        {
            GameObject obj = new GameObject($"DiscoTrail_{targetPos.x}_{targetPos.y}");
            obj.transform.position = pos;
            TrailRenderer trail = obj.AddComponent<TrailRenderer>();
            trail.time = Mathf.Max(0.05f, lifeTime);
            trail.startWidth = 0.08f;
            trail.endWidth = 0f;
            trail.numCapVertices = 6;
            trail.numCornerVertices = 6;
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new[] { new GradientColorKey(Color.cyan, 0f), new GradientColorKey(Color.magenta, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            trail.colorGradient = gradient;
            trail.material = new Material(Shader.Find("Sprites/Default"));
            return obj;
        }

        private IEnumerator ActivateBomb(Vector2Int bombPos)
        {
            Grid3D.GridCell bombCell = grid.GetCellPublic(bombPos);
            if (bombCell?.elementInfo == null || bombCell.elementInfo.powerUpType != ElementPowerUpType.Bomb)
                yield break;

            Vector2Int targetPos = grid.GetBombTargetPosition();
            EventManager.TriggerEvent(GameEvent.SPECIAL_ELEMENT_ACTIVATED);

            GridElement bombElement = grid.GetElementAt(bombPos);
            if (bombElement != null)
            {
                Transform tempParent = grid.GridParent != null ? grid.GridParent : grid.transform;
                bombElement.transform.SetParent(tempParent, true);

                Vector3 impactWorldPos = grid.GetWorldPosition(targetPos);
                yield return grid.StartCoroutine(AnimateBombFlight(bombElement, impactWorldPos));

                PlayBombImpactEffects(impactWorldPos);
                grid.StartCoroutine(bombElement.DestroyElement());
            }

            bombCell.elementInfo = null;
            yield return grid.StartCoroutine(grid.ClearAreaAt(targetPos, 1));
            yield return grid.StartCoroutine(grid.ResolveBoardAfterSpecialClear());
        }

        private IEnumerator ActivateRocket(Vector2Int rocketPos, ElementPowerUpType rocketType)
        {
            EventManager.TriggerEvent(GameEvent.SPECIAL_ELEMENT_ACTIVATED);

            GridElement rocketElement = grid.GetElementAt(rocketPos);
            grid.GetCellPublic(rocketPos).elementInfo = null;

            bool isHorizontal = rocketType == ElementPowerUpType.HorizontalRocket;
            Vector2Int dirPositive = isHorizontal ? Vector2Int.right : Vector2Int.up;
            Vector2Int dirNegative = isHorizontal ? Vector2Int.left : Vector2Int.down;

            Vector3 originWorld = grid.GetWorldPosition(rocketPos);
            ConstantManager cm = GameManager.Instance.constantManager;

            List<Vector2Int> positiveCells = CollectLineCells(rocketPos, dirPositive);
            List<Vector2Int> negativeCells = CollectLineCells(rocketPos, dirNegative);

            Vector3 positiveEnd = positiveCells.Count > 0
                ? grid.GetWorldPosition(positiveCells[positiveCells.Count - 1]) + (Vector3)(Vector2)dirPositive * 0.5f
                : originWorld + (Vector3)(Vector2)dirPositive * 0.5f;
            Vector3 negativeEnd = negativeCells.Count > 0
                ? grid.GetWorldPosition(negativeCells[negativeCells.Count - 1]) + (Vector3)(Vector2)dirNegative * 0.5f
                : originWorld + (Vector3)(Vector2)dirNegative * 0.5f;

            if (rocketElement != null)
            {
                rocketElement.transform.DOKill();
                Collider[] cols = rocketElement.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;
            }

            GameObject rocketCopyA = CreateRocketCopy(rocketElement, originWorld, cm, isHorizontal, 1f);
            GameObject rocketCopyB = CreateRocketCopy(rocketElement, originWorld, cm, isHorizontal, -1f);

            if (rocketElement != null) Object.Destroy(rocketElement.gameObject);

            GridHelper.ShakeCamera(cm.rocketShakeDuration, cm.rocketShakeMagnitude, cm.rocketShakeVibrato, cm.rocketShakeRandomness);

            Coroutine travelA = grid.StartCoroutine(TravelRocketCopy(rocketCopyA, originWorld, positiveEnd, positiveCells, cm));
            Coroutine travelB = grid.StartCoroutine(TravelRocketCopy(rocketCopyB, originWorld, negativeEnd, negativeCells, cm));

            yield return travelA;
            Object.Destroy(rocketCopyA);
            yield return travelB;
            Object.Destroy(rocketCopyB);

            HashSet<Vector2Int> walls = new HashSet<Vector2Int>();
            CollectAdjacentWalls(rocketPos, walls);
            foreach (Vector2Int cell in positiveCells) CollectAdjacentWalls(cell, walls);
            foreach (Vector2Int cell in negativeCells) CollectAdjacentWalls(cell, walls);

            yield return grid.StartCoroutine(grid.BreakWallsSimultaneous(walls));
            yield return grid.StartCoroutine(grid.ResolveBoardAfterSpecialClear());
        }

        private List<Vector2Int> CollectLineCells(Vector2Int origin, Vector2Int direction)
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            Vector2Int current = origin + direction;
            while (true)
            {
                Grid3D.GridCell cell = grid.GetCellPublic(current);
                if (cell == null) break;
                cells.Add(current);
                current += direction;
            }
            return cells;
        }

        private void CollectAdjacentWalls(Vector2Int pos, HashSet<Vector2Int> walls)
        {
            Vector2Int[] offsets = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            for (int i = 0; i < offsets.Length; i++)
            {
                Vector2Int adj = pos + offsets[i];
                Grid3D.GridCell cell = grid.GetCellPublic(adj);
                if (cell != null && cell.cellType == Grid3D.CellType.BreakableWall)
                    walls.Add(adj);
            }
        }

        private GameObject CreateRocketCopy(GridElement sourceElement, Vector3 origin, ConstantManager cm, bool isHorizontal, float directionSign)
        {
            GameObject copy = new GameObject("RocketCopy");
            copy.transform.position = origin;

            if (sourceElement != null && sourceElement.elementRenderer is SpriteRenderer srcSR)
            {
                SpriteRenderer sr = copy.AddComponent<SpriteRenderer>();
                sr.sprite = srcSR.sprite;
                sr.material = srcSR.material;
                sr.sortingLayerID = srcSR.sortingLayerID;
                sr.sortingOrder = srcSR.sortingOrder + SortingOrderBoost;
                sr.color = srcSR.color;

                if (isHorizontal)
                {
                    copy.transform.rotation = Quaternion.Euler(0f, 0f, 90f);
                    sr.flipY = directionSign < 0f;
                }
                else
                {
                    copy.transform.rotation = Quaternion.Euler(0f, 0f, directionSign > 0f ? 0f : 180f);
                }
            }

            if (cm.rocketTrailParticlePrefab != null)
            {
                ParticleSystem trail = Object.Instantiate(cm.rocketTrailParticlePrefab, copy.transform);
                trail.transform.localPosition = Vector3.zero;
                trail.Play();
            }

            return copy;
        }

        private IEnumerator TravelRocketCopy(GameObject rocketCopy, Vector3 start, Vector3 end, List<Vector2Int> cellsInOrder, ConstantManager cm)
        {
            if (rocketCopy == null) yield break;

            float totalDist = Vector3.Distance(start, end);
            float speed = cm.rocketTravelSpeed > 0f ? cm.rocketTravelSpeed : 12f;
            float duration = totalDist / speed;

            List<float> cellDistances = new List<float>();
            for (int i = 0; i < cellsInOrder.Count; i++)
            {
                Vector3 cellWorld = grid.GetWorldPosition(cellsInOrder[i]);
                cellDistances.Add(Vector3.Distance(start, cellWorld));
            }

            int nextCellIndex = 0;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                if (rocketCopy != null)
                    rocketCopy.transform.position = Vector3.Lerp(start, end, t);

                float currentDist = t * totalDist;

                while (nextCellIndex < cellsInOrder.Count && currentDist >= cellDistances[nextCellIndex])
                {
                    ClearLineCellImmediate(cellsInOrder[nextCellIndex]);
                    nextCellIndex++;
                }

                yield return null;
            }

            while (nextCellIndex < cellsInOrder.Count)
            {
                ClearLineCellImmediate(cellsInOrder[nextCellIndex]);
                nextCellIndex++;
            }
        }

        private void ClearLineCellImmediate(Vector2Int pos)
        {
            Grid3D.GridCell cell = grid.GetCellPublic(pos);
            if (cell == null) return;
            if (cell.cellType != Grid3D.CellType.Normal || cell.elementInfo == null) return;

            cell.elementInfo = null;
            GridElement element = grid.GetElementAt(pos);
            if (element != null) grid.StartCoroutine(element.DestroyElement());
        }

        private IEnumerator AnimateBombFlight(GridElement bombElement, Vector3 target)
        {
            if (bombElement == null) yield break;
            Transform t = bombElement.transform;
            Vector3 start = t.position;
            float duration = Mathf.Max(0.25f, GameManager.Instance.constantManager.elementSwapMoveDuration * 2f);

            Vector3 mid = Vector3.Lerp(start, target, 0.5f) + Vector3.up * 1.1f;
            Vector3[] path = { start, mid, target };

            Sequence seq = DOTween.Sequence();
            seq.Join(t.DOPath(path, duration, PathType.CatmullRom).SetEase(Ease.OutQuad).SetOptions(false));
            seq.Join(t.DORotate(new Vector3(0f, 0f, 540f), duration, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetRelative());
            seq.Join(t.DOScale(1.18f, duration * 0.45f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine));
            yield return seq.WaitForCompletion();
        }

        private void PlayBombImpactEffects(Vector3 impactPos)
        {
            ConstantManager cm = GameManager.Instance != null ? GameManager.Instance.constantManager : null;
            if (cm == null) return;
            if (cm.bombImpactParticlePrefab != null)
            {
                ParticleSystem p = Object.Instantiate(cm.bombImpactParticlePrefab, impactPos, Quaternion.identity);
                p.Play();
                Object.Destroy(p.gameObject, p.main.duration + p.main.startLifetime.constantMax + 0.2f);
            }
            GridHelper.ShakeCamera(cm.bombImpactShakeDuration, cm.bombImpactShakeMagnitude, cm.bombImpactShakeVibrato, cm.bombImpactShakeRandomness);
        }

        public void ApplySortingBoost(GridElement element, bool boost)
        {
            if (element == null) return;
            Renderer[] renderers = element.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (boost) renderers[i].sortingOrder += SortingOrderBoost;
                else if (renderers[i].sortingOrder >= SortingOrderBoost) renderers[i].sortingOrder -= SortingOrderBoost;
            }
        }

        private ElementData ResolveVisualData(ElementData sourceData, ElementPowerUpType type)
        {
            if (!(GameManager.Instance.CurrentLevel is LevelScene_Match3Game lvl)) return sourceData;
            if (type == ElementPowerUpType.Bomb && lvl.bombElementData != null) return lvl.bombElementData;
            if (IsRocket(type) && lvl.rocketElementData != null) return lvl.rocketElementData;
            if (IsDiscoBall(type) && lvl.discoBallElementData != null) return lvl.discoBallElementData;
            return sourceData;
        }

        private static Vector2Int PickPreferred(HashSet<Vector2Int> groupSet, Vector2Int[] candidates, Vector2Int init1, Vector2Int init2)
        {
            foreach (Vector2Int c in candidates)
                if (groupSet.Contains(init2) && c == init2) return init2;
            foreach (Vector2Int c in candidates)
                if (groupSet.Contains(init1) && c == init1) return init1;
            return candidates[0];
        }

        private static Vector2Int PickPreferredFromSet(HashSet<Vector2Int> candidates, Vector2Int init1, Vector2Int init2)
        {
            if (candidates.Contains(init2)) return init2;
            if (candidates.Contains(init1)) return init1;
            foreach (Vector2Int p in candidates) return p;
            return init1;
        }
    }
}