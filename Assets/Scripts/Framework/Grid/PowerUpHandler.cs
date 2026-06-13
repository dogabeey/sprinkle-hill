using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine; using Game.EventManagement;

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
        private const int StandardDiscoBallReplaceCount = 8;
        private const float ComboIntroMinRaiseHeight = 0.55f;
        private const float ComboIntroMaxRaiseHeight = 1.1f;
        private const float ComboIntroMinOrbitRadius = 0.25f;
        private const float ComboIntroMaxOrbitRadius = 0.6f;
        private const int ComboIntroSpinDegrees = 1080;
        private const float ComboIntroMergeDuration = 0.14f;
        private readonly HashSet<Vector2Int> activatingPowerUpPositions = new HashSet<Vector2Int>();
        private int activePowerUpChainCount;
        private readonly Dictionary<ElementPowerUpType, IPowerUpActivationStrategy> activationStrategies;
        private readonly List<IPowerUpCreationStrategy> creationStrategies;

        private sealed class ComboIntroResult
        {
            public GridElement persistentElement;
            public GridElement disappearingElement;
            public Vector2Int persistentGridPosition;
            public Vector2Int disappearingGridPosition;
            public Vector3 mergeWorldPosition;
        }

        private IEnumerator WaitForActivationAnimation(Animator animator, string triggerName)
        {
            if (animator == null || string.IsNullOrEmpty(triggerName))
                yield break;

            // Wait until animator is in a state that contains the trigger name or until a timeout.
            float timeout = 1.5f; // fallback maximum wait
            float timer = 0f;

            // If animator has no runtime controller, skip wait
            if (animator.runtimeAnimatorController == null)
                yield break;

            // Try to detect animation by observing animator's state changes.
            int layer = 0;
            AnimatorStateInfo prevState = animator.GetCurrentAnimatorStateInfo(layer);
            while (timer < timeout)
            {
                AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(layer);
                if (state.fullPathHash != prevState.fullPathHash)
                {
                    // state changed; assume animation started
                    float remaining = state.length * (1f - state.normalizedTime);
                    // safety clamp
                    remaining = Mathf.Min(remaining, timeout - timer);
                    yield return new WaitForSeconds(remaining);
                    yield break;
                }

                timer += 0.02f;
                yield return new WaitForSeconds(0.02f);
            }
        }

        private enum SwapComboVisualType
        {
            None,
            DiscoBallAndDiscoBall,
            DiscoBallAndRocket,
            DiscoBallAndPropeller,
            DiscoBallAndBomb
        }

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
                new VerticalRocketCreationStrategy(this),
                new HorizontalRocketCreationStrategy(this)
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

        public bool IsChainReactionInProgress()
        {
            return activePowerUpChainCount > 0;
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
            public bool isIndirectCreation;
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

            ConstantManager constantManager = GameManager.Instance != null ? ConstantManager.Instance : null;

            for (int i = 0; i < creationStrategies.Count; i++)
            {
                IPowerUpCreationStrategy strategy = creationStrategies[i];
                if (!strategy.IsEnabled(context))
                    continue;

                if (constantManager != null && IsCreationLimitedByCurrentCount(strategy.GetType(), context, constantManager))
                    continue;

                strategy.CollectSpawns(context, resolution, claimedPositions);
            }

            return resolution;
        }

        private bool IsCreationLimitedByCurrentCount(System.Type strategyType, CreationContext context, ConstantManager constantManager)
        {
            if (strategyType == typeof(DiscoBallCreationStrategy))
                return GetPowerUpCount(ElementPowerUpType.DiscoBall) >= GetLimit(context.isIndirectCreation, constantManager.maxDiscoBallCount, constantManager.maxDiscoBallIndirectCount);

            if (strategyType == typeof(PropellerCreationStrategy))
                return GetPowerUpCount(ElementPowerUpType.Propeller) >= GetLimit(context.isIndirectCreation, constantManager.maxPropellerCount, constantManager.maxPropellerIndirectCount);

            if (strategyType == typeof(BombCreationStrategy))
                return GetPowerUpCount(ElementPowerUpType.Bomb) >= GetLimit(context.isIndirectCreation, constantManager.maxBombCount, constantManager.maxBombIndirectCount);

            if (strategyType == typeof(VerticalRocketCreationStrategy) || strategyType == typeof(HorizontalRocketCreationStrategy))
            {
                int rocketCount = GetPowerUpCount(ElementPowerUpType.Rocket)
                                  + GetPowerUpCount(ElementPowerUpType.HorizontalRocket)
                                  + GetPowerUpCount(ElementPowerUpType.VerticalRocket);
                return rocketCount >= GetLimit(context.isIndirectCreation, constantManager.maxRocketCount, constantManager.maxRocketIndirectCount);
            }

            return false;
        }

        private static int GetLimit(bool isIndirectCreation, int directLimit, int indirectLimit)
        {
            return Mathf.Max(0, isIndirectCreation ? indirectLimit : directLimit);
        }

        private int GetPowerUpCount(ElementPowerUpType powerUpType)
        {
            int count = 0;
            for (int x = 0; x < grid.GridSize.x; x++)
            {
                for (int y = 0; y < grid.GridSize.y; y++)
                {
                    Grid3D.GridCell cell = grid.GetCellPublic(new Vector2Int(x, y));
                    if (cell == null || cell.cellType != Grid3D.CellType.Normal)
                        continue;

                    if (cell.elementInfo == null)
                        continue;

                    if (cell.elementInfo.powerUpType == powerUpType)
                        count++;
                }
            }

            return count;
        }

        public List<SpawnRequest> FindBombSpawns(List<List<Vector2Int>> matchedGroups, Vector2Int init1, Vector2Int init2, HashSet<Vector2Int> claimedPositions = null)
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
                        IsValidBombL(upLen, leftLen) ||
                        IsValidBombL(upLen, rightLen) ||
                        IsValidBombL(downLen, leftLen) ||
                        IsValidBombL(downLen, rightLen);

                    bool hasTShape =
                        IsValidBombT(upLen, leftLen, rightLen) ||
                        IsValidBombT(downLen, leftLen, rightLen) ||
                        IsValidBombT(leftLen, upLen, downLen) ||
                        IsValidBombT(rightLen, upLen, downLen);

                    if (!hasLShape && !hasTShape)
                        continue;

                    Vector2Int spawnPos = groupSet.Contains(init2) ? init2 : (groupSet.Contains(init1) ? init1 : pivot);
                    if (claimedPositions != null && claimedPositions.Contains(spawnPos))
                    {
                        found = true;
                        break;
                    }

                    if (used.Add(spawnPos))
                        spawns.Add(new SpawnRequest { position = spawnPos, sourceData = src, powerUpType = ElementPowerUpType.Bomb });
                    found = true;
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
                            spawns.Add(new SpawnRequest { position = spawnPos, sourceData = src, powerUpType = ElementPowerUpType.Propeller });
                        found = true;
                    }
                }
            }

            return spawns;
        }

        private static bool IsValidBombL(int verticalArmLen, int horizontalArmLen)
        {
            if (verticalArmLen < 1 || horizontalArmLen < 1)
                return false;

            int uniqueCount = verticalArmLen + horizontalArmLen + 1;
            if (uniqueCount < 4)
                return false;

            // Prevent 2x2 square from being treated as bomb (1+1+1 == 3),
            // and require at least one arm to extend beyond a single neighbor.
            return verticalArmLen >= 2 || horizontalArmLen >= 2;
        }

        private static bool IsValidBombT(int stemArmLen, int branchArmLen1, int branchArmLen2)
        {
            if (stemArmLen < 1 || branchArmLen1 < 1 || branchArmLen2 < 1)
                return false;

            int uniqueCount = stemArmLen + branchArmLen1 + branchArmLen2 + 1;
            return uniqueCount >= 4;
        }

        public List<SpawnRequest> FindVerticalRocketSpawns(List<List<Vector2Int>> matchedGroups, Vector2Int init1, Vector2Int init2, HashSet<Vector2Int> claimedPositions = null)
        {
            List<SpawnRequest> spawns = new List<SpawnRequest>();
            HashSet<Vector2Int> used = new HashSet<Vector2Int>();

            for (int g = 0; g < matchedGroups.Count; g++)
            {
                List<Vector2Int> group = matchedGroups[g];
                if (group == null || group.Count < 4) continue;

                // Groups of 5+ are reserved for Disco Ball � skip entirely
                if (group.Count >= 5) continue;

                HashSet<Vector2Int> groupSet = new HashSet<Vector2Int>(group);
                ElementData src = grid.GetCellPublic(group[0])?.elementInfo?.elementData;
                if (src == null) continue;
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
                            if (run >= 4)
                            {
                                Vector2Int sp = PickPreferredFromSet(runPos, init1, init2);
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

        public List<SpawnRequest> FindHorizontalRocketSpawns(List<List<Vector2Int>> matchedGroups, Vector2Int init1, Vector2Int init2, HashSet<Vector2Int> claimedPositions = null)
        {
            List<SpawnRequest> spawns = new List<SpawnRequest>();
            HashSet<Vector2Int> used = new HashSet<Vector2Int>();

            for (int g = 0; g < matchedGroups.Count; g++)
            {
                List<Vector2Int> group = matchedGroups[g];
                if (group == null || group.Count < 4) continue;

                if (group.Count >= 5) continue;

                HashSet<Vector2Int> groupSet = new HashSet<Vector2Int>(group);
                ElementData src = grid.GetCellPublic(group[0])?.elementInfo?.elementData;
                if (src == null) continue;
                bool found = false;

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
                                if (claimedPositions != null && claimedPositions.Contains(sp)) { found = true; break; }
                                if (used.Add(sp))
                                    spawns.Add(new SpawnRequest { position = sp, sourceData = src, powerUpType = ElementPowerUpType.HorizontalRocket });
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

        private sealed class VerticalRocketCreationStrategy : IPowerUpCreationStrategy
        {
            private readonly PowerUpHandler handler;

            public VerticalRocketCreationStrategy(PowerUpHandler handler)
            {
                this.handler = handler;
            }

            public bool IsEnabled(CreationContext context) => context.allowRocket;

            public void CollectSpawns(CreationContext context, SpawnResolution resolution, HashSet<Vector2Int> claimedPositions)
            {
                List<SpawnRequest> spawns = handler.FindVerticalRocketSpawns(context.matchedGroups, context.init1, context.init2, claimedPositions);
                for (int i = 0; i < spawns.Count; i++)
                {
                    SpawnRequest spawn = spawns[i];
                    resolution.rocketSpawns.Add(spawn);
                    resolution.protectedPositions.Add(spawn.position);
                    claimedPositions.Add(spawn.position);
                }
            }
        }

        private sealed class HorizontalRocketCreationStrategy : IPowerUpCreationStrategy
        {
            private readonly PowerUpHandler handler;

            public HorizontalRocketCreationStrategy(PowerUpHandler handler)
            {
                this.handler = handler;
            }

            public bool IsEnabled(CreationContext context) => context.allowRocket;

            public void CollectSpawns(CreationContext context, SpawnResolution resolution, HashSet<Vector2Int> claimedPositions)
            {
                List<SpawnRequest> spawns = handler.FindHorizontalRocketSpawns(context.matchedGroups, context.init1, context.init2, claimedPositions);
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
            if (!activatingPowerUpPositions.Add(pos))
                yield break;

            Grid3D.GridCell cell = grid.GetCellPublic(pos);
            ElementPowerUpType type = cell?.elementInfo?.powerUpType ?? ElementPowerUpType.None;
            try
            {
                if (!activationStrategies.TryGetValue(type, out IPowerUpActivationStrategy strategy))
                    yield break;

                activePowerUpChainCount++;

                // If the element at position has an animator and a power-up activation trigger name,
                // play the activation animation first and wait for it to complete before running the power-up.
                GridElement elem = grid.GetElementAt(pos);
                if (elem != null && elem.elementAnimator != null && !string.IsNullOrEmpty(elem.powerUpActivationString))
                {
                    bool triggered = false;
                    try
                    {
                        elem.elementAnimator.SetTrigger(elem.powerUpActivationString);
                        triggered = true;
                    }
                    catch { triggered = false; }

                    if (triggered)
                        yield return grid.StartCoroutine(WaitForActivationAnimation(elem.elementAnimator, elem.powerUpActivationString));
                }

                yield return grid.StartCoroutine(strategy.Activate(pos, swappedElementData));
            }
            finally
            {
                if (activePowerUpChainCount > 0)
                    activePowerUpChainCount--;

                activatingPowerUpPositions.Remove(pos);
            }
        }

        public IEnumerator ActivateSwapComboAt(Vector2Int firstPos, Vector2Int secondPos)
        {
            if (!activatingPowerUpPositions.Add(firstPos))
                yield break;

            if (!activatingPowerUpPositions.Add(secondPos))
            {
                activatingPowerUpPositions.Remove(firstPos);
                yield break;
            }

            try
            {
                Grid3D.GridCell firstCell = grid.GetCellPublic(firstPos);
                Grid3D.GridCell secondCell = grid.GetCellPublic(secondPos);
                ElementPowerUpType firstType = firstCell?.elementInfo?.powerUpType ?? ElementPowerUpType.None;
                ElementPowerUpType secondType = secondCell?.elementInfo?.powerUpType ?? ElementPowerUpType.None;

                if (IsDiscoBall(secondType) && !IsDiscoBall(firstType))
                {
                    Vector2Int tempPos = firstPos;
                    firstPos = secondPos;
                    secondPos = tempPos;

                    ElementPowerUpType tempType = firstType;
                    firstType = secondType;
                    secondType = tempType;
                }
                else if (secondType == ElementPowerUpType.Bomb && IsRocket(firstType))
                {
                    Vector2Int tempPos = firstPos;
                    firstPos = secondPos;
                    secondPos = tempPos;

                    ElementPowerUpType tempType = firstType;
                    firstType = secondType;
                    secondType = tempType;
                }
                else if (IsRocket(secondType) && IsPropeller(firstType))
                {
                    Vector2Int tempPos = firstPos;
                    firstPos = secondPos;
                    secondPos = tempPos;

                    ElementPowerUpType tempType = firstType;
                    firstType = secondType;
                    secondType = tempType;
                }
                else if (secondType == ElementPowerUpType.Bomb && IsPropeller(firstType))
                {
                    Vector2Int tempPos = firstPos;
                    firstPos = secondPos;
                    secondPos = tempPos;

                    ElementPowerUpType tempType = firstType;
                    firstType = secondType;
                    secondType = tempType;
                }

                IPowerUpActivationStrategy strategy = GetSwapComboStrategy(firstType, secondType);
                if (strategy == null)
                    yield break;

                activePowerUpChainCount++;
                ComboIntroResult comboIntroResult = new ComboIntroResult();
                SwapComboVisualType comboVisualType = ResolveSwapComboVisualType(strategy);
                yield return grid.StartCoroutine(PlayPowerUpComboIntro(firstPos, secondPos, comboVisualType, comboIntroResult));
                yield return grid.StartCoroutine(strategy.Activate(firstPos, null));
            }
            finally
            {
                if (activePowerUpChainCount > 0)
                    activePowerUpChainCount--;

                activatingPowerUpPositions.Remove(firstPos);
                activatingPowerUpPositions.Remove(secondPos);
            }
        }

        private IPowerUpActivationStrategy GetSwapComboStrategy(ElementPowerUpType firstType, ElementPowerUpType secondType)
        {
            if (IsDiscoBall(firstType) && IsDiscoBall(secondType))
                return new DiscoBallAndDiscoBallComboActivationStrategy(this);

            if (IsDiscoBall(firstType) && IsRocket(secondType))
                return new DiscoBallAndRocketComboActivationStrategy(this);

            if (IsDiscoBall(firstType) && IsPropeller(secondType))
                return new DiscoBallAndPropellerComboActivationStrategy(this);

            if (IsDiscoBall(firstType) && secondType == ElementPowerUpType.Bomb)
                return new DiscoBallAndBombComboActivationStrategy(this);

            if (IsPropeller(firstType) && IsPropeller(secondType))
                return new PropellerAndPropellerComboActivationStrategy(this);

            if (firstType == ElementPowerUpType.Bomb && IsPropeller(secondType))
                return new PropellerAndBombComboActivationStrategy(this);

            if (IsRocket(firstType) && IsRocket(secondType))
                return new RocketAndRocketComboActivationStrategy(this);

            if (IsRocket(firstType) && IsPropeller(secondType))
                return new RocketAndPropellerComboActivationStrategy(this);

            if (firstType == ElementPowerUpType.Bomb && IsRocket(secondType))
                return new RocketAndBombComboActivationStrategy(this);

            if (firstType == ElementPowerUpType.Bomb && secondType == ElementPowerUpType.Bomb)
                return new BombAndBombComboActivationStrategy(this);

            return null;
        }

        private IEnumerator PlayPowerUpComboIntro(Vector2Int firstPos, Vector2Int secondPos, SwapComboVisualType comboVisualType, ComboIntroResult introResult)
        {
            if (introResult == null)
                yield break;

            GridElement firstElement = grid.GetElementAt(firstPos);
            GridElement secondElement = grid.GetElementAt(secondPos);
            Sprite comboMergeSprite = ResolveDiscoBallComboMergeSprite(comboVisualType);

            Vector3 firstStartWorld = firstElement != null ? firstElement.transform.position : grid.GetWorldPosition(firstPos);
            Vector3 secondStartWorld = secondElement != null ? secondElement.transform.position : grid.GetWorldPosition(secondPos);
            Vector3 comboCenter = (firstStartWorld + secondStartWorld) * 0.5f;
            float distance = Vector3.Distance(firstStartWorld, secondStartWorld);
            float raiseHeight = Mathf.Clamp(distance * 0.35f, ComboIntroMinRaiseHeight, ComboIntroMaxRaiseHeight);
            float orbitRadius = Mathf.Clamp(distance * 0.5f, ComboIntroMinOrbitRadius, ComboIntroMaxOrbitRadius);
            float raiseDuration = Mathf.Max(0.15f, ConstantManager.Instance.elementSwapMoveDuration * 0.8f);
            float spinDuration = Mathf.Max(0.3f, ConstantManager.Instance.elementSwapMoveDuration * 1.4f);

            Transform animationParent = grid.GridParent != null ? grid.GridParent : grid.transform;
            GameObject orbitPivot = new GameObject("PowerUpComboIntroPivot");
            orbitPivot.transform.SetParent(animationParent, true);
            orbitPivot.transform.position = comboCenter + (Vector3.up * raiseHeight);

            Transform firstOriginalParent = firstElement != null ? firstElement.transform.parent : null;
            Transform secondOriginalParent = secondElement != null ? secondElement.transform.parent : null;

            if (firstElement != null)
            {
                firstElement.transform.DOKill();
                firstElement.transform.SetParent(orbitPivot.transform, true);
                AdjustSortingOrder(firstElement, SortingOrderBoost);
            }

            if (secondElement != null)
            {
                secondElement.transform.DOKill();
                secondElement.transform.SetParent(orbitPivot.transform, true);
                AdjustSortingOrder(secondElement, SortingOrderBoost);
            }

            ParticleSystem comboParticle = SpawnPowerUpComboParticle(orbitPivot.transform);
            Sequence comboSequence = DOTween.Sequence();

            if (firstElement != null)
            {
                comboSequence.Join(firstElement.transform.DOLocalMove(new Vector3(-orbitRadius, 0f, 0f), raiseDuration).SetEase(Ease.OutBack));
                comboSequence.Join(firstElement.transform.DOScale(1.15f, raiseDuration * 0.5f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine));
            }

            if (secondElement != null)
            {
                comboSequence.Join(secondElement.transform.DOLocalMove(new Vector3(orbitRadius, 0f, 0f), raiseDuration).SetEase(Ease.OutBack));
                comboSequence.Join(secondElement.transform.DOScale(1.15f, raiseDuration * 0.5f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine));
            }

            comboSequence.Append(orbitPivot.transform
                .DORotate(new Vector3(0f, 0f, ComboIntroSpinDegrees), spinDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetRelative());

            yield return comboSequence.WaitForCompletion();

            Sequence mergeSequence = DOTween.Sequence();

            if (comboMergeSprite != null)
            {
                ApplyComboMergeSprite(firstElement, comboMergeSprite);
                ApplyComboMergeSprite(secondElement, comboMergeSprite);
            }

            if (firstElement != null)
            {
                mergeSequence.Join(firstElement.transform.DOMove(comboCenter, ComboIntroMergeDuration).SetEase(Ease.InBack));
            }

            if (secondElement != null)
            {
                mergeSequence.Join(secondElement.transform.DOMove(comboCenter, ComboIntroMergeDuration).SetEase(Ease.InBack));
                mergeSequence.Join(secondElement.transform.DOScale(0f, ComboIntroMergeDuration).SetEase(Ease.InBack));
            }

            if (mergeSequence.active)
                yield return mergeSequence.WaitForCompletion();

            introResult.persistentElement = firstElement;
            introResult.disappearingElement = secondElement;
            introResult.persistentGridPosition = firstPos;
            introResult.disappearingGridPosition = secondPos;
            introResult.mergeWorldPosition = comboCenter;

            RestorePersistentComboElement(firstElement, firstOriginalParent, comboCenter, firstPos);
            if (comboMergeSprite != null)
                ApplyComboMergeSprite(firstElement, comboMergeSprite);
            DestroyComboElementVisual(secondElement);

            if (comboParticle != null)
            {
                comboParticle.transform.SetParent(null, true);
                float particleLifetime = comboParticle.main.duration + comboParticle.main.startLifetime.constantMax + 0.2f;
                Object.Destroy(comboParticle.gameObject, particleLifetime);
            }

            Object.Destroy(orbitPivot);
        }

        private SwapComboVisualType ResolveSwapComboVisualType(IPowerUpActivationStrategy strategy)
        {
            if (strategy is DiscoBallAndDiscoBallComboActivationStrategy)
                return SwapComboVisualType.DiscoBallAndDiscoBall;

            if (strategy is DiscoBallAndRocketComboActivationStrategy)
                return SwapComboVisualType.DiscoBallAndRocket;

            if (strategy is DiscoBallAndPropellerComboActivationStrategy)
                return SwapComboVisualType.DiscoBallAndPropeller;

            if (strategy is DiscoBallAndBombComboActivationStrategy)
                return SwapComboVisualType.DiscoBallAndBomb;

            return SwapComboVisualType.None;
        }

        private Sprite ResolveDiscoBallComboMergeSprite(SwapComboVisualType comboVisualType)
        {
            Gfx gfxManager = GameManager.Instance != null ? Gfx.Instance : null;
            if (gfxManager == null)
                return null;

            switch (comboVisualType)
            {
                case SwapComboVisualType.DiscoBallAndDiscoBall:
                    return gfxManager.discoBallAndDiscoBallComboSprite;
                case SwapComboVisualType.DiscoBallAndRocket:
                    return gfxManager.discoBallAndRocketComboSprite;
                case SwapComboVisualType.DiscoBallAndPropeller:
                    return gfxManager.discoBallAndPropellerComboSprite;
                case SwapComboVisualType.DiscoBallAndBomb:
                    return gfxManager.discoBallAndBombComboSprite;
                default:
                    return null;
            }
        }

        private void ApplyComboMergeSprite(GridElement element, Sprite comboSprite)
        {
            if (element == null || comboSprite == null)
                return;

            if (element.elementRenderer is SpriteRenderer spriteRenderer)
                spriteRenderer.sprite = comboSprite;
        }

        private ParticleSystem SpawnPowerUpComboParticle(Transform comboAnchor)
        {
            Gfx gfxManager = GameManager.Instance != null ? Gfx.Instance : null;
            if (gfxManager == null || gfxManager.powerUpComboParticlePrefab == null || comboAnchor == null)
                return null;

            ParticleSystem comboParticle = Object.Instantiate(gfxManager.powerUpComboParticlePrefab, comboAnchor.position, Quaternion.identity, comboAnchor);
            comboParticle.Play();
            return comboParticle;
        }

        private void RestorePersistentComboElement(GridElement element, Transform originalParent, Vector3 worldPosition, Vector2Int gridPos)
        {
            if (element == null)
                return;

            element.transform.DOKill();
            if (originalParent != null)
                element.transform.SetParent(originalParent, true);

            element.transform.position = worldPosition;

            Grid3D.GridCell cell = grid.GetCellPublic(gridPos);
            if (cell?.elementInfo != null)
            {
                element.elementInfo = cell.elementInfo;
                element.InitElement(grid, cell.elementInfo);
            }
            else
            {
                element.transform.localScale = Vector3.one;
            }

            AdjustSortingOrder(element, -SortingOrderBoost);
        }

        private void DestroyComboElementVisual(GridElement element)
        {
            if (element == null)
                return;

            element.transform.DOKill();
            grid.StartCoroutine(element.DestroyElement());
        }

        private void AdjustSortingOrder(GridElement element, int delta)
        {
            if (element == null || delta == 0)
                return;

            Renderer[] renderers = element.GetComponentsInChildren<Renderer>(true);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                    renderers[i].sortingOrder += delta;
            }
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
        private sealed class DiscoBallAndDiscoBallComboActivationStrategy : IPowerUpActivationStrategy
        {
            private readonly PowerUpHandler handler;
            public DiscoBallAndDiscoBallComboActivationStrategy(PowerUpHandler handler)
            {
                this.handler = handler;
            }
            public IEnumerator Activate(Vector2Int pos, ElementData swappedElementData)
            {
                return handler.ActivateDiscoBallAndDiscoBallCombo(pos);
            }
        }
        private sealed class DiscoBallAndRocketComboActivationStrategy : IPowerUpActivationStrategy
        {
            private readonly PowerUpHandler handler;
            public DiscoBallAndRocketComboActivationStrategy(PowerUpHandler handler)
            {
                this.handler = handler;
            }
            public IEnumerator Activate(Vector2Int pos, ElementData swappedElementData)
            {
                return handler.ActivateDiscoBallAndRocketCombo(pos);
            }
        }
        private sealed class DiscoBallAndPropellerComboActivationStrategy : IPowerUpActivationStrategy
        {
            private readonly PowerUpHandler handler;
            public DiscoBallAndPropellerComboActivationStrategy(PowerUpHandler handler)
            {
                this.handler = handler;
            }
            public IEnumerator Activate(Vector2Int pos, ElementData swappedElementData)
            {
                return handler.ActivateDiscoBallAndPropellerCombo(pos);
            }
        }
        private sealed class DiscoBallAndBombComboActivationStrategy : IPowerUpActivationStrategy
        {
            private readonly PowerUpHandler handler;
            public DiscoBallAndBombComboActivationStrategy(PowerUpHandler handler)
            {
                this.handler = handler;
            }
            public IEnumerator Activate(Vector2Int pos, ElementData swappedElementData)
            {
                return handler.ActivateDiscoBallAndBombCombo(pos);
            }
        }
        private sealed class RocketAndRocketComboActivationStrategy : IPowerUpActivationStrategy
        {
            private readonly PowerUpHandler handler;
            public RocketAndRocketComboActivationStrategy(PowerUpHandler handler)
            {
                this.handler = handler;
            }
            public IEnumerator Activate(Vector2Int pos, ElementData swappedElementData)
            {
                return handler.ActivateRocketAndRocketCombo(pos);
            }
        }
        private sealed class RocketAndPropellerComboActivationStrategy : IPowerUpActivationStrategy
        {
            private readonly PowerUpHandler handler;
            public RocketAndPropellerComboActivationStrategy(PowerUpHandler handler)
            {
                this.handler = handler;
            }
            public IEnumerator Activate(Vector2Int pos, ElementData swappedElementData)
            {
                return handler.ActivateRocketAndPropellerCombo(pos);
            }
        }
        private sealed class RocketAndBombComboActivationStrategy : IPowerUpActivationStrategy
        {
            private readonly PowerUpHandler handler;
            public RocketAndBombComboActivationStrategy(PowerUpHandler handler)
            {
                this.handler = handler;
            }
            public IEnumerator Activate(Vector2Int pos, ElementData swappedElementData)
            {
                return handler.ActivateRocketAndBombCombo(pos);
            }
        }
        private sealed class PropellerAndPropellerComboActivationStrategy : IPowerUpActivationStrategy
        {
            private readonly PowerUpHandler handler;
            public PropellerAndPropellerComboActivationStrategy(PowerUpHandler handler)
            {
                this.handler = handler;
            }
            public IEnumerator Activate(Vector2Int pos, ElementData swappedElementData)
            {
                return handler.ActivatePropellerAndPropellerCombo(pos);
            }
        }
        private sealed class PropellerAndBombComboActivationStrategy : IPowerUpActivationStrategy
        {
            private readonly PowerUpHandler handler;
            public PropellerAndBombComboActivationStrategy(PowerUpHandler handler)
            {
                this.handler = handler;
            }
            public IEnumerator Activate(Vector2Int pos, ElementData swappedElementData)
            {
                return handler.ActivatePropellerAndBombCombo(pos);
            }
        }
        private sealed class BombAndBombComboActivationStrategy : IPowerUpActivationStrategy
        {
            private readonly PowerUpHandler handler;
            public BombAndBombComboActivationStrategy(PowerUpHandler handler)
            {
                this.handler = handler;
            }
            public IEnumerator Activate(Vector2Int pos, ElementData swappedElementData)
            {
                return handler.ActivateBombAndBombCombo(pos);
            }
        }

        private IEnumerator ActivateDiscoBall(Vector2Int discoBallPos, ElementData targetElementData)
        {
            // Validate disco ball presence at the position before proceeding with activation.
            Grid3D.GridCell discoBallCell = grid.GetCellPublic(discoBallPos);
            if (discoBallCell?.elementInfo == null || discoBallCell.elementInfo.powerUpType != ElementPowerUpType.DiscoBall)
                yield break;

            // If target element data is null (which can happen if the disco ball was activated via a match that doesn't contain any regular elements, like a 4-match), resolve to a default target element type (e.g. the most common element type on the board) to ensure the disco ball still does something impactful.
            targetElementData = ResolveDefaultDiscoBallTargetElement(targetElementData);

            if (targetElementData == null) yield break;

            // Event Trigger
            EventManager.TriggerEvent(GameEvent.SPECIAL_ELEMENT_ACTIVATED);

            // Sound Effect
            PlayEffect(ConstantManager.SOUNDS.EFFECTS.DISCO_BALL_ACTIVATE);

            // Visual: Start spinning the disco ball.
            GridElement discoBallElement = grid.GetElementAt(discoBallPos);
            Tween spinTween = null;
            StartDiscoBallSpin(discoBallElement, ref spinTween);

            // Remove logical occupancy, keep visual until trail animation finishes.
            grid.TriggerCellFeatureMatchedOverAt(discoBallPos);
            discoBallCell.elementInfo = null;

            // Send trails to all cells containing the target element, then destroy those elements.
            List<Vector2Int> matchingCells = GetAllCellsWithElement(targetElementData);
            if (matchingCells.Count > 0)
            {
                yield return grid.StartCoroutine(AnimateDiscoBallTrails(discoBallPos, matchingCells, targetElementData));
                DestroyDiscoBallConvertedCells(matchingCells);
            }


            // Clear disco ball
            StopAndDestroyDiscoBallElement(discoBallElement, spinTween);

        }

        private IEnumerator ActivateDiscoBallAndDiscoBallCombo(Vector2Int primaryDiscoBallPos)
        {
            // Validate primary disco ball presence at the position before proceeding with activation.
            Grid3D.GridCell primaryCell = grid.GetCellPublic(primaryDiscoBallPos);
            if (primaryCell?.elementInfo == null || primaryCell.elementInfo.powerUpType != ElementPowerUpType.DiscoBall)
                yield break;

            // Find secondary disco ball and validate its presence.
            Vector2Int secondaryDiscoBallPos = FindAdjacentDiscoBall(primaryDiscoBallPos);
            if (secondaryDiscoBallPos == primaryDiscoBallPos)
                yield break;

            // Determine target element type for the combo. This can be based on the swapped element data if the combo was triggered by a swap, 
            // or it can be resolved to most common element type if both disco balls were activated via matches that don't contain any regular elements.
            ElementData designatedElementData = ResolveRandomDiscoBallComboTargetElement(primaryDiscoBallPos, secondaryDiscoBallPos);
            if (designatedElementData == null)
                yield break;

            // Event Trigger
            EventManager.TriggerEvent(GameEvent.SPECIAL_ELEMENT_ACTIVATED);

            // Sound Effect
            PlayEffect(ConstantManager.SOUNDS.EFFECTS.DISCO_BALL_ACTIVATE, volumeMultiplier: 1.1f, pitchOffset: -0.04f);

            // Visual: Start spinning the disco balls.  
            GridElement primaryElement = grid.GetElementAt(primaryDiscoBallPos);
            GridElement secondaryElement = grid.GetElementAt(secondaryDiscoBallPos);
            Tween primarySpinTween = null;
            Tween secondarySpinTween = null;

            // Trigger the primary disco ball's spin first, then the secondary.
            StartDiscoBallSpin(primaryElement, ref primarySpinTween);
            StartDiscoBallSpin(secondaryElement, ref secondarySpinTween);

            // Remove logical occupancy, keep visuals until trail animation finishes.
            grid.TriggerCellFeatureMatchedOverAt(primaryDiscoBallPos);
            primaryCell.elementInfo = null;

            // For the secondary disco ball, we can trigger its matched over event and clear its element info immediately since the primary disco 
            // ball's trails will cover the entire path to the secondary, so there won't be a visual gap exposing the absence of the secondary 
            // disco ball's visual until the trails start animating.
            Grid3D.GridCell secondaryCell = grid.GetCellPublic(secondaryDiscoBallPos);
            if (secondaryCell != null)
            {
                grid.TriggerCellFeatureMatchedOverAt(secondaryDiscoBallPos);
                secondaryCell.elementInfo = null;
            }

            // Pick all elements in the entire grid since disco+disco should destroy all elements.
            List<Vector2Int> selectedCells = GetAllCellsInGrid();
            if (selectedCells.Count > 0)
            {
                grid.StartCoroutine(AnimateDiscoBallTrails(primaryDiscoBallPos, selectedCells, designatedElementData));
                DestroyDiscoBallConvertedCells(selectedCells);
            }

            StopAndDestroyDiscoBallElement(primaryElement, primarySpinTween);
            StopAndDestroyDiscoBallElement(secondaryElement, secondarySpinTween);
        }

        private List<Vector2Int> GetAllCellsInGrid()
        {
            List<Vector2Int> allCells = new List<Vector2Int>();
            for (int x = 0; x < grid.GridSize.x; x++)
            {
                for (int y = 0; y < grid.GridSize.y; y++)
                {
                    allCells.Add(new Vector2Int(x, y));
                }
            }
            return allCells;
        }

        private IEnumerator ActivateDiscoBallAndBombCombo(Vector2Int discoBallPos)
        {
            Grid3D.GridCell discoBallCell = grid.GetCellPublic(discoBallPos);
            if (discoBallCell?.elementInfo == null || discoBallCell.elementInfo.powerUpType != ElementPowerUpType.DiscoBall)
                yield break;

            Vector2Int bombPos = FindAdjacentPowerUpOfType(discoBallPos, ElementPowerUpType.Bomb);
            if (bombPos == discoBallPos)
                yield break;

            EventManager.TriggerEvent(GameEvent.SPECIAL_ELEMENT_ACTIVATED);
            PlayEffect(ConstantManager.SOUNDS.EFFECTS.DISCO_BALL_ACTIVATE, volumeMultiplier: 1.05f, pitchOffset: -0.02f);

            GridElement discoBallElement = grid.GetElementAt(discoBallPos);
            Tween discoBallSpinTween = null;

            StartDiscoBallSpin(discoBallElement, ref discoBallSpinTween);

            grid.TriggerCellFeatureMatchedOverAt(discoBallPos);
            discoBallCell.elementInfo = null;

            Grid3D.GridCell bombCell = grid.GetCellPublic(bombPos);
            if (bombCell != null)
            {
                grid.TriggerCellFeatureMatchedOverAt(bombPos);
                bombCell.elementInfo = null;
            }

            List<Vector2Int> selectedCells = GetRandomEligibleDiscoBallTargets(discoBallPos, bombPos, StandardDiscoBallReplaceCount);
            if (selectedCells.Count > 0)
            {
                yield return grid.StartCoroutine(AnimateDiscoBallPowerUpTrails(discoBallPos, selectedCells, ElementPowerUpType.Bomb));

                yield return null;

                for (int i = 0; i < selectedCells.Count; i++)
                    yield return grid.StartCoroutine(ActivateAt(selectedCells[i], null));
            }

            StopAndDestroyDiscoBallElement(discoBallElement, discoBallSpinTween);

        }

        private IEnumerator ActivateDiscoBallAndRocketCombo(Vector2Int discoBallPos)
        {
            Grid3D.GridCell discoBallCell = grid.GetCellPublic(discoBallPos);
            if (discoBallCell?.elementInfo == null || discoBallCell.elementInfo.powerUpType != ElementPowerUpType.DiscoBall)
                yield break;

            Vector2Int rocketPos = FindAdjacentRocket(discoBallPos);
            if (rocketPos == discoBallPos)
                yield break;

            EventManager.TriggerEvent(GameEvent.SPECIAL_ELEMENT_ACTIVATED);
            PlayEffect(ConstantManager.SOUNDS.EFFECTS.DISCO_BALL_ACTIVATE, volumeMultiplier: 1.05f, pitchOffset: 0.02f);

            GridElement discoBallElement = grid.GetElementAt(discoBallPos);
            Tween discoBallSpinTween = null;

            StartDiscoBallSpin(discoBallElement, ref discoBallSpinTween);

            grid.TriggerCellFeatureMatchedOverAt(discoBallPos);
            discoBallCell.elementInfo = null;

            Grid3D.GridCell rocketCell = grid.GetCellPublic(rocketPos);
            ElementPowerUpType rocketType = rocketCell?.elementInfo?.powerUpType ?? ElementPowerUpType.Rocket;
            if (rocketCell != null)
            {
                grid.TriggerCellFeatureMatchedOverAt(rocketPos);
                rocketCell.elementInfo = null;
            }

            List<Vector2Int> selectedCells = GetRandomEligibleDiscoBallTargets(discoBallPos, rocketPos, StandardDiscoBallReplaceCount);
            if (selectedCells.Count > 0)
            {
                yield return grid.StartCoroutine(AnimateDiscoBallPowerUpTrails(discoBallPos, selectedCells, rocketType));

                yield return null;

                for (int i = 0; i < selectedCells.Count; i++)
                    yield return grid.StartCoroutine(ActivateAt(selectedCells[i], null));
            }

            StopAndDestroyDiscoBallElement(discoBallElement, discoBallSpinTween);
        }

        private IEnumerator ActivatePropellerAndPropellerCombo(Vector2Int firstPropellerPos)
        {
            Grid3D.GridCell firstPropellerCell = grid.GetCellPublic(firstPropellerPos);
            if (firstPropellerCell?.elementInfo == null || firstPropellerCell.elementInfo.powerUpType != ElementPowerUpType.Propeller)
                yield break;

            Vector2Int secondPropellerPos = FindAdjacentPropeller(firstPropellerPos);
            if (secondPropellerPos == firstPropellerPos)
                yield break;

            EventManager.TriggerEvent(GameEvent.SPECIAL_ELEMENT_ACTIVATED);
            PlayEffect(ConstantManager.SOUNDS.EFFECTS.ROCKET, volumeMultiplier: 1.05f, pitchOffset: 0.14f);

            GridElement sourcePropellerElement = grid.GetElementAt(firstPropellerPos);

            grid.TriggerCellFeatureMatchedOverAt(firstPropellerPos);
            firstPropellerCell.elementInfo = null;

            Grid3D.GridCell secondPropellerCell = grid.GetCellPublic(secondPropellerPos);
            if (secondPropellerCell != null)
            {
                grid.TriggerCellFeatureMatchedOverAt(secondPropellerPos);
                secondPropellerCell.elementInfo = null;
            }

            List<Vector2Int> reservedTargets = ReservePropellerTargets(firstPropellerPos, 4, firstPropellerPos, secondPropellerPos);
            if (reservedTargets.Count > 0)
                yield return grid.StartCoroutine(ActivatePropellerBurstFromOrigin(firstPropellerPos, sourcePropellerElement, reservedTargets));

            if (sourcePropellerElement != null)
                grid.StartCoroutine(sourcePropellerElement.DestroyElement());
        }

        private IEnumerator ActivatePropellerAndBombCombo(Vector2Int bombPos)
        {
            Grid3D.GridCell bombCell = grid.GetCellPublic(bombPos);
            if (bombCell?.elementInfo == null || bombCell.elementInfo.powerUpType != ElementPowerUpType.Bomb)
                yield break;

            Vector2Int propellerPos = FindAdjacentPropeller(bombPos);
            if (propellerPos == bombPos)
                yield break;

            EventManager.TriggerEvent(GameEvent.SPECIAL_ELEMENT_ACTIVATED);
            PlayEffect(ConstantManager.SOUNDS.EFFECTS.ROCKET, volumeMultiplier: 1.05f, pitchOffset: -0.02f);

            GridElement bombElement = grid.GetElementAt(bombPos);
            Vector2Int targetPos = PickPropellerTargetPosition(propellerPos, new HashSet<Vector2Int> { bombPos, propellerPos });

            grid.TriggerCellFeatureMatchedOverAt(bombPos);
            bombCell.elementInfo = null;

            Grid3D.GridCell propellerCell = grid.GetCellPublic(propellerPos);
            if (propellerCell != null)
            {
                grid.TriggerCellFeatureMatchedOverAt(propellerPos);
                propellerCell.elementInfo = null;
            }

            yield return grid.StartCoroutine(FlyBombToTargetAndActivate(bombPos, targetPos, bombElement));
        }

        private IEnumerator ActivateRocketAndRocketCombo(Vector2Int firstRocketPos)
        {
            Grid3D.GridCell firstRocketCell = grid.GetCellPublic(firstRocketPos);
            ElementPowerUpType firstRocketType = firstRocketCell?.elementInfo?.powerUpType ?? ElementPowerUpType.None;
            if (!IsRocket(firstRocketType))
                yield break;

            Vector2Int secondRocketPos = FindAdjacentRocket(firstRocketPos);
            if (secondRocketPos == firstRocketPos)
                yield break;

            EventManager.TriggerEvent(GameEvent.SPECIAL_ELEMENT_ACTIVATED);

            GridElement rocketElement = grid.GetElementAt(firstRocketPos);

            grid.TriggerCellFeatureMatchedOverAt(firstRocketPos);
            firstRocketCell.elementInfo = null;

            Grid3D.GridCell secondRocketCell = grid.GetCellPublic(secondRocketPos);
            if (secondRocketCell != null)
            {
                grid.TriggerCellFeatureMatchedOverAt(secondRocketPos);
                secondRocketCell.elementInfo = null;
            }

            Vector2Int[] rocketDirections =
            {
                Vector2Int.right,
                Vector2Int.left,
                Vector2Int.up,
                Vector2Int.down
            };

            yield return grid.StartCoroutine(ActivateRocketBurst(firstRocketPos, rocketElement, rocketDirections, ElementPowerUpType.Rocket, clearSourceCell: false, clearOriginCell: false, preLaunchDelay: 0.1f, pitchOffset: 0.04f));
        }

        private IEnumerator ActivateRocketAndPropellerCombo(Vector2Int rocketPos)
        {
            Grid3D.GridCell rocketCell = grid.GetCellPublic(rocketPos);
            ElementPowerUpType rocketType = rocketCell?.elementInfo?.powerUpType ?? ElementPowerUpType.None;
            if (!IsRocket(rocketType))
                yield break;

            Vector2Int propellerPos = FindAdjacentPropeller(rocketPos);
            if (propellerPos == rocketPos)
                yield break;

            EventManager.TriggerEvent(GameEvent.SPECIAL_ELEMENT_ACTIVATED);

            GridElement rocketElement = grid.GetElementAt(rocketPos);
            Vector2Int targetPos = PickPropellerTargetPosition(propellerPos, new HashSet<Vector2Int> { rocketPos, propellerPos });

            grid.TriggerCellFeatureMatchedOverAt(rocketPos);
            rocketCell.elementInfo = null;

            Grid3D.GridCell propellerCell = grid.GetCellPublic(propellerPos);
            if (propellerCell != null)
            {
                grid.TriggerCellFeatureMatchedOverAt(propellerPos);
                propellerCell.elementInfo = null;
            }

            yield return grid.StartCoroutine(FlyRocketToTargetAndActivate(rocketPos, targetPos, rocketElement, rocketType));
        }

        private IEnumerator ActivateRocketAndBombCombo(Vector2Int bombPos)
        {
            Grid3D.GridCell bombCell = grid.GetCellPublic(bombPos);
            if (bombCell?.elementInfo == null || bombCell.elementInfo.powerUpType != ElementPowerUpType.Bomb)
                yield break;

            Vector2Int rocketPos = FindAdjacentRocket(bombPos);
            if (rocketPos == bombPos)
                yield break;

            EventManager.TriggerEvent(GameEvent.SPECIAL_ELEMENT_ACTIVATED);
            PlayEffect(ConstantManager.SOUNDS.EFFECTS.BOMB, volumeMultiplier: 1.05f, pitchOffset: 0.02f);

            GridElement bombElement = grid.GetElementAt(bombPos);

            grid.TriggerCellFeatureMatchedOverAt(bombPos);
            bombCell.elementInfo = null;

            Grid3D.GridCell rocketCell = grid.GetCellPublic(rocketPos);
            ElementPowerUpType rocketType = rocketCell?.elementInfo?.powerUpType ?? ElementPowerUpType.Rocket;
            if (rocketCell != null)
            {
                grid.TriggerCellFeatureMatchedOverAt(rocketPos);
                rocketCell.elementInfo = null;
            }

            List<Vector2Int> areaCells = GetEligibleCellsInSquareArea(bombPos, 1, bombPos, rocketPos);
            if (areaCells.Count > 0)
            {
                ConvertCellsToPowerUp(areaCells, rocketType);
                yield return null;

                for (int i = 0; i < areaCells.Count; i++)
                    yield return grid.StartCoroutine(ActivateAt(areaCells[i], null));
            }

            if (bombElement != null)
                grid.StartCoroutine(bombElement.DestroyElement());
        }

        private IEnumerator ActivateBombAndBombCombo(Vector2Int primaryBombPos)
        {
            Grid3D.GridCell primaryBombCell = grid.GetCellPublic(primaryBombPos);
            if (primaryBombCell?.elementInfo == null || primaryBombCell.elementInfo.powerUpType != ElementPowerUpType.Bomb)
                yield break;

            Vector2Int secondaryBombPos = FindAdjacentPowerUpOfType(primaryBombPos, ElementPowerUpType.Bomb);
            if (secondaryBombPos == primaryBombPos)
                yield break;

            EventManager.TriggerEvent(GameEvent.SPECIAL_ELEMENT_ACTIVATED);
            PlayEffect(ConstantManager.SOUNDS.EFFECTS.BOMB, volumeMultiplier: 1.12f, pitchOffset: -0.04f);

            GridElement primaryBombElement = grid.GetElementAt(primaryBombPos);
            Vector3 impactWorldPos = grid.GetWorldPosition(primaryBombPos);
            PlayBombImpactEffects(impactWorldPos);

            grid.TriggerCellFeatureMatchedOverAt(primaryBombPos);
            primaryBombCell.elementInfo = null;

            Grid3D.GridCell secondaryBombCell = grid.GetCellPublic(secondaryBombPos);
            if (secondaryBombCell != null)
            {
                grid.TriggerCellFeatureMatchedOverAt(secondaryBombPos);
                secondaryBombCell.elementInfo = null;
            }

            if (primaryBombElement != null)
                grid.StartCoroutine(primaryBombElement.DestroyElement());

            yield return grid.StartCoroutine(ClearBombAreaProgressive(primaryBombPos, 3, false));
        }

        private IEnumerator ActivateDiscoBallAndPropellerCombo(Vector2Int discoBallPos)
        {
            Grid3D.GridCell discoBallCell = grid.GetCellPublic(discoBallPos);
            if (discoBallCell?.elementInfo == null || discoBallCell.elementInfo.powerUpType != ElementPowerUpType.DiscoBall)
                yield break;

            Vector2Int propellerPos = FindAdjacentPropeller(discoBallPos);
            if (propellerPos == discoBallPos)
                yield break;

            EventManager.TriggerEvent(GameEvent.SPECIAL_ELEMENT_ACTIVATED);
            PlayEffect(ConstantManager.SOUNDS.EFFECTS.DISCO_BALL_ACTIVATE, volumeMultiplier: 1.05f, pitchOffset: 0.06f);

            GridElement discoBallElement = grid.GetElementAt(discoBallPos);
            Tween discoBallSpinTween = null;

            StartDiscoBallSpin(discoBallElement, ref discoBallSpinTween);

            grid.TriggerCellFeatureMatchedOverAt(discoBallPos);
            discoBallCell.elementInfo = null;

            Grid3D.GridCell propellerCell = grid.GetCellPublic(propellerPos);
            if (propellerCell != null)
            {
                grid.TriggerCellFeatureMatchedOverAt(propellerPos);
                propellerCell.elementInfo = null;
            }

            List<Vector2Int> selectedCells = GetRandomEligibleDiscoBallTargets(discoBallPos, propellerPos, StandardDiscoBallReplaceCount);
            if (selectedCells.Count > 0)
            {
                yield return grid.StartCoroutine(AnimateDiscoBallPowerUpTrails(discoBallPos, selectedCells, ElementPowerUpType.Propeller));

                yield return null;

                List<Vector2Int> reservedTargets = ReservePropellerTargets(selectedCells);
                yield return grid.StartCoroutine(ActivateReservedPropellerBurst(selectedCells, reservedTargets));
            }

            StopAndDestroyDiscoBallElement(discoBallElement, discoBallSpinTween);
        }

        private ElementData ResolveDefaultDiscoBallTargetElement(ElementData requestedTargetElementData)
        {
            if (requestedTargetElementData != null)
                return requestedTargetElementData;

            return GetMostCommonElementOnGrid();
        }

        private ElementData ResolveRandomDiscoBallComboTargetElement(params Vector2Int[] excludedPositions)
        {
            List<ElementData> uniqueElementTypes = new List<ElementData>();

            for (int x = 0; x < grid.GridSize.x; x++)
            {
                for (int y = 0; y < grid.GridSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (IsExcludedPosition(pos, excludedPositions))
                        continue;

                    Grid3D.GridCell cell = grid.GetCellPublic(pos);
                    if (!IsEligibleDiscoBallTargetCell(cell))
                        continue;

                    ElementData elementData = cell.elementInfo.elementData;
                    if (elementData != null && !uniqueElementTypes.Contains(elementData))
                        uniqueElementTypes.Add(elementData);
                }
            }

            if (uniqueElementTypes.Count == 0)
                return null;

            return uniqueElementTypes[Random.Range(0, uniqueElementTypes.Count)];
        }

        private List<Vector2Int> GetRandomEligibleDiscoBallTargets(Vector2Int excludedPosition, int maxCount)
        {
            return GetRandomEligibleDiscoBallTargets(new[] { excludedPosition }, maxCount);
        }

        private List<Vector2Int> GetRandomEligibleDiscoBallTargets(Vector2Int excludedPosition1, Vector2Int excludedPosition2, int maxCount)
        {
            return GetRandomEligibleDiscoBallTargets(new[] { excludedPosition1, excludedPosition2 }, maxCount);
        }

        private ElementData GetMostCommonElementOnGrid()
        {
            Dictionary<ElementData, int> elementCounts = new Dictionary<ElementData, int>();

            for (int x = 0; x < grid.GridSize.x; x++)
            {
                for (int y = 0; y < grid.GridSize.y; y++)
                {
                    Grid3D.GridCell cell = grid.GetCellPublic(new Vector2Int(x, y));
                    if (!IsEligibleDiscoBallTargetCell(cell))
                        continue;

                    ElementData elementData = cell.elementInfo.elementData;
                    if (elementData == null)
                        continue;

                    if (!elementCounts.ContainsKey(elementData))
                        elementCounts[elementData] = 0;

                    elementCounts[elementData]++;
                }
            }

            ElementData mostCommon = null;
            int maxCount = 0;

            foreach (var kvp in elementCounts)
            {
                if (kvp.Value > maxCount)
                {
                    maxCount = kvp.Value;
                    mostCommon = kvp.Key;
                }
            }

            return mostCommon;
        }

        private List<Vector2Int> GetAllCellsWithElement(ElementData targetElement)
        {
            List<Vector2Int> cells = new List<Vector2Int>();

            for (int x = 0; x < grid.GridSize.x; x++)
            {
                for (int y = 0; y < grid.GridSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    Grid3D.GridCell cell = grid.GetCellPublic(pos);

                    if (cell == null || cell.cellType != Grid3D.CellType.Normal || cell.elementInfo == null)
                        continue;

                    if (cell.elementInfo.elementData == targetElement)
                        cells.Add(pos);
                }
            }

            return cells;
        }

        private List<Vector2Int> GetRandomEligibleDiscoBallTargets(Vector2Int[] excludedPositions, int maxCount)
        {
            List<Vector2Int> candidates = new List<Vector2Int>();
            for (int x = 0; x < grid.GridSize.x; x++)
            {
                for (int y = 0; y < grid.GridSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (IsExcludedPosition(pos, excludedPositions))
                        continue;

                    Grid3D.GridCell cell = grid.GetCellPublic(pos);
                    if (!IsEligibleDiscoBallTargetCell(cell))
                        continue;

                    candidates.Add(pos);
                }
            }

            int replaceCount = Mathf.Min(maxCount, candidates.Count);
            List<Vector2Int> selectedCells = new List<Vector2Int>(replaceCount);
            for (int i = 0; i < replaceCount; i++)
            {
                int randIdx = Random.Range(i, candidates.Count);
                Vector2Int tmp = candidates[randIdx];
                candidates[randIdx] = candidates[i];
                candidates[i] = tmp;
                selectedCells.Add(candidates[i]);
            }

            return selectedCells;
        }

        private List<Vector2Int> GetEligibleCellsInSquareArea(Vector2Int center, int radius, params Vector2Int[] excludedPositions)
        {
            List<Vector2Int> cells = new List<Vector2Int>();

            for (int x = center.x - radius; x <= center.x + radius; x++)
            {
                for (int y = center.y - radius; y <= center.y + radius; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (IsExcludedPosition(pos, excludedPositions))
                        continue;

                    Grid3D.GridCell cell = grid.GetCellPublic(pos);
                    if (!IsEligibleDiscoBallTargetCell(cell))
                        continue;

                    cells.Add(pos);
                }
            }

            return cells;
        }

        private void ConvertCellsToPowerUp(List<Vector2Int> positions, ElementPowerUpType targetPowerUpType)
        {
            if (positions == null)
                return;

            for (int i = 0; i < positions.Count; i++)
            {
                Vector2Int pos = positions[i];
                Grid3D.GridCell cell = grid.GetCellPublic(pos);
                if (cell?.elementInfo == null)
                    continue;

                ElementData sourceData = cell.elementInfo.elementData;
                cell.elementInfo.elementData = ResolveVisualData(sourceData, targetPowerUpType);
                cell.elementInfo.powerUpType = targetPowerUpType;
                cell.elementInfo.isSparkling = false;
                cell.elementInfo.isHidden = false;

                GridElement element = grid.GetElementAt(pos);
                if (element != null)
                {
                    element.elementInfo = cell.elementInfo;
                    element.InitElement(grid, cell.elementInfo);
                    GridHelper.SetEmission(element, 0f);
                    ApplySortingBoost(element, targetPowerUpType == ElementPowerUpType.Bomb);
                }
            }
        }

        private bool IsEligibleDiscoBallTargetCell(Grid3D.GridCell cell)
        {
            if (cell == null || cell.cellType != Grid3D.CellType.Normal || cell.elementInfo == null)
                return false;

            if (cell.elementInfo.isHidden)
                return false;

            if (IsSpecialPowerUp(cell.elementInfo.powerUpType))
                return false;

            ElementData elementData = cell.elementInfo.elementData;
            if (elementData == null)
                return false;

            if (elementData.HasBehavior(ElementData.ElementBehaviorFlags.NonShuffleable) ||
                elementData.HasBehavior(ElementData.ElementBehaviorFlags.NonSwappable))
                return false;

            return true;
        }

        private static bool IsExcludedPosition(Vector2Int pos, Vector2Int[] excludedPositions)
        {
            if (excludedPositions == null)
                return false;

            for (int i = 0; i < excludedPositions.Length; i++)
            {
                if (pos == excludedPositions[i])
                    return true;
            }

            return false;
        }

        private Vector2Int FindAdjacentDiscoBall(Vector2Int origin)
        {
            Vector2Int[] offsets = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            for (int i = 0; i < offsets.Length; i++)
            {
                Vector2Int candidatePos = origin + offsets[i];
                Grid3D.GridCell candidateCell = grid.GetCellPublic(candidatePos);
                if (candidateCell?.elementInfo?.powerUpType == ElementPowerUpType.DiscoBall)
                    return candidatePos;
            }

            return origin;
        }

        private Vector2Int FindAdjacentPropeller(Vector2Int origin)
        {
            Vector2Int[] offsets = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            for (int i = 0; i < offsets.Length; i++)
            {
                Vector2Int candidatePos = origin + offsets[i];
                Grid3D.GridCell candidateCell = grid.GetCellPublic(candidatePos);
                if (IsPropeller(candidateCell?.elementInfo?.powerUpType ?? ElementPowerUpType.None))
                    return candidatePos;
            }

            return origin;
        }

        private Vector2Int FindAdjacentRocket(Vector2Int origin)
        {
            Vector2Int[] offsets = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            for (int i = 0; i < offsets.Length; i++)
            {
                Vector2Int candidatePos = origin + offsets[i];
                Grid3D.GridCell candidateCell = grid.GetCellPublic(candidatePos);
                if (IsRocket(candidateCell?.elementInfo?.powerUpType ?? ElementPowerUpType.None))
                    return candidatePos;
            }

            return origin;
        }

        private Vector2Int FindAdjacentPowerUpOfType(Vector2Int origin, ElementPowerUpType powerUpType)
        {
            Vector2Int[] offsets = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            for (int i = 0; i < offsets.Length; i++)
            {
                Vector2Int candidatePos = origin + offsets[i];
                Grid3D.GridCell candidateCell = grid.GetCellPublic(candidatePos);
                if (candidateCell?.elementInfo?.powerUpType == powerUpType)
                    return candidatePos;
            }

            return origin;
        }

        private void StartDiscoBallSpin(GridElement discoBallElement, ref Tween spinTween)
        {
            if (discoBallElement == null)
                return;

            discoBallElement.transform.DOKill();
            float spinDuration = ConstantManager.Instance.discoBallSpinLoopDuration;
            float spinDegrees = ConstantManager.Instance.discoBallSpinDegreesPerLoop;
            spinTween = discoBallElement.transform
                .DORotate(new Vector3(0f, 0f, spinDegrees), spinDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetRelative()
                .SetLoops(-1, LoopType.Restart);
        }

        private void StopAndDestroyDiscoBallElement(GridElement discoBallElement, Tween spinTween)
        {
            if (spinTween != null)
                spinTween.Kill();

            if (discoBallElement != null)
                grid.StartCoroutine(discoBallElement.DestroyElement());

        }

        private void DestroyDiscoBallConvertedCells(List<Vector2Int> convertedCells)
        {
            if (convertedCells == null)
                return;

            for (int i = 0; i < convertedCells.Count; i++)
            {
                Vector2Int pos = convertedCells[i];
                Grid3D.GridCell cell = grid.GetCellPublic(pos);
                if (cell == null || cell.cellType != Grid3D.CellType.Normal || cell.elementInfo == null)
                    continue;

                if (cell.cellFeature is GlassFeature)
                {
                    grid.DamageGlassFeatureAt(pos);
                    continue;
                }

                GridElement matchedElement = grid.GetElementAt(pos);
                grid.TriggerCellFeatureMatchedOverAt(pos);
                grid.TriggerCellFeatureMatchedAdjacentToAt(pos, cell, matchedElement);

                if (grid.TryRevealHiddenBoxAt(pos))
                    continue;

                if (GameManager.Instance != null &&
                    cell.elementInfo.elementData != null && GameManager.Instance.garbageBagElementData == cell.elementInfo.elementData)
                    continue;

                if (GameManager.Instance != null &&
                    cell.elementInfo.elementData != null && GameManager.Instance.powerGeneratorElementData == cell.elementInfo.elementData)
                    continue;

                if (cell.elementInfo.elementData != null &&
                    cell.elementInfo.elementData.HasBehavior(ElementData.ElementBehaviorFlags.ImmuneToClear))
                    continue;

                if (IsSpecialPowerUp(cell.elementInfo.powerUpType) || cell.elementInfo.powerUpType == ElementPowerUpType.Cauldron)
                    continue;

                grid.NotifyElementCleared(pos);
                cell.elementInfo = null;
                if (matchedElement != null)
                    grid.StartCoroutine(matchedElement.DestroyElement());
            }
        }

        private IEnumerator ActivatePropeller(Vector2Int propellerPos)
        {
            return ActivatePropeller(propellerPos, PickPropellerTargetPosition(propellerPos));
        }

        private IEnumerator ActivatePropeller(Vector2Int propellerPos, Vector2Int targetPos)
        {
            Grid3D.GridCell propellerCell = grid.GetCellPublic(propellerPos);
            if (propellerCell?.elementInfo == null || propellerCell.elementInfo.powerUpType != ElementPowerUpType.Propeller)
                yield break;

            EventManager.TriggerEvent(GameEvent.SPECIAL_ELEMENT_ACTIVATED);
            PlayEffect(ConstantManager.SOUNDS.EFFECTS.ROCKET, volumeMultiplier: 0.9f, pitchOffset: 0.08f);

            GridElement propellerElement = grid.GetElementAt(propellerPos);
            if (propellerElement != null)
                propellerElement.transform.DOKill();

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
        }

        private IEnumerator ActivatePropellerBurstFromOrigin(Vector2Int originPos, GridElement sourcePropellerElement, List<Vector2Int> targetPositions)
        {
            if (targetPositions == null || targetPositions.Count == 0)
                yield break;

            float staggerDelay = Mathf.Max(0.03f, ConstantManager.Instance.discoBallTrailSpawnDelay * 0.45f);
            List<Coroutine> flightCoroutines = new List<Coroutine>(targetPositions.Count);

            for (int i = 0; i < targetPositions.Count; i++)
            {
                flightCoroutines.Add(grid.StartCoroutine(FlyTemporaryPropellerToTarget(originPos, targetPositions[i], sourcePropellerElement, i)));

                if (i < targetPositions.Count - 1)
                    yield return new WaitForSeconds(staggerDelay);
            }

            for (int i = 0; i < flightCoroutines.Count; i++)
                yield return flightCoroutines[i];
        }

        private IEnumerator FlyTemporaryPropellerToTarget(Vector2Int originPos, Vector2Int targetPos, GridElement sourcePropellerElement, int burstIndex)
        {
            Vector3 startWorldPos = grid.GetWorldPosition(originPos);
            Vector3 targetWorldPos = grid.GetWorldPosition(targetPos);
            GameObject propellerCopy = CreateTemporaryElementCopy(sourcePropellerElement, startWorldPos, "PropellerComboCopy");

            PlayEffect(ConstantManager.SOUNDS.EFFECTS.ROCKET, volumeMultiplier: 0.8f, pitchOffset: 0.06f + Mathf.Clamp(burstIndex * 0.02f, 0f, 0.12f));

            if (propellerCopy != null)
            {
                float travelDuration = 0.3f;
                float arcHeight = Mathf.Clamp(Vector3.Distance(startWorldPos, targetWorldPos) * 0.22f, 0.3f, 0.9f);
                Vector3 midPoint = Vector3.Lerp(startWorldPos, targetWorldPos, 0.5f) + (Vector3.up * arcHeight);

                Sequence travelSequence = DOTween.Sequence();
                travelSequence.Join(
                    propellerCopy.transform
                        .DOPath(new[] { startWorldPos, midPoint, targetWorldPos }, travelDuration, PathType.CatmullRom)
                        .SetEase(Ease.InOutSine)
                        .SetOptions(false));
                travelSequence.Join(
                    propellerCopy.transform
                        .DORotate(new Vector3(0f, 0f, 1080f), travelDuration, RotateMode.FastBeyond360)
                        .SetEase(Ease.Linear)
                        .SetRelative());

                yield return travelSequence.WaitForCompletion();
                Object.Destroy(propellerCopy);
            }
            else
            {
                yield return new WaitForSeconds(0.3f);
            }

            yield return grid.StartCoroutine(grid.ClearAreaAt(targetPos, 0));
        }

        private IEnumerator FlyBombToTargetAndActivate(Vector2Int bombPos, Vector2Int targetPos, GridElement bombElement)
        {
            Vector3 startWorldPos = bombElement != null ? bombElement.transform.position : grid.GetWorldPosition(bombPos);
            Vector3 targetWorldPos = grid.GetWorldPosition(targetPos);

            if (bombElement != null)
            {
                bombElement.transform.DOKill();

                Transform tempParent = grid.GridParent != null ? grid.GridParent : grid.transform;
                bombElement.transform.SetParent(tempParent, true);

                float travelDuration = 0.32f;
                float arcHeight = Mathf.Clamp(Vector3.Distance(startWorldPos, targetWorldPos) * 0.2f, 0.3f, 0.85f);
                Vector3 midPoint = Vector3.Lerp(startWorldPos, targetWorldPos, 0.5f) + (Vector3.up * arcHeight);

                Sequence travelSequence = DOTween.Sequence();
                travelSequence.Join(
                    bombElement.transform
                        .DOPath(new[] { startWorldPos, midPoint, targetWorldPos }, travelDuration, PathType.CatmullRom)
                        .SetEase(Ease.InOutSine)
                        .SetOptions(false));
                travelSequence.Join(
                    bombElement.transform
                        .DORotate(new Vector3(0f, 0f, 720f), travelDuration, RotateMode.FastBeyond360)
                        .SetEase(Ease.Linear)
                        .SetRelative());

                yield return travelSequence.WaitForCompletion();
                grid.StartCoroutine(bombElement.DestroyElement());
            }
            else
            {
                yield return new WaitForSeconds(0.32f);
            }

            PlayEffect(ConstantManager.SOUNDS.EFFECTS.BOMB);
            PlayBombImpactEffects(targetWorldPos);
            yield return grid.StartCoroutine(ClearBombAreaProgressive(targetPos, 2, false));
        }

        private IEnumerator ActivateReservedPropellerBurst(List<Vector2Int> propellerPositions, List<Vector2Int> reservedTargets)
        {
            if (propellerPositions == null || reservedTargets == null)
                yield break;

            int activationCount = Mathf.Min(propellerPositions.Count, reservedTargets.Count);
            if (activationCount <= 0)
                yield break;

            float staggerDelay = Mathf.Max(0.03f, ConstantManager.Instance.discoBallTrailSpawnDelay * 0.5f);
            List<Coroutine> activationCoroutines = new List<Coroutine>(activationCount);

            for (int i = 0; i < activationCount; i++)
            {
                activationCoroutines.Add(grid.StartCoroutine(ActivatePropeller(propellerPositions[i], reservedTargets[i])));

                if (i < activationCount - 1)
                    yield return new WaitForSeconds(staggerDelay);
            }

            for (int i = 0; i < activationCoroutines.Count; i++)
                yield return activationCoroutines[i];
        }

        private Vector2Int PickPropellerTargetPosition(Vector2Int origin)
        {
            return PickPropellerTargetPosition(origin, null);
        }

        private Vector2Int PickPropellerTargetPosition(Vector2Int origin, HashSet<Vector2Int> reservedTargets)
        {
            List<Vector2Int> breakableWallCells = new List<Vector2Int>();
            List<Vector2Int> hiddenElements = new List<Vector2Int>();
            List<Vector2Int> waferCells = new List<Vector2Int>();
            List<Vector2Int> normalCandidates = new List<Vector2Int>();
            List<Vector2Int> belowGarbageCandidates = new List<Vector2Int>();
            GameManager gm = GameManager.Instance;

            for (int x = 0; x < grid.GridSize.x; x++)
            {
                for (int y = 0; y < grid.GridSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (pos == origin)
                        continue;

                    if (reservedTargets != null && reservedTargets.Contains(pos))
                        continue;

                    Grid3D.GridCell cell = grid.GetCellPublic(pos);
                    if (cell == null)
                        continue;

                    if (cell.cellType == Grid3D.CellType.BreakableWall)
                    {
                        breakableWallCells.Add(pos);
                        continue;
                    }

                    if (cell.cellType != Grid3D.CellType.Normal)
                        continue;

                    // Propeller should never target garbage bag cells.
                    if (cell.elementInfo?.elementData != null)
                    {
                        if (gm != null && gm.garbageBagElementData == cell.elementInfo.elementData)
                            continue;
                    }

                    if (cell.elementInfo != null)
                    {
                        // Check for hidden elements (second priority)
                        if (cell.elementInfo.isHidden)
                        {
                            hiddenElements.Add(pos);
                        }
                        // Check for wafer features (third priority)
                        else if (cell.cellFeature is WaferFeature)
                        {
                            waferCells.Add(pos);
                        }
                        else
                        {
                            Grid3D.GridCell aboveCell = grid.GetCellPublic(pos + Vector2Int.up);
                            bool isBelowGarbage = gm != null &&
                                aboveCell?.elementInfo?.elementData != null &&
                                aboveCell.elementInfo.elementData == gm.garbageBagElementData;

                            if (isBelowGarbage)
                                belowGarbageCandidates.Add(pos);
                            else
                                normalCandidates.Add(pos);
                        }
                    }
                }
            }

            // Priority: Breakable Wall Cells -> Hidden Elements -> Wafer Features -> Normal Cells -> Elements below garbage bags
            if (breakableWallCells.Count > 0)
                return breakableWallCells[Random.Range(0, breakableWallCells.Count)];
            if (hiddenElements.Count > 0)
                return hiddenElements[Random.Range(0, hiddenElements.Count)];
            if (waferCells.Count > 0)
                return waferCells[Random.Range(0, waferCells.Count)];
            if (normalCandidates.Count > 0)
                return normalCandidates[Random.Range(0, normalCandidates.Count)];
            if (belowGarbageCandidates.Count > 0)
                return belowGarbageCandidates[Random.Range(0, belowGarbageCandidates.Count)];
            return origin;
        }

        private List<Vector2Int> ReservePropellerTargets(List<Vector2Int> propellerPositions)
        {
            List<Vector2Int> reservedTargets = new List<Vector2Int>();
            if (propellerPositions == null)
                return reservedTargets;

            HashSet<Vector2Int> usedTargets = new HashSet<Vector2Int>();
            for (int i = 0; i < propellerPositions.Count; i++)
            {
                Vector2Int propellerPos = propellerPositions[i];
                Vector2Int targetPos = PickPropellerTargetPosition(propellerPos, usedTargets);

                if (targetPos == propellerPos && usedTargets.Contains(targetPos))
                    targetPos = PickPropellerTargetPosition(propellerPos);

                usedTargets.Add(targetPos);
                reservedTargets.Add(targetPos);
            }

            return reservedTargets;
        }

        private List<Vector2Int> ReservePropellerTargets(Vector2Int originPos, int count, params Vector2Int[] blockedPositions)
        {
            List<Vector2Int> reservedTargets = new List<Vector2Int>();
            if (count <= 0)
                return reservedTargets;

            HashSet<Vector2Int> usedTargets = new HashSet<Vector2Int>();
            if (blockedPositions != null)
            {
                for (int i = 0; i < blockedPositions.Length; i++)
                    usedTargets.Add(blockedPositions[i]);
            }

            for (int i = 0; i < count; i++)
            {
                Vector2Int targetPos = PickPropellerTargetPosition(originPos, usedTargets);
                if (usedTargets.Contains(targetPos))
                    break;

                usedTargets.Add(targetPos);
                reservedTargets.Add(targetPos);
            }

            return reservedTargets;
        }

        private GameObject CreateTemporaryElementCopy(GridElement sourceElement, Vector3 origin, string objectName)
        {
            GameObject copy = new GameObject(objectName);
            copy.transform.position = origin;

            if (sourceElement != null && sourceElement.elementRenderer is SpriteRenderer srcSR)
            {
                SpriteRenderer sr = copy.AddComponent<SpriteRenderer>();
                sr.sprite = srcSR.sprite;
                sr.material = srcSR.material;
                sr.sortingLayerID = srcSR.sortingLayerID;
                sr.sortingOrder = srcSR.sortingOrder + SortingOrderBoost;
                sr.color = srcSR.color;
            }

            return copy;
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
            ConstantManager cm = ConstantManager.Instance;
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

        private IEnumerator AnimateDiscoBallPowerUpTrails(Vector2Int sourcePos, List<Vector2Int> targets, ElementPowerUpType targetPowerUpType)
        {
            ConstantManager cm = ConstantManager.Instance;
            Vector3 sourceWorldPos = grid.GetWorldPosition(sourcePos);

            int trailIndex = 0;
            for (int i = 0; i < targets.Count; i++)
            {
                grid.StartCoroutine(AnimateSingleDiscoPowerUpTrail(sourceWorldPos, targets[i], targetPowerUpType, trailIndex));
                trailIndex++;
                yield return new WaitForSeconds(cm.discoBallTrailSpawnDelay);
            }

            float totalWait = cm.discoBallTrailDuration + cm.discoBallTrailSpawnDelay * Mathf.Max(0, targets.Count - 1);
            yield return new WaitForSeconds(totalWait);
        }

        private IEnumerator AnimateSingleDiscoTrail(Vector3 sourcePos, Vector2Int targetPos, ElementData targetElementData, int trailIndex)
        {
            ConstantManager cm = ConstantManager.Instance;
            Vector3 targetWorldPos = grid.GetWorldPosition(targetPos);

            GameObject trailObj = Object.Instantiate(cm.sparklingTrailPrefab, sourcePos, Quaternion.identity);

            PlayEffect(ConstantManager.SOUNDS.EFFECTS.DISCO_BALL_TRAIL, volumeMultiplier: 0.75f, pitchOffset: Mathf.Clamp(trailIndex * 0.01f, 0f, 0.12f));

            Tween trailTween = trailObj.transform.DOMove(targetWorldPos, cm.discoBallTrailDuration).SetEase(Ease.OutQuad);
            if (trailTween != null && trailTween.active)
                yield return trailTween.WaitForCompletion();

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
                    GridHelper.SetEmission(element, cm.discoBallEmissionOnTrailArrival);
                    //grid.StartCoroutine(ResetElementEmission(element, cm.discoBallEmissionResetDelay));
                }
            }


            if (trailObj != null) Object.Destroy(trailObj);
        }

        private IEnumerator AnimateSingleDiscoPowerUpTrail(Vector3 sourcePos, Vector2Int targetPos, ElementPowerUpType targetPowerUpType, int trailIndex)
        {
            ConstantManager cm = ConstantManager.Instance;
            Vector3 targetWorldPos = grid.GetWorldPosition(targetPos);

            GameObject trailObj = Object.Instantiate(cm.sparklingTrailPrefab, sourcePos, Quaternion.identity);

            PlayEffect(ConstantManager.SOUNDS.EFFECTS.DISCO_BALL_TRAIL, volumeMultiplier: 0.75f, pitchOffset: Mathf.Clamp(trailIndex * 0.01f, 0f, 0.12f));

            Tween trailTween = trailObj.transform.DOMove(targetWorldPos, cm.discoBallTrailDuration).SetEase(Ease.OutQuad);
            if (trailTween != null && trailTween.active)
                yield return trailTween.WaitForCompletion();

            Grid3D.GridCell cell = grid.GetCellPublic(targetPos);
            if (cell?.elementInfo != null)
            {
                ElementData sourceData = cell.elementInfo.elementData;
                cell.elementInfo.elementData = ResolveVisualData(sourceData, targetPowerUpType);
                cell.elementInfo.powerUpType = targetPowerUpType;
                cell.elementInfo.isSparkling = false;
                cell.elementInfo.isHidden = false;

                GridElement element = grid.GetElementAt(targetPos);
                if (element != null)
                {
                    element.elementInfo = cell.elementInfo;
                    element.InitElement(grid, cell.elementInfo);
                    GridHelper.SetEmission(element, cm.discoBallEmissionOnTrailArrival);
                    ApplySortingBoost(element, targetPowerUpType == ElementPowerUpType.Bomb);
                    grid.StartCoroutine(ResetElementEmission(element, cm.discoBallEmissionResetDelay));
                }
            }

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
            yield return grid.StartCoroutine(ClearBombAreaProgressive(bombPos, 2, false));
        }

        private IEnumerator ActivateRocket(Vector2Int rocketPos)
        {
            Grid3D.GridCell rocketCell = grid.GetCellPublic(rocketPos);
            ElementPowerUpType rocketType = rocketCell?.elementInfo?.powerUpType ?? ElementPowerUpType.None;
            if (!IsRocket(rocketType))
                yield break;

            EventManager.TriggerEvent(GameEvent.SPECIAL_ELEMENT_ACTIVATED);

            GridElement rocketElement = grid.GetElementAt(rocketPos);

            grid.TriggerCellFeatureMatchedOverAt(rocketPos);
            rocketCell.elementInfo = null;

            Vector2Int[] rocketDirections = GetRocketDirections(rocketType);

            yield return grid.StartCoroutine(ActivateRocketBurst(rocketPos, rocketElement, rocketDirections, rocketType, clearSourceCell: false, clearOriginCell: false, preLaunchDelay: 0.1f));
        }

        private static Vector2Int[] GetRocketDirections(ElementPowerUpType rocketType)
        {
            if (rocketType == ElementPowerUpType.HorizontalRocket)
            {
                return new[]
                {
                    Vector2Int.right,
                    Vector2Int.left
                };
            }

            if (rocketType == ElementPowerUpType.VerticalRocket)
            {
                return new[]
                {
                    Vector2Int.up,
                    Vector2Int.down
                };
            }

            return new[]
            {
                Vector2Int.right,
                Vector2Int.left,
                Vector2Int.up,
                Vector2Int.down
            };
        }

        private IEnumerator ClearBombAreaProgressive(Vector2Int center, int radius, bool allowConditionedBreakableWalls)
        {
            HashSet<Vector2Int> processedWalls = new HashSet<Vector2Int>();
            float ringDelay = Mathf.Max(0.02f, ConstantManager.Instance.matchClearDelay * 0.35f);

            for (int ring = 0; ring <= radius; ring++)
            {
                for (int x = center.x - radius; x <= center.x + radius; x++)
                {
                    for (int y = center.y - radius; y <= center.y + radius; y++)
                    {
                        Vector2Int pos = new Vector2Int(x, y);
                        int chebyshevDistance = Mathf.Max(Mathf.Abs(pos.x - center.x), Mathf.Abs(pos.y - center.y));
                        if (chebyshevDistance != ring)
                            continue;

                        ClearBombCellImmediate(pos, processedWalls, allowConditionedBreakableWalls);
                    }
                }

                if (ring < radius)
                    yield return new WaitForSeconds(ringDelay);
            }
        }

        private void ClearBombCellImmediate(Vector2Int pos, HashSet<Vector2Int> processedWalls, bool allowConditionedBreakableWalls)
        {
            Grid3D.GridCell cell = grid.GetCellPublic(pos);
            if (cell == null)
                return;

            if (cell.cellType == Grid3D.CellType.BreakableWall)
            {
                if (!allowConditionedBreakableWalls && cell.breakableWallElementCondition != null)
                    return;

                if (processedWalls.Add(pos))
                    grid.StartCoroutine(grid.BreakWallAt(pos));

                return;
            }

            if (cell.cellFeature is GlassFeature)
            {
                grid.DamageGlassFeatureAt(pos);
                return;
            }

            if (cell.cellType != Grid3D.CellType.Normal || cell.elementInfo == null)
                return;

            GridElement matchedElement = grid.GetElementAt(pos);
            grid.TriggerCellFeatureMatchedOverAt(pos);
            grid.TriggerCellFeatureMatchedAdjacentToAt(pos, cell, matchedElement);

            if (grid.TryRevealHiddenBoxAt(pos))
                return;

            if (GameManager.Instance != null &&
                cell.elementInfo.elementData != null && GameManager.Instance.garbageBagElementData == cell.elementInfo.elementData)
                return;

            if (GameManager.Instance != null &&
                cell.elementInfo.elementData != null && GameManager.Instance.powerGeneratorElementData == cell.elementInfo.elementData)
                return;

            if (cell.elementInfo.elementData != null &&
                cell.elementInfo.elementData.HasBehavior(ElementData.ElementBehaviorFlags.ImmuneToClear))
                return;

            if (IsSpecialPowerUp(cell.elementInfo.powerUpType))
            {
                grid.StartCoroutine(ActivateAt(pos, null));
                return;
            }

            if (cell.elementInfo.powerUpType == ElementPowerUpType.Cauldron)
                return;

            grid.NotifyElementCleared(pos);
            cell.elementInfo = null;
            if (matchedElement != null)
                grid.StartCoroutine(matchedElement.DestroyElement());
        }

        private IEnumerator ActivateRocketBurst(Vector2Int rocketPos, GridElement rocketElement, Vector2Int[] directions, ElementPowerUpType rocketType, bool clearSourceCell, bool clearOriginCell, float preLaunchDelay, float volumeMultiplier = 1f, float pitchOffset = 0f)
        {
            if (preLaunchDelay > 0f)
                yield return new WaitForSeconds(preLaunchDelay);

            PlayEffect(ConstantManager.SOUNDS.EFFECTS.ROCKET, volumeMultiplier, pitchOffset);

            Vector3 originWorld = grid.GetWorldPosition(rocketPos);
            ConstantManager cm = ConstantManager.Instance;

            if (rocketElement != null)
            {
                rocketElement.transform.DOKill();
                Collider[] cols = rocketElement.GetComponentsInChildren<Collider>(true);
                for (int i = 0; i < cols.Length; i++) cols[i].enabled = false;
            }

            HashSet<Vector2Int> processedWalls = new HashSet<Vector2Int>();

            if (clearSourceCell)
            {
                grid.TriggerCellFeatureMatchedOverAt(rocketPos);
                Grid3D.GridCell sourceCell = grid.GetCellPublic(rocketPos);
                if (sourceCell != null)
                    sourceCell.elementInfo = null;
            }

            if (clearOriginCell)
                ClearLineCellImmediate(rocketPos, processedWalls);

            List<GameObject> rocketCopies = new List<GameObject>(directions.Length);
            List<Coroutine> travelCoroutines = new List<Coroutine>(directions.Length);

            for (int i = 0; i < directions.Length; i++)
            {
                Vector2Int direction = directions[i];
                List<Vector2Int> lineCells = CollectLineCells(rocketPos, direction);
                Vector3 lineEnd = GetRocketLineEnd(originWorld, lineCells, direction);
                GameObject rocketCopy = CreateRocketCopyForDirection(rocketElement, originWorld, cm, direction, rocketType);
                rocketCopies.Add(rocketCopy);
                travelCoroutines.Add(grid.StartCoroutine(TravelRocketCopy(rocketCopy, originWorld, lineEnd, lineCells, cm, processedWalls)));
            }

            if (rocketElement != null)
                Object.Destroy(rocketElement.gameObject);

            BreakAdjacentWallsImmediate(rocketPos, processedWalls);

            for (int i = 0; i < travelCoroutines.Count; i++)
            {
                yield return travelCoroutines[i];
                Object.Destroy(rocketCopies[i]);
            }
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

        private Vector3 GetRocketLineEnd(Vector3 originWorld, List<Vector2Int> lineCells, Vector2Int direction)
        {
            return lineCells.Count > 0
                ? grid.GetWorldPosition(lineCells[lineCells.Count - 1]) + (Vector3)(Vector2)direction * 0.5f
                : originWorld + (Vector3)(Vector2)direction * 0.5f;
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

                if (grid.GetCellControllerAt(current) == null)
                {
                    current += direction;
                    continue;
                }

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

        private GameObject CreateRocketCopyForDirection(GridElement sourceElement, Vector3 origin, ConstantManager cm, Vector2Int direction, ElementPowerUpType rocketType)
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

                // Only rotate for omnidirectional rockets; horizontal and vertical rockets don't need rotation
                if (rocketType == ElementPowerUpType.Rocket)
                {
                    float rotationZ = -Vector2.SignedAngle(Vector2.up, new Vector2(direction.x, direction.y));
                    copy.transform.rotation = Quaternion.Euler(0f, 0f, rotationZ);
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

        private IEnumerator FlyRocketToTargetAndActivate(Vector2Int rocketPos, Vector2Int targetPos, GridElement rocketElement, ElementPowerUpType rocketType)
        {
            Vector3 startWorldPos = rocketElement != null ? rocketElement.transform.position : grid.GetWorldPosition(rocketPos);
            Vector3 targetWorldPos = grid.GetWorldPosition(targetPos);

            if (rocketElement != null)
            {
                rocketElement.transform.DOKill();

                Transform tempParent = grid.GridParent != null ? grid.GridParent : grid.transform;
                rocketElement.transform.SetParent(tempParent, true);

                float travelDuration = 0.3f;
                float arcHeight = Mathf.Clamp(Vector3.Distance(startWorldPos, targetWorldPos) * 0.2f, 0.3f, 0.85f);
                Vector3 midPoint = Vector3.Lerp(startWorldPos, targetWorldPos, 0.5f) + (Vector3.up * arcHeight);

                Sequence travelSequence = DOTween.Sequence();
                travelSequence.Join(
                    rocketElement.transform
                        .DOPath(new[] { startWorldPos, midPoint, targetWorldPos }, travelDuration, PathType.CatmullRom)
                        .SetEase(Ease.InOutSine)
                        .SetOptions(false));
                travelSequence.Join(
                    rocketElement.transform
                        .DORotate(new Vector3(0f, 0f, 720f), travelDuration, RotateMode.FastBeyond360)
                        .SetEase(Ease.Linear)
                        .SetRelative());

                yield return travelSequence.WaitForCompletion();
            }
            else
            {
                yield return new WaitForSeconds(0.3f);
            }

            Vector2Int[] rocketDirections = GetRocketDirections(rocketType);

            yield return grid.StartCoroutine(ActivateRocketBurst(targetPos, rocketElement, rocketDirections, rocketType, clearSourceCell: false, clearOriginCell: true, preLaunchDelay: 0f, volumeMultiplier: 1f, pitchOffset: 0.02f));
        }

        private IEnumerator TravelRocketCopy(GameObject rocketCopy, Vector3 start, Vector3 end, List<Vector2Int> cellsInOrder, ConstantManager cm, HashSet<Vector2Int> processedWalls)
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
                    ClearLineCellImmediate(cellsInOrder[nextCellIndex], processedWalls);
                    nextCellIndex++;
                }

                yield return null;
            }

            while (nextCellIndex < cellsInOrder.Count)
            {
                ClearLineCellImmediate(cellsInOrder[nextCellIndex], processedWalls);
                nextCellIndex++;
            }
        }

        private void ClearLineCellImmediate(Vector2Int pos, HashSet<Vector2Int> processedWalls)
        {
            Grid3D.GridCell cell = grid.GetCellPublic(pos);
            if (cell == null) return;

            if (cell.cellType == Grid3D.CellType.BreakableWall)
            {
                TryBreakRocketWallImmediate(pos, processedWalls);
                return;
            }

            if (cell.cellType != Grid3D.CellType.Normal)
            {
                BreakAdjacentWallsImmediate(pos, processedWalls);
                return;
            }

            if (cell.cellFeature is GlassFeature)
            {
                grid.DamageGlassFeatureAt(pos);
                BreakAdjacentWallsImmediate(pos, processedWalls);
                return;
            }

            if (cell.elementInfo == null)
            {
                BreakAdjacentWallsImmediate(pos, processedWalls);
                return;
            }

            GridElement matchedElement = grid.GetElementAt(pos);
            grid.TriggerCellFeatureMatchedOverAt(pos);
            grid.TriggerCellFeatureMatchedAdjacentToAt(pos, cell, matchedElement);
            BreakAdjacentWallsImmediate(pos, processedWalls);

            if (grid.TryRevealHiddenBoxAt(pos))
                return;

            if (GameManager.Instance != null &&
                cell.elementInfo.elementData != null && GameManager.Instance.garbageBagElementData == cell.elementInfo.elementData)
                return;

            if (GameManager.Instance != null &&
                cell.elementInfo.elementData != null && GameManager.Instance.powerGeneratorElementData == cell.elementInfo.elementData)
                return;

            if (cell.elementInfo.elementData != null &&
                cell.elementInfo.elementData.HasBehavior(ElementData.ElementBehaviorFlags.ImmuneToClear))
                return;

            if (IsSpecialPowerUp(cell.elementInfo.powerUpType))
            {
                grid.StartCoroutine(ActivateAt(pos, null));
                return;
            }

            if (cell.elementInfo.powerUpType == ElementPowerUpType.Cauldron) return;

            grid.NotifyElementCleared(pos);
            cell.elementInfo = null;
            if (matchedElement != null) grid.StartCoroutine(matchedElement.DestroyElement());
        }

        private void BreakAdjacentWallsImmediate(Vector2Int pos, HashSet<Vector2Int> processedWalls)
        {
            Vector2Int[] offsets = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            for (int i = 0; i < offsets.Length; i++)
                TryBreakRocketWallImmediate(pos + offsets[i], processedWalls);
        }

        private void TryBreakRocketWallImmediate(Vector2Int wallPos, HashSet<Vector2Int> processedWalls)
        {
            Grid3D.GridCell wallCell = grid.GetCellPublic(wallPos);
            if (wallCell == null || wallCell.cellType != Grid3D.CellType.BreakableWall)
                return;

            if (wallCell.breakableWallElementCondition != null)
                return;

            if (processedWalls != null && !processedWalls.Add(wallPos))
                return;

            grid.StartCoroutine(grid.BreakWallAt(wallPos));
        }

        private void PlayBombImpactEffects(Vector3 impactPos)
        {
            ConstantManager cm = GameManager.Instance != null ? ConstantManager.Instance : null;
            if (cm == null) return;
            if (cm.bombImpactParticlePrefab != null)
            {
                ParticleSystem p = Object.Instantiate(cm.bombImpactParticlePrefab, impactPos, Quaternion.identity);
                p.Play();
                Object.Destroy(p.gameObject, p.main.duration + p.main.startLifetime.constantMax + 0.2f);
            }
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
            if ((type == ElementPowerUpType.Rocket || type == ElementPowerUpType.HorizontalRocket) && gm.horizontalRocketElementData != null) return gm.horizontalRocketElementData;
            if (type == ElementPowerUpType.VerticalRocket && gm.verticalRocketElementData != null) return gm.verticalRocketElementData;
            if (IsPropeller(type) && gm.propellerElementData != null) return gm.propellerElementData;
            if (IsDiscoBall(type) && gm.discoBallElementData != null) return gm.discoBallElementData;
            return sourceData;
        }

        private void PlayEffect(string effectId, float volumeMultiplier = 1f, float pitchOffset = 0f)
        {
            if (GameManager.Instance == null || SoundManager.Instance == null)
                return;

            SoundManager.Instance.Play(effectId, false, 0f, volumeMultiplier, pitchOffset);
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