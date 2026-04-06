using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ObjectiveUINode : MonoBehaviour
{
    public Objective referenceObjective;
    public Image objectiveIcon;
    public Image checkmarkIcon;
    public TMP_Text countText;

    public void Initialize(Objective objective)
    {
        referenceObjective = objective;

        Sprite icon = objective != null && objective.objectiveType != null
            ? objective.objectiveType.ResolveObjectiveSprite(objective.scriptableObjectParameter)
            : null;

        objectiveIcon.sprite = icon;
        checkmarkIcon.enabled = false;
        countText.text = objective.requiredCount.ToString();
    }
    public void UpdateNode(int currentCount)
    {
        countText.text = currentCount <= 0 ? string.Empty : currentCount.ToString();
        checkmarkIcon.enabled = currentCount <= 0;
    }
}