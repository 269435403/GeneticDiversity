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
            GD_SettingsAccess.ApplyChanged(force: true);
        }

        public override string SettingsCategory()
        {
            return "\u57fa\u56e0\u591a\u6837\u6027";
        }

        public override void DoSettingsWindowContents(Rect inRect)
        {
            GD_SettingsWindow.Draw(inRect, Settings);
        }
    }
}
