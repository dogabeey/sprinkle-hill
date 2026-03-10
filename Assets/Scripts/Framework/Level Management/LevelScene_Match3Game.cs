using Sirenix.OdinInspector;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public class LevelScene_Match3Game : LevelScene
    {
        [FoldoutGroup("Level Settings")]
        public List<Objective> objectives;
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