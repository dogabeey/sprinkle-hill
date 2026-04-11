using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MobileHapticsProFreeEdition
{
    public class Obstacle : MonoBehaviour
    {
        private GameManager gameManager;
        private GameHaptics gameHaptics;

        public GameObject obstacleCubes;
        public float speed;

        private void Awake()
        {
            gameManager = GameManager.Instance;
            gameHaptics = FindObjectOfType<GameHaptics>();
        }

        void Update()
        {
            transform.Translate(speed * Time.deltaTime * Vector3.forward);

            if (!gameManager.isGameStared)
                ObstacleExpload();
        }

        private void OnTriggerEnter(Collider other)
        {
            // Obstacle Triggers player
            if (other.gameObject.CompareTag("Player"))
            {
                gameHaptics.Select();
                ObstacleExpload();
            }
        }

        public void ObstacleExpload()
        {
            Instantiate(obstacleCubes, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }

        private void OnBecameInvisible()
        {
            // when obstacle get out of the screen -> Destroy
            Destroy(gameObject);
        }
    }
}