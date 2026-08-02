using UnityEngine;
using Verse;

namespace GeneticDiversity
{
    internal sealed class GD_Mod : Mod
    {
        internal static GD_Mod Instance;
        internal GD_Settings Settings { get; private set; }

        public GD_Mod(ModContentPack content) : base(content)
        {
            Instance = this;
            Settings = GetSettings<GD_Settings>();
            Settings.Normalize();
            GD_SettingsAccess.ApplyChanged(force: true, logChange: false);
        }

        public override string SettingsCategory()
        {
            return "Mutation and Hererity";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            GD_SettingsWindow.Draw(inRect, Settings);
        }
    }
}
