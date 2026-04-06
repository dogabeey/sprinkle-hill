using System.Collections;
using System;
using System.Collections.Generic;
using UnityEngine;
using Unity.VisualScripting;
using Game.SimpleJSON;

namespace Game
{
    public class World : MonoBehaviour, ISaveable
    {
        public static World Instance
        {
            get
            {
                return GameManager.Instance.CurrentWorld;
            }
        }

        public string worldName;
        public bool mainWorld;
        public List<LevelScene> levelScenes;
        public int lastPlayedLevelIndex;

        private LevelScene currentLevel;

        public LevelScene CurrentLevel { get => currentLevel;
            set
            {
                currentLevel = value;
                //lastPlayedLevelIndex = levelScenes.IndexOf(currentLevel);
            }
        }

        public string SaveId => "World_" + worldName;

        public SaveDataType SaveDataType => SaveDataType.WorldProgression;

        protected void Awake()
        {
            GameManager.Instance.saveManager.Register(this);
            if(!Load())
            {
                lastPlayedLevelIndex = 0;
            }
            
        }

        public void PauseLevel(bool pause)
        {
            currentLevel.gameObject.SetActive(!pause);
        }

        public Dictionary<string, object> Save()
        {
            Dictionary<string, object> saveData = new Dictionary<string, object>
            {
                { "lastPlayedLevelIndex", lastPlayedLevelIndex }
            };

            return saveData;
        }

        public bool Load(Action onLoadSuccess = null, Action onLoadFail = null)
        {
            JSONNode saveData = GameManager.Instance.saveManager.LoadSave(this);

            if (saveData == null)
            {
                onLoadFail?.Invoke();
                return false;
            }

            lastPlayedLevelIndex = (int)saveData["lastPlayedLevelIndex"];
            onLoadSuccess?.Invoke();
            return true;
        }
    }

}
