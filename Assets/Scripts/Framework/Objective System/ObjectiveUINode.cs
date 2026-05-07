using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Game
{
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
            bool isCompleted = referenceObjective != null && referenceObjective.isCompleted;
            countText.text = isCompleted ? string.Empty : Mathf.Max(0, currentCount).ToString();
            checkmarkIcon.enabled = isCompleted;
        }
    }
}