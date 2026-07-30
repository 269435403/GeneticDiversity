using System;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GeneticDiversity
{
    [HarmonyPatch(typeof(PawnGenerator), "GenerateGenes", new Type[] { typeof(Pawn), typeof(XenotypeDef), typeof(PawnGenerationRequest) })]
    [HarmonyAfter("rimworld.erdelf.alien_race.main", GD_FrdAdapter.HarmonyId)]
    internal static class GD_Patch_PawnGenerator_GenerateGenes
    {
        [HarmonyPostfix]
        private static void Postfix(Pawn pawn, XenotypeDef xenotype, PawnGenerationRequest request)
        {
            GD_Diagnostics.RecordPatchCall();

            string skipReason;
            if (!ShouldProcess(pawn, request, out skipReason))
            {
                GD_Diagnostics.RecordSkipped(skipReason);
                return;
            }

            GD_GenePoolSnapshot snapshot = GD_WorldGenePool.Current;
            GD_Settings settings = GD_SettingsAccess.Current;
            int slots = settings.RollVariationSlotCount(pawn, request);
            GD_Diagnostics.RecordEligiblePawn(slots, pawn.def, pawn.def != ThingDefOf.Human);
            if (slots == 0)
            {
                return;
            }

            GD_GeneSelector.AddVariations(pawn, request, snapshot, slots);
        }

        private static bool ShouldProcess(Pawn pawn, PawnGenerationRequest request, out string reason)
        {
            GD_Settings settings = GD_SettingsAccess.Current;
            if (!settings.SettingsAffectPawn(pawn, request))
            {
                reason = settings.Enabled ? "disabled for faction" : "mod disabled";
                return false;
            }

            if (!ModsConfig.BiotechActive)
            {
                reason = "Biotech inactive";
                return false;
            }

            if (request.AllowedDevelopmentalStages.Newborn())
            {
                reason = "newborn request";
                return false;
            }

            if (request.ForcedCustomXenotype != null)
            {
                reason = "forced custom xenotype";
                return false;
            }

            if (!request.ForcedEndogenes.NullOrEmpty())
            {
                reason = "forced endogenes";
                return false;
            }

            if (!request.ForcedXenogenes.NullOrEmpty())
            {
                reason = "forced xenogenes";
                return false;
            }

            if (pawn?.genes == null)
            {
                reason = "missing gene tracker";
                return false;
            }

            if (GD_CompatibilityRegistry.IsPawnKindExcluded(pawn.kindDef, pawn.def))
            {
                reason = "pawn kind excluded by precise compatibility rule";
                return false;
            }

            if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike)
            {
                reason = "non-humanlike race";
                return false;
            }

            if (pawn.def == ThingDefOf.Human)
            {
                reason = null;
                return true;
            }

            if (!GD_HarAdapter.IsAvailable)
            {
                reason = "non-Human race while HAR is not detected";
                return false;
            }

            if (GD_HarAdapter.AdapterFailed)
            {
                reason = "non-Human race while HAR adapter failed";
                return false;
            }

            if (!GD_HarAdapter.IsHarRace(pawn.def))
            {
                reason = "unsupported non-HAR Humanlike race";
                return false;
            }

            reason = null;
            return true;
        }
    }
}

