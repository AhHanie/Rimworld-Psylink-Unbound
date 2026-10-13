using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Psylink_Unbound
{
    internal static class CachedTranslations
    {
        private static LoadedLanguage cachedLanguage;
        private static string abilityPsyfocusCostPrefix;

        internal static string AbilityPsyfocusCostPrefix
        {
            get
            {
                if (cachedLanguage != LanguageDatabase.activeLanguage)
                {
                    cachedLanguage = LanguageDatabase.activeLanguage;
                    abilityPsyfocusCostPrefix = "AbilityPsyfocusCost".Translate() + ": ";
                }

                return abilityPsyfocusCostPrefix;
            }
        }
    }

    [HarmonyPatch(typeof(Ability), nameof(Ability.FinalPsyfocusCost))]
    public static class Ability_FinalPsyfocusCost_Patch
    {
        public static void Postfix(Ability __instance, ref float __result)
        {
            if (__instance is Psycast psycast)
            {
                __result = PsyfocusCostUtility.AdjustCost(psycast.pawn, __result);
            }
        }
    }

    [HarmonyPatch(typeof(Psycast), nameof(Psycast.CanCast), MethodType.Getter)]
    public static class Psycast_CanCast_Patch
    {
        [HarmonyInfix(typeof(AbilityDef), nameof(AbilityDef.PsyfocusCost), InnerTargetKind.Getter)]
        public static float Postfix(float __result, [HarmonyOuter] Psycast __instance)
        {
            return PsyfocusCostUtility.AdjustCost(__instance.pawn, __result);
        }
    }

    [HarmonyPatch(typeof(Psycast), nameof(Psycast.GizmoDisabled))]
    public static class Psycast_GizmoDisabled_Patch
    {
        [HarmonyPostfix]
        [HarmonyInfix(typeof(AbilityDef), nameof(AbilityDef.PsyfocusCost), InnerTargetKind.Getter)]
        public static float AdjustCost(float __result, [HarmonyOuter] Psycast __instance)
        {
            return PsyfocusCostUtility.AdjustCost(__instance.pawn, __result);
        }

        [HarmonyPostfix]
        [HarmonyInfix(typeof(AbilityDef), nameof(AbilityDef.PsyfocusCostPercent), InnerTargetKind.Getter)]
        public static string AdjustCostPercent(string __result, [HarmonyOuter] Psycast __instance)
        {
            return PsyfocusCostUtility.GetAdjustedCostPercentString(__instance.pawn, __instance.def);
        }
    }

    [HarmonyPatch(typeof(Psycast), nameof(Psycast.Activate), new[] { typeof(GlobalTargetInfo) })]
    public static class Psycast_ActivateGlobal_Patch
    {
        [HarmonyInfix(typeof(AbilityDef), nameof(AbilityDef.PsyfocusCost), InnerTargetKind.Getter)]
        public static float Postfix(float __result, [HarmonyOuter] Psycast __instance)
        {
            return PsyfocusCostUtility.AdjustCost(__instance.pawn, __result);
        }
    }

    [HarmonyPatch(typeof(Command_Psycast), nameof(Command_Psycast.TopRightLabel), MethodType.Getter)]
    public static class Command_Psycast_TopRightLabel_Patch
    {
        [HarmonyPostfix]
        [HarmonyInfix(typeof(AbilityDef), nameof(AbilityDef.PsyfocusCostPercent), InnerTargetKind.Getter)]
        public static string AdjustCostPercent(string __result, [HarmonyOuter] Command_Psycast __instance)
        {
            return PsyfocusCostUtility.GetAdjustedCostPercentString(__instance.Pawn, __instance.Ability.def);
        }

        [HarmonyPostfix]
        [HarmonyInfix(typeof(AbilityDef), nameof(AbilityDef.PsyfocusCostRange), InnerTargetKind.Getter)]
        public static FloatRange AdjustCostRange(FloatRange __result, [HarmonyOuter] Command_Psycast __instance)
        {
            return PsyfocusCostUtility.GetAdjustedCostRange(__instance.Pawn, __result);
        }

        [HarmonyPostfix]
        [HarmonyInfix(typeof(AbilityDef), nameof(AbilityDef.PsyfocusCostPercentMax), InnerTargetKind.Getter)]
        public static string AdjustCostPercentMax(string __result, [HarmonyOuter] Command_Psycast __instance)
        {
            return PsyfocusCostUtility.GetAdjustedMaxCostPercentString(__instance.Pawn, __instance.Ability.def);
        }
    }

    [HarmonyPatch(typeof(AbilityDef), nameof(AbilityDef.GetTooltip))]
    public static class AbilityDef_GetTooltip_Patch
    {
        public static void Postfix(AbilityDef __instance, Pawn pawn, ref string __result)
        {
            if (pawn == null || __result == null || !__instance.IsPsycast)
            {
                return;
            }

            if (__instance.AnyCompOverridesPsyfocusCost)
            {
                ApplyCompExplanationDiscount(__instance, pawn, ref __result);
                return;
            }

            string prefix = CachedTranslations.AbilityPsyfocusCostPrefix;
            int prefixIndex = __result.IndexOf(prefix);
            if (prefixIndex < 0)
            {
                return;
            }

            int valueStart = prefixIndex + prefix.Length;
            int valueEnd = __result.IndexOf('\n', valueStart);
            string tail = valueEnd < 0 ? "" : __result.Substring(valueEnd);
            string adjustedValue = PsyfocusCostUtility.GetAdjustedCostPercentString(pawn, __instance);
            __result = __result.Substring(0, valueStart) + adjustedValue + tail;
        }

        private static void ApplyCompExplanationDiscount(AbilityDef def, Pawn pawn, ref string result)
        {
            foreach (AbilityCompProperties comp in def.comps)
            {
                if (!comp.OverridesPsyfocusCost)
                {
                    continue;
                }

                string adjustedExplanation = PsyfocusCostUtility.GetAdjustedCompExplanation(pawn, comp, out string rawExplanation);
                if (string.IsNullOrEmpty(rawExplanation) || adjustedExplanation == rawExplanation)
                {
                    return;
                }

                int index = result.IndexOf(rawExplanation);
                if (index < 0)
                {
                    return;
                }

                result = result.Substring(0, index) + adjustedExplanation + result.Substring(index + rawExplanation.Length);
                return;
            }
        }
    }

    [HarmonyPatch(typeof(PsychicEntropyGizmo), nameof(PsychicEntropyGizmo.GizmoOnGUI))]
    public static class PsychicEntropyGizmo_GizmoOnGUI_Patch
    {
        [HarmonyInfix(typeof(AbilityDef), nameof(AbilityDef.PsyfocusCostRange), InnerTargetKind.Getter)]
        public static FloatRange Postfix(FloatRange __result, [HarmonyOuter] Pawn_PsychicEntropyTracker ___tracker)
        {
            return PsyfocusCostUtility.GetAdjustedCostRange(___tracker.Pawn, __result);
        }
    }
}
