#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Editor
{
    /// <summary>Creates editable starter prefabs. Run from Tools/Game/Market/Create Default Prefabs.</summary>
    public static class MarketPrefabCreator
    {
        private const string Root = "Assets/Resources/Market";

        [MenuItem("Tools/Game/Market/Create Default Prefabs")]
        public static void CreateDefaultPrefabs()
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Market Panel.prefab") != null)
            {
                EditorUtility.DisplayDialog("Market prefabs already exist", "Delete or move the existing Resources/Market prefabs before creating new defaults.", "OK");
                return;
            }

            EnsureFolder("Assets/Resources");
            EnsureFolder(Root);
            MarketListingView listing = CreateListingPrefab();
            CreateMarketPanelPrefab(listing);
            CreateManagerPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Market Panel.prefab");
        }

        private static void CreateManagerPrefab()
        {
            GameObject manager = new GameObject("Market Manager");
            manager.AddComponent<MarketManager>();
            PrefabUtility.SaveAsPrefabAsset(manager, Root + "/Market Manager.prefab");
            Object.DestroyImmediate(manager);
        }

        private static MarketListingView CreateListingPrefab()
        {
            GameObject root = UIObject("Market Listing", null);
            root.AddComponent<Image>().color = new Color(.12f, .2f, .36f, 1f);
            VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(18, 18, 14, 14);
            layout.spacing = 8;
            root.AddComponent<LayoutElement>().minHeight = 170;
            MarketListingView view = root.AddComponent<MarketListingView>();
            Image icon = UIObject("Icon", root.transform).AddComponent<Image>();
            icon.preserveAspect = true;
            TMP_Text title = Text("Title", root.transform, "PRODUCT", 30);
            TMP_Text description = Text("Description", root.transform, "Product description", 20);
            Transform offers = UIObject("Offers", root.transform).transform;
            offers.gameObject.AddComponent<HorizontalLayoutGroup>().spacing = 10;
            MarketOfferButton offer = CreateOfferPrefab();
            Set(view, "icon", icon); Set(view, "titleText", title); Set(view, "descriptionText", description);
            Set(view, "offerContainer", offers); Set(view, "offerButtonPrefab", offer);
            MarketListingView prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Market Listing.prefab").GetComponent<MarketListingView>();
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static MarketOfferButton CreateOfferPrefab()
        {
            GameObject root = UIObject("Market Offer", null);
            root.AddComponent<Image>().color = new Color(.2f, .53f, .82f);
            Button button = root.AddComponent<Button>();
            root.AddComponent<LayoutElement>().minHeight = 62;
            MarketOfferButton offer = root.AddComponent<MarketOfferButton>();
            TMP_Text amount = Text("Amount", root.transform, "x1", 20);
            TMP_Text price = Text("Price", root.transform, "100", 20);
            GameObject ad = UIObject("Ad Indicator", root.transform); Text("Label", ad.transform, "AD", 18);
            Set(offer, "button", button); Set(offer, "amountText", amount); Set(offer, "priceText", price); Set(offer, "adIndicator", ad);
            MarketOfferButton prefab = PrefabUtility.SaveAsPrefabAsset(root, Root + "/Market Offer.prefab").GetComponent<MarketOfferButton>();
            Object.DestroyImmediate(root);
            return prefab;
        }

        private static void CreateMarketPanelPrefab(MarketListingView listing)
        {
            GameObject root = UIObject("Market Panel", null);
            Stretch(root.GetComponent<RectTransform>());
            root.AddComponent<Image>().color = new Color(.06f, .1f, .19f, .98f);
            VerticalLayoutGroup layout = root.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(40, 40, 44, 38); layout.spacing = 16;
            MarketScreen screen = root.AddComponent<MarketScreen>();
            Text("Title", root.transform, "MARKET", 48).alignment = TextAlignmentOptions.Center;
            GameObject scroll = UIObject("Category Scroll View", root.transform); scroll.AddComponent<Image>().color = new Color(1, 1, 1, .04f);
            scroll.AddComponent<LayoutElement>().flexibleHeight = 1;
            Transform categoryRoot = UIObject("Categories", scroll.transform).transform;
            categoryRoot.gameObject.AddComponent<VerticalLayoutGroup>().spacing = 20;
            List<MarketCategoryContainer> containers = new List<MarketCategoryContainer>();
            foreach (MarketCategory category in System.Enum.GetValues(typeof(MarketCategory))) containers.Add(Category(category, categoryRoot));
            GameObject empty = UIObject("Empty State", root.transform); Text("Text", empty.transform, "No offers available", 24).alignment = TextAlignmentOptions.Center;
            GameObject closeObject = UIObject("Close Button", root.transform); closeObject.AddComponent<Image>().color = new Color(.18f, .68f, .42f); Button close = closeObject.AddComponent<Button>(); Text("Label", closeObject.transform, "CLOSE", 24).alignment = TextAlignmentOptions.Center;
            Set(screen, "listingPrefab", listing); Set(screen, "closeButton", close); Set(screen, "emptyState", empty); Set(screen, "categoryContainers", containers);
            PrefabUtility.SaveAsPrefabAsset(root, Root + "/Market Panel.prefab");
            Object.DestroyImmediate(root);
        }

        private static MarketCategoryContainer Category(MarketCategory category, Transform parent)
        {
            GameObject root = UIObject(category + " Category", parent); root.AddComponent<VerticalLayoutGroup>().spacing = 8;
            MarketCategoryContainer container = root.AddComponent<MarketCategoryContainer>();
            Text("Header", root.transform, category.ToString().ToUpperInvariant(), 28);
            Transform content = UIObject("Content", root.transform).transform; content.gameObject.AddComponent<VerticalLayoutGroup>().spacing = 10;
            GameObject empty = UIObject("Empty", root.transform); Text("Text", empty.transform, "No offers", 18);
            Set(container, "category", category); Set(container, "content", content); Set(container, "emptyState", empty); return container;
        }

        private static GameObject UIObject(string name, Transform parent)
        {
            GameObject result = new GameObject(name, typeof(RectTransform)); result.transform.SetParent(parent, false); return result;
        }
        private static TMP_Text Text(string name, Transform parent, string value, float size)
        {
            GameObject result = UIObject(name, parent); TMP_Text text = result.AddComponent<TextMeshProUGUI>(); text.font = TMP_Settings.defaultFontAsset; text.text = value; text.fontSize = size; text.color = Color.white; result.AddComponent<LayoutElement>().minHeight = size + 8; Stretch(text.rectTransform); return text;
        }
        private static void Stretch(RectTransform transform) { transform.anchorMin = Vector2.zero; transform.anchorMax = Vector2.one; transform.offsetMin = Vector2.zero; transform.offsetMax = Vector2.zero; }
        private static void Set(Object target, string property, object value) { SerializedObject serialized = new SerializedObject(target); SerializedProperty p = serialized.FindProperty(property); if (value is Object obj) p.objectReferenceValue = obj; else if (value is System.Enum e) p.enumValueIndex = System.Convert.ToInt32(e); else if (value is List<MarketCategoryContainer> list) { p.arraySize = list.Count; for (int i = 0; i < list.Count; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = list[i]; } serialized.ApplyModifiedPropertiesWithoutUndo(); }
        private static void EnsureFolder(string path) { if (!AssetDatabase.IsValidFolder(path)) AssetDatabase.CreateFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'), System.IO.Path.GetFileName(path)); }
    }
}
#endif
