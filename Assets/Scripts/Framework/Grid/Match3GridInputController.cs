using UnityEngine;
using System.Collections;

namespace Game
{
    public class Match3GridInputController : MonoBehaviour
    {
        [SerializeField] private Match3Grid match3Grid;
        [SerializeField] private Camera inputCamera;

        private GridElement_Match3Game selectedElement;
        private bool isProcessing;

        private void Awake()
        {
            if (inputCamera == null)
            {
                inputCamera = Camera.main;
            }
        }

        private void Update()
        {
            if (isProcessing)
            {
                return;
            }

            if (Input.GetMouseButtonDown(0))
            {
                TryHandleClick();
            }
        }

        private void TryHandleClick()
        {
            if (match3Grid == null)
            {
                return;
            }

            Camera cam = inputCamera != null ? inputCamera : Camera.main;
            if (cam == null)
            {
                return;
            }

            GridElement_Match3Game clickedElement = GetClickedElement(cam);
            if (clickedElement == null || clickedElement.ownerGrid != match3Grid)
            {
                return;
            }

            if (selectedElement == null)
            {
                SetSelectedElement(clickedElement);
                return;
            }

            if (clickedElement == selectedElement)
            {
                ClearSelection();
                return;
            }

            if (!match3Grid.TryGetElementPosition(selectedElement, out Vector2Int firstPos) ||
                !match3Grid.TryGetElementPosition(clickedElement, out Vector2Int secondPos))
            {
                ClearSelection();
                return;
            }

            if (!Match3Grid.AreAdjacent(firstPos, secondPos))
            {
                SetSelectedElement(clickedElement);
                return;
            }

            StartCoroutine(SwapAndMatchRoutine(firstPos, secondPos));
        }

        private GridElement_Match3Game GetClickedElement(Camera cam)
        {
            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                return hit.collider.GetComponentInParent<GridElement_Match3Game>();
            }

            RaycastHit2D hit2D = Physics2D.GetRayIntersection(ray);
            if (hit2D.collider != null)
            {
                return hit2D.collider.GetComponentInParent<GridElement_Match3Game>();
            }

            return null;
        }

        private IEnumerator SwapAndMatchRoutine(Vector2Int firstPos, Vector2Int secondPos)
        {
            isProcessing = true;
            ClearSelection();

            yield return StartCoroutine(match3Grid.SwapAndMatch(firstPos, secondPos));

            isProcessing = false;
        }

        private void SetSelectedElement(GridElement_Match3Game element)
        {
            if (selectedElement != null)
            {
                selectedElement.SetSelected(false);
            }

            selectedElement = element;
            if (selectedElement != null)
            {
                selectedElement.SetSelected(true);
                EventManager.TriggerEvent(GameEvent.ELEMENT_SELECTED, new EventParam(
                    paramObj: selectedElement.gameObject,
                    paramScriptable: selectedElement.elementInfo?.elementData
                ));
            }
        }

        private void ClearSelection()
        {
            if (selectedElement != null)
            {
                selectedElement.SetSelected(false);
                selectedElement = null;
            }
        }
    }
}