using UnityEngine;
using Verse;

namespace Psylink_Unbound
{
    public static class ModSettingsWindow
    {
        private const float ExampleVanillaCost = 0.5f;

        public static void Draw(Rect parent)
        {
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(parent);

            listing.Label("PsylinkUnbound.MaxLevelLabel".Translate(ModSettings.maxPsylinkLevel));
            ModSettings.maxPsylinkLevel = Mathf.RoundToInt(listing.Slider(ModSettings.maxPsylinkLevel, ModSettings.MinPsylinkLevel, ModSettings.MaxPsylinkLevelCap));

            listing.Gap();

            listing.Label("PsylinkUnbound.ReductionLabel".Translate(ModSettings.psyfocusCostReductionPerLevel.ToStringPercent()));
            ModSettings.psyfocusCostReductionPerLevel = listing.Slider(ModSettings.psyfocusCostReductionPerLevel, ModSettings.MinReductionPerLevel, ModSettings.MaxReductionPerLevel);

            listing.GapLine();

            float exampleMultiplier = Mathf.Clamp(1f - ModSettings.maxPsylinkLevel * ModSettings.psyfocusCostReductionPerLevel, 0.05f, 1f);
            float exampleAdjustedCost = ExampleVanillaCost * exampleMultiplier;
            listing.Label("PsylinkUnbound.ExampleLabel".Translate(ModSettings.maxPsylinkLevel, ExampleVanillaCost.ToStringPercent(), exampleAdjustedCost.ToStringPercent()));

            listing.End();

            if (GUI.changed)
            {
                ModSettings.Validate();
                PsylinkCapPatches.ApplyCap();
                PsyfocusCostUtility.ClearCache();
                LoadedModManager.GetMod<Mod>().WriteSettings();
            }
        }
    }
}
