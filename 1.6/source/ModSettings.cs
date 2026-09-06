using UnityEngine;
using Verse;

namespace Psylink_Unbound
{
    public class ModSettings : Verse.ModSettings
    {
        public const int MinPsylinkLevel = 6;
        public const int MaxPsylinkLevelCap = 100;
        public const float MinReductionPerLevel = 0f;
        public const float MaxReductionPerLevel = 0.05f;

        public const int DefaultMaxPsylinkLevel = 20;
        public const float DefaultReductionPerLevel = 0.01f;

        public static int maxPsylinkLevel = DefaultMaxPsylinkLevel;
        public static float psyfocusCostReductionPerLevel = DefaultReductionPerLevel;

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Values.Look(ref maxPsylinkLevel, "maxPsylinkLevel", DefaultMaxPsylinkLevel);
            Scribe_Values.Look(ref psyfocusCostReductionPerLevel, "psyfocusCostReductionPerLevel", DefaultReductionPerLevel);

            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Validate();
            }
        }

        public static void Validate()
        {
            maxPsylinkLevel = Mathf.Clamp(maxPsylinkLevel, MinPsylinkLevel, MaxPsylinkLevelCap);
            psyfocusCostReductionPerLevel = Mathf.Clamp(psyfocusCostReductionPerLevel, MinReductionPerLevel, MaxReductionPerLevel);
        }
    }
}
