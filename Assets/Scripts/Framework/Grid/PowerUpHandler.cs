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
        private readonly Dictionary<ElementPowerUpType, IPowerUpActivationStrategy> activationStrategies;
        private readonly List<IPowerUpCreationStrategy> creationStrategies;

        public PowerUpHandler(Match3Grid grid)
        {
            this.grid = grid;
            activationStrategies = new Dictionary<ElementPowerUpType, IPowerUpActivationStrategy>
            {
                { ElementPowerUpType.Bomb, new BombActivationStrategy(this) },
                { ElementPowerUpType.Rocket, new RocketActivationStrategy(this) },
                { ElementPowerUpType.Propeller, new PropellerActivationStrategy(this) },
                { ElementPowerUpType.HorizontalRocket, new RocketActivationStrategy(this) },
                { ElementPowerUpType.VerticalRocket, new RocketActivationStrategy(this) },
                { ElementPowerUpType.Cauldron, new CauldronActivationStrategy(this) },
                { ElementPowerUpType.DiscoBall, new DiscoBallActivationStrategy(this) }
            };

            creationStrategies = new List<IPowerUpCreationStrategy>
            {
                new DiscoBallCreationStrategy(this),
                new PropellerCreationStrategy(this),
                new BombCreationStrategy(this),
                new RocketCreationStrategy(this)
            };
        }

        public static bool IsSpecialPowerUp(ElementPowerUpType type)
        {
            return type != ElementPowerUpType.None;
        }

        public static bool IsRocket(ElementPowerUpType type)
        {
            return type == ElementPowerUpType.Rocket ||
                   type == ElementPowerUpType.HorizontalRocket ||
                   type == ElementPowerUpType.VerticalRocket;
        }

        public static bool IsDiscoBall(ElementPowerUpType type)
        {
            return type == ElementPowerUpType.DiscoBall;
        }

        public static bool IsPropeller(ElementPowerUpType type)
        {
            return type == ElementPowerUpType.Propeller;
        }

        public struct SpawnRequest
        {
            public Vector2Int position;
            public ElementData sourceData;
            public ElementPowerUpType powerUpType;
        }

        public struct CreationContext
        {
            public List<List<Vector2Int>> matchedGroups;
            public Vector2Int init1;
            public Vector2Int init2;
            public bool allowDiscoBall;
            public bool allowPropeller;
            public bool allowBomb;
            public bool allowRocket;
        }

        public sealed class SpawnResolution
        {
            public readonly List<SpawnRequest> discoBallSpawns = new List<SpawnRequest>();
            public readonly List<SpawnRequest> propellerSpawns = new List<SpawnRequest>();
            public readonly List<SpawnRequest> bombSpawns = new List<SpawnRequest>();
            public readonly List<SpawnRequest> rocketSpawns = new List<SpawnRequest>();
            public readonly HashSet<Vector2Int> protectedPositions = new HashSet<Vector2Int>();
        }

        // ------------------------------------------------------------------
        //  Spawn detection
        // ------------------------------------------------------------------

        public SpawnResolution ResolveSpawns(CreationContext context)
        {
            SpawnResolution resolution = new SpawnResolution();
            HashSet<Vector2Int> claimedPositions = new HashSet<Vector2Int>();

            for (int i = 0; i < creationStrategies.Count; i++)
            {
                IPowerUpCreationStrategy strategy = creationStrategies[i];
                if (!strategy.IsEnabled(context))
                    continue;

                strategy.CollectSpawns(context, resolution, claimedPositions);
            }

            return resolution;
        }

        public List<SpawnRequest> FindBombSpawns(List<List<Vector2Int>> matchedGroups, Vector2Int init1, Vector2Int init2, HashSet<Vector2Int> claimedPositions = null)
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
                        if (claimedPositions != null && claimedPositions.Contains(spawnPos)) { found = true; continue; }
                        if (used.Add(spawnPos))
                            spawns.Add(new SpawnRequest { position = spawnPos, sourceData = src, powerUpType = ElementPowerUpType.Bomb });
                        found = true;
                    }
                }
            }
            return spawns;
        }

        public List<SpawnRequest> FindPropellerSpawns(List<List<Vector2Int>> matchedGroups, Vector2Int init1, Vector2Int init2, HashSet<Vector2Int> claimedPositions = null)
        {
            List<SpawnRequest> spawns = new List<SpawnRequest>();
            HashSet<Vector2Int> used = new HashSet<Vector2Int>();

            for (int g = 0; g < matchedGroups.Count; g++)
            {
                List<Vector2Int> group = matchedGroups[g];
                if (group == null || group.Count < 4)
                    continue;

                HashSet<Vector2Int> groupSet = new HashSet<Vector2Int>(group);
                ElementData src = grid.GetCellPublic(group[0])?.elementInfo?.elementData;
                if (src == null)
                    continue;

                bool found = false;
                for (int i = 0; i < group.Count && !found; i++)
                {
                    Vector2Int pivot = group[i];
                    int upLen = 0;
                    while (groupSet.Contains(pivot + (Vector2Int.up * (upLen + 1)))) upLen++;
                    int downLen = 0;
                    while (groupSet.Contains(pivot + (Vector2Int.down * (downLen + 1)))) downLen++;
                    int leftLen = 0;
                    while (groupSet.Contains(pivot + (Vector2Int.left * (leftLen + 1)))) leftLen++;
                    int rightLen = 0;
                    while (groupSet.Contains(pivot + (Vector2Int.right * (rightLen + 1)))) rightLen++;

                    bool hasLShape =
                        IsValidPropellerL(upLen, leftLen) ||
                        IsValidPropellerL(upLen, rightLen) ||
                        IsValidPropellerL(downLen, leftLen) ||
                        IsValidPropellerL(downLen, rightLen);

                    if (!hasLShape)
                        continue;

                    Vector2Int spawnPos = groupSet.Contains(init2) ? init2 : (groupSet.Contains(init1) ? init1 : pivot);
                    if (claimedPositions != null && claimedPositions.Contains(spawnPos))
                    {
                        found = true;
                        break;
                    }

                    if (used.Add(spawnPos))
                        spawns.Add(new SpawnRequest { position = spawnPos, sourceData = src, powerUpType = ElementPowerUpType.Propeller });
                    found = true;
                }
            }

            return spawns;
        }

        private static bool IsValidPropellerL(int verticalArmLen, int horizontalArmLen)
        {
            if (verticalArmLen < 1 || horizontalArmLen < 1)
                return false;

            int uniqueCount = verticalArmLen + horizontalArmLen + 1;
            if (uniqueCount < 4)
                return false;

            // Prevent 2x2 square from being treated as propeller (1+1+1 == 3),
            // and require at least one arm to extend beyond a single neighbor.
            return verticalArmLen >= 2 || horizontalArmLen >= 2;
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
                                    spawns.Add(new SpawnRequest { position = sp, sourceData = src, powerUpType = ElementPowerUpType.Rocket });
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
                                    spawns.Add(new SpawnRequest { position = sp, sourceData = src, powerUpType = ElementPowerUpType.Rocket });
                                found = true; break;
                            }
                            run = 0; runPos.Clear();
                        }
                    }
                }
            }
            return spawns;
        }

        public List<SpawnRequest> FindDiscoBallSpawns(List<List<Vector2Int>> matchedGroups, Vector2Int init1, Vector2Int init2, HashSet<Vector2Int> claimedPositions = null)
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
                bool found = false;

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
                            if (run >= 5)
                            {
                                Vector2Int sp = PickPreferredFromSet(runPos, init1, init2);
                                if (claimedPositions == null || !claimedPositions.Contains(sp))
                                {
                                    if (used.Add(sp))
                                        spawns.Add(new SpawnRequest { position = sp, sourceData = src, powerUpType = ElementPowerUpType.DiscoBall });
                                }
                                found = true;
                                break;
                            }
                            run = 0;
                            runPos.Clear();
                        }
                    }
                }

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
                            if (run >= 5)
                            {
                                Vector2Int sp = PickPreferredFromSet(runPos, init1, init2);
                                if (claimedPositions == null || !claimedPositions.Contains(sp))
                                {
                                    if (used.Add(sp))
                                        spawns.Add(new SpawnRequest { position = sp, sourceData = src, powerUpType = ElementPowerUpType.DiscoBall });
                                }
                                found = true;
                                break;
                            }
                            run = 0;
                            runPos.Clear();
                        }
                    }
                }
            }
            return spawns;
        }

        private interface IPowerUpCreationStrategy
        {
            bool IsEnabled(CreationContext context);
            void CollectSpawns(CreationContext context, SpawnResolution resolution, HashSet<Vector2Int> claimedPositions);
        }

        private sealed class DiscoBallCreationStrategy : IPowerUpCreationStrategy
        {
            private readonly PowerUpHandler handler;

            public DiscoBallCreationStrategy(PowerUpHandler handler)
            {
                this.handler = handler;
            }

            public bool IsEnabled(CreationContext context) => context.allowDiscoBall;

            public void CollectSpawns(CreationContext context, SpawnResolution resolution, HashSet<Vector2Int> claimedPositions)
            {
                List<SpawnRequest> spawns = handler.FindDiscoBallSpawns(context.matchedGroups, context.init1, context.init2, claimedPositions);
                for (int i = 0; i < spawns.Count; i++)
                {
                    SpawnRequest spawn = spawns[i];
                    resolution.discoBallSpawns.Add(spawn);
                    resolution.protectedPositions.Add(spawn.position);
                    claimedPositions.Add(spawn.position);
                }
            }
        }

        private sealed class BombCreationStrategy : IPowerUpCreationStrategy
        {
            private readonly PowerUpHandler handler;

            public BombCreationStrategy(PowerUpHandler handler)
            {
                this.handler = handler;
            }

            public bool IsEnabled(CreationContext context) => context.allowBomb;

            public void CollectSpawns(CreationContext context, SpawnResolution resolution, HashSet<Vector2Int> claimedPositions)
            {
                List<SpawnRequest> spawns = handler.FindBombSpawns(context.matchedGroups, context.init1, context.init2, claimedPositions);
                for (int i = 0; i < spawns.Count; i++)
                {
                    SpawnRequest spawn = spawns[i];
                    resolution.bombSpawns.Add(spawn);
                    resolution.protectedPositions.Add(spawn.position);
                    claimedPositions.Add(spawn.position);
                }
            }
        }

        private sealed class PropellerCreationStrategy : IPowerUpCreationStrategy
        {
            private readonly PowerUpHandler handler;

            public PropellerCreationStrategy(PowerUpHandler handler)
            {
                this.handler = handler;
            }

            public bool IsEnabled(CreationContext context) => context.allowPropeller;

            public void CollectSpawns(CreationContext context, SpawnResolution resolution, HashSet<Vector2Int> claimedPositions)
            {
                List<SpawnRequest> spawns = handler.FindPropellerSpawns(context.matchedGroups, context.init1, context.init2, claimedPositions);
                for (int i = 0; i < spawns.Count; i++)
                {
                    SpawnRequest spawn = spawns[i];
                    resolution.propellerSpawns.Add(spawn);
                    resolution.protectedPositions.Add(spawn.position);
                    claimedPositions.Add(spawn.position);
                }
            }
        }

        private sealed class RocketCreationStrategy : IPowerUpCreationStrategy
        {
            private readonly PowerUpHandler handler;

            public RocketCreationStrategy(PowerUpHandler handler)
            {
                this.handler = handler;
            }

            public bool IsEnabled(CreationContext context) => context.allowRocket;

            public void CollectSpawns(CreationContext context, SpawnResolution resolution, HashSet<Vector2Int> claimedPositions)
            {
                List<SpawnRequest> spawns = handler.FindRocketSpawns(context.matchedGroups, context.init1, context.init2, claimedPositions);
                for (int i = 0; i < spawns.Count; i++)
                {
                    SpawnRequest spawn = spawns[i];
                    resolution.rocketSpawns.Add(spawn);
                    resolution.protectedPositions.Add(spawn.position);
                    claimedPositions.Add(spawn.position);
                }
            }
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

            GridElement element = grid.ReplaceElementAt(pos, cell.elementInfo);
            if (element != null)
            {
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
            else if (IsPropeller(type)) evt = GameEvent.PROPELLER_CREATED;
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
            if (!activationStrategies.TryGetValue(type, out IPowerUpActivationStrategy strategy))
                yield break;

            yield return grid.StartCoroutine(strategy.Activate(pos, swappedElementData));
        }

        private interface IPowerUpActivationStrategy
        {
            IEnumerator Activate(Vector2Int pos, ElementData swappedElementData);
        }

        private sealed class BombActivationStrategy : IPowerUpActivationStrategy
        {
            private readonly PowerUpHandler handler;

            public BombActivationStrategy(PowerUpHandler handler)
            {
                this.handler = handler;
            }

            public IEnumerator Activate(Vector2Int pos, ElementData swappedElementData)
            {
                return handler.ActivateBomb(pos);
            }
        }

        private sealed class RocketActivationStrategy : IPowerUpActivationStrategy
        {
            private readonly PowerUpHandler handler;

            public RocketActivationStrategy(PowerUpHandler handler)
            {
                this.handler = handler;
            }

            public IEnumerator Activate(Vector2Int pos, ElementData swappedElementData)
            {
                return handler.ActivateRocket(pos);
            }
        }

        private sealed class PropellerActivationStrategy : IPowerUpActivationStrategy
        {
            private readonly PowerUpHandler handler;

            public PropellerActivationStrategy(PowerUpHandler handler)
            {
                this.handler = handler;
            }

            public IEnumerator Activate(Vector2Int pos, ElementData swappedElementData)
            {
                return handler.ActivatePropeller(pos);
            }
        }

        private sealed class CauldronActivationStrategy : IPowerUpActivationStrategy
        {
            private readonly PowerUpHandler handler;

            public CauldronActivationStrategy(PowerUpHandler handler)
            {
                this.handler = handler;
            }

            public IEnumerator Activate(Vector2Int pos, ElementData swappedElementData)
            {
                return handler.ActivateCauldron(pos);
            }
        }

        private sealed class DiscoBallActivationStrategy : IPowerUpActivationStrategy
        {
            private readonly PowerUpHandler handler;

            public DiscoBallActivationStrategy(PowerUpHandler handler)
            {
                this.handler = handler;
            }

            public IEnumerator Activate(Vector2Int pos, ElementData swappedElementData)
            {
                return handler.ActivateDiscoBall(pos, swappedElementData);
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
            grid.TriggerCellFeatureMatchedOverAt(discoBallPos);
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

        private IEnumerator ActivatePropeller(Vector2Int propellerPos)
        {
            Grid3D.GridCell propellerCell = grid.GetCellPublic(propellerPos);
            if (propellerCell?.elementInfo == null || propellerCell.elementInfo.powerUpType != ElementPowerUpType.Propeller)
                yield break;

            EventManager.TriggerEvent(GameEvent.SPECIAL_ELEMENT_ACTIVATED);
            PlayEffect(ConstantManager.SOUNDS.EFFECTS.ROCKET, volumeMultiplier: 0.9f, pitchOffset: 0.08f);

            GridElement propellerElement = grid.GetElementAt(propellerPos);
            if (propellerElement != null)
                propellerElement.transform.DOKill();

            Vector2Int targetPos = PickPropellerTargetPosition(propellerPos);
            Vector3 targetWorldPos = grid.GetWorldPosition(targetPos);

            if (propellerElement != null)
            {
                Transform tempParent = grid.GridParent != null ? grid.GridParent : grid.transform;
                propellerElement.transform.SetParent(tempParent, true);

                Vector3 startWorldPos = propellerElement.transform.position;
                float travelDuration = 0.3f;
                float arcHeight = Mathf.Clamp(Vector3.Distance(startWorldPos, targetWorldPos) * 0.22f, 0.3f, 0.9f);
                Vector3 midPoint = Vector3.Lerp(startWorldPos, targetWorldPos, 0.5f) + (Vector3.up * arcHeight);

                Sequence travelSequence = DOTween.Sequence();
                travelSequence.Join(
                    propellerElement.transform
                        .DOPath(new[] { startWorldPos, midPoint, targetWorldPos }, travelDuration, PathType.CatmullRom)
                        .SetEase(Ease.InOutSine)
                        .SetOptions(false));
                travelSequence.Join(
                    propellerElement.transform
                        .DORotate(new Vector3(0f, 0f, 1080f), travelDuration, RotateMode.FastBeyond360)
                        .SetEase(Ease.Linear)
                        .SetRelative());

                yield return travelSequence.WaitForCompletion();
                grid.StartCoroutine(propellerElement.DestroyElement());
            }

            grid.TriggerCellFeatureMatchedOverAt(propellerPos);
            propellerCell.elementInfo = null;
            yield return grid.StartCoroutine(grid.ClearAreaAt(targetPos, 0));
            yield return grid.StartCoroutine(grid.ResolveBoardAfterSpecialClear());
        }

        private Vector2Int PickPropellerTargetPosition(Vector2Int origin)
        {
            List<Vector2Int> breakableAdjacent = new List<Vector2Int>();
            List<Vector2Int> normalCandidates = new List<Vector2Int>();
            Vector2Int[] offsets = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

            for (int x = 0; x < grid.GridSize.x; x++)
            {
                for (int y = 0; y < grid.GridSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (pos == origin)
                        continue;

                    Grid3D.GridCell cell = grid.GetCellPublic(pos);
                    if (cell == null || cell.cellType != Grid3D.CellType.Normal)
                        continue;

                    bool nearBreakable = false;
                    for (int i = 0; i < offsets.Length; i++)
                    {
                        Grid3D.GridCell adj = grid.GetCellPublic(pos + offsets[i]);
                        if (adj != null && adj.cellType == Grid3D.CellType.BreakableWall)
                        {
                            nearBreakable = true;
                            break;
                        }
                    }

                    if (nearBreakable) breakableAdjacent.Add(pos);
                    else if (cell.elementInfo != null) normalCandidates.Add(pos);
                }
            }

            if (breakableAdjacent.Count > 0)
                return breakableAdjacent[Random.Range(0, breakableAdjacent.Count)];
            if (normalCandidates.Count > 0)
                return normalCandidates[Random.Range(0, normalCandidates.Count)];
            return origin;
        }

        private IEnumerator ActivateCauldron(Vector2Int cauldronPos)
        {
            if (!grid.IsCauldronReadyAt(cauldronPos))
                yield break;

            PlayEffect(ConstantManager.SOUNDS.EFFECTS.BOMB, volumeMultiplier: 1.1f, pitchOffset: -0.05f);
            yield return grid.StartCoroutine(grid.TriggerCauldronExplosion(cauldronPos));
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

            EventManager.TriggerEvent(GameEvent.SPECIAL_ELEMENT_ACTIVATED);
            PlayEffect(ConstantManager.SOUNDS.EFFECTS.BOMB);

            GridElement bombElement = grid.GetElementAt(bombPos);
            Vector3 impactWorldPos = grid.GetWorldPosition(bombPos);
            if (bombElement != null)
            {
                PlayBombImpactEffects(impactWorldPos);
                grid.StartCoroutine(bombElement.DestroyElement());
            }
            else
            {
                PlayBombImpactEffects(impactWorldPos);
            }

            grid.TriggerCellFeatureMatchedOverAt(bombPos);
            bombCell.elementInfo = null;
            yield return grid.StartCoroutine(grid.ClearAreaAt(bombPos, 1, false));
            yield return grid.StartCoroutine(grid.ResolveBoardAfterSpecialClear());
        }

        private IEnumerator ActivateRocket(Vector2Int rocketPos)
        {
            EventManager.TriggerEvent(GameEvent.SPECIAL_ELEMENT_ACTIVATED);
            yield return new WaitForSeconds(0.1f);
            PlayEffect(ConstantManager.SOUNDS.EFFECTS.ROCKET);

            GridElement rocketElement = grid.GetElementAt(rocketPos);
            grid.TriggerCellFeatureMatchedOverAt(rocketPos);
            grid.GetCellPublic(rocketPos).elementInfo = null;

            Vector3 originWorld = grid.GetWorldPosition(rocketPos);
            ConstantManager cm = GameManager.Instance.constantManager;

            Vector2Int dirRight = Vector2Int.right;
            Vector2Int dirLeft = Vector2Int.left;
            Vector2Int dirUp = Vector2Int.up;
            Vector2Int dirDown = Vector2Int.down;

            List<Vector2Int> rightCells = CollectLineCells(rocketPos, dirRight);
            List<Vector2Int> leftCells = CollectLineCells(rocketPos, dirLeft);
            List<Vector2Int> upCells = CollectLineCells(rocketPos, dirUp);
            List<Vector2Int> downCells = CollectLineCells(rocketPos, dirDown);

            Vector3 rightEnd = rightCells.Count > 0
                ? grid.GetWorldPosition(rightCells[rightCells.Count - 1]) + (Vector3)(Vector2)dirRight * 0.5f
                : originWorld + (Vector3)(Vector2)dirRight * 0.5f;
            Vector3 leftEnd = leftCells.Count > 0
                ? grid.GetWorldPosition(leftCells[leftCells.Count - 1]) + (Vector3)(Vector2)dirLeft * 0.5f
                : originWorld + (Vector3)(Vector2)dirLeft * 0.5f;
            Vector3 upEnd = upCells.Count > 0
                ? grid.GetWorldPosition(upCells[upCells.Count - 1]) + (Vector3)(Vector2)dirUp * 0.5f
                : originWorld + (Vector3)(Vector2)dirUp * 0.5f;
            Vector3 downEnd = downCells.Count > 0
                ? grid.GetWorldPosition(downCells[downCells.Count - 1]) + (Vector3)(Vector2)dirDown * 0.5f
                : originWorld + (Vector3)(Vector2)dirDown * 0.5f;

            if (rocketElement != null)
            {
                rocketElement.transform.DOKill();
                Collider[] cols = rocketElement.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;
            }

            GameObject rocketCopyRight = CreateRocketCopy(rocketElement, originWorld, cm, true, 1f);
            GameObject rocketCopyLeft = CreateRocketCopy(rocketElement, originWorld, cm, true, -1f);
            GameObject rocketCopyUp = CreateRocketCopy(rocketElement, originWorld, cm, false, 1f);
            GameObject rocketCopyDown = CreateRocketCopy(rocketElement, originWorld, cm, false, -1f);

            if (rocketElement != null) Object.Destroy(rocketElement.gameObject);

            GridHelper.ShakeCamera(cm.rocketShakeDuration, cm.rocketShakeMagnitude, cm.rocketShakeVibrato, cm.rocketShakeRandomness);

            Coroutine travelRight = grid.StartCoroutine(TravelRocketCopy(rocketCopyRight, originWorld, rightEnd, rightCells, cm));
            Coroutine travelLeft = grid.StartCoroutine(TravelRocketCopy(rocketCopyLeft, originWorld, leftEnd, leftCells, cm));
            Coroutine travelUp = grid.StartCoroutine(TravelRocketCopy(rocketCopyUp, originWorld, upEnd, upCells, cm));
            Coroutine travelDown = grid.StartCoroutine(TravelRocketCopy(rocketCopyDown, originWorld, downEnd, downCells, cm));

            yield return travelRight;
            Object.Destroy(rocketCopyRight);
            yield return travelLeft;
            Object.Destroy(rocketCopyLeft);
            yield return travelUp;
            Object.Destroy(rocketCopyUp);
            yield return travelDown;
            Object.Destroy(rocketCopyDown);

            HashSet<Vector2Int> walls = new HashSet<Vector2Int>();
            CollectAdjacentWalls(rocketPos, walls);
            CollectAffectedWallsForRocketLine(rightCells, walls);
            CollectAffectedWallsForRocketLine(leftCells, walls);
            CollectAffectedWallsForRocketLine(upCells, walls);
            CollectAffectedWallsForRocketLine(downCells, walls);

            yield return grid.StartCoroutine(grid.BreakWallsSimultaneous(walls));
            yield return grid.StartCoroutine(grid.ResolveBoardAfterSpecialClear());
        }

        private void CollectAffectedWallsForRocketLine(List<Vector2Int> lineCells, HashSet<Vector2Int> walls)
        {
            if (lineCells == null)
                return;

            for (int i = 0; i < lineCells.Count; i++)
            {
                Vector2Int pos = lineCells[i];
                Grid3D.GridCell cell = grid.GetCellPublic(pos);
                if (cell == null)
                    continue;

                if (cell.cellType == Grid3D.CellType.BreakableWall)
                {
                    if (cell.breakableWallElementCondition != null)
                        continue;

                    walls.Add(pos);
                    continue;
                }

                CollectAdjacentWalls(pos, walls);
            }
        }

        private List<Vector2Int> CollectLineCells(Vector2Int origin, Vector2Int direction)
        {
            List<Vector2Int> cells = new List<Vector2Int>();
            Vector2Int current = origin + direction;
            while (true)
            {
                Grid3D.GridCell cell = grid.GetCellPublic(current);
                if (cell == null) break;

                if (cell.cellType == Grid3D.CellType.UnbreakableWall)
                    break;

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
                if (cell != null && cell.cellType == Grid3D.CellType.BreakableWall && cell.breakableWallElementCondition == null)
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

            grid.TriggerCellFeatureMatchedOverAt(pos);

            if (grid.TryRevealHiddenBoxAt(pos))
                return;

            if (GameManager.Instance != null &&
                cell.elementInfo.elementData != null && GameManager.Instance.garbageBagElementData == cell.elementInfo.elementData)
                return;

            if (IsSpecialPowerUp(cell.elementInfo.powerUpType))
            {
                grid.StartCoroutine(ActivateAt(pos, null));
                return;
            }

            if (cell.elementInfo.powerUpType == ElementPowerUpType.Cauldron) return;

            grid.NotifyElementCleared(pos);
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
            GameManager gm = GameManager.Instance;
            if (gm == null) return sourceData;
            if (type == ElementPowerUpType.Bomb && gm.bombElementData != null) return gm.bombElementData;
            if (IsRocket(type) && gm.rocketElementData != null) return gm.rocketElementData;
            if (IsPropeller(type) && gm.propellerElementData != null) return gm.propellerElementData;
            if (IsDiscoBall(type) && gm.discoBallElementData != null) return gm.discoBallElementData;
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