using Sirenix.OdinInspector;
using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game
{
    public class LevelScene_Match3Game : LevelScene
    {
        [FoldoutGroup("Level Settings")]
        public List<Objective> objectives;
        [FoldoutGroup("Level Settings")]
        public ElementData targetElement;
        [FoldoutGroup("Level Settings")]
        public int timer;
        [FoldoutGroup("Level Settings")]
        public Grid3D grid;
        [FoldoutGroup("Level Settings")]
        public Grid3D.LevelCreationMode levelCreationMode = Grid3D.LevelCreationMode.LevelEditor;
        [FoldoutGroup("Level Settings"), ShowIf(nameof(UseLevelEditor))]
        public LevelEditor levelEditor;
        [FoldoutGroup("Level Settings"), ShowIf(nameof(UseProcedural))]
        public Vector2Int proceduralGridSize = new Vector2Int(8, 8);
        [FoldoutGroup("Level Settings"), ShowIf(nameof(UseProcedural))]
        public Grid3D.ProceduralGenerationSettings proceduralGeneration = new Grid3D.ProceduralGenerationSettings();
        [FoldoutGroup("UI References")]
        public Image objectiveTargetImage;
        [FoldoutGroup("UI References")]
        public TMP_Text timerText;

        protected override void Awake()
        {
            ApplyLevelSettings();
            base.Awake();
        }
        private void ApplyLevelSettings()
        {
            if (grid == null)
            {
                return;
            }

            ObjectiveManager.Instance.activeObjectives = objectives;
            ObjectiveManager.Instance.InitializeObjectives();
            EventManager.TriggerEvent(GameEvent.OBJECTIVES_INITIALIZED);
            grid.ConfigureLevelSettings(levelCreationMode, levelEditor, proceduralGeneration, proceduralGridSize);
        }

        private bool UseLevelEditor => levelCreationMode == Grid3D.LevelCreationMode.LevelEditor;
        private bool UseProcedural => levelCreationMode == Grid3D.LevelCreationMode.Procedural;
    }
}