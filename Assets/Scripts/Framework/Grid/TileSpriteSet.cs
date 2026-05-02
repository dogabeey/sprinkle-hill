using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.Rendering;

namespace Game
{
    [CreateAssetMenu(fileName = "TileSpriteSet", menuName = "Game/Tile Sprite Set", order = 2)]
    public class TileSpriteSet : ScriptableObject
    {
        [SerializeReference] public ColorRuleSet colorRuleset;
        public Sprite topLeftCorner;
        public Sprite topRightCorner;
        public Sprite bottomLeftCorner;
        public Sprite bottomRightCorner;
        public Sprite topEdge;
        public Sprite bottomEdge;
        public Sprite leftEdge;
        public Sprite rightEdge;
        public Sprite topTip;
        public Sprite bottomTip;
        public Sprite leftTip;
        public Sprite rightTip;
        public Sprite verticalTile;
        public Sprite horizontalTile;
        public Sprite verticalWithLeftConnectionTile;
        public Sprite verticalWithRightConnectionTile;
        public Sprite horizontalWithUpConnectionTile;
        public Sprite horizontalWithDownConnectionTile;
        public Sprite onlyVerticalAndHorizontalConnectionTile;
        public Sprite upperAndLeftTile;
        public Sprite upperAndRightTile;
        public Sprite lowerAndLeftTile;
        public Sprite lowerAndRightTile;
        public Sprite singleTileStandalone;
        public Sprite topLeftConvexCorner;
        public Sprite topRightConvexCorner;
        public Sprite bottomLeftConvexCorner;
        public Sprite bottomRightConvexCorner;
        public Sprite singleInnerTile;
        public Sprite errorTile;

        public Color GetColorForTile(Vector2Int coordinates, Color currentColor)
        {
            if (colorRuleset == null)
                return currentColor;

            Color ruleColor = colorRuleset.GetTileColor(coordinates);
            return colorRuleset.ApplyBlend(currentColor, ruleColor);
        }
    }


    [System.Serializable]
    public abstract class ColorRuleSet
    {
        public enum BlendMode
        {
            Replace,
            Additive,
            Subtractive,
            Multiplicative
        }

        public BlendMode blendMode = BlendMode.Replace;

        public abstract Color GetTileColor(Vector2Int coordinates);

        public Color ApplyBlend(Color currentColor, Color ruleColor)
        {
            switch (blendMode)
            {
                case BlendMode.Additive:
                    return new Color(
                        Mathf.Clamp01(currentColor.r + ruleColor.r),
                        Mathf.Clamp01(currentColor.g + ruleColor.g),
                        Mathf.Clamp01(currentColor.b + ruleColor.b),
                        Mathf.Clamp01(currentColor.a + ruleColor.a));
                case BlendMode.Subtractive:
                    return new Color(
                        Mathf.Clamp01(currentColor.r - ruleColor.r),
                        Mathf.Clamp01(currentColor.g - ruleColor.g),
                        Mathf.Clamp01(currentColor.b - ruleColor.b),
                        Mathf.Clamp01(currentColor.a - ruleColor.a));
                case BlendMode.Multiplicative:
                    return new Color(
                        currentColor.r * ruleColor.r,
                        currentColor.g * ruleColor.g,
                        currentColor.b * ruleColor.b,
                        currentColor.a * ruleColor.a);
                case BlendMode.Replace:
                default:
                    return ruleColor;
            }
        }
    }

    [System.Serializable]
    public class CheckerboardColorRuleSet : ColorRuleSet
    {
        public CheckerboardColorRuleSet()
        {
            color1 = Color.white;
            color2 = new Color(0.92f, 0.92f, 0.92f, 1f);
        }

        public Color color1;
        public Color color2;

        public override Color GetTileColor(Vector2Int coordinates)
        {
            bool isEven = (coordinates.x + coordinates.y) % 2 == 0;
            return isEven ? color1 : color2;
        }
    }
    [System.Serializable]
    public class SingleColorRuleSet : ColorRuleSet
    {

        public Color color;

        public override Color GetTileColor(Vector2Int coordinates) => color;
    }
}