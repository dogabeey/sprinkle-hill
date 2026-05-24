using Game.SimpleJSON;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEditor;
using UnityEngine; using Game.EventManagement;

namespace Game
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance
        {
            get
            {
                return GameManager.Instance.saveManager;
            }
        }

        #region Member Variables

        private List<ISaveable> saveables;
        private JSONNode loadedSave;

        [SerializeField] SaveData[] saveDatas;
        [SerializeField] string saveProfile = "default";
        public bool saveOnQuit = true;

        #endregion

        #region Properties

        /// <summary>
        /// Path to the save file on the device
        /// </summary>

        public string GetSaveFilePath(bool isGlobal)
        {
            if (isGlobal)
            {
                return Application.persistentDataPath + "/Global";
            }
            else
            {
                return Application.persistentDataPath + "/" + saveProfile;
            }
        }

        /// <summary>
        /// List of registered saveables
        /// </summary>
        private List<ISaveable> Saveables
        {
            get
            {
                if (saveables == null)
                {
                    saveables = new List<ISaveable>();
                }

                return saveables;
            }
        }
        #endregion


        #region Public Methods

        /// <summary>
        /// Registers a saveable to be saved
        /// </summary>
        public void Register(ISaveable saveable)
        {
            Saveables.Add(saveable);
        }

        public JSONNode LoadSave(ISaveable saveable)
        {
            // Always load the JSON that corresponds to this saveable's SaveDataType.
            // A single shared cache can point to a different file and cause misses
            // when different save systems load in different orders.
            if (!LoadSave(saveable, out JSONNode json))
            {
                return null;
            }

            // Check if the loaded save file has the given save id
            if (!json.AsObject.HasKey(saveable.SaveId))
            {
                return null;
            }

            // Return the JSONNode for the save id
            return json[saveable.SaveId];
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Saves all registered saveables to the save file
        /// </summary>
        public void Save(Action onSaveComplete = null)
        {

            if (saveables != null)
            {
                foreach (SaveData saveData in saveDatas)
                {
                    string saveString = saveData.saveDataType.ToString();
                    string directoryPath = GetSaveFilePath(saveData.isGlobalProfile);
                    string filePath = $"{directoryPath}/{saveString}.json";
                    Dictionary<string, object> saveJson = LoadExistingSaveDictionary(filePath);

                    for (int i = 0; i < saveables.Count; i++)
                    {
                        if (saveables[i].SaveDataType != saveData.saveDataType)
                        {
                            continue;
                        }
                        //saveJson.Add(saveables[i].SaveId, saveables[i].Save());
                        if (saveJson.ContainsKey(saveables[i].SaveId))
                        {
                            saveJson[saveables[i].SaveId] = saveables[i].Save();
                        }
                        else
                        {
                            saveJson.Add(saveables[i].SaveId, saveables[i].Save());
                        }
                    }

                    System.IO.Directory.CreateDirectory(directoryPath);
                    System.IO.File.WriteAllText(filePath, JsonConvert.SerializeObject(saveJson));
                }
            }

            onSaveComplete?.Invoke();
        }

        private static Dictionary<string, object> LoadExistingSaveDictionary(string filePath)
        {
            if (!System.IO.File.Exists(filePath))
                return new Dictionary<string, object>();

            string fileContents = System.IO.File.ReadAllText(filePath);
            if (string.IsNullOrWhiteSpace(fileContents))
                return new Dictionary<string, object>();

            Dictionary<string, object> existingSave = JsonConvert.DeserializeObject<Dictionary<string, object>>(fileContents);
            return existingSave ?? new Dictionary<string, object>();
        }


        /// <summary>
        /// Tries to load the save file
        /// </summary>
        private bool LoadSave(ISaveable saveable, out JSONNode json)
        {
            SaveData saveData = Array.Find(saveDatas, data => data.saveDataType == saveable.SaveDataType);
            string saveString = saveData.saveDataType.ToString();
            string filePath = $"{GetSaveFilePath(saveData.isGlobalProfile)}/{saveString}.json";
            if (!System.IO.File.Exists(filePath))
            {
                json = null;
                return false;
            }
            string fileContents = System.IO.File.ReadAllText(filePath);
            json = JSON.Parse(fileContents);
            return true;
        }
#if UNITY_EDITOR
        [MenuItem("Game/Save Manager/Clear All Saves")]
        [Button]
        public static void ClearAllSaves()
        {
            foreach (SaveData saveData in Instance.saveDatas)
            {
                string saveString = saveData.saveDataType.ToString();
                string filePath = $"{Instance.GetSaveFilePath(saveData.isGlobalProfile)}/{saveString}.json";
                if (System.IO.File.Exists(filePath))
                {
                    System.IO.File.Delete(filePath);
                }
            }
        }
        [MenuItem("Game/Save Manager/Save Now")]
        [Button]
        private static void SaveNow()
        {
            Instance.Save();
        }
        [MenuItem("Game/Save Manager/Show Save Folder")]
        [Button]
        private static void ShowSaveFolder()
        {
            EditorUtility.RevealInFinder(Application.persistentDataPath);
        }
#endif

        public void OnApplicationPause()
        {
            if (saveOnQuit) Save();
        }

        public void OnApplicationQuit()
        {
            if (saveOnQuit) Save();
        }
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus && saveOnQuit)
            {
                Save();
            }
        }
    }
    #endregion
    public enum SaveDataType
    {
        MetaData,
        Settings,
        WorldProgression,
        LevelProgression,
        Tutorial
    }
    [System.Serializable]
    public class SaveData
    {
        public SaveDataType saveDataType;
        [Tooltip("If true, this save will be a global save that is not tied to a specific profile (i.e. graphics settings)")]
        public bool isGlobalProfile = false;
    }
}
