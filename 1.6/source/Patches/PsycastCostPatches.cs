using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace Psylink_Unbound
{
    internal static class PsycastCostReflection
    {
        internal static readonly MethodInfo PsyfocusCostGetter = AccessTools.PropertyGetter(typeof(AbilityDef), nameof(AbilityDef.PsyfocusCost));
        internal static readonly MethodInfo PsyfocusCostPercentGetter = AccessTools.PropertyGetter(typeof(AbilityDef), nameof(AbilityDef.PsyfocusCostPercent));
        internal static readonly MethodInfo PsyfocusCostRangeGetter = AccessTools.PropertyGetter(typeof(AbilityDef), nameof(AbilityDef.PsyfocusCostRange));
        internal static readonly FieldInfo AbilityPawnField = AccessTools.Field(typeof(Ability), nameof(Ability.pawn));
        internal static readonly FieldInfo AbilityDefField = AccessTools.Field(typeof(Ability), nameof(Ability.def));
        internal static readonly FieldInfo FloatRangeMinField = AccessTools.Field(typeof(FloatRange), nameof(FloatRange.min));
        internal static readonly FieldInfo GizmoTrackerField = AccessTools.Field(typeof(PsychicEntropyGizmo), "tracker");
        internal static readonly MethodInfo TrackerPawnGetter = AccessTools.PropertyGetter(typeof(Pawn_PsychicEntropyTracker), nameof(Pawn_PsychicEntropyTracker.Pawn));
        internal static readonly MethodInfo AdjustCostForPatchMethod = AccessTools.Method(typeof(PsyfocusCostUtility), nameof(PsyfocusCostUtility.AdjustCostForPatch));
        internal static readonly MethodInfo GetAdjustedCostPercentStringMethod = AccessTools.Method(typeof(PsyfocusCostUtility), nameof(PsyfocusCostUtility.GetAdjustedCostPercentString));

        internal static IEnumerable<CodeInstruction> InjectPawnCostAdjustment(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            int patchedCount = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;
                if (instruction.Calls(PsyfocusCostGetter))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, AbilityPawnField);
                    yield return new CodeInstruction(OpCodes.Call, AdjustCostForPatchMethod);
                    patchedCount++;
                }
            }

            if (patchedCount == 0)
            {
                Log.Warning($"[Psylink Unbound] Could not find the expected AbilityDef.PsyfocusCost read in {original.DeclaringType?.FullName}.{original.Name}. The psylink cost discount will NOT apply there - this likely means the RimWorld version changed this method.");
            }
        }
    }

    internal static class CachedTranslations
    {
        private static LoadedLanguage cachedLanguage;
        private static string neuralHeatLetter;
        private static string psyfocusLetterPrefix;
        private static string abilityPsyfocusCostPrefix;
        private static Dictionary<AbilityDef, string> heatLineByDef = new Dictionary<AbilityDef, string>();

        private static void RefreshIfNeeded()
        {
            if (cachedLanguage == LanguageDatabase.activeLanguage)
            {
                return;
            }

            cachedLanguage = LanguageDatabase.activeLanguage;
            neuralHeatLetter = "NeuralHeatLetter".Translate();
            psyfocusLetterPrefix = "PsyfocusLetter".Translate() + ": ";
            abilityPsyfocusCostPrefix = "AbilityPsyfocusCost".Translate() + ": ";
            heatLineByDef = new Dictionary<AbilityDef, string>();
        }

        internal static string PsyfocusLetterPrefix
        {
            get
            {
                RefreshIfNeeded();
                return psyfocusLetterPrefix;
            }
        }

        internal static string AbilityPsyfocusCostPrefix
        {
            get
            {
                RefreshIfNeeded();
                return abilityPsyfocusCostPrefix;
            }
        }

        internal static string GetHeatLine(AbilityDef def)
        {
            RefreshIfNeeded();
            if (!heatLineByDef.TryGetValue(def, out string line))
            {
                line = neuralHeatLetter + ": " + def.EntropyGain + "\n";
                heatLineByDef[def] = line;
            }

            return line;
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

    [HarmonyPatch(typeof(Psycast))]
    [HarmonyPatch(nameof(Psycast.CanCast), MethodType.Getter)]
    public static class Psycast_CanCast_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            return PsycastCostReflection.InjectPawnCostAdjustment(instructions, original);
        }
    }

    [HarmonyPatch(typeof(Psycast), nameof(Psycast.GizmoDisabled))]
    public static class Psycast_GizmoDisabled_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            int costPatchedCount = 0;
            int percentPatchedCount = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;
                if (instruction.Calls(PsycastCostReflection.PsyfocusCostGetter))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, PsycastCostReflection.AbilityPawnField);
                    yield return new CodeInstruction(OpCodes.Call, PsycastCostReflection.AdjustCostForPatchMethod);
                    costPatchedCount++;
                }
                else if (instruction.Calls(PsycastCostReflection.PsyfocusCostPercentGetter))
                {
                    yield return new CodeInstruction(OpCodes.Pop);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, PsycastCostReflection.AbilityPawnField);
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, PsycastCostReflection.AbilityDefField);
                    yield return new CodeInstruction(OpCodes.Call, PsycastCostReflection.GetAdjustedCostPercentStringMethod);
                    percentPatchedCount++;
                }
            }

            if (costPatchedCount == 0)
            {
                Log.Warning($"[Psylink Unbound] Could not find the expected AbilityDef.PsyfocusCost read in {original.DeclaringType?.FullName}.{original.Name}. The psylink cost discount will NOT apply to the gizmo-disabled check there - this likely means the RimWorld version changed this method.");
            }

            if (percentPatchedCount == 0)
            {
                Log.Warning($"[Psylink Unbound] Could not find the expected AbilityDef.PsyfocusCostPercent read in {original.DeclaringType?.FullName}.{original.Name}. The insufficient-psyfocus message will show the un-discounted cost there - this likely means the RimWorld version changed this method.");
            }
        }
    }

    [HarmonyPatch(typeof(Psycast), nameof(Psycast.Activate), new[] { typeof(GlobalTargetInfo) })]
    public static class Psycast_ActivateGlobal_Patch
    {
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            return PsycastCostReflection.InjectPawnCostAdjustment(instructions, original);
        }
    }

    [HarmonyPatch(typeof(Command_Psycast))]
    [HarmonyPatch(nameof(Command_Psycast.TopRightLabel), MethodType.Getter)]
    public static class Command_Psycast_TopRightLabel_Patch
    {
        public static bool Prefix(Command_Psycast __instance, ref string __result)
        {
            AbilityDef def = __instance.Ability.def;
            Pawn pawn = __instance.Pawn;
            string text = "";
            if (def.EntropyGain > float.Epsilon)
            {
                text += CachedTranslations.GetHeatLine(def);
            }

            if (def.PsyfocusCost > float.Epsilon)
            {
                string costText;
                if (!def.AnyCompOverridesPsyfocusCost)
                {
                    costText = PsyfocusCostUtility.GetAdjustedCostPercentString(pawn, def);
                }
                else
                {
                    FloatRange adjustedRange = PsyfocusCostUtility.GetAdjustedCostRange(pawn, def.PsyfocusCostRange);
                    costText = (adjustedRange.Span > float.Epsilon)
                        ? (adjustedRange.min * 100f) + "-" + adjustedRange.max.ToStringPercent()
                        : adjustedRange.max.ToStringPercent();
                }

                text += CachedTranslations.PsyfocusLetterPrefix + costText;
            }

            __result = text.TrimEndNewlines();
            return false;
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
        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            List<CodeInstruction> codes = instructions.ToList();
            int patchedCount = 0;
            for (int i = 0; i < codes.Count; i++)
            {
                yield return codes[i];
                if (i + 1 < codes.Count
                    && codes[i].Calls(PsycastCostReflection.PsyfocusCostRangeGetter)
                    && codes[i + 1].LoadsField(PsycastCostReflection.FloatRangeMinField))
                {
                    yield return codes[i + 1];
                    i++;
                    yield return new CodeInstruction(OpCodes.Ldarg_0);
                    yield return new CodeInstruction(OpCodes.Ldfld, PsycastCostReflection.GizmoTrackerField);
                    yield return new CodeInstruction(OpCodes.Callvirt, PsycastCostReflection.TrackerPawnGetter);
                    yield return new CodeInstruction(OpCodes.Call, PsycastCostReflection.AdjustCostForPatchMethod);
                    patchedCount++;
                }
            }

            if (patchedCount == 0)
            {
                Log.Warning("[Psylink Unbound] Could not find the expected AbilityDef.PsyfocusCostRange.min read in PsychicEntropyGizmo.GizmoOnGUI. The hover psyfocus-cost preview will show the un-discounted range - this likely means the RimWorld version changed this method.");
            }
        }
    }
}
