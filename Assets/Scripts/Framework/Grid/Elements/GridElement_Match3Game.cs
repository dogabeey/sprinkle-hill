using Sirenix.OdinInspector;
using UnityEngine;

namespace Game
{
    public class GridElement_Match3Game : GridElement
    {
        [FoldoutGroup("Match3")]
        public SpriteRenderer highlightSprite;

        public override void PostInit()
        {
        }

        public override void PreInit()
        {
        }

        public void SetSelected(bool isSelected)
        {
            if (highlightSprite != null)
            {
                highlightSprite.enabled = isSelected;
            }
        }

        protected override GridCell DrawGridCells(Rect rect, GridCell value)
        {
            return value;
        }

        private void OnDisable()
        {
            SetSelected(false);
        }
    }
}
