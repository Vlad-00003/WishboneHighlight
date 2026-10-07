using HarmonyLib;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace WishboneHighlight
{
    [HarmonyPatch(typeof(SE_Finder),nameof(SE_Finder.UpdateStatusEffect))]
    internal static class SE_Finder_Patch
    {
        private static FieldInfo m_beacon_field = AccessTools.Field(typeof(SE_Finder), "m_beacon");
        private static List<Component> _components = new List<Component>();
        private static void Postfix(SE_Finder __instance)
        {
            var beacon = m_beacon_field.GetValue(__instance) as Beacon;
            if (beacon == null || !WishboneHighlight.Instance.IsEnabled)
            {
                WishboneHighlightIndicator.CurrentBeacon = null;
                return;
            }
            WishboneHighlightIndicator.CurrentBeacon = beacon;
        }
    }
}
