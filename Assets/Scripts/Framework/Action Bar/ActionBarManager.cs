using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    /// <summary>
    /// Action bar manager holds the current action bar items the player can use. The action bar item prefabs added to the list will be 
    /// instantiated and placed in the appropriate positions. This class can be extended to add more functionality to the drawing process.
    /// </summary>
    public class ActionBarManager : MonoBehaviour
    {
        public ActionBarView actionBarViewPrefab;
        [SerializeReference]
        public List<ActionBarItem> actionBarItemList;
        public Transform actionBarParent;

        private void Start()
        {
            DrawUI();
        }

        protected virtual void DrawUI()
        {
            foreach (ActionBarItem actionBarItem in actionBarItemList)
            {
                Instantiate(actionBarViewPrefab, actionBarParent);
            }
        }
    }
}