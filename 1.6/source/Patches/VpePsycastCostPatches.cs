using System;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using RimWorld.Planet;
using Verse;

namespace Psylink_Unbound
{
    internal static class VpePresence
    {
        internal const string PackageId = "VanillaExpanded.VPsycastsE";

        internal static bool IsActive()
        {
            return ModsConfig.IsActive(PackageId);
        }
    }

    [HarmonyPatch]
    public static class Vpe_GetPsyfocusUsedByPawn_Patch
    {
        public static bool Prepare()
        {
            return VpePresence.IsActive();
        }

        public static MethodBase TargetMethod()
        {
            MethodInfo method = AccessTools.Method("VanillaPsycastsExpanded.AbilityExtension_Psycast:GetPsyfocusUsedByPawn");
            if (method == null)
            {
                Log.Warning("[Psylink Unbound] VPE integration: AbilityExtension_Psycast.GetPsyfocusUsedByPawn not found. The psylink discount will not apply to VPE psycasts - this likely means VPE's API changed.");
            }

            return method;
        }

        public static void Postfix(Pawn pawn, ref float __result)
        {
            __result = PsyfocusCostUtility.AdjustCost(pawn, __result);
        }
    }

    [HarmonyPatch]
    public static class Vpe_WordOfSerenity_Patch
    {
        private static FieldInfo abilityPawnField;

        public static bool Prepare()
        {
            return VpePresence.IsActive();
        }

        public static IEnumerable<MethodBase> TargetMethods()
        {
            Type wosType = AccessTools.TypeByName("VanillaPsycastsExpanded.AbilityExtension_PsycastWordOfSerenity");
            Type abilityType = AccessTools.TypeByName("VEF.Abilities.Ability");
            Type targetsType = typeof(GlobalTargetInfo[]);

            MethodInfo cast = wosType != null && abilityType != null
                ? AccessTools.Method(wosType, "Cast", new[] { targetsType, abilityType })
                : null;
            MethodInfo valid = wosType != null && abilityType != null
                ? AccessTools.Method(wosType, "Valid", new[] { targetsType, abilityType, typeof(bool) })
                : null;

            if (cast != null)
            {
                yield return cast;
            }
            else
            {
                Log.Warning("[Psylink Unbound] VPE integration: AbilityExtension_PsycastWordOfSerenity.Cast not found. Word of Serenity's tiered cost will NOT be discounted at cast time - this likely means VPE's API changed.");
            }

            if (valid != null)
            {
                yield return valid;
            }
            else
            {
                Log.Warning("[Psylink Unbound] VPE integration: AbilityExtension_PsycastWordOfSerenity.Valid not found. Word of Serenity's tiered cost will NOT be discounted at the affordability check - this likely means VPE's API changed.");
            }
        }

        public static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions, MethodBase original)
        {
            MethodInfo psyfocusCostForTargetMethod = AccessTools.Method("VanillaPsycastsExpanded.AbilityExtension_PsycastWordOfSerenity:PsyfocusCostForTarget");
            MethodInfo adjustCostForPatchMethod = AccessTools.Method(typeof(PsyfocusCostUtility), nameof(PsyfocusCostUtility.AdjustCostForPatch));
            if (abilityPawnField == null)
            {
                abilityPawnField = AccessTools.Field("VEF.Abilities.Ability:pawn");
            }

            int patchedCount = 0;
            foreach (CodeInstruction instruction in instructions)
            {
                yield return instruction;
                if (psyfocusCostForTargetMethod != null && instruction.Calls(psyfocusCostForTargetMethod))
                {
                    yield return new CodeInstruction(OpCodes.Ldarg_2);
                    yield return new CodeInstruction(OpCodes.Ldfld, abilityPawnField);
                    yield return new CodeInstruction(OpCodes.Call, adjustCostForPatchMethod);
                    patchedCount++;
                }
            }

            if (patchedCount == 0)
            {
                Log.Warning($"[Psylink Unbound] VPE integration: could not find the expected PsyfocusCostForTarget read in {original.DeclaringType?.FullName}.{original.Name}. Word of Serenity's cost will NOT be discounted there - this likely means VPE's API changed.");
            }
        }
    }
}
