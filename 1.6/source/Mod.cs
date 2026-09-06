using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using Verse;

namespace Psylink_Unbound
{
    public class Mod : Verse.Mod
    {
        public Mod(ModContentPack content) : base(content)
        {
            LongEventHandler.QueueLongEvent(Init, "PsylinkUnbound.LoadingLabel", doAsynchronously: true, null);
        }

        private void Init()
        {
            GetSettings<ModSettings>();
            ModSettings.Validate();
            new Harmony("sk.psyunbound").PatchAll();
            PsylinkCapPatches.ApplyCap();
        }

        public override string SettingsCategory()
        {
            return "PsylinkUnbound.SettingsTitle".Translate();
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            ModSettingsWindow.Draw(inRect);
            base.DoSettingsWindowContents(inRect);
        }
    }
}
