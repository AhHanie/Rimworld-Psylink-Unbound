using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using RimWorld;
using UnityEngine;
using Verse;

namespace Psylink_Unbound
{
    public static class PsyfocusCostUtility
    {
        private const float MinMultiplier = 0.05f;

        private static readonly Regex PercentPattern = new Regex(@"(\d+(?:\.\d+)?)%", RegexOptions.Compiled);

        private static readonly Dictionary<int, float> multiplierByLevel = new Dictionary<int, float>();
        private static readonly Dictionary<AbilityDef, Dictionary<int, string>> percentStringByDefAndLevel = new Dictionary<AbilityDef, Dictionary<int, string>>();
        private static readonly Dictionary<AbilityCompProperties, string> rawCompExplanationByComp = new Dictionary<AbilityCompProperties, string>();
        private static readonly Dictionary<AbilityCompProperties, Dictionary<int, string>> adjustedCompExplanationByCompAndLevel = new Dictionary<AbilityCompProperties, Dictionary<int, string>>();
        private static LoadedLanguage compExplanationLanguage;

        public static void ClearCache()
        {
            multiplierByLevel.Clear();
            percentStringByDefAndLevel.Clear();
            adjustedCompExplanationByCompAndLevel.Clear();
        }

        private static float GetReductionMultiplierForLevel(int level)
        {
            if (level <= 0)
            {
                return 1f;
            }

            if (multiplierByLevel.TryGetValue(level, out float cached))
            {
                return cached;
            }

            float multiplier = Mathf.Clamp(1f - level * ModSettings.psyfocusCostReductionPerLevel, MinMultiplier, 1f);
            multiplierByLevel[level] = multiplier;
            return multiplier;
        }

        public static float GetReductionMultiplier(Pawn pawn)
        {
            if (pawn?.health == null)
            {
                return 1f;
            }

            return GetReductionMultiplierForLevel(pawn.GetPsylinkLevel());
        }

        public static float AdjustCost(Pawn pawn, float vanillaCost)
        {
            if (pawn == null || vanillaCost <= 0f)
            {
                return vanillaCost;
            }

            return vanillaCost * GetReductionMultiplier(pawn);
        }

        public static float AdjustCostForPatch(float vanillaCost, Pawn pawn)
        {
            return AdjustCost(pawn, vanillaCost);
        }

        public static FloatRange GetAdjustedCostRange(Pawn pawn, FloatRange vanillaRange)
        {
            if (pawn == null)
            {
                return vanillaRange;
            }

            float multiplier = GetReductionMultiplier(pawn);
            return new FloatRange(vanillaRange.min * multiplier, vanillaRange.max * multiplier);
        }

        public static string GetAdjustedCostPercentString(Pawn pawn, AbilityDef def)
        {
            if (pawn == null || def == null)
            {
                return AdjustCost(pawn, def?.PsyfocusCost ?? 0f).ToStringPercent();
            }

            int level = pawn.GetPsylinkLevel();
            if (!percentStringByDefAndLevel.TryGetValue(def, out Dictionary<int, string> byLevel))
            {
                byLevel = new Dictionary<int, string>();
                percentStringByDefAndLevel[def] = byLevel;
            }

            if (!byLevel.TryGetValue(level, out string cached))
            {
                cached = (def.PsyfocusCost * GetReductionMultiplierForLevel(level)).ToStringPercent();
                byLevel[level] = cached;
            }

            return cached;
        }

        public static string GetAdjustedCompExplanation(Pawn pawn, AbilityCompProperties comp, out string rawExplanation)
        {
            if (compExplanationLanguage != LanguageDatabase.activeLanguage)
            {
                compExplanationLanguage = LanguageDatabase.activeLanguage;
                rawCompExplanationByComp.Clear();
                adjustedCompExplanationByCompAndLevel.Clear();
            }

            if (!rawCompExplanationByComp.TryGetValue(comp, out rawExplanation))
            {
                rawExplanation = comp.PsyfocusCostExplanation;
                rawCompExplanationByComp[comp] = rawExplanation;
            }

            if (pawn == null || string.IsNullOrEmpty(rawExplanation))
            {
                return rawExplanation;
            }

            int level = pawn.GetPsylinkLevel();
            if (!adjustedCompExplanationByCompAndLevel.TryGetValue(comp, out Dictionary<int, string> byLevel))
            {
                byLevel = new Dictionary<int, string>();
                adjustedCompExplanationByCompAndLevel[comp] = byLevel;
            }

            if (!byLevel.TryGetValue(level, out string adjusted))
            {
                float multiplier = GetReductionMultiplierForLevel(level);
                adjusted = PercentPattern.Replace(rawExplanation, match =>
                {
                    float rawPercent = float.Parse(match.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture);
                    return (rawPercent / 100f * multiplier).ToStringPercent();
                });
                byLevel[level] = adjusted;
            }

            return adjusted;
        }
    }
}
