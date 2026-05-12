using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// Locked areas are special cell features that represent locked sections of the grid. They require certain objectives to be completed before
    /// they are unlocked and can accept elements. They can be used to create progression and challenge in the game by blocking access to certain areas of the grid until specific conditions are met.
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Cell Feature/Locked Area...")]
    public class LockedAreaFeature : CellFeature
    {
        public ObjectiveUINode objectiveNodePrefab;
        public LayoutGroup lockedAreaCanvasParentPrefab;

        public override bool AcceptElements => true;
        public override void OnElementMatchedOverTheCell(Grid3D.GridCell cell, GridElement element)
        {
        }
        public override void OnElementMatchedAdjacentToTheCell(Grid3D.GridCell thisCell, Grid3D.GridCell matchedCell, GridElement element)
        {
        }
    }

    public class LockedAreaConfig
    {
        public LockedAreaFeature lockedAreaReference; 
        public List<Objective> activeObjectives;
        public List<ObjectiveUINode> objectiveNodes = new List<ObjectiveUINode>();

        public LockedAreaConfig(LockedAreaFeature lockedAreaReference, List<Objective> activeObjectives, List<ObjectiveUINode> objectiveNodes)
        {
            this.lockedAreaReference = lockedAreaReference;
            this.activeObjectives = activeObjectives;
            this.objectiveNodes = objectiveNodes;
            InstantiateObjectiveNodes();
        }

        private void InstantiateObjectiveNodes()
        {
            objectiveNodes.ForEach(node => Object.Destroy(node.gameObject));
            objectiveNodes.Clear();

            activeObjectives.ForEach(objective => {
                ObjectiveUINode node = Object.Instantiate(lockedAreaReference.objectiveNodePrefab, lockedAreaReference.lockedAreaCanvasParentPrefab.transform);
                node.Initialize(objective);
                objectiveNodes.Add(node);
            });
        }
    }
}
