using System.Collections;

using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;
using System;
using Game.SimpleJSON;
// using Firebase.Database;
using System.Threading;

namespace Game
{
    [CreateAssetMenu(fileName = "SaveManager", menuName = "Game/Managers/SaveManager")]
    public class SaveManager : ScriptableObject, IManager
	{
        #region Member Variables
        public static string userId;

        private List<ISaveable>	saveables;
		private JSONNode		loadedSave;

        public string saveID = "Save_1";
        public bool saveOnQuit = true;

		#endregion

		#region Properties

		/// <summary>
		/// Path to the save file on the device
		/// </summary>
		public static string SaveFilePath { get { return Application.persistentDataPath + "/save.json"; } }

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

		#region Unity Methods

		private void OnDestroy()
		{
			if(saveOnQuit)
			{
				Save();
			}
			
		}

		public void OnInit()
        {
            Debug.Log("Save file path: " + SaveFilePath);
        }
        public void OnUpdate()
        {
        }
        public void OnApplicationPause()
        {
        }
        public void OnApplicationQuit()
        {
            if (saveOnQuit)
            {
                Save();
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

		/// <summary>
		/// Loads the save data for the given saveable
		/// </summary>
		public JSONNode LoadSave(ISaveable saveable)
		{
			return LoadSave(saveID + "_" + saveable.SaveId);
		}

		/// <summary>
		/// Loads the save data for the given save id
		/// </summary>
		public JSONNode LoadSave(string saveId)
		{
			saveId = saveID + "_" + saveId;
            // Check if the save file has been loaded and if not try and load it
            if (loadedSave == null && !LoadSave(out loadedSave))
			{
				return null;
			}

			// Check if the loaded save file has the given save id
			if (!loadedSave.AsObject.HasKey(saveId))
			{
				return null;
			}

			// Return the JSONNode for the save id
			return loadedSave[saveId];
		}

		#endregion

		#region Private Methods

		/// <summary>
		/// Saves all registered saveables to the save file
		/// </summary>
		public void Save(Action completionHandler = null)
		{
			Dictionary<string, object> saveJson = new Dictionary<string, object>();
			if(saveables != null)
			{
				for (int i = 0; i < saveables.Count; i++)
				{
					//saveJson.Add(saveables[i].SaveId, saveables[i].Save());
					if(saveJson.ContainsKey(saveID + "_" + saveables[i].SaveId))
					{
						saveJson[saveID + "_" + saveables[i].SaveId] = saveables[i].Save();
					}else
					{
						saveJson.Add(saveID + "_" + saveables[i].SaveId, saveables[i].Save());
					}
				}
				
				System.IO.File.WriteAllText(SaveFilePath, JsonConvert.SerializeObject(saveJson));
			}
			
			if(completionHandler != null){completionHandler();};
		}

		/// <summary>
		/// Tries to load the save file
		/// </summary>
		private bool LoadSave(out JSONNode json)
		{
			json = null;

			if (!System.IO.File.Exists(SaveFilePath))
			{
				return false;
			}


			// if(UserManager.currentUser != null && UserManager.currentUser.gameData != null)
			// {
			// 	//Firebase bos stringleri donuste okunamadigi icin yerine dolar koyuyoruz. Simdi donus degerinde replace ediyoruz.
			// 	json = JSON.Parse(JsonConvert.SerializeObject(UserManager.currentUser.gameData).Replace("$$","\\u0000"));
			// 	//json = JSON.Parse(JsonUtility.ToJson(UserManager.currentUser.gameData));
			// 	Debug.Log("--------> read data from user data");
			// }else
			// {
			// 	json = JSON.Parse(System.IO.File.ReadAllText(SaveFilePath));
			// 	Debug.Log("--------> read data from local");
			// }

			json = JSON.Parse(System.IO.File.ReadAllText(SaveFilePath));

			

			return json != null;
		}

		#endregion
	}
}
