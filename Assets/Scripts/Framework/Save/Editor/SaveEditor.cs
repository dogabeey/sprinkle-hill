using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;  

namespace Game
{
    public class SaveEditor : MonoBehaviour
    {
        // Start is called before the first frame update
        void Start()
        {

        }

        // Update is called once per frame
        void Update()
        {

        }

        [MenuItem("Tools/Clear Save Games")]
        static void ClearSaves()
        {
            string savePath = SaveManager.Instance.GetSaveFilePath(true) ;
            // Remove the save file
            if (System.IO.File.Exists(savePath))
            {
                System.IO.File.Delete(savePath);
                Debug.Log("Save file deleted");
            }
            else
            {
                Debug.Log("No save file found");
            }
        }
    }

}