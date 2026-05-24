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
			SoundManager.Instance.Play(soundName);
        }
		public void PlayRandom()
        {
			SoundManager.Instance.Play(randomList[Random.Range(0, randomList.Length - 1)]);
        }
	}
}