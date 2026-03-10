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
        objectiveIcon.sprite = objective.scriptableObjectParameter.displayIcon;
        checkmarkIcon.gameObject.SetActive(false);
        countText.text = objective.requiredCount.ToString();
    }
    public void UpdateNode(int currentCount)
    {
        countText.text = currentCount == 0 ? string.Empty : currentCount.ToString();
        checkmarkIcon.gameObject.SetActive(currentCount <= 0);
    }
}