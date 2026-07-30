using HarmonyLib;
using Verse;

namespace GeneticDiversity
{
    [StaticConstructorOnStartup]
    internal static class GD_HarmonyBootstrap
    {
        internal const string HarmonyId = "yyyyy.geneticdiversity";

        static GD_HarmonyBootstrap()
        {
            new Harmony(HarmonyId).PatchAll();
            LongEventHandler.ExecuteWhenFinished(delegate
            {
                GD_CompatibilityRegistry.Initialize();
                GD_Log.Message("Phase 6 loaded. Settings, statistics, and opt-in old-save supplementation are active; vanilla Human and optional HAR Humanlike generation diversity retain the accepted safety boundaries. HAR status: " + GD_HarAdapter.StatusLabel + ". Precise compatibility: " + GD_CompatibilityRegistry.BuildStatusReport() + ". FRD integration: " + GD_FrdAdapter.BuildStatusReport());
            });
        }
    }
}

