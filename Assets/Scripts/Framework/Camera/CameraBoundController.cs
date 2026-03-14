using System.Collections;
using UnityEngine;

namespace Game
{
    public class CameraBoundController : MonoBehaviour
    {
        [SerializeField] private float initDelay = 0.1f;
        [SerializeField] private Vector4 boundsOffset;

        public Vector2 MinBounds { get; private set; }
        public Vector2 MaxBounds { get; private set; }

        private Coroutine refreshRoutine;

        private void OnEnable()
        {
            EventManager.StartListening(GameEvent.GRID_INITIALIZED, OnGameInitialized);
            EventManager.StartListening(GameEvent.LEVEL_STARTED, OnGameInitialized);
        }

        private void OnDisable()
        {
            EventManager.StopListening(GameEvent.GRID_INITIALIZED, OnGameInitialized);
            EventManager.StopListening(GameEvent.LEVEL_STARTED, OnGameInitialized);

            if (refreshRoutine != null)
            {
                StopCoroutine(refreshRoutine);
                refreshRoutine = null;
            }
        }

        private void OnGameInitialized(EventParam _)
        {
            if (refreshRoutine != null)
            {
                StopCoroutine(refreshRoutine);
            }

            refreshRoutine = StartCoroutine(RefreshBoundsAfterDelay());
        }

        private IEnumerator RefreshBoundsAfterDelay()
        {
            yield return new WaitForSeconds(initDelay);

            Camera mainCamera = Camera.main;
            if (mainCamera == null)
            {
                yield break;
            }

            mainCamera.orthographic = true;

            MonoBehaviour[] components = FindObjectsOfType<MonoBehaviour>(false);

            bool hasAny = false;
            Vector2 min = Vector2.zero;
            Vector2 max = Vector2.zero;

            for (int i = 0; i < components.Length; i++)
            {
                MonoBehaviour component = components[i];
                if (component == null)
                {
                    continue;
                }

                if (component is not ICameraBoundSetter boundSetter)
                {
                    continue;
                }

                Vector2 point = boundSetter.CameraBound;
                if (!hasAny)
                {
                    min = point;
                    max = point;
                    hasAny = true;
                }
                else
                {
                    min = Vector2.Min(min, point);
                    max = Vector2.Max(max, point);
                }
            }

            if (!hasAny)
            {
                yield break;
            }

            min.x -= boundsOffset.x;
            max.x += boundsOffset.y;
            min.y -= boundsOffset.z;
            max.y += boundsOffset.w;

            MinBounds = min;
            MaxBounds = max;

            Vector3 cameraPosition = mainCamera.transform.position;
            cameraPosition.x = (MinBounds.x + MaxBounds.x) * 0.5f;
            cameraPosition.y = (MinBounds.y + MaxBounds.y) * 0.5f;
            mainCamera.transform.position = cameraPosition;

            float boundsHeight = Mathf.Max(0.0001f, MaxBounds.y - MinBounds.y);
            float boundsWidth = Mathf.Max(0.0001f, MaxBounds.x - MinBounds.x);
            float aspect = Mathf.Max(0.0001f, mainCamera.aspect);

            float sizeFromHeight = boundsHeight * 0.5f;
            float sizeFromWidth = (boundsWidth * 0.5f) / aspect;

            mainCamera.orthographicSize = Mathf.Max(sizeFromHeight, sizeFromWidth);
        }
    }
}
