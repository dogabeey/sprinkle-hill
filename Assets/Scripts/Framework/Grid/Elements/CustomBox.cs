using System.Collections;
using System.Collections.Generic;
using UnityEngine; using Game.EventManagement;

namespace Game
{
    public class CustomBox : GridElement
    {
        [Tooltip("List of custom parent transforms for each generated element. The elements will be positioned at the local origin of their respective parents.")]
        public List<Transform> customElementParentList;

        public override void PostInit()
        {
            // Put each element to its corresponding customElementParent
            for (int i = 0; i < customElementParentList.Count; i++)
            {
                if (i < generatedElements.Count)
                {
                    GridElement element = generatedElements[i];
                    Transform parentTransform = customElementParentList[i];
                    element.transform.SetParent(parentTransform, false);
                    element.transform.localPosition = Vector3.zero;
                }
                else
                {
                    Debug.LogWarning("Not enough generated elements to assign to customElementParentList after index: " + i);
                    break;
                }
            }
        }
        public override void PreInit()
        {
        }

        public override IEnumerator DestroyElement()
        {
            
            Destroy(gameObject);
            yield break;
        }
    }
}
