using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace WishboneHighlight
{
    [BepInPlugin(WishboneHighlightInfo.MOD_ID,WishboneHighlightInfo.MOD_NAME,WishboneHighlightInfo.MOD_VERSION)]
    [BepInProcess("valheim.exe")]
    internal class WishboneHighlight : BaseUnityPlugin
    {
        private static ConfigEntry<bool> _enabled;
        private static ConfigEntry<bool> _arrowAlwaysVisible;
        private static ConfigEntry<Color> _arrowColor;
        private static ConfigEntry<float> _arrowSize;
        private static ConfigEntry<float> _arrowOffset;
        private static ConfigEntry<float> _worldMarkerSize;
        private static ConfigEntry<Color> _worldMarkerColor;

        public static readonly Harmony Harmony = new Harmony(WishboneHighlightInfo.MOD_ID);

        public static WishboneHighlight Instance { get; private set; }
        public  WishboneHighlightIndicator Indicator { get; private set; }

        private void Awake()
        {
            _enabled = Config.Bind(new ConfigDefinition("General", "Enabled"), true,
                new ConfigDescription("Is Mod Enabled"));
            _arrowAlwaysVisible = Config.Bind(new ConfigDefinition("Arrow", "Always Visible"), true,
                new ConfigDescription(
                    "If enabled hud arrow would always be on the screen. If not - only if the world marker is not on the screen"));
            ColorUtility.TryParseHtmlString("#00FF009E", out var highlightColor);
            _arrowColor = Config.Bind(new ConfigDefinition("Arrow", "Color"), highlightColor);
            _arrowSize = Config.Bind(new ConfigDefinition("Arrow", "Size"), 80f);
            _arrowOffset = Config.Bind(new ConfigDefinition("Arrow", "Offset From The Top"), 110f);
            _worldMarkerSize = Config.Bind(new ConfigDefinition("World Marker", "Size"), 100f);
            _worldMarkerColor = Config.Bind(new ConfigDefinition("World Marker", "Color"), highlightColor);
            Instance = this;
            Indicator = gameObject.AddComponent<WishboneHighlightIndicator>();
            Harmony.PatchAll();
        }

        private void OnDestroy()
        {
            Harmony?.UnpatchSelf();
        }

        internal new ManualLogSource Logger => base.Logger;
        internal bool IsEnabled => _enabled.Value;
        internal bool ArrowAlwaysVisible => _arrowAlwaysVisible.Value;
        internal Color ArrowColor => _arrowColor.Value;
        internal float ArrowSize => _arrowSize.Value;
        internal float ArrowOffset => _arrowOffset.Value;
        internal float WorldMarkerSize => _worldMarkerSize.Value;
        internal Color WorldMarkerColor => _worldMarkerColor.Value;

    }
}
