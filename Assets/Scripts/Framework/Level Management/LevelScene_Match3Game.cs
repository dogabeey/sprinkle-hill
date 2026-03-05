using Sirenix.OdinInspector;
using UnityEngine;

namespace Game
{
    public class LevelScene_Match3Game : LevelScene
    {
        [FoldoutGroup("Level Settings")]
        [SerializeField] private Grid3D grid;
        [FoldoutGroup("Level Settings")]
        [SerializeField] private Grid3D.LevelCreationMode levelCreationMode = Grid3D.LevelCreationMode.LevelEditor;
        [FoldoutGroup("Level Settings"), ShowIf(nameof(UseLevelEditor))]
        [SerializeField] private LevelEditor levelEditor;
        [FoldoutGroup("Level Settings"), ShowIf(nameof(UseProcedural))]
        [SerializeField] private Vector2Int proceduralGridSize = new Vector2Int(8, 8);
        [FoldoutGroup("Level Settings"), ShowIf(nameof(UseProcedural))]
        [SerializeField] private Grid3D.ProceduralGenerationSettings proceduralGeneration = new Grid3D.ProceduralGenerationSettings();

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

            grid.ConfigureLevelSettings(levelCreationMode, levelEditor, proceduralGeneration, proceduralGridSize);
        }

        private bool UseLevelEditor => levelCreationMode == Grid3D.LevelCreationMode.LevelEditor;
        private bool UseProcedural => levelCreationMode == Grid3D.LevelCreationMode.Procedural;
    }
}