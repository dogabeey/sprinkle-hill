using Game;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Sirenix.OdinInspector;


public class ObjectiveUI : UIElement
{
    public ObjectiveManager objectiveManager;
    [AssetsOnly]
    public ObjectiveUINode objectiveNodePrefab;

    private List<ObjectiveUINode> objectiveNodes = new List<ObjectiveUINode>();

    public override void InitUI()
    {
        InstantiateObjectiveNodes();
    }
    public override void DrawUI()
    {
        UpdateObjectiveNodes();
    }

    private void InstantiateObjectiveNodes()
    {
        objectiveManager.activeObjectives.ForEach(objective => {
            ObjectiveUINode node = Instantiate(objectiveNodePrefab, transform);
            node.Initialize(objective);
            objectiveNodes.Add(node);
        });
    }
    private void UpdateObjectiveNodes()
    {
        objectiveNodes.ForEach(node => {
            Objective objective = node.referenceObjective;
            int currentCount = objectiveManager.GetCurrentCount(objective);
            node.UpdateNode(currentCount);
        });
    }
}
