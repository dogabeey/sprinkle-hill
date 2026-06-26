using System.ComponentModel;
using Game.Singleton;
using UnityEditor;
using UnityEngine.AddressableAssets;

namespace Game
{
    [InitializeOnLoad]
    public static class EditorAddressables
    {
        public static HorizontalRocketElementData horizontalRocketData;
        public static VerticalRocketElementData verticalRocketData;
        public static BombElementData bombData;
        public static PropellerElementData propellerData;
        public static DiscoBallElementData discoBallData;
        public static CauldronElementData cauldronElementData;
        public static GarbageBagElementData garbageBagElementData;
        public static PowerGeneratorElementData powerGeneratorElementData;
        public static PowerOutletElementData powerOutletElementData;
        public static BreakableBoxElementData breakableBoxElementData;
        public static BreakableBoxElementData jarElementData;

        static EditorAddressables()
        {
            Initialize();
        }

        private static void Initialize()
        {
            horizontalRocketData = LoadAsset<HorizontalRocketElementData>("Powerup/HorizontalRocket");
            verticalRocketData = LoadAsset<VerticalRocketElementData>("Powerup/VerticalRocket");
            bombData = LoadAsset<BombElementData>("Powerup/Bomb");
            propellerData = LoadAsset<PropellerElementData>("Powerup/Propeller");
            discoBallData = LoadAsset<DiscoBallElementData>("Powerup/DiscoBall");
            cauldronElementData = LoadAsset<CauldronElementData>("SpecialElement/Cauldron");
            garbageBagElementData = LoadAsset<GarbageBagElementData>("SpecialElement/GarbageBag");
            breakableBoxElementData = LoadAsset<BreakableBoxElementData>("SpecialElement/BreakableBox");
            jarElementData = LoadAsset<BreakableBoxElementData>("SpecialElement/Jar");
            powerGeneratorElementData = LoadAsset<PowerGeneratorElementData>("SpecialElement/PowerGenerator");
            powerOutletElementData = LoadAsset<PowerOutletElementData>("SpecialElement/PowerOutlet");
        }

        public static T LoadAsset<T>(string address) where T : UnityEngine.Object
        {
            return Addressables.LoadAssetAsync<T>(address).WaitForCompletion();
        }
    }
}

