using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MobileHapticsProFreeEdition
{
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance;

        public Transform spawnPoint;
        public GameObject player;
        public GameObject obstaclePrefab;
        public GameObject particleSystemPrefab;

        public bool isGameStared;

        private void Awake()
        {
            // Set the static reference when the GameManager is created
            Instance = this;
        }

        private void Start()
        {
            // Increases gravity on the Y-axis
            Physics.gravity = new Vector3(0, -50.0f, 0);  // Default Y-axis is (-9.81)

            GameObject ps = Instantiate(particleSystemPrefab);
            ps.transform.SetPositionAndRotation(new Vector3(0, -15, 232), Quaternion.Euler(-90, 0, 0));

            GameStart();
        }

        IEnumerator SpawnObstacles()
        {
            // spawn obstacles inside while loop
            while (isGameStared)
            {
                // create a random timing to spawn obstacles one after another
                float waitTime = Random.Range(0.5f, 2.0f); // Random val between 0.5 - 2.0
                yield return new WaitForSeconds(waitTime); // wait for time given, after that re-create obstacles

                GameObject spawnedObstacle = Instantiate(obstaclePrefab, spawnPoint.position, Quaternion.identity);
                spawnedObstacle.transform.localScale = new Vector3(2f, 1.5f, 1f);
            }
        }

        public void GameStart()
        {
            isGameStared = true;
            StartCoroutine(SpawnObstacles());
        }
    }
}