using System.Linq;
using HarmonyLib;
using RimWorld;
using Verse;

namespace Psylink_Unbound
{
    public static class PsylinkCapPatches
    {
        public static void ApplyCap()
        {
            HediffDef amplifier = HediffDefOf.PsychicAmplifier;
            if (amplifier == null)
            {
                Log.Warning("[Psylink Unbound] HediffDefOf.PsychicAmplifier was not resolved; cannot apply the configured psylink cap.");
                return;
            }

            amplifier.maxSeverity = ModSettings.maxPsylinkLevel;
        }
    }

    [HarmonyPatch(typeof(Hediff_Psylink), nameof(Hediff_Psylink.TryGiveAbilityOfLevel))]
    public static class Hediff_Psylink_TryGiveAbilityOfLevel_Patch
    {
        public static bool Prefix(int abilityLevel)
        {
            bool anyDefined = DefDatabase<AbilityDef>.AllDefsListForReading.Any(a => a.IsPsycast && a.level == abilityLevel);
            if (anyDefined)
            {
                return true;
            }

            Logger.Message($"No AbilityDef found for psycast level {abilityLevel}; skipping TryGiveAbilityOfLevel.");
            return false;
        }
    }
}
