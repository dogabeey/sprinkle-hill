using System.Collections;
using System.Collections.Generic;
using UnityEngine; using Game.EventManagement;
using DG.Tweening;
using Sirenix.OdinInspector;

namespace Game
{
	public class SoundManagerHelper : MonoBehaviour
	{
		public string[] randomList;
		public void Play(string soundName)
        {
			GameManager.Instance.soundManager.Play(soundName);
        }
		public void PlayRandom()
        {
			GameManager.Instance.soundManager.Play(randomList[Random.Range(0, randomList.Length - 1)]);
        }
	}
}