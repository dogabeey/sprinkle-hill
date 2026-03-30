using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace Game
{
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

            TriggerPowerUpCreatedEvent(type, visualData);
        }

        private void TriggerPowerUpCreatedEvent(ElementPowerUpType type, ElementData data)
        {
            GameEvent evt;
            if (type == ElementPowerUpType.Bomb) evt = GameEvent.BOMB_CREATED;
            else if (IsRocket(type)) evt = GameEvent.ROCKET_CREATED;
            else if (IsDiscoBall(type)) evt = GameEvent.DISCO_BALL_CREATED;
            else return;

            EventManager.TriggerEvent(evt, new EventParam(paramScriptable: data));
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
            PlayEffect(ConstantManager.SOUNDS.EFFECTS.DISCO_BALL_ACTIVATE);

            GridElement discoBallElement = grid.GetElementAt(discoBallPos);
            Tween spinTween = null;

            if (discoBallElement != null)
            {
                discoBallElement.transform.DOKill();
                float spinDuration = GameManager.Instance.constantManager.discoBallSpinLoopDuration;
                float spinDegrees = GameManager.Instance.constantManager.discoBallSpinDegreesPerLoop;
                spinTween = discoBallElement.transform
                    .DORotate(new Vector3(0f, 0f, spinDegrees), spinDuration, RotateMode.FastBeyond360)
                    .SetEase(Ease.Linear)
                    .SetRelative()
                    .SetLoops(-1, LoopType.Restart);
            }

            // Remove logical occupancy, keep visual until trail animation finishes.
            discoBallCell.elementInfo = null;

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

            if (spinTween != null)
                spinTween.Kill();

            if (discoBallElement != null)
                grid.StartCoroutine(discoBallElement.DestroyElement());

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

            GameObject trailObj = Object.Instantiate(cm.sparklingTrailPrefab, sourcePos, Quaternion.identity);

            PlayEffect(ConstantManager.SOUNDS.EFFECTS.DISCO_BALL_TRAIL, volumeMultiplier: 0.75f, pitchOffset: Mathf.Clamp(trailIndex * 0.01f, 0f, 0.12f));

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

        private IEnumerator ActivateBomb(Vector2Int bombPos)
        {
            Grid3D.GridCell bombCell = grid.GetCellPublic(bombPos);
            if (bombCell?.elementInfo == null || bombCell.elementInfo.powerUpType != ElementPowerUpType.Bomb)
                yield break;

            Vector2Int targetPos = grid.GetBombTargetPosition();
            EventManager.TriggerEvent(GameEvent.SPECIAL_ELEMENT_ACTIVATED);
            PlayEffect(ConstantManager.SOUNDS.EFFECTS.BOMB);

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
            PlayEffect(ConstantManager.SOUNDS.EFFECTS.ROCKET);

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

        private void PlayEffect(string effectId, float volumeMultiplier = 1f, float pitchOffset = 0f)
        {
            if (GameManager.Instance == null || GameManager.Instance.soundManager == null)
                return;

            GameManager.Instance.soundManager.Play(effectId, false, 0f, volumeMultiplier, pitchOffset);
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