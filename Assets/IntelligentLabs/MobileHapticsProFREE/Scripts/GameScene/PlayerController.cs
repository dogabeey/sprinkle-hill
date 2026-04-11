using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MobileHapticsProFreeEdition
{
    public class PlayerController : MonoBehaviour
    {
        private GameManager gameManager;
        private GameHaptics gameHaptics;
        private Rigidbody rb;
        private Vector3 playersDefaultPos = new Vector3(0, 0.625f, 0);

        [Header("Reference To Animator Component")]
        public GameObject mainCamera;
        private Animator mainCameraAnimator;

        [Header("Player setup")]
        public float jumpForce;
        public bool canJump;
        public bool launcherReady;

        [Header("Launchers & Missile")]
        public GameObject launcherL;
        public GameObject launcherR;
        public GameObject cubeMissile;

        [Header("Activated Launcher")]
        public bool isTopLauncherS;
        public bool isTopLauncherX;
        public bool isSideLaunchers;

        [Header("Shooting")]
        public float cubeMissileSpeed = 80f;  // Speed at which the CubeMissile is shot
        private bool isLeftLauncherNext = true; // Keeps track of which launcher should move next

        [Header("Auto Mode")]
        public Button autoModeButton;
        public CanvasGroup autoModeCanvasGroup;  // Control the interactability of the auto mode button
        public bool isAutoModeEnabled = false;  // Tracks whether auto mode is enabled
        private bool isHolding;  // Tracks whether the player is holding the screen for auto-shooting
        private Coroutine autoShootCoroutine; // Stores the coroutine for auto-shoot

        private void Awake()
        {
            // Get the Animator component
            mainCameraAnimator = mainCamera.GetComponent<Animator>();

            gameManager = GameManager.Instance;
            gameHaptics = FindObjectOfType<GameHaptics>();
            rb = GetComponent<Rigidbody>();

            // Ensure the auto mode button is not interactable by default
            SetAutoModeInteractable(false);

            // Add listener for the auto mode button
            autoModeButton.onClick.AddListener(ToggleAutoMode);
        }

        private void Start()
        {
            // Disable Camera's Animator
            StartCoroutine(DisableAnimatorAfterSeconds(2.5f));
        }

        void Update()
        {
            // Check if touch input is not over a UI element
            if (!IsPointerOverUI())
            {
                if (Input.GetMouseButtonDown(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began))
                {
                    if (canJump)
                    {
                        // Perform jump
                        rb.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
                    }
                    else
                    {
                        // Check if auto mode is enabled
                        if (isAutoModeEnabled && launcherReady)
                        {
                            // Start auto-shoot if holding down the screen
                            if (autoShootCoroutine == null)
                            {
                                isHolding = true;
                                autoShootCoroutine = StartCoroutine(AutoShoot());
                            }
                        }
                        else if (launcherReady)
                        {
                            PerformShooting();
                        }

                        // Trigger the haptic feedback
                        TriggerHapticFeedback();
                    }
                }

                if (Input.GetMouseButtonUp(0) || (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
                {
                    StopAutoShoot();
                }
            }
        }

        private bool IsPointerOverUI()
        {
            if (Input.touchCount > 0)
            {
                Touch touch = Input.GetTouch(0);
                return EventSystem.current.IsPointerOverGameObject(touch.fingerId);
            }
            else
            {
                return EventSystem.current.IsPointerOverGameObject();
            }
        }

        private void ToggleAutoMode()
        {
            isAutoModeEnabled = !isAutoModeEnabled;
        }

        private void StopAutoShoot()
        {
            isHolding = false;

            // Stop the auto-shoot coroutine if it is running
            if (autoShootCoroutine != null)
            {
                StopCoroutine(autoShootCoroutine);
                autoShootCoroutine = null;
            }
        }

        private IEnumerator AutoShoot()
        {
            while (launcherReady && isHolding)
            {
                PerformShooting();
                TriggerHapticFeedback();
                yield return new WaitForSeconds(0.1f); // Adjust the interval between auto-shots
            }
        }

        private void PerformShooting()
        {
            if (isLeftLauncherNext)
            {
                StartCoroutine(LauncherShoot(launcherL));
                ShootMissile(launcherL.transform.position);
            }
            else
            {
                StartCoroutine(LauncherShoot(launcherR));
                ShootMissile(launcherR.transform.position);
            }



            // Toggle to alternate launchers
            isLeftLauncherNext = !isLeftLauncherNext;
        }

        //// - LAUNCHERS - ////
        // TOP SINGLE
        public IEnumerator SetTopLauncherS()
        {
            ResetLaunchers();

            if (launcherReady)
                yield return StartCoroutine(BackToCube());

            if (Vector3.Distance(transform.position, playersDefaultPos) < 0.01f)
            {
                rb.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;
                canJump = false;

                float liftDuration = 1f;
                float elapsedTime = 0f;

                launcherL.SetActive(true);
                launcherR.SetActive(true);

                Vector3 initialPositionL = launcherL.transform.position;
                Vector3 targetPositionL = new Vector3(0, 1.55f, initialPositionL.z);

                Vector3 initialPositionR = launcherR.transform.position;
                Vector3 targetPositionR = new Vector3(0, 1.55f, initialPositionR.z);

                

                while (elapsedTime < liftDuration)
                {
                    launcherL.transform.position = Vector3.Lerp(initialPositionL, targetPositionL, elapsedTime / liftDuration);
                    launcherR.transform.position = Vector3.Lerp(initialPositionR, targetPositionR, elapsedTime / liftDuration);

                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                launcherL.transform.position = targetPositionL;
                launcherR.transform.position = targetPositionR;

                launcherReady = true;
                isTopLauncherS = true;

                SetAutoModeInteractable(true);  // Enable auto mode button when a launcher is selected
            }
        }

        // TOP DOUBLE
        public IEnumerator SetTopLauncherX()
        {
            ResetLaunchers();

            if (launcherReady)
                yield return StartCoroutine(BackToCube());

            if (Vector3.Distance(transform.position, playersDefaultPos) < 0.01f)
            {
                rb.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;
                canJump = false;

                float liftDuration = 1f;
                float elapsedTime = 0f;

                launcherL.SetActive(true);
                launcherR.SetActive(true);

                Vector3 initialPositionL = launcherL.transform.position;
                Vector3 targetPositionL = new Vector3(initialPositionL.x, 1.55f, initialPositionL.z);

                Vector3 initialPositionR = launcherR.transform.position;
                Vector3 targetPositionR = new Vector3(initialPositionR.x, 1.55f, initialPositionR.z);

                

                while (elapsedTime < liftDuration)
                {
                    launcherL.transform.position = Vector3.Lerp(initialPositionL, targetPositionL, elapsedTime / liftDuration);
                    launcherR.transform.position = Vector3.Lerp(initialPositionR, targetPositionR, elapsedTime / liftDuration);

                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                launcherL.transform.position = targetPositionL;
                launcherR.transform.position = targetPositionR;

                launcherReady = true;
                isTopLauncherX = true;

                SetAutoModeInteractable(true);  // Enable auto mode button when a launcher is selected
            }
        }

        // SIDES
        public IEnumerator SetSideLaunchers()
        {
            ResetLaunchers();

            if (launcherReady)
                yield return StartCoroutine(BackToCube());

            if (Vector3.Distance(transform.position, playersDefaultPos) < 0.01f)
            {
                rb.constraints = RigidbodyConstraints.FreezePosition | RigidbodyConstraints.FreezeRotation;
                canJump = false;

                float liftDuration = 1f;
                float elapsedTime = 0f;

                launcherL.SetActive(true);
                launcherR.SetActive(true);

                Vector3 initialPositionL = launcherL.transform.position;
                Vector3 targetPositionL = new Vector3(-0.9f, 0.625f, initialPositionL.z);

                Vector3 initialPositionR = launcherR.transform.position;
                Vector3 targetPositionR = new Vector3(0.9f, 0.625f, initialPositionR.z);

                

                while (elapsedTime < liftDuration)
                {
                    launcherL.transform.position = Vector3.Lerp(initialPositionL, targetPositionL, elapsedTime / liftDuration);
                    launcherR.transform.position = Vector3.Lerp(initialPositionR, targetPositionR, elapsedTime / liftDuration);

                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                launcherL.transform.position = targetPositionL;
                launcherR.transform.position = targetPositionR;

                launcherReady = true;
                isSideLaunchers = true;

                SetAutoModeInteractable(true);  // Enable auto mode button when a launcher is selected
            }
        }

        public IEnumerator BackToCube()
        {
            if (launcherReady)
            {
                isTopLauncherS = false;
                isTopLauncherX = false;
                isSideLaunchers = false;
                launcherReady = false;

                float retractDuration = 1f;
                float elapsedTime = 0f;

                Vector3 initialPositionL = launcherL.transform.position;
                Vector3 initialPositionR = launcherR.transform.position;

                Vector3 targetPositionL = new Vector3(-0.25f, 0.625f, 0f);
                Vector3 targetPositionR = new Vector3(0.25f, 0.625f, 0f);

                while (elapsedTime < retractDuration)
                {
                    launcherL.transform.position = Vector3.Lerp(initialPositionL, targetPositionL, elapsedTime / retractDuration);
                    launcherR.transform.position = Vector3.Lerp(initialPositionR, targetPositionR, elapsedTime / retractDuration);
                    elapsedTime += Time.deltaTime;
                    yield return null;
                }

                launcherL.transform.position = targetPositionL;
                launcherR.transform.position = targetPositionR;

                launcherL.SetActive(false);
                launcherR.SetActive(false);

                rb.constraints = RigidbodyConstraints.None;
                canJump = true;

                SetAutoModeInteractable(false);  // Disable auto mode button when switching to cube mode
            }
        }

        // Smooth movement logic when jumping
        private void OnCollisionEnter(Collision collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                if (!launcherReady)
                {
                    // Player touches the Ground and can jump
                    canJump = true;
                }

                // Smoothly move to the default position
                StartCoroutine(SmoothMovement(playersDefaultPos, Quaternion.Euler(0, 0, 0)));
            }
        }

        private void OnCollisionExit(Collision collision)
        {
            if (collision.gameObject.CompareTag("Ground"))
            {
                // Player is not touching Ground and cannot jump
                canJump = false;
            }
        }

        private IEnumerator SmoothMovement(Vector3 targetPosition, Quaternion targetRotation)
        {
            float elapsedTime = 0f;
            float duration = 0.25f;

            Vector3 startingPos = transform.position;
            Quaternion startingRot = transform.rotation;

            while (elapsedTime < duration)
            {
                transform.position = Vector3.Lerp(startingPos, targetPosition, elapsedTime / duration);
                transform.rotation = Quaternion.Lerp(startingRot, targetRotation, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Ensure the final position and rotation are set
            transform.position = targetPosition;
            transform.rotation = targetRotation;
        }

        private IEnumerator LauncherShoot(GameObject launcher)
        {
            float moveDistance = 1f;
            float moveDuration = 0.05f;

            Vector3 originalPosition = launcher.transform.position;
            Vector3 targetPosition = originalPosition + new Vector3(0, 0, moveDistance);

            float elapsedTime = 0f;
            while (elapsedTime < moveDuration)
            {
                launcher.transform.position = Vector3.Lerp(originalPosition, targetPosition, elapsedTime / moveDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            launcher.transform.position = targetPosition;

            elapsedTime = 0f;
            while (elapsedTime < moveDuration)
            {
                launcher.transform.position = Vector3.Lerp(targetPosition, originalPosition, elapsedTime / moveDuration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }
            launcher.transform.position = originalPosition;
        }

        private void ShootMissile(Vector3 spawnPosition)
        {
            GameObject missile = Instantiate(cubeMissile, spawnPosition, Quaternion.identity);
            missile.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            Rigidbody missileRb = missile.GetComponent<Rigidbody>();
            missileRb.linearVelocity = Vector3.forward * cubeMissileSpeed;
        }

        private void ResetLaunchers()
        {
            isTopLauncherS = false;
            isTopLauncherX = false;
            isSideLaunchers = false;
        }

        private void SetAutoModeInteractable(bool isInteractable)
        {
            autoModeCanvasGroup.interactable = isInteractable;
            autoModeCanvasGroup.alpha = isInteractable ? 1f : 0.5f;
        }

        ///// - HAPTICS - /////
        private void TriggerHapticFeedback()
        {
            if (gameHaptics != null)
            {
                if (isTopLauncherS)
                    gameHaptics.Select();
                else if (isTopLauncherX)
                    gameHaptics.Select();
                else if (isSideLaunchers)
                    gameHaptics.Select();
            }
        }

        private IEnumerator DisableAnimatorAfterSeconds(float seconds)
        {
            yield return new WaitForSeconds(seconds);

            // Disable the Animator
            mainCameraAnimator.enabled = false;
        }
    }
}
