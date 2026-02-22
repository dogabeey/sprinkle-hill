using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Game
{
    public struct Gfx
    {
        public struct Colors
        {
            public struct Items
            {
                public static Color GoodItem = new Color(0f / 255f, 255f / 255f, 0f / 255f);
                public static Color BadItem = new Color(255f / 255f, 0f / 255f, 0f / 255f);
                public static Color Neutralıtem = new Color(255f / 255f, 255f / 255f, 255f / 255f);
            }
            public struct Status
            {
                public static Color poison = new Color(186f / 255f, 85f / 255f, 211f / 255f);
                public static Color regen = Color.red;
                public static Color stun = new Color(255f / 255f, 165f / 255f, 0f / 255f);
                // A colot for confusion status effect.
                public static Color confusion = new Color(255f / 255f, 255f / 255f, 0f / 255f);
            }
        }
        public struct Icon
        {
            public struct Spell
            {
                public const string fire = "icon_spell_fire";
            }
            public struct Status
            {
                public const string poison = "icon_status_poison";
                public const string regen = "icon_status_regen";
                public const string stun = "icon_status_stun";
                public const string confusion = "icon_status_confusion";
            }
        }
    }
}
