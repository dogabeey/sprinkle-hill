using System.Collections;
using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using MobileHapticsProFreeEdition;
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
        private struct GlassGroupId
        {
            public GlassFeature feature;
            public int groupIndex;
            public bool isGroupless;
            public Vector2Int grouplessCell;

            public GlassGroupId(GlassFeature feature, int groupIndex, bool isGroupless, Vector2Int grouplessCell)
            {
                this.feature = feature;
                this.groupIndex = groupIndex;
                this.isGroupless = isGroupless;
                this.grouplessCell = grouplessCell;
            }

            public override int GetHashCode()
            {
                int featureHash = feature != null ? feature.GetInstanceID() : 0;
                unchecked
                {
                    int hash = (featureHash * 397) ^ groupIndex;
                    if (isGroupless)
                        hash = (hash * 397) ^ grouplessCell.GetHashCode();

                    return hash;
                }
            }

            public override bool Equals(object obj)
            {
                if (!(obj is GlassGroupId other))
                    return false;

                return feature == other.feature &&
                       groupIndex == other.groupIndex &&
                       isGroupless == other.isGroupless &&
                       grouplessCell == other.grouplessCell;
            }
        }

        private static readonly CurrencyModel MatchRewardCurrency;
        private int currentComboCount = 0;
        private PowerUpHandler powerUpHandler;

        private LevelScene_Match3Game Match3Level => GameManager.Instance != null ? GameManager.Instance.CurrentLevel as LevelScene_Match3Game : null;

        private void PlayCellFeatureDestroyEffect(CellFeature feature, Vector2Int pos)
        {
            if (feature?.destroyParticleEffect == null)
                return;

            Vector3 worldPos = GetWorldPosition(pos);
            ParticleSystem fx = Instantiate(feature.destroyParticleEffect, worldPos, Quaternion.identity);
            fx.Play();
            float lifeTime = fx.main.duration + fx.main.startLifetime.constantMax + 0.2f;
            Destroy(fx.gameObject, lifeTime);
        }

        public void PlayCellFeatureDestroyEffectAt(CellFeature feature, Vector2Int pos)
        {
            PlayCellFeatureDestroyEffect(feature, pos);
        }

        // ------------------------------------------------------------------
        //  Public accessors used by PowerUpHandler & helpers
        // ------------------------------------------------------------------
        public Vector2Int GridSize => gridSize;
        public Transform GridParent => parent;

        public GridCell GetCellPublic(Vector2Int pos) => GetCell(pos);

        public IEnumerator ApplyGravityPublic() => ApplyGravity();

        public IEnumerator PlaceDiscoBallActionAt(Vector2Int center)
        {
            GridCell cell = GetCell(center);
            if (cell == null || cell.cellType != CellType.Normal || cell.elementInfo == null)
                yield break;

            if (cell.elementInfo.powerUpType != ElementPowerUpType.None || cell.elementInfo.elementData == null)
                yield break;

            ElementData selectedElementData = cell.elementInfo.elementData;
            powerUpHandler.CreatePowerUpAt(center, selectedElementData, ElementPowerUpType.DiscoBall);
            yield return StartCoroutine(powerUpHandler.ActivateAt(center, selectedElementData));
            yield return StartCoroutine(ResolveBoardAfterSpecialClear());
        }

        public IEnumerator PlaceHorizontalRocketActionAt(Vector2Int center)
        {
            GridCell cell = GetCell(center);
            if (cell == null || cell.cellType != CellType.Normal || cell.elementInfo == null)
                yield break;

            if (cell.elementInfo.powerUpType != ElementPowerUpType.None || cell.elementInfo.elementData == null)
                yield break;

            ElementData selectedElementData = cell.elementInfo.elementData;
            powerUpHandler.CreatePowerUpAt(center, selectedElementData, ElementPowerUpType.Rocket);
            yield break;
        }

        public GridElement GetElementAt(Vector2Int pos)
        {
            if (generatedTiles.TryGetValue(pos, out GridCellController tile) && tile != null)
                return tile.GetComponentInChildren<GridElement>();
            return null;
        }

        public GridElement ReplaceElementAt(Vector2Int pos, GridElementInfo elementInfo)
        {
            if (!generatedTiles.TryGetValue(pos, out GridCellController tile) || tile == null)
                return null;

            GridElement oldElement = tile.GetComponentInChildren<GridElement>();
            if (oldElement != null)
            {
                oldElement.transform.DOKill();
                oldElement.transform.SetParent(null, true);
                generatedElements.Remove(oldElement);
                Destroy(oldElement.gameObject);
            }

            GridElement newElement = Instantiate(gridElementPrefab, tile.transform.position, Quaternion.identity, tile.transform);
            newElement.elementInfo = elementInfo;
            generatedElements.Add(newElement);
            newElement.InitElement(this, elementInfo);
            return newElement;
        }

        public List<GridElement> GetAllActiveElements()
        {
            generatedElements.RemoveAll(e => e == null);
            return new List<GridElement>(generatedElements);
        }

        public Vector3 GetWorldPosition(Vector2Int pos)
        {
            if (generatedTiles.TryGetValue(pos, out GridCellController tile) && tile != null)
                return tile.transform.position;
            return Vector3.zero;


        }

        public GridCellController GetCellControllerAt(Vector2Int pos)
        {
            if (generatedTiles.TryGetValue(pos, out GridCellController tile) && tile != null)
                return tile;

            return null;
        }

        // ------------------------------------------------------------------
        //  Lifecycle
        // ------------------------------------------------------------------
        public override void PreInit()
        {
            powerUpHandler = new PowerUpHandler(this);
        }

        public override void PostInit()
        {
            LockedAreaFeature.InitializeForGrid(this);
            RefreshAllGlassDamageIndicators();
        }

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

        private void CollectGlassGroupAt(Vector2Int pos, HashSet<GlassGroupId> groups)
        {
            GridCell cell = GetCell(pos);
            if (TryGetGlassGroupId(pos, cell, out GlassGroupId groupId))
                groups.Add(groupId);
        }

        private void CollectAdjacentGlassGroups(Vector2Int center, HashSet<GlassGroupId> groups)
        {
            Vector2Int[] adjacentOffsets = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            for (int i = 0; i < adjacentOffsets.Length; i++)
                CollectGlassGroupAt(center + adjacentOffsets[i], groups);
        }

        private void CollectGlassGroupBelowDropTarget(Vector2Int landingPos, HashSet<GlassGroupId> groups)
        {
            if (groups == null)
                return;

            Vector2Int belowPos = new Vector2Int(landingPos.x, landingPos.y + 1);
            CollectGlassGroupAt(belowPos, groups);
        }

        private void RefreshCellFeatureVisual(Vector2Int pos)
        {
            if (tileGenerationData == null)
                return;

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dy = -1; dy <= 1; dy++)
                {
                    Vector2Int refreshPos = new Vector2Int(pos.x + dx, pos.y + dy);
                    if (refreshPos.x < 0 || refreshPos.y < 0 || refreshPos.x >= gridSize.x || refreshPos.y >= gridSize.y)
                        continue;

                    if (!generatedTiles.TryGetValue(refreshPos, out GridCellController tile) || tile == null)
                        continue;

                    tileGenerationData.ApplyFeatureSprite(tile, gridCells, refreshPos.x, refreshPos.y, TileData.DrawStartingCorner.TopLeft);

                    if (tile.damageIndicator != null)
                    {
                        tile.damageIndicator.enabled = false;
                        tile.damageIndicator.sprite = null;
                    }
                }
            }

            RefreshAllGlassDamageIndicators();
        }

        public void RefreshCellFeatureVisualAt(Vector2Int pos)
        {
            RefreshCellFeatureVisual(pos);
        }

        private bool TryGetGlassGroupId(Vector2Int position, GridCell cell, out GlassGroupId id)
        {
            if (cell != null && cell.cellFeature is GlassFeature glassFeature)
            {
                bool isGroupless = cell.cellFeatureGroupIndex == 0;
                id = new GlassGroupId(glassFeature, cell.cellFeatureGroupIndex, isGroupless, isGroupless ? position : default);
                return true;
            }

            id = default;
            return false;
        }

        private Dictionary<GlassGroupId, List<Vector2Int>> BuildGlassGroups()
        {
            Dictionary<GlassGroupId, List<Vector2Int>> groups = new Dictionary<GlassGroupId, List<Vector2Int>>();

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    GridCell cell = GetCell(pos);
                    if (!TryGetGlassGroupId(pos, cell, out GlassGroupId groupId))
                        continue;

                    if (!groups.TryGetValue(groupId, out List<Vector2Int> positions))
                    {
                        positions = new List<Vector2Int>();
                        groups[groupId] = positions;
                    }

                    positions.Add(pos);
                }
            }

            return groups;
        }

        private int ResolveGlassGroupMaxHealth(GlassGroupId groupId, List<Vector2Int> groupCells)
        {
            int maxHealth = Mathf.Max(1, groupId.feature != null ? groupId.feature.defaultGroupHealth : 1);
            for (int i = 0; i < groupCells.Count; i++)
            {
                GridCell cell = GetCell(groupCells[i]);
                if (cell == null)
                    continue;

                if (cell.cellFeatureGroupMaxHealth > maxHealth)
                    maxHealth = cell.cellFeatureGroupMaxHealth;

                if (cell.cellFeatureGroupHealth > maxHealth)
                    maxHealth = cell.cellFeatureGroupHealth;
            }

            return Mathf.Max(1, maxHealth);
        }

        private int ResolveGlassGroupCurrentHealth(int maxHealth, List<Vector2Int> groupCells)
        {
            int currentHealth = maxHealth;
            for (int i = 0; i < groupCells.Count; i++)
            {
                GridCell cell = GetCell(groupCells[i]);
                if (cell == null)
                    continue;

                int cellHealth = cell.cellFeatureGroupHealth > 0 ? cell.cellFeatureGroupHealth : maxHealth;
                if (cellHealth < currentHealth)
                    currentHealth = cellHealth;

                if (cell.cellFeatureGroupMaxHealth <= 0)
                    cell.cellFeatureGroupMaxHealth = maxHealth;
            }

            return Mathf.Clamp(currentHealth, 0, maxHealth);
        }

        private void RefreshAllGlassDamageIndicators()
        {
            Dictionary<GlassGroupId, List<Vector2Int>> groups = BuildGlassGroups();
            foreach (var groupKvp in groups)
            {
                GlassGroupId groupId = groupKvp.Key;
                List<Vector2Int> groupCells = groupKvp.Value;
                if (groupCells == null || groupCells.Count == 0)
                    continue;

                bool hasImprisonedElement = false;

                int maxHealth = ResolveGlassGroupMaxHealth(groupId, groupCells);
                int currentHealth = ResolveGlassGroupCurrentHealth(maxHealth, groupCells);
                int missingHealth = Mathf.Clamp(maxHealth - currentHealth, 0, maxHealth);

                for (int i = 0; i < groupCells.Count; i++)
                {
                    GridCell groupCell = GetCell(groupCells[i]);
                    if (groupCell?.elementInfo?.elementData != null)
                    {
                        hasImprisonedElement = true;
                    }
                    generatedTiles.TryGetValue(groupCells[i], out GridCellController groupCellController);
                    Sprite damageSprite = groupId.feature != null ? groupId.feature.GetDamageSprite(missingHealth) : null;

                    if (groupCellController.featureSprite.TryGetComponent(out SpriteMask mask)) 
                    {
                        if (damageSprite != null)
                        {
                            mask.sprite = damageSprite;
                        }
                        else
                        {
                            mask.sprite = null;
                        }
                    }
                }

                if (!hasImprisonedElement)
                    continue;
                

            }
        }

        private static void ResetDamageIndicatorTransform(SpriteRenderer indicator)
        {
            if (indicator == null)
                return;

            indicator.transform.localPosition = Vector3.zero;
            indicator.transform.localScale = Vector3.one;
            indicator.transform.localRotation = Quaternion.identity;
        }

        private void ConfigureGroupDamageIndicator(GridCellController anchorTile, List<Vector2Int> groupCells)
        {
            if (anchorTile == null || anchorTile.damageIndicator == null || groupCells == null || groupCells.Count == 0)
                return;

            float minWorldX = float.MaxValue;
            float maxWorldX = float.MinValue;
            float minWorldY = float.MaxValue;
            float maxWorldY = float.MinValue;

            int minGridX = int.MaxValue;
            int maxGridX = int.MinValue;
            int minGridY = int.MaxValue;
            int maxGridY = int.MinValue;

            for (int i = 0; i < groupCells.Count; i++)
            {
                Vector2Int pos = groupCells[i];
                if (pos.x < minGridX) minGridX = pos.x;
                if (pos.x > maxGridX) maxGridX = pos.x;
                if (pos.y < minGridY) minGridY = pos.y;
                if (pos.y > maxGridY) maxGridY = pos.y;

                if (!generatedTiles.TryGetValue(pos, out GridCellController tile) || tile == null)
                    continue;

                Vector3 worldPos = tile.transform.position;
                if (worldPos.x < minWorldX) minWorldX = worldPos.x;
                if (worldPos.x > maxWorldX) maxWorldX = worldPos.x;
                if (worldPos.y < minWorldY) minWorldY = worldPos.y;
                if (worldPos.y > maxWorldY) maxWorldY = worldPos.y;
            }

            if (minWorldX > maxWorldX || minWorldY > maxWorldY)
                return;

            SpriteRenderer indicator = anchorTile.damageIndicator;
            ResetDamageIndicatorTransform(indicator);

            float cellWidth = anchorTile.gridSprite != null ? anchorTile.gridSprite.bounds.size.x : 1f;
            float cellHeight = anchorTile.gridSprite != null ? anchorTile.gridSprite.bounds.size.y : 1f;

            float targetWorldWidth = (maxGridX - minGridX + 1) * cellWidth;
            float targetWorldHeight = (maxGridY - minGridY + 1) * cellHeight;

            Vector3 centerWorld = new Vector3((minWorldX + maxWorldX) * 0.5f, (minWorldY + maxWorldY) * 0.5f, indicator.transform.position.z);
            indicator.transform.position = centerWorld;

            Vector3 baseWorldSize = indicator.bounds.size;
            if (baseWorldSize.x <= 0f || baseWorldSize.y <= 0f)
                return;

            Vector3 localScale = indicator.transform.localScale;
            localScale.x *= targetWorldWidth / baseWorldSize.x;
            localScale.y *= targetWorldHeight / baseWorldSize.y;
            indicator.transform.localScale = localScale;
        }

        private bool DamageGlassGroupsForMatch(HashSet<GlassGroupId> groupsToDamage)
        {
            if (groupsToDamage == null || groupsToDamage.Count == 0)
                return false;

            bool didBreakAnyGlass = false;

            Dictionary<GlassGroupId, List<Vector2Int>> groups = BuildGlassGroups();
            foreach (GlassGroupId groupId in groupsToDamage)
            {
                if (!groups.TryGetValue(groupId, out List<Vector2Int> groupCells) || groupCells.Count == 0)
                    continue;

                int maxHealth = ResolveGlassGroupMaxHealth(groupId, groupCells);
                int currentHealth = ResolveGlassGroupCurrentHealth(maxHealth, groupCells);
                int nextHealth = currentHealth - 1;

                if (nextHealth <= 0)
                {
                    didBreakAnyGlass = true;
                    for (int i = 0; i < groupCells.Count; i++)
                    {
                        Vector2Int pos = groupCells[i];
                        GridCell cell = GetCell(pos);
                        if (cell == null)
                            continue;

                        CellFeature destroyedFeature = cell.cellFeature;
                        cell.cellFeature = null;
                        cell.cellFeatureGroupIndex = 0;
                        cell.cellFeatureGroupHealth = 0;
                        cell.cellFeatureGroupMaxHealth = 0;

                        EventManager.TriggerEvent(GameEvent.GLASS_SHATTERED, new EventParam(
                            paramScriptable: destroyedFeature,
                            vectorList: new Vector3[] { new Vector3(pos.x, pos.y, 0f) }
                        ));

                        PlayCellFeatureDestroyEffect(destroyedFeature, pos);
                        RefreshCellFeatureVisual(pos);
                    }
                }
                else
                {
                    for (int i = 0; i < groupCells.Count; i++)
                    {
                        GridCell cell = GetCell(groupCells[i]);
                        if (cell == null)
                            continue;

                        cell.cellFeatureGroupHealth = nextHealth;
                        if (cell.cellFeatureGroupMaxHealth < maxHealth)
                            cell.cellFeatureGroupMaxHealth = maxHealth;
                    }
                }
            }

            RefreshAllGlassDamageIndicators();
            return didBreakAnyGlass;
        }

        public void TriggerCellFeatureMatchedOverAt(Vector2Int pos)
        {
            GridCell cell = GetCell(pos);
            if (cell?.cellFeature == null)
                return;

            GridElement element = GetElementAt(pos);
            cell.cellFeature.OnElementMatchedOverTheCell(cell, element);
            RefreshCellFeatureVisual(pos);
        }

        public void TriggerCellFeatureMatchedAdjacentToAt(Vector2Int matchedPos, GridCell matchedCell, GridElement matchedElement)
        {
            Vector2Int[] adjacentOffsets = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            for (int i = 0; i < adjacentOffsets.Length; i++)
            {
                Vector2Int adjacentPos = matchedPos + adjacentOffsets[i];
                GridCell adjacentCell = GetCell(adjacentPos);
                if (adjacentCell?.cellFeature == null)
                    continue;

                adjacentCell.cellFeature.OnElementMatchedAdjacentToTheCell(adjacentCell, matchedCell, matchedElement);
                RefreshCellFeatureVisual(adjacentPos);
            }
        }

        public static bool AreAdjacent(Vector2Int a, Vector2Int b)
        {
            return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y - b.y) == 1;
        }

        public bool IsValidPosition(Vector2Int pos) => GetCell(pos) != null;

        public bool IsCauldronReadyAt(Vector2Int pos)
        {
            GridCell cell = GetCell(pos);
            if (cell?.elementInfo == null || cell.elementInfo.powerUpType != ElementPowerUpType.Cauldron)
                return false;

            if (!IsCauldronData(cell.elementInfo.elementData))
                return false;

            int required = Mathf.Max(1, cell.elementInfo.elementData != null ? cell.elementInfo.elementData.cauldronChargeRequired : 1);
            return cell.elementInfo.cauldronProgress >= required;
        }

        public void NotifyElementCleared(Vector2Int clearedPos)
        {
            GridCell clearedCell = GetCell(clearedPos);
            if (clearedCell?.elementInfo != null && clearedCell.elementInfo.powerUpType == ElementPowerUpType.Cauldron)
                return;

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Vector2Int cauldronPos = new Vector2Int(x, y);
                    GridCell cauldronCell = GetCell(cauldronPos);
                    if (cauldronCell?.elementInfo == null || cauldronCell.elementInfo.powerUpType != ElementPowerUpType.Cauldron)
                        continue;

                    ElementData data = cauldronCell.elementInfo.elementData;
                    Vector2Int coverage = GetGridCoverage(data);
                    int radius = Mathf.Max(1, data != null ? data.cauldronChargeRadius : 1);
                    int distance = GetManhattanDistanceToCoverage(clearedPos, cauldronPos, coverage);
                    if (distance > radius)
                        continue;

                    int required = Mathf.Max(1, data != null ? data.cauldronChargeRequired : 1);
                    if (cauldronCell.elementInfo.cauldronProgress >= required)
                        continue;

                    cauldronCell.elementInfo.cauldronProgress = Mathf.Min(required, cauldronCell.elementInfo.cauldronProgress + 1);
                    RefreshElementVisual(cauldronPos);
                }
            }
        }

        // ------------------------------------------------------------------
        //  Swap & Match entry point
        // ------------------------------------------------------------------
        public IEnumerator SwapAndMatch(Vector2Int first, Vector2Int second)
        {
            GridCell firstCell = GetCell(first);
            GridCell secondCell = GetCell(second);

            if (firstCell?.cellFeature is GlassFeature || secondCell?.cellFeature is GlassFeature)
                yield break;

            // Activate power-ups on swap
            ElementPowerUpType firstType = firstCell?.elementInfo?.powerUpType ?? ElementPowerUpType.None;
            ElementPowerUpType secondType = secondCell?.elementInfo?.powerUpType ?? ElementPowerUpType.None;

            if (firstType == ElementPowerUpType.Cauldron || secondType == ElementPowerUpType.Cauldron)
            {
                if (first == second && firstType == ElementPowerUpType.Cauldron)
                {
                    yield return StartCoroutine(powerUpHandler.ActivateAt(first, null));
                    yield return StartCoroutine(ResolveBoardAfterSpecialClear());
                }
                yield break;
            }

            // Click activation (no drag, same position)
            if (first == second)
            {
                if (PowerUpHandler.IsSpecialPowerUp(firstType))
                {
                    yield return StartCoroutine(powerUpHandler.ActivateAt(first, null));
                    yield return StartCoroutine(ResolveBoardAfterSpecialClear());
                    yield break;
                }
                yield break;
            }

            if (IsGarbageBagCell(firstCell) || IsGarbageBagCell(secondCell))
                yield break;

            if (IsMultiCellElementAnchor(first) || IsMultiCellElementAnchor(second))
                yield break;

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
                GameManager.Instance.soundManager.Play(ConstantManager.SOUNDS.EFFECTS.ELEMENT_SWAP);
                GridHelper.TriggerHaptic(HapticModes.Select);

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

                yield return StartCoroutine(ResolveBoardAfterSpecialClear());
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
            GameManager.Instance.soundManager.Play(ConstantManager.SOUNDS.EFFECTS.ELEMENT_SWAP);
            GridHelper.TriggerHaptic(HapticModes.Select);
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
                GameObject matchOrigin = GetMatchOriginObject(matchedGroups);
                RewardCurrencyForCombo(currentComboCount, matchOrigin);
                GameManager.Instance.soundManager.Play(ConstantManager.SOUNDS.EFFECTS.MATCH, pitchOffset: currentComboCount * 0.1f);
                GridHelper.TriggerHaptic(HapticModes.Confirm);

                LevelScene_Match3Game level = GameManager.Instance.CurrentLevel as LevelScene_Match3Game;
                bool allowDiscoBall = level.AllowDiscoBallCreation;
                bool allowPropeller = level.AllowPropellerCreation;
                bool allowBomb = level.AllowBombCreation;
                bool allowRocket = level.AllowRocketCreation;

                // Detect power-up spawns (strategy ordered pipeline)
                PowerUpHandler.SpawnResolution spawnResolution = powerUpHandler.ResolveSpawns(new PowerUpHandler.CreationContext
                {
                    matchedGroups = matchedGroups,
                    init1 = init1,
                    init2 = init2,
                    isIndirectCreation = false,
                    allowDiscoBall = allowDiscoBall,
                    allowPropeller = allowPropeller,
                    allowBomb = allowBomb,
                    allowRocket = allowRocket
                });

                List<PowerUpHandler.SpawnRequest> discoBallSpawns = spawnResolution.discoBallSpawns;
                List<PowerUpHandler.SpawnRequest> propellerSpawns = spawnResolution.propellerSpawns;
                List<PowerUpHandler.SpawnRequest> bombSpawns = spawnResolution.bombSpawns;
                List<PowerUpHandler.SpawnRequest> rocketSpawns = spawnResolution.rocketSpawns;
                HashSet<Vector2Int> protectedPositions = spawnResolution.protectedPositions;

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
                for (int i = 0; i < propellerSpawns.Count; i++)
                    powerUpHandler.CreatePowerUpAt(propellerSpawns[i].position, propellerSpawns[i].sourceData, propellerSpawns[i].powerUpType);
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
                GridHelper.TriggerHaptic(HapticModes.Alert);
                yield return StartCoroutine(SwapElements(init1, init2));
            }
            else
            {
                RunOccupancySanityPass();
                TriggerGridSettledEvents();
            }
        }

        // ------------------------------------------------------------------
        //  Board resolution (after special clears)
        // ------------------------------------------------------------------
        public IEnumerator ResolveBoardAfterSpecialClear()
        {
            currentComboCount = 0;
            yield return new WaitUntil(() => !powerUpHandler.IsChainReactionInProgress());
            yield return StartCoroutine(ApplyGravity());

            List<List<Vector2Int>> matchedGroups;
            while ((matchedGroups = CheckMatchOf(3)).Count > 0)
            {
                currentComboCount++;

                LevelScene_Match3Game level = GameManager.Instance.CurrentLevel as LevelScene_Match3Game;
                bool allowDiscoBall = level != null && level.AllowDiscoBallCreation;
                bool allowPropeller = level != null && level.AllowPropellerCreation;
                bool allowBomb = level != null && level.AllowBombCreation;
                bool allowRocket = level != null && level.AllowRocketCreation;

                PowerUpHandler.SpawnResolution spawnResolution = powerUpHandler.ResolveSpawns(new PowerUpHandler.CreationContext
                {
                    matchedGroups = matchedGroups,
                    init1 = new Vector2Int(-1, -1),
                    init2 = new Vector2Int(-1, -1),
                    isIndirectCreation = true,
                    allowDiscoBall = allowDiscoBall,
                    allowPropeller = allowPropeller,
                    allowBomb = allowBomb,
                    allowRocket = allowRocket
                });

                List<PowerUpHandler.SpawnRequest> discoBallSpawns = spawnResolution.discoBallSpawns;
                List<PowerUpHandler.SpawnRequest> propellerSpawns = spawnResolution.propellerSpawns;
                List<PowerUpHandler.SpawnRequest> bombSpawns = spawnResolution.bombSpawns;
                List<PowerUpHandler.SpawnRequest> rocketSpawns = spawnResolution.rocketSpawns;
                HashSet<Vector2Int> protectedPositions = spawnResolution.protectedPositions;

                EventManager.TriggerEvent(GameEvent.MATCH_DETECTED, new EventParam(paramInt: matchedGroups.Count));
                if (currentComboCount > 1)
                    EventManager.TriggerEvent(GameEvent.COMBO_TRIGGERED, new EventParam(paramInt: currentComboCount));
                foreach (var group in matchedGroups)
                    EventManager.TriggerEvent(GameEvent.ELEMENT_MATCHED, new EventParam(
                        paramScriptable: GetCell(group[0])?.elementInfo?.elementData, paramInt: group.Count));

                yield return StartCoroutine(ClearMatches(matchedGroups, protectedPositions));

                for (int i = 0; i < discoBallSpawns.Count; i++)
                    powerUpHandler.CreatePowerUpAt(discoBallSpawns[i].position, discoBallSpawns[i].sourceData, discoBallSpawns[i].powerUpType);
                for (int i = 0; i < propellerSpawns.Count; i++)
                    powerUpHandler.CreatePowerUpAt(propellerSpawns[i].position, propellerSpawns[i].sourceData, propellerSpawns[i].powerUpType);
                for (int i = 0; i < bombSpawns.Count; i++)
                    powerUpHandler.CreatePowerUpAt(bombSpawns[i].position, bombSpawns[i].sourceData, bombSpawns[i].powerUpType);
                for (int i = 0; i < rocketSpawns.Count; i++)
                    powerUpHandler.CreatePowerUpAt(rocketSpawns[i].position, rocketSpawns[i].sourceData, rocketSpawns[i].powerUpType);

                yield return StartCoroutine(ApplyGravity());
            }

            RunOccupancySanityPass();
            TriggerGridSettledEvents();
        }

        private void TriggerGridSettledEvents()
        {
            EventManager.TriggerEvent(GameEvent.GRID_STABLE);

            if (!HasAnyPossibleMove())
            {
                EventManager.TriggerEvent(GameEvent.NO_POSSIBLE_MOVES);
            }
        }

        // ------------------------------------------------------------------
        //  Area clear (used by bomb)
        // ------------------------------------------------------------------
        public IEnumerator ClearAreaAt(Vector2Int center, int radius, bool allowConditionedBreakableWalls = true)
        {
            HashSet<Vector2Int> wallsToBreak = new HashSet<Vector2Int>();

            for (int x = center.x - radius; x <= center.x + radius; x++)
            {
                for (int y = center.y - radius; y <= center.y + radius; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    GridCell cell = GetCell(pos);
                    if (cell == null) continue;
                    if (cell.cellType == CellType.BreakableWall)
                    {
                        if (allowConditionedBreakableWalls || cell.breakableWallElementCondition == null)
                            wallsToBreak.Add(pos);
                        continue;
                    }
                    if (cell.cellType != CellType.Normal || cell.elementInfo == null) continue;

                    GridElement matchedElement = GetElementAt(pos);
                    TriggerCellFeatureMatchedOverAt(pos);
                    TriggerCellFeatureMatchedAdjacentToAt(pos, cell, matchedElement);

                    if (TryRevealHiddenBoxAt(pos))
                        continue;

                    if (IsGarbageBagData(cell.elementInfo.elementData))
                        continue;

                    if (IsPowerGeneratorData(cell.elementInfo.elementData))
                        continue;

                    if (PowerUpHandler.IsSpecialPowerUp(cell.elementInfo.powerUpType))
                    {
                        StartCoroutine(powerUpHandler.ActivateAt(pos, null));
                        continue;
                    }

                    if (cell.elementInfo.powerUpType == ElementPowerUpType.Cauldron) continue;

                    NotifyElementCleared(pos);
                    cell.elementInfo = null;
                    if (matchedElement != null) StartCoroutine(matchedElement.DestroyElement());
                }
            }

            //GameManager.Instance.actionBarManager.actionBarItemList.Find(item => item is BombPlacementAction).CurrentCount--;
            yield return new WaitForSeconds(GameManager.Instance.constantManager.matchClearDelay);

            yield return StartCoroutine(BreakWallsSimultaneous(wallsToBreak));
        }

        public IEnumerator ClearColumnAt(int columnX, bool allowConditionedBreakableWalls = true)
        {
            if (columnX < 0 || columnX >= gridSize.x)
                yield break;

            HashSet<Vector2Int> wallsToBreak = new HashSet<Vector2Int>();

            for (int y = 0; y < gridSize.y; y++)
            {
                Vector2Int pos = new Vector2Int(columnX, y);
                GridCell cell = GetCell(pos);
                if (cell == null)
                    continue;

                if (cell.cellType == CellType.BreakableWall)
                {
                    if (allowConditionedBreakableWalls || cell.breakableWallElementCondition == null)
                        wallsToBreak.Add(pos);
                    continue;
                }

                if (cell.cellType != CellType.Normal || cell.elementInfo == null)
                    continue;

                GridElement matchedElement = GetElementAt(pos);
                TriggerCellFeatureMatchedOverAt(pos);
                TriggerCellFeatureMatchedAdjacentToAt(pos, cell, matchedElement);

                if (TryRevealHiddenBoxAt(pos))
                    continue;

                if (IsGarbageBagData(cell.elementInfo.elementData))
                    continue;

                if (IsPowerGeneratorData(cell.elementInfo.elementData))
                    continue;

                if (PowerUpHandler.IsSpecialPowerUp(cell.elementInfo.powerUpType))
                {
                    StartCoroutine(powerUpHandler.ActivateAt(pos, null));
                    continue;
                }

                if (cell.elementInfo.powerUpType == ElementPowerUpType.Cauldron)
                    continue;

                NotifyElementCleared(pos);
                cell.elementInfo = null;
                if (matchedElement != null)
                    StartCoroutine(matchedElement.DestroyElement());
            }

            yield return new WaitForSeconds(GameManager.Instance.constantManager.matchClearDelay);
            yield return StartCoroutine(BreakWallsSimultaneous(wallsToBreak));
        }

        public IEnumerator ClearCrossAt(Vector2Int center, bool allowConditionedBreakableWalls = true)
        {
            Vector2Int[] offsets =
            {
                Vector2Int.zero,
                Vector2Int.left,
                Vector2Int.right,
                Vector2Int.up,
                Vector2Int.down
            };

            HashSet<Vector2Int> wallsToBreak = new HashSet<Vector2Int>();

            for (int i = 0; i < offsets.Length; i++)
            {
                Vector2Int pos = center + offsets[i];
                GridCell cell = GetCell(pos);
                if (cell == null)
                    continue;

                if (cell.cellType == CellType.BreakableWall)
                {
                    if (allowConditionedBreakableWalls || cell.breakableWallElementCondition == null)
                        wallsToBreak.Add(pos);
                    continue;
                }

                if (cell.cellType != CellType.Normal || cell.elementInfo == null)
                    continue;

                GridElement matchedElement = GetElementAt(pos);
                TriggerCellFeatureMatchedOverAt(pos);
                TriggerCellFeatureMatchedAdjacentToAt(pos, cell, matchedElement);

                if (TryRevealHiddenBoxAt(pos))
                    continue;

                if (IsGarbageBagData(cell.elementInfo.elementData))
                    continue;

                if (IsPowerGeneratorData(cell.elementInfo.elementData))
                    continue;

                if (PowerUpHandler.IsSpecialPowerUp(cell.elementInfo.powerUpType))
                {
                    StartCoroutine(powerUpHandler.ActivateAt(pos, null));
                    continue;
                }

                if (cell.elementInfo.powerUpType == ElementPowerUpType.Cauldron)
                    continue;

                NotifyElementCleared(pos);
                cell.elementInfo = null;
                if (matchedElement != null)
                    StartCoroutine(matchedElement.DestroyElement());
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
            TriggerCellFeatureMatchedOverAt(sparklingPos);
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

        // ------------------------------------------------------------------
        //  Shuffle
        // ------------------------------------------------------------------
        public IEnumerator ShuffleBoardAndResolve()
        {
            yield return StartCoroutine(ShuffleBoardAnimated());
            yield return StartCoroutine(ResolveBoardAfterSpecialClear());
        }

        private IEnumerator ShuffleBoardAnimated()
        {
            List<Vector2Int> positions = new List<Vector2Int>();
            List<GridElementInfo> originalInfos = new List<GridElementInfo>();
            Dictionary<Vector2Int, GridElement> originalElements = new Dictionary<Vector2Int, GridElement>();

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    GridCell cell = GetCell(pos);
                    if (cell == null || cell.cellType != CellType.Normal || cell.elementInfo == null || IsMultiCellElementAnchor(pos)) continue;
                    if (cell.elementInfo.isHidden) continue;
                    if (IsGarbageBagData(cell.elementInfo.elementData)) continue;
                    if (IsCauldronCell(cell)) continue;
                    if (HasBehavior(cell.elementInfo.elementData, ElementData.ElementBehaviorFlags.NonShuffleable)) continue;

                    positions.Add(pos);
                    originalInfos.Add(cell.elementInfo);
                    originalElements[pos] = GetElementAt(pos);
                }
            }

            if (positions.Count < 2) yield break;

            List<int> shuffled = new List<int>(positions.Count);
            for (int i = 0; i < positions.Count; i++) shuffled.Add(i);

            List<ElementData> elementPool = BuildElementPool();
            bool layoutReady = false;
            for (int attempt = 0; attempt < 24 && !layoutReady; attempt++)
            {
                shuffled.Shuffle();

                bool changed = false;
                for (int i = 0; i < shuffled.Count; i++)
                {
                    if (shuffled[i] != i)
                    {
                        changed = true;
                        break;
                    }
                }

                if (!changed)
                    continue;

                for (int i = 0; i < positions.Count; i++)
                {
                    GridCell targetCell = GetCell(positions[i]);
                    if (targetCell != null)
                        targetCell.elementInfo = originalInfos[shuffled[i]];
                }

                EnsureAtLeastOneMoveAvailable(elementPool);
                layoutReady = HasAnyPossibleMove();
            }

            if (!layoutReady)
                EnsureAtLeastOneMoveAvailable(elementPool);

            float duration = Mathf.Max(0.2f, GameManager.Instance.constantManager.elementSwapMoveDuration * 1.35f);
            Sequence shuffleSeq = DOTween.Sequence();
            bool hasTween = false;

            for (int i = 0; i < positions.Count; i++)
            {
                Vector2Int targetPos = positions[i];
                Vector2Int sourcePos = positions[shuffled[i]];

                if (!originalElements.TryGetValue(sourcePos, out GridElement element) || element == null) continue;
                if (!generatedTiles.TryGetValue(targetPos, out GridCellController targetTile) || targetTile == null) continue;

                element.transform.DOKill();
                element.transform.SetParent(targetTile.transform, true);

                GridCell targetCell = GetCell(targetPos);
                if (targetCell?.elementInfo != null)
                {
                    element.elementInfo = targetCell.elementInfo;
                    element.InitElement(this, targetCell.elementInfo);
                }

                shuffleSeq.Join(element.transform.DOLocalMove(Vector3.zero, duration).SetEase(Ease.InOutQuad));
                shuffleSeq.Join(element.transform.DOLocalRotate(new Vector3(0f, 0f, 360f), duration, RotateMode.FastBeyond360)
                    .SetEase(Ease.OutQuad).SetRelative());
                shuffleSeq.Join(element.transform.DOScale(0.88f, duration * 0.5f).SetLoops(2, LoopType.Yoyo).SetEase(Ease.InOutSine));
                hasTween = true;
            }

            GridHelper.ShakeCamera(duration * 0.7f, 0.08f, 8, 20f);

            if (hasTween)
                yield return shuffleSeq.WaitForCompletion();
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
            Dictionary<GlassGroupId, int> resolvedGroupHealth = new Dictionary<GlassGroupId, int>();
            Dictionary<GlassGroupId, int> resolvedGroupMaxHealth = new Dictionary<GlassGroupId, int>();
            for (int x = 0; x < gridSize.x; x++)
                for (int y = 0; y < gridSize.y; y++)
                {
                    GridCell cell = GetCell(new Vector2Int(x, y));
                    NormalizeSpecialElementInfo(cell);

                    if (!TryGetGlassGroupId(new Vector2Int(x, y), cell, out GlassGroupId groupId))
                        continue;

                    int configuredHealth = cell.cellHealth;
                    if (configuredHealth <= 0)
                        configuredHealth = Mathf.Max(1, groupId.feature != null ? groupId.feature.defaultGroupHealth : 1);

                    int configuredMaxHealth = cell.cellFeatureGroupMaxHealth;
                    if (configuredMaxHealth <= 0)
                        configuredMaxHealth = configuredHealth;
                    configuredMaxHealth = Mathf.Max(configuredMaxHealth, configuredHealth);

                    if (resolvedGroupHealth.TryGetValue(groupId, out int existingHealth))
                        resolvedGroupHealth[groupId] = Mathf.Min(existingHealth, configuredHealth);
                    else
                        resolvedGroupHealth[groupId] = configuredHealth;

                    if (resolvedGroupMaxHealth.TryGetValue(groupId, out int existingMaxHealth))
                        resolvedGroupMaxHealth[groupId] = Mathf.Max(existingMaxHealth, configuredMaxHealth);
                    else
                        resolvedGroupMaxHealth[groupId] = configuredMaxHealth;
                }

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    GridCell cell = GetCell(new Vector2Int(x, y));
                    if (cell != null && cell.cellType == CellType.BreakableWall)
                        cell.cellHealth = Mathf.Max(1, cell.cellHealth);

                    if (!TryGetGlassGroupId(new Vector2Int(x, y), cell, out GlassGroupId groupId))
                        continue;

                    if (resolvedGroupHealth.TryGetValue(groupId, out int groupHealth))
                        cell.cellHealth = groupHealth;

                    if (resolvedGroupMaxHealth.TryGetValue(groupId, out int groupMaxHealth))
                        cell.cellFeatureGroupMaxHealth = Mathf.Max(groupHealth, groupMaxHealth);
                }
            }

            InitializeBreakableWallVisuals();

            List<ElementData> elementPool = BuildElementPool();

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    GridCell cell = gridCells[x, y];
                    bool acceptsElements = cell?.cellFeature == null || cell.cellFeature.AcceptElements;
                    if (cell != null && cell.cellType == CellType.Normal && !acceptsElements)
                    {
                        cell.elementInfo = null;
                    }

                    if (cell == null || cell.cellType != CellType.Normal || !acceptsElements || cell.elementInfo?.elementData == null)
                        continue;

                    NormalizeSpecialElementInfo(cell);

                    if (!UseLevelEditor &&
                        !cell.elementInfo.isHidden &&
                        !HasBehavior(cell.elementInfo.elementData, ElementData.ElementBehaviorFlags.NonShuffleable) &&
                        cell.elementInfo.powerUpType != ElementPowerUpType.Cauldron &&
                        elementPool.Count > 1)
                    {
                        List<ElementData> candidates = new List<ElementData>();
                        foreach (ElementData data in elementPool)
                            if (!WouldCreateMatch(x, y, data)) candidates.Add(data);
                        if (candidates.Count > 0)
                            cell.elementInfo.elementData = candidates[Random.Range(0, candidates.Count)];
                    }

                    if (!generatedTiles.TryGetValue(cell.coordinates, out GridCellController tile)) continue;

                    if (cell.cellFeature is GlassFeature && cell.elementInfo?.elementData == null)
                        continue;

                    GridElement element = Instantiate(gridElementPrefab, tile.transform.position, Quaternion.identity, tile.transform); // TODO: Use pooling
                    element.elementInfo = cell.elementInfo;
                    generatedElements.Add(element);
                    element.InitElement(this, element.elementInfo);
                    powerUpHandler.ApplySortingBoost(element, cell.elementInfo.powerUpType == ElementPowerUpType.Bomb);

                    if (cell.elementInfo.isHidden)
                    {
                        EventManager.TriggerEvent(GameEvent.HIDDEN_BOX_CREATED, new EventParam(
                            vectorList: new Vector3[] { new Vector3(cell.coordinates.x, cell.coordinates.y, 0f) },
                            paramScriptable: cell.elementInfo.elementData
                        ));
                    }

                    if (IsGarbageBagData(cell.elementInfo.elementData))
                    {
                        EventManager.TriggerEvent(GameEvent.GARBAGE_CREATED, new EventParam(
                            vectorList: new Vector3[] { new Vector3(cell.coordinates.x, cell.coordinates.y, 0f) },
                            paramScriptable: cell.elementInfo.elementData
                        ));
                    }
                }
            }

            if (!UseLevelEditor)
                EnsureAtLeastOneMoveAvailable(elementPool);

            RefreshAllGlassDamageIndicators();
        }

        private void InitializeBreakableWallVisuals()
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    GridCell cell = GetCell(pos);
                    if (cell == null || cell.cellType != CellType.BreakableWall)
                        continue;

                    cell.cellHealth = Mathf.Max(1, cell.cellHealth);

                    if (generatedTiles.TryGetValue(pos, out GridCellController tile) && tile is BreakableWall breakableWall)
                        breakableWall.InitializeHealth(cell.cellHealth);
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
                   !(cell.cellFeature is GlassFeature) &&
                   cell.elementInfo != null &&
                   !cell.elementInfo.isHidden &&
                   cell.elementInfo.powerUpType == ElementPowerUpType.None &&
                   !HasBehavior(cell.elementInfo.elementData, ElementData.ElementBehaviorFlags.NonMatchable) &&
                   !IsGarbageBagData(cell.elementInfo.elementData) &&
                   cell.elementInfo.elementData == data;
        }

        private List<List<Vector2Int>> CheckMatchOf(int minCount = 3)
        {
            Dictionary<Vector2Int, ElementData> matched = new Dictionary<Vector2Int, ElementData>();

            ElementData GetData(int x, int y)
            {
                GridCell cell = GetCell(new Vector2Int(x, y));
                if (cell == null || cell.cellType != CellType.Normal || cell.elementInfo == null ||
                    cell.cellFeature is GlassFeature ||
                    cell.elementInfo.isHidden || PowerUpHandler.IsSpecialPowerUp(cell.elementInfo.powerUpType) ||
                    IsMultiCellData(cell.elementInfo.elementData) ||
                    HasBehavior(cell.elementInfo.elementData, ElementData.ElementBehaviorFlags.NonMatchable) ||
                    IsGarbageBagData(cell.elementInfo.elementData))
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
            HashSet<Vector2Int> allMatchedPositions = new HashSet<Vector2Int>();
            HashSet<Vector2Int> adjacentFeatureProcessed = new HashSet<Vector2Int>();

            if (matchedPositions != null)
            {
                for (int i = 0; i < matchedPositions.Count; i++)
                {
                    List<Vector2Int> group = matchedPositions[i];
                    if (group == null)
                        continue;

                    for (int j = 0; j < group.Count; j++)
                        allMatchedPositions.Add(group[j]);
                }
            }

            foreach (var group in matchedPositions)
            {
                Vector2Int? mergeTarget = FindMergeTarget(group, protectedPositions);

                if (mergeTarget.HasValue)
                {
                    GridElement mergeEl = GetElementAt(mergeTarget.Value);
                    if (mergeEl != null) GridHelper.AnimateEmission(mergeEl, 1.5f, 0.2f);
                }

                foreach (var pos in group)
                {
                    if (protectedPositions != null && protectedPositions.Contains(pos))
                    {
                        GridCell protectedCell = GetCell(pos);
                        if (protectedCell?.elementInfo != null)
                        {
                            ElementData protectedElementData = protectedCell.elementInfo.elementData;
                            GridElement protectedElement = GetElementAt(pos);
                            ProcessAdjacentFeatureMatchEffects(pos, protectedCell, protectedElement, allMatchedPositions, adjacentFeatureProcessed, adjacentOffsets);
                            ProcessAdjacentWallAndHiddenEffects(pos, protectedElementData, adjacentOffsets, wallsToBreak, hiddenToReveal);
                        }
                        continue;
                    }
                    if (cleared.Contains(pos)) continue;

                    GridCell cell = GetCell(pos);
                    if (cell?.elementInfo == null) continue;
                    if (cell.elementInfo.powerUpType == ElementPowerUpType.Cauldron) continue;
                    ElementData destroyedElementData = cell.elementInfo.elementData;

                    GridElement matchedElement = GetElementAt(pos);


                    TriggerCellFeatureMatchedOverAt(pos);

                    ProcessAdjacentFeatureMatchEffects(pos, cell, matchedElement, allMatchedPositions, adjacentFeatureProcessed, adjacentOffsets);

                    NotifyElementCleared(pos);
                    cell.elementInfo = null;
                    GridElement element = matchedElement;
                    if (element != null)
                    {
                        if (mergeTarget.HasValue) StartCoroutine(MergeElementIntoTarget(element, mergeTarget.Value));
                        else StartCoroutine(element.DestroyElement());
                    }

                    ProcessAdjacentWallAndHiddenEffects(pos, destroyedElementData, adjacentOffsets, wallsToBreak, hiddenToReveal);

                    cleared.Add(pos);
                }

                yield return new WaitForSeconds(cm.matchClearDelay);
            }

            foreach (Vector2Int rp in hiddenToReveal) RevealHiddenElement(rp);
            yield return StartCoroutine(BreakWallsSimultaneous(wallsToBreak));
        }

        private void ProcessAdjacentFeatureMatchEffects(
            Vector2Int originPos,
            GridCell originCell,
            GridElement originElement,
            HashSet<Vector2Int> allMatchedPositions,
            HashSet<Vector2Int> adjacentFeatureProcessed,
            Vector2Int[] adjacentOffsets)
        {
            for (int i = 0; i < adjacentOffsets.Length; i++)
            {
                Vector2Int adjacentPos = originPos + adjacentOffsets[i];
                if (!adjacentFeatureProcessed.Add(adjacentPos))
                    continue;

                if (allMatchedPositions.Contains(adjacentPos))
                    continue;

                GridCell adjacentCell = GetCell(adjacentPos);
                if (adjacentCell?.cellFeature == null)
                    continue;

                adjacentCell.cellFeature.OnElementMatchedAdjacentToTheCell(adjacentCell, originCell, originElement);
                RefreshCellFeatureVisual(adjacentPos);
            }
        }

        private void ProcessAdjacentWallAndHiddenEffects(
            Vector2Int originPos,
            ElementData destroyedElementData,
            Vector2Int[] adjacentOffsets,
            HashSet<Vector2Int> wallsToBreak,
            HashSet<Vector2Int> hiddenToReveal)
        {
            for (int i = 0; i < adjacentOffsets.Length; i++)
            {
                Vector2Int adjacentPos = originPos + adjacentOffsets[i];
                GridCell adjacentCell = GetCell(adjacentPos);
                if (adjacentCell == null)
                    continue;

                if (adjacentCell.cellType == CellType.BreakableWall && CanBreakWallWithElement(adjacentCell, destroyedElementData))
                    wallsToBreak.Add(adjacentPos);

                if (adjacentCell.cellType == CellType.Normal && adjacentCell.elementInfo != null && adjacentCell.elementInfo.isHidden)
                    hiddenToReveal.Add(adjacentPos);
            }
        }

        private void DamageGlassAtPosition(Vector2Int pos)
        {
            HashSet<GlassGroupId> groupsToDamage = new HashSet<GlassGroupId>();
            CollectGlassGroupAt(pos, groupsToDamage);
            DamageGlassGroupsForMatch(groupsToDamage);
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

            GridHelper.SetEmission(element, 1.5f);

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

            EventManager.TriggerEvent(GameEvent.HIDDEN_BOX_REVEALED, new EventParam(
                vectorList: new Vector3[] { new Vector3(pos.x, pos.y, 0f) },
                paramScriptable: cell.elementInfo.elementData
            ));
        }

        public IEnumerator BreakWallAt(Vector2Int wallPos)
        {
            GridCell cell = GetCell(wallPos);
            if (cell == null || cell.cellType != CellType.BreakableWall) yield break;

            cell.cellHealth = Mathf.Max(1, cell.cellHealth);
            cell.cellHealth--;

            if (generatedTiles.TryGetValue(wallPos, out GridCellController wallTile) && wallTile != null)
            {
                Transform wallParent = wallTile.transform.parent;
                Vector3 wallPosition = wallTile.transform.position;
                Quaternion wallRotation = wallTile.transform.rotation;
                Vector3 wallScale = wallTile.transform.localScale;

                if (wallTile is BreakableWall breakable)
                    breakable.InitializeHealth(cell.cellHealth);

                if (cell.cellHealth <= 0)
                {
                    if (wallTile is BreakableWall breakableWall)
                        yield return StartCoroutine(breakableWall.WallBreak());

                    if (tileGenerationData != null && tileGenerationData.normalCell != null)
                    {
                        GridCellController normalTile = Instantiate(tileGenerationData.normalCell, wallPosition, wallRotation, wallParent);
                        normalTile.transform.localScale = wallScale;
                        normalTile.Bind(wallPos);
                        generatedTiles[wallPos] = normalTile;
                    }

                    if (wallTile != null) Destroy(wallTile.gameObject);

                    cell.cellType = CellType.Normal;
                    cell.cellHealth = 0;

                    if (tileGenerationData != null)
                    {
                        tileGenerationData.RefreshTileSprites(generatedTiles, gridCells, wallPos);
                    }

                    EventManager.TriggerEvent(GameEvent.BREAKABLE_WALL_DESTROYED,
                        new EventParam(vectorList: new Vector3[] { new Vector3(wallPos.x, wallPos.y, 0f) }));
                    yield break;
                }

                yield break;
            }

            if (cell.cellHealth <= 0)
            {
                cell.cellType = CellType.Normal;
                cell.cellHealth = 0;
                EventManager.TriggerEvent(GameEvent.BREAKABLE_WALL_DESTROYED,
                    new EventParam(vectorList: new Vector3[] { new Vector3(wallPos.x, wallPos.y, 0f) }));
            }
        }

        private bool CanBreakWallWithElement(GridCell wallCell, ElementData destroyedElementData)
        {
            if (wallCell == null || wallCell.cellType != CellType.BreakableWall)
                return false;

            ElementData requiredElement = wallCell.breakableWallElementCondition;
            if (requiredElement == null)
                return true;

            return destroyedElementData == requiredElement;
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

            HashSet<GlassGroupId> glassGroupsToDamageByDrop = new HashSet<GlassGroupId>();

            ConstantManager cm = GameManager.Instance != null ? GameManager.Instance.constantManager : null;
            float fallSpeed = cm != null ? cm.elementFallSpeed : 3.3f;

            EnsureGridCells();
            List<ElementData> elementPool = BuildElementPool();
            if (elementPool.Count == 0)
            {
                // Fallback: scan instantiated elements
                foreach (GridElement el in generatedElements)
                    if (el != null && el.elementInfo != null && el.elementInfo.elementData != null &&
                        !IsCauldronData(el.elementInfo.elementData) &&
                        !HasBehavior(el.elementInfo.elementData, ElementData.ElementBehaviorFlags.NonShuffleable) &&
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
                int bottomMostNormalY = GetBottomMostNormalCellY(x);

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

                    if (IsGarbageBagData(movingInfo.elementData) && readY == bottomMostNormalY)
                    {
                        TriggerCellFeatureMatchedOverAt(readPos);
                        NotifyGarbageBagCleaned(readPos, movingInfo.elementData);
                        readCell.elementInfo = null;

                        if (generatedTiles.TryGetValue(readPos, out GridCellController bagTile) && bagTile != null)
                        {
                            GridElement bagElement = bagTile.GetComponentInChildren<GridElement>();
                            if (bagElement != null)
                            {
                                bagElement.transform.SetParent(null, true);
                                float dropDist = Mathf.Max(0.5f, bagTile.transform.localScale.y);
                                float dropDur = 0f;
                                gravitySeq.Join(bagElement.transform.DOMove(bagElement.transform.position + Vector3.down * dropDist, dropDur).SetEase(Ease.InQuad));
                                gravitySeq.Join(bagElement.transform.DOScale(0.92f, dropDur).SetEase(Ease.InQuad));
                                generatedElements.Remove(bagElement);
                                Destroy(bagElement.gameObject, dropDur + 0.05f);
                                hasTween = true;
                            }
                        }

                        continue;
                    }

                        if (highestExisting == -1) highestExisting = readIndex;

                        int targetY = playableRows[writeIndex];
                        Vector2Int targetPos = new Vector2Int(x, targetY);
                        GridCell targetCell = GetCell(targetPos);
                        if (targetCell == null) continue;

                        if (IsGarbageBagData(movingInfo.elementData) && targetY == bottomMostNormalY)
                        {
                            TriggerCellFeatureMatchedOverAt(readPos);
                            NotifyGarbageBagCleaned(targetPos, movingInfo.elementData);
                            readCell.elementInfo = null;
                            targetCell.elementInfo = null;

                            if (generatedTiles.TryGetValue(readPos, out GridCellController fromTile) &&
                                generatedTiles.TryGetValue(targetPos, out GridCellController toTile) &&
                                fromTile != null && toTile != null)
                            {
                                GridElement bagElement = fromTile.GetComponentInChildren<GridElement>();
                                if (bagElement != null)
                                {
                                    bagElement.transform.SetParent(null, true);
                                    float dropDist = Mathf.Max(0.5f, toTile.transform.localScale.y);
                                    Vector3 endWorld = toTile.transform.position + Vector3.down * dropDist;
                                    float regularFallDist = Vector3.Distance(bagElement.transform.position, toTile.transform.position);
                                    float dropDur = fallSpeed > 0f ? regularFallDist / fallSpeed : 0.2f;
                                    gravitySeq.Join(bagElement.transform.DOMove(endWorld, dropDur).SetEase(Ease.InQuad));
                                    gravitySeq.Join(bagElement.transform.DOScale(0.92f, dropDur).SetEase(Ease.InQuad));
                                    generatedElements.Remove(bagElement);
                                    Destroy(bagElement.gameObject, dropDur + 0.05f);
                                    hasTween = true;
                                }
                            }

                            continue;
                        }

                        if (targetPos != readPos)
                        {
                            readCell.elementInfo = null;
                            targetCell.elementInfo = movingInfo;
                            CollectGlassGroupBelowDropTarget(targetPos, glassGroupsToDamageByDrop);

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
                        CollectGlassGroupBelowDropTarget(targetPos, glassGroupsToDamageByDrop);

                        if (generatedTiles.TryGetValue(targetPos, out GridCellController targetTile) && targetTile != null)
                        {
                            int stackOffset = spawnBase + (writeIndex - emptyIdx + 1);
                            Vector3 spawnWorldPos = targetTile.transform.position + Vector3.up * stackOffset;
                            GridElement newEl = Instantiate(gridElementPrefab, spawnWorldPos, Quaternion.identity); // TODO: Use pooling
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

            bool brokeGlassDuringGravity = DamageGlassGroupsForMatch(glassGroupsToDamageByDrop);
            if (brokeGlassDuringGravity)
            {
                yield return StartCoroutine(ApplyGravity());
                yield break;
            }

            RunOccupancySanityPass();

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

        private void RunOccupancySanityPass()
        {
            EnsureGridCells();
            generatedElements.RemoveAll(e => e == null);

            HashSet<GridElement> validElements = new HashSet<GridElement>();

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    GridCell cell = GetCell(pos);
                    if (cell == null || !generatedTiles.TryGetValue(pos, out GridCellController tile) || tile == null)
                        continue;

                    bool acceptsElements = cell.cellFeature == null || cell.cellFeature.AcceptElements;
                    bool shouldHaveElement = cell.cellType == CellType.Normal && acceptsElements && cell.elementInfo?.elementData != null;

                    GridElement[] tileElements = tile.GetComponentsInChildren<GridElement>(true);
                    GridElement selected = null;

                    for (int i = 0; i < tileElements.Length; i++)
                    {
                        GridElement element = tileElements[i];
                        if (element == null)
                            continue;

                        if (selected == null)
                        {
                            selected = element;
                            continue;
                        }

                        element.transform.DOKill();
                        generatedElements.Remove(element);
                        Destroy(element.gameObject);
                    }

                    if (!shouldHaveElement)
                    {
                        if (selected != null)
                        {
                            selected.transform.DOKill();
                            generatedElements.Remove(selected);
                            Destroy(selected.gameObject);
                        }

                        continue;
                    }

                    if (selected == null)
                    {
                        GridElement created = Instantiate(gridElementPrefab, tile.transform.position, Quaternion.identity, tile.transform);
                        created.elementInfo = cell.elementInfo;
                        created.InitElement(this, cell.elementInfo);
                        generatedElements.Add(created);
                        validElements.Add(created);
                        continue;
                    }

                    selected.transform.DOKill();
                    selected.transform.SetParent(tile.transform, true);

                    if (selected.elementInfo != cell.elementInfo)
                    {
                        selected.elementInfo = cell.elementInfo;
                        selected.InitElement(this, cell.elementInfo);
                    }

                    if (!generatedElements.Contains(selected))
                        generatedElements.Add(selected);

                    validElements.Add(selected);
                }
            }

            for (int i = generatedElements.Count - 1; i >= 0; i--)
            {
                GridElement element = generatedElements[i];
                if (element == null)
                {
                    generatedElements.RemoveAt(i);
                    continue;
                }

                if (validElements.Contains(element))
                    continue;

                element.transform.DOKill();
                Destroy(element.gameObject);
                generatedElements.RemoveAt(i);
            }
        }

        public IEnumerator TriggerCauldronExplosion(Vector2Int cauldronPos)
        {
            GridCell cauldronCell = GetCell(cauldronPos);
            if (cauldronCell?.elementInfo == null || cauldronCell.elementInfo.powerUpType != ElementPowerUpType.Cauldron)
                yield break;

            GridElement cauldronElement = GetElementAt(cauldronPos);
            Vector3 center = GetElementCoverageCenterWorld(cauldronPos, cauldronCell.elementInfo.elementData);

            ParticleSystem customExplosion = cauldronCell.elementInfo.elementData != null
                ? cauldronCell.elementInfo.elementData.cauldronExplosionParticle
                : null;
            if (customExplosion != null)
            {
                ParticleSystem fx = Instantiate(customExplosion, center, Quaternion.identity);
                fx.Play();
                Destroy(fx.gameObject, fx.main.duration + fx.main.startLifetime.constantMax + 0.2f);
            }

            TriggerCellFeatureMatchedOverAt(cauldronPos);
            cauldronCell.elementInfo = null;

            Sequence clearSeq = DOTween.Sequence();
            bool hasTween = false;

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Vector2Int pos = new Vector2Int(x, y);
                    if (pos == cauldronPos)
                        continue;

                    GridCell cell = GetCell(pos);
                    if (cell == null)
                        continue;

                    if (cell.cellType == CellType.Normal && cell.elementInfo != null)
                    {
                        TriggerCellFeatureMatchedOverAt(pos);

                        if (TryRevealHiddenBoxAt(pos))
                            continue;

                    if (HasBehavior(cell.elementInfo.elementData, ElementData.ElementBehaviorFlags.ImmuneToClear))
                            continue;

                        if (PowerUpHandler.IsSpecialPowerUp(cell.elementInfo.powerUpType))
                        {
                            StartCoroutine(powerUpHandler.ActivateAt(pos, null));
                            continue;
                        }

                        NotifyElementCleared(pos);
                        cell.elementInfo = null;

                        GridElement element = GetElementAt(pos);
                        if (element != null)
                        {
                            float delay = Vector2Int.Distance(cauldronPos, pos) * 0.02f;
                            Vector3 start = element.transform.position;
                            Vector3 arcPeak = GetCauldronArcPeak(start, center);
                            element.transform.DOKill();
                            clearSeq.Join(element.transform
                                .DOPath(new[] { arcPeak, center }, 0.26f, PathType.CatmullRom)
                                .SetDelay(delay)
                                .SetEase(Ease.InQuad));
                            clearSeq.Join(element.transform.DOScale(0f, 0.2f).SetDelay(delay).SetEase(Ease.InBack));
                            hasTween = true;
                        }
                    }
                }
            }

            if (cauldronElement != null)
            {
                cauldronElement.transform.DOKill();
                clearSeq.Join(cauldronElement.transform.DOScale(0f, 0.22f).SetEase(Ease.InBack));
                hasTween = true;
            }

            if (hasTween)
                yield return clearSeq.WaitForCompletion();

            List<GridElement> activeElements = GetAllActiveElements();
            for (int i = 0; i < activeElements.Count; i++)
            {
                if (activeElements[i] != null)
                    Destroy(activeElements[i].gameObject);
            }
            generatedElements.Clear();

            EventManager.TriggerEvent(GameEvent.SPECIAL_ELEMENT_ACTIVATED);
        }

        public bool TryRevealHiddenBoxAt(Vector2Int pos)
        {
            GridCell cell = GetCell(pos);
            if (cell == null || cell.cellType != CellType.Normal || cell.elementInfo == null || !cell.elementInfo.isHidden)
                return false;

            RevealHiddenElement(pos);
            return true;
        }

        private Vector3 GetElementCoverageCenterWorld(Vector2Int anchorPos, ElementData data)
        {
            Vector2Int coverage = GetGridCoverage(data);
            Vector2Int endPos = anchorPos + new Vector2Int(coverage.x - 1, coverage.y - 1);

            if (IsInsideGrid(endPos))
            {
                Vector3 startWorld = GetCellPositionInGrid(anchorPos);
                Vector3 endWorld = GetCellPositionInGrid(endPos);
                return (startWorld + endWorld) * 0.5f;
            }

            return GetWorldPosition(anchorPos);
        }

        private static Vector3 GetCauldronArcPeak(Vector3 start, Vector3 target)
        {
            float elementMaxHeight = GameManager.Instance.constantManager.elementToCauldronMaxHeight;
            float elementHeightMultiplier = GameManager.Instance.constantManager.elementToCauldronHeightMultiplier;

            Vector3 midpoint = Vector3.Lerp(start, target, 0.5f);
            float distance = Vector3.Distance(start, target);
            float arcHeight = Mathf.Max(elementMaxHeight, distance * elementHeightMultiplier);
            midpoint.y += arcHeight;
            return midpoint;
        }

        // ------------------------------------------------------------------
        //  Shared internal helpers
        // ------------------------------------------------------------------
        private bool IsCauldronCell(GridCell cell)
        {
            if (cell?.elementInfo == null)
                return false;

            NormalizeSpecialElementInfo(cell);
            return cell.elementInfo.powerUpType == ElementPowerUpType.Cauldron;
        }

        private static bool HasBehavior(ElementData data, ElementData.ElementBehaviorFlags flag)
        {
            return data != null && data.HasBehavior(flag);
        }

        private static bool IsGarbageBagData(ElementData data)
        {
            GameManager gm = GameManager.Instance;
            return gm != null && data != null && gm.garbageBagElementData == data;
        }

        private static bool IsPowerGeneratorData(ElementData data)
        {
            GameManager gm = GameManager.Instance;
            return gm != null && data != null && gm.powerGeneratorElementData == data;
        }

        private static bool IsPowerOutletData(ElementData data)
        {
            GameManager gm = GameManager.Instance;
            return gm != null && data != null && gm.powerOutletElementData == data;
        }

        private bool IsCauldronData(ElementData data)
        {
            GameManager gm = GameManager.Instance;
            return gm != null && data != null && gm.cauldronElementData == data;
        }

        private static bool IsGarbageBagCell(GridCell cell)
        {
            return cell != null && cell.cellType == CellType.Normal && cell.elementInfo != null && IsGarbageBagData(cell.elementInfo.elementData);
        }

        private static bool IsPowerGeneratorCell(GridCell cell)
        {
            return cell != null && cell.cellType == CellType.Normal && cell.elementInfo != null && IsPowerGeneratorData(cell.elementInfo.elementData);
        }

        private static bool IsNonSwappableCell(GridCell cell)
        {
            return cell != null && cell.cellType == CellType.Normal && cell.elementInfo != null && HasBehavior(cell.elementInfo.elementData, ElementData.ElementBehaviorFlags.NonSwappable);
        }

        private static void NotifyGarbageBagCleaned(Vector2Int pos, ElementData data)
        {
            EventManager.TriggerEvent(GameEvent.GARBAGE_CLEANED, new EventParam(
                vectorList: new Vector3[] { new Vector3(pos.x, pos.y, 0f) },
                paramScriptable: data
            ));
        }

        private void NormalizeSpecialElementInfo(GridCell cell)
        {
            if (cell?.elementInfo == null)
                return;

            ElementData data = cell.elementInfo.elementData;
            if (data == null)
                return;

            if (IsCauldronData(data))
            {
                cell.elementInfo.powerUpType = ElementPowerUpType.Cauldron;
                if (cell.elementInfo.cauldronProgress < 0)
                    cell.elementInfo.cauldronProgress = 0;
            }
            else if (cell.elementInfo.powerUpType == ElementPowerUpType.Cauldron)
            {
                cell.elementInfo.powerUpType = ElementPowerUpType.None;
                cell.elementInfo.cauldronProgress = 0;
            }
        }

        private List<ElementData> BuildElementPool()
        {
            List<ElementData> configuredPool = GetConfiguredElementPool();
            configuredPool.RemoveAll(d => d == null || IsCauldronData(d) || HasBehavior(d, ElementData.ElementBehaviorFlags.NonShuffleable) || IsMultiCellData(d));
            if (configuredPool.Count > 0)
            {
                return configuredPool;
            }

            List<ElementData> pool = new List<ElementData>();
            for (int x = 0; x < gridSize.x; x++)
                for (int y = 0; y < gridSize.y; y++)
                {
                    GridCell cell = GetCell(new Vector2Int(x, y));
                    ElementData data = cell?.elementInfo?.elementData;
                    if (data != null && !IsCauldronData(data) && !HasBehavior(data, ElementData.ElementBehaviorFlags.NonShuffleable) && !IsMultiCellData(data) && cell.elementInfo.powerUpType == ElementPowerUpType.None && !pool.Contains(data))
                        pool.Add(data);
                }
            return pool;
        }

        private int GetBottomMostNormalCellY(int x)
        {
            for (int y = gridSize.y - 1; y >= 0; y--)
            {
                Vector2Int pos = new Vector2Int(x, y);
                GridCell cell = GetCell(pos);
                if (cell == null || cell.cellType != CellType.Normal)
                    continue;

                bool acceptsElements = cell.cellFeature == null || cell.cellFeature.AcceptElements;
                if (!acceptsElements)
                    continue;

                if (cell.cellFeature is GlassFeature)
                    continue;

                if (IsCellCoveredByMultiCellElement(pos))
                    continue;

                if (IsCauldronCell(cell))
                    continue;

                if (HasBehavior(cell.elementInfo?.elementData, ElementData.ElementBehaviorFlags.PassThrough))
                    continue;

                return y;
            }

            return -1;
        }

        private List<List<int>> BuildColumnSections(int x)
        {
            List<List<int>> sections = new List<List<int>>();
            List<int> current = new List<int>();
            for (int y = 0; y < gridSize.y; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                GridCell cell = GetCell(pos);
                if (cell != null && cell.cellType == CellType.Normal)
                {
                    bool acceptsElements = cell.cellFeature == null || cell.cellFeature.AcceptElements;
                    if (!acceptsElements)
                    {
                        if (current.Count > 0) { sections.Add(new List<int>(current)); current.Clear(); }
                        continue;
                    }

                    if (cell.cellFeature is GlassFeature)
                        continue;

                    if (IsCellCoveredByMultiCellElement(pos))
                        continue;

                    if (!IsCauldronCell(cell) && !HasBehavior(cell.elementInfo?.elementData, ElementData.ElementBehaviorFlags.PassThrough) && acceptsElements)
                        current.Add(y);
                }
                else
                {
                    if (current.Count > 0) { sections.Add(new List<int>(current)); current.Clear(); }
                }
            }
            if (current.Count > 0) sections.Add(current);
            return sections;
        }

        private bool IsMatchableForMove(GridCell cell)
        {
            ElementData elementData = cell?.elementInfo?.elementData;
            return cell != null &&
                   cell.cellType == CellType.Normal &&
                   !(cell.cellFeature is GlassFeature) &&
                   cell.elementInfo != null &&
                   elementData != null &&
                   !IsPowerGeneratorData(elementData) &&
                   !IsPowerOutletData(elementData) &&
                   !IsMultiCellData(elementData) &&
                    !HasBehavior(elementData, ElementData.ElementBehaviorFlags.NonMatchable) &&
                    !HasBehavior(elementData, ElementData.ElementBehaviorFlags.NonSwappable) &&
                   !cell.elementInfo.isHidden &&
                   cell.elementInfo.powerUpType == ElementPowerUpType.None;
        }

        private static Vector2Int GetGridCoverage(ElementData data)
        {
            if (data == null)
                return Vector2Int.one;

            return new Vector2Int(Mathf.Max(1, data.gridCoverage.x), Mathf.Max(1, data.gridCoverage.y));
        }

        private static bool IsMultiCellData(ElementData data)
        {
            Vector2Int coverage = GetGridCoverage(data);
            return coverage.x > 1 || coverage.y > 1;
        }

        private static int GetManhattanDistanceToCoverage(Vector2Int pos, Vector2Int coverageStart, Vector2Int coverage)
        {
            int minX = coverageStart.x;
            int maxX = coverageStart.x + coverage.x - 1;
            int minY = coverageStart.y;
            int maxY = coverageStart.y + coverage.y - 1;

            int dx = pos.x < minX ? minX - pos.x : (pos.x > maxX ? pos.x - maxX : 0);
            int dy = pos.y < minY ? minY - pos.y : (pos.y > maxY ? pos.y - maxY : 0);
            return dx + dy;
        }

        private bool IsInsideGrid(Vector2Int pos)
        {
            return pos.x >= 0 && pos.y >= 0 && pos.x < gridSize.x && pos.y < gridSize.y;
        }

        private bool IsInsideCoverage(Vector2Int pos, Vector2Int anchorPos, Vector2Int coverage)
        {
            return pos.x >= anchorPos.x && pos.x < anchorPos.x + coverage.x &&
                   pos.y >= anchorPos.y && pos.y < anchorPos.y + coverage.y;
        }

        private bool IsMultiCellElementAnchor(Vector2Int pos)
        {
            GridCell cell = GetCell(pos);
            return cell != null && cell.cellType == CellType.Normal && cell.elementInfo?.elementData != null && IsMultiCellData(cell.elementInfo.elementData);
        }

        private bool IsCellCoveredByMultiCellElement(Vector2Int pos)
        {
            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    GridCell anchorCell = GetCell(new Vector2Int(x, y));
                    ElementData data = anchorCell?.elementInfo?.elementData;
                    if (anchorCell == null || anchorCell.cellType != CellType.Normal || data == null || !IsMultiCellData(data))
                        continue;

                    Vector2Int anchorPos = new Vector2Int(x, y);
                    Vector2Int coverage = GetGridCoverage(data);
                    if (IsInsideCoverage(pos, anchorPos, coverage))
                        return true;
                }
            }

            return false;
        }

        private void NormalizeMultiCellLayout()
        {
            bool[,] occupied = new bool[gridSize.x, gridSize.y];

            for (int y = 0; y < gridSize.y; y++)
            {
                for (int x = 0; x < gridSize.x; x++)
                {
                    Vector2Int anchorPos = new Vector2Int(x, y);
                    GridCell anchorCell = GetCell(anchorPos);
                    if (anchorCell == null)
                        continue;

                    if (anchorCell.cellType != CellType.Normal)
                    {
                        anchorCell.elementInfo = null;
                        continue;
                    }

                    if (anchorCell.elementInfo == null || anchorCell.elementInfo.elementData == null)
                        continue;

                    Vector2Int coverage = GetGridCoverage(anchorCell.elementInfo.elementData);
                    bool isValid = true;

                    for (int dx = 0; dx < coverage.x && isValid; dx++)
                    {
                        for (int dy = 0; dy < coverage.y; dy++)
                        {
                            Vector2Int coveredPos = new Vector2Int(x + dx, y + dy);
                            if (!IsInsideGrid(coveredPos))
                            {
                                isValid = false;
                                break;
                            }

                            GridCell coveredCell = GetCell(coveredPos);
                            if (coveredCell == null || coveredCell.cellType != CellType.Normal || occupied[coveredPos.x, coveredPos.y])
                            {
                                isValid = false;
                                break;
                            }
                        }
                    }

                    if (!isValid)
                    {
                        anchorCell.elementInfo = null;
                        continue;
                    }

                    for (int dx = 0; dx < coverage.x; dx++)
                    {
                        for (int dy = 0; dy < coverage.y; dy++)
                        {
                            Vector2Int coveredPos = new Vector2Int(x + dx, y + dy);
                            occupied[coveredPos.x, coveredPos.y] = true;
                            if (dx != 0 || dy != 0)
                            {
                                GridCell coveredCell = GetCell(coveredPos);
                                if (coveredCell != null)
                                    coveredCell.elementInfo = null;
                            }
                        }
                    }
                }
            }
        }

        public bool HasAnyPossibleMove()
        {
            Vector2Int[] dirs = { Vector2Int.right, Vector2Int.up };

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Vector2Int aPos = new Vector2Int(x, y);
                    GridCell aCell = GetCell(aPos);
                    if (!IsMatchableForMove(aCell)) continue;

                    for (int i = 0; i < dirs.Length; i++)
                    {
                        Vector2Int bPos = aPos + dirs[i];
                        GridCell bCell = GetCell(bPos);
                        if (!IsMatchableForMove(bCell)) continue;

                        ElementData aData = aCell.elementInfo.elementData;
                        ElementData bData = bCell.elementInfo.elementData;
                        if (aData == bData) continue;

                        GridElementInfo aInfo = aCell.elementInfo;
                        GridElementInfo bInfo = bCell.elementInfo;
                        aCell.elementInfo = bInfo;
                        bCell.elementInfo = aInfo;

                        bool createsMatch = CreatesMatchAt(aPos) || CreatesMatchAt(bPos);

                        aCell.elementInfo = aInfo;
                        bCell.elementInfo = bInfo;

                        if (createsMatch) return true;
                    }
                }
            }

            return false;
        }

        public bool TryGetRandomPossibleMove(out Vector2Int mover, out Vector2Int target, out List<Vector2Int> matchedGroup)
        {
            mover = default;
            target = default;
            matchedGroup = null;

            Vector2Int[] dirs = { Vector2Int.right, Vector2Int.up };
            List<(Vector2Int mover, Vector2Int target, List<Vector2Int> group)> candidates = new List<(Vector2Int, Vector2Int, List<Vector2Int>)>();

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Vector2Int aPos = new Vector2Int(x, y);
                    GridCell aCell = GetCell(aPos);
                    if (!IsMatchableForMove(aCell)) continue;

                    for (int i = 0; i < dirs.Length; i++)
                    {
                        Vector2Int bPos = aPos + dirs[i];
                        GridCell bCell = GetCell(bPos);
                        if (!IsMatchableForMove(bCell)) continue;

                        ElementData aData = aCell.elementInfo.elementData;
                        ElementData bData = bCell.elementInfo.elementData;
                        if (aData == bData) continue;

                        GridElementInfo aInfo = aCell.elementInfo;
                        GridElementInfo bInfo = bCell.elementInfo;
                        aCell.elementInfo = bInfo;
                        bCell.elementInfo = aInfo;

                        bool aIsMover = CreatesMatchAt(bPos);
                        bool bIsMover = CreatesMatchAt(aPos);

                        if (aIsMover)
                        {
                            candidates.Add((aPos, bPos, BuildMatchedGroupForSwap(aPos, bPos)));
                        }

                        if (bIsMover)
                        {
                            candidates.Add((bPos, aPos, BuildMatchedGroupForSwap(bPos, aPos)));
                        }

                        aCell.elementInfo = aInfo;
                        bCell.elementInfo = bInfo;
                    }
                }
            }

            if (candidates.Count == 0)
            {
                return false;
            }

            var selected = candidates[Random.Range(0, candidates.Count)];
            mover = selected.mover;
            target = selected.target;
            matchedGroup = selected.group;
            return true;
        }

        private List<Vector2Int> BuildMatchedGroupForSwap(Vector2Int moverPos, Vector2Int targetPos)
        {
            HashSet<Vector2Int> group = new HashSet<Vector2Int>();

            GridCell targetCell = GetCell(targetPos);
            if (targetCell != null && IsMatchableForMove(targetCell))
            {
                ElementData movedData = targetCell.elementInfo.elementData;
                CollectLineMatch(targetPos, movedData, group);
                CollectSquareMatch(targetPos, movedData, group);
            }

            GridCell moverCell = GetCell(moverPos);
            if (moverCell != null && IsMatchableForMove(moverCell))
            {
                ElementData secondData = moverCell.elementInfo.elementData;
                CollectLineMatch(moverPos, secondData, group);
                CollectSquareMatch(moverPos, secondData, group);
            }

            return new List<Vector2Int>(group);
        }

        private void CollectLineMatch(Vector2Int pos, ElementData data, HashSet<Vector2Int> group)
        {
            List<Vector2Int> horizontal = new List<Vector2Int> { pos };
            Vector2Int cur = pos + Vector2Int.left;
            while (true)
            {
                GridCell c = GetCell(cur);
                if (!IsMatchableForMove(c) || c.elementInfo.elementData != data) break;
                horizontal.Add(cur);
                cur += Vector2Int.left;
            }

            cur = pos + Vector2Int.right;
            while (true)
            {
                GridCell c = GetCell(cur);
                if (!IsMatchableForMove(c) || c.elementInfo.elementData != data) break;
                horizontal.Add(cur);
                cur += Vector2Int.right;
            }

            if (horizontal.Count >= 3)
            {
                for (int i = 0; i < horizontal.Count; i++) group.Add(horizontal[i]);
            }

            List<Vector2Int> vertical = new List<Vector2Int> { pos };
            cur = pos + Vector2Int.down;
            while (true)
            {
                GridCell c = GetCell(cur);
                if (!IsMatchableForMove(c) || c.elementInfo.elementData != data) break;
                vertical.Add(cur);
                cur += Vector2Int.down;
            }

            cur = pos + Vector2Int.up;
            while (true)
            {
                GridCell c = GetCell(cur);
                if (!IsMatchableForMove(c) || c.elementInfo.elementData != data) break;
                vertical.Add(cur);
                cur += Vector2Int.up;
            }

            if (vertical.Count >= 3)
            {
                for (int i = 0; i < vertical.Count; i++) group.Add(vertical[i]);
            }
        }

        private void CollectSquareMatch(Vector2Int pos, ElementData data, HashSet<Vector2Int> group)
        {
            Vector2Int[] origins =
            {
                pos,
                pos + Vector2Int.left,
                pos + Vector2Int.down,
                pos + Vector2Int.left + Vector2Int.down
            };

            for (int i = 0; i < origins.Length; i++)
            {
                Vector2Int o = origins[i];
                if (!IsSquareAt(o, data)) continue;

                group.Add(o);
                group.Add(o + Vector2Int.right);
                group.Add(o + Vector2Int.up);
                group.Add(o + Vector2Int.right + Vector2Int.up);
            }
        }

        private bool CreatesMatchAt(Vector2Int pos)
        {
            return CreatesLineMatchAt(pos) || CreatesSquareMatchAt(pos);
        }

        private bool CreatesLineMatchAt(Vector2Int pos)
        {
            GridCell center = GetCell(pos);
            if (!IsMatchableForMove(center)) return false;

            ElementData data = center.elementInfo.elementData;

            int horiz = 1;
            for (int x = pos.x - 1; x >= 0; x--)
            {
                GridCell c = GetCell(new Vector2Int(x, pos.y));
                if (!IsMatchableForMove(c) || c.elementInfo.elementData != data) break;
                horiz++;
            }
            for (int x = pos.x + 1; x < gridSize.x; x++)
            {
                GridCell c = GetCell(new Vector2Int(x, pos.y));
                if (!IsMatchableForMove(c) || c.elementInfo.elementData != data) break;
                horiz++;
            }
            if (horiz >= 3) return true;

            int vert = 1;
            for (int y = pos.y - 1; y >= 0; y--)
            {
                GridCell c = GetCell(new Vector2Int(pos.x, y));
                if (!IsMatchableForMove(c) || c.elementInfo.elementData != data) break;
                vert++;
            }
            for (int y = pos.y + 1; y < gridSize.y; y++)
            {
                GridCell c = GetCell(new Vector2Int(pos.x, y));
                if (!IsMatchableForMove(c) || c.elementInfo.elementData != data) break;
                vert++;
            }

            return vert >= 3;
        }

        private bool CreatesSquareMatchAt(Vector2Int pos)
        {
            GridCell center = GetCell(pos);
            if (!IsMatchableForMove(center)) return false;

            ElementData data = center.elementInfo.elementData;
            Vector2Int[] origins =
            {
                pos,
                pos + Vector2Int.left,
                pos + Vector2Int.down,
                pos + Vector2Int.left + Vector2Int.down
            };

            for (int i = 0; i < origins.Length; i++)
            {
                if (IsSquareAt(origins[i], data)) return true;
            }

            return false;
        }

        private bool IsSquareAt(Vector2Int origin, ElementData data)
        {
            GridCell c00 = GetCell(origin);
            GridCell c10 = GetCell(origin + Vector2Int.right);
            GridCell c01 = GetCell(origin + Vector2Int.up);
            GridCell c11 = GetCell(origin + Vector2Int.right + Vector2Int.up);

            return IsMatchableForMove(c00) && c00.elementInfo.elementData == data
                && IsMatchableForMove(c10) && c10.elementInfo.elementData == data
                && IsMatchableForMove(c01) && c01.elementInfo.elementData == data
                && IsMatchableForMove(c11) && c11.elementInfo.elementData == data;
        }

        private void EnsureAtLeastOneMoveAvailable(List<ElementData> elementPool)
        {
            if (HasAnyPossibleMove()) return;
            if (elementPool == null || elementPool.Count < 2) return;

            Vector2Int[][] patterns =
            {
                new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(2, 1) },
                new[] { new Vector2Int(0, 0), new Vector2Int(1, 0), new Vector2Int(2, 0), new Vector2Int(2, -1) },
                new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(1, 2) },
                new[] { new Vector2Int(0, 0), new Vector2Int(0, 1), new Vector2Int(0, 2), new Vector2Int(-1, 2) }
            };

            for (int x = 0; x < gridSize.x; x++)
            {
                for (int y = 0; y < gridSize.y; y++)
                {
                    Vector2Int anchor = new Vector2Int(x, y);

                    for (int p = 0; p < patterns.Length; p++)
                    {
                        Vector2Int p0 = anchor + patterns[p][0];
                        Vector2Int p1 = anchor + patterns[p][1];
                        Vector2Int p2 = anchor + patterns[p][2];
                        Vector2Int p3 = anchor + patterns[p][3];

                        GridCell c0 = GetCell(p0);
                        GridCell c1 = GetCell(p1);
                        GridCell c2 = GetCell(p2);
                        GridCell c3 = GetCell(p3);

                        if (!IsMatchableForMove(c0) || !IsMatchableForMove(c1) || !IsMatchableForMove(c2) || !IsMatchableForMove(c3))
                            continue;

                        ElementData old0 = c0.elementInfo.elementData;
                        ElementData old1 = c1.elementInfo.elementData;
                        ElementData old2 = c2.elementInfo.elementData;
                        ElementData old3 = c3.elementInfo.elementData;

                        ElementData matchData = elementPool[Random.Range(0, elementPool.Count)];
                        ElementData blockerData = matchData;
                        for (int i = 0; i < elementPool.Count; i++)
                        {
                            if (elementPool[i] != matchData)
                            {
                                blockerData = elementPool[i];
                                break;
                            }
                        }

                        c0.elementInfo.elementData = matchData;
                        c1.elementInfo.elementData = matchData;
                        c3.elementInfo.elementData = matchData;
                        c2.elementInfo.elementData = blockerData;

                        bool valid = CheckMatchOf(3).Count == 0 && HasAnyPossibleMove();
                        if (valid)
                        {
                            RefreshElementVisual(p0);
                            RefreshElementVisual(p1);
                            RefreshElementVisual(p2);
                            RefreshElementVisual(p3);
                            return;
                        }

                        c0.elementInfo.elementData = old0;
                        c1.elementInfo.elementData = old1;
                        c2.elementInfo.elementData = old2;
                        c3.elementInfo.elementData = old3;
                    }
                }
            }
        }

        private void RefreshElementVisual(Vector2Int pos)
        {
            GridCell cell = GetCell(pos);
            GridElement element = GetElementAt(pos);
            if (cell?.elementInfo == null || element == null) return;

            element.elementInfo = cell.elementInfo;
            element.InitElement(this, element.elementInfo);
        }

        internal Vector3 GetCellPositionInGrid(Vector2Int center)
        {
            if (generatedTiles.TryGetValue(center, out GridCellController tile) && tile != null)
                return tile.transform.position;
            return Vector3.zero;
        }

        private GameObject GetMatchOriginObject(List<List<Vector2Int>> matchedGroups)
        {
            if (matchedGroups == null || matchedGroups.Count == 0 || matchedGroups[0] == null || matchedGroups[0].Count == 0)
                return null;

            Vector2Int originPos = matchedGroups[0][0];
            if (generatedTiles.TryGetValue(originPos, out GridCellController tile) && tile != null)
                return tile.gameObject;

            return null;
        }

        private void RewardCurrencyForCombo(int comboCount, GameObject source)
        {
            CurrencyManager currencyManager = CurrencyManager.Instance;
            if (currencyManager == null) return;

            currencyManager.AddCurrency(GameManager.Instance.cashCurrency, Mathf.Max(1, comboCount), null);
        }
    }
}