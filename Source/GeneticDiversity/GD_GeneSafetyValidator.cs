using System.Collections.Generic;
using RimWorld;
using Verse;

namespace GeneticDiversity
{
    internal static class GD_GeneSafetyValidator
    {
        internal static bool Accepts(
            GeneDef candidate,
            Pawn pawn,
            PawnGenerationRequest request,
            GD_GenePoolSnapshot snapshot,
            out string rejectionReason)
        {
            if (candidate == null)
            {
                rejectionReason = "null candidate";
                return false;
            }

            if (IsBlacklisted(candidate, out rejectionReason))
            {
                return false;
            }

            if (pawn?.genes == null)
            {
                rejectionReason = "missing gene tracker";
                return false;
            }

            if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike)
            {
                rejectionReason = "non-humanlike race";
                return false;
            }

            bool harAware = snapshot != null && snapshot.HarAware;
            if (!harAware)
            {
                if (pawn.def != ThingDefOf.Human)
                {
                    rejectionReason = "non-vanilla Human race without HAR adapter";
                    return false;
                }

                if (!GD_WorldGenePool.IsMutationPoolGene(candidate))
                {
                    rejectionReason = "not an eligible vanilla base Gene";
                    return false;
                }
            }
            else
            {
                if (pawn.def != ThingDefOf.Human && !GD_HarAdapter.IsHarRace(pawn.def))
                {
                    rejectionReason = "unsupported non-HAR Humanlike race";
                    return false;
                }

                if (!GD_WorldGenePool.IsWorldPoolGene(candidate))
                {
                    rejectionReason = "not an eligible inheritable pool Gene";
                    return false;
                }

                if (!GD_CompatibilityRegistry.CanUseForTargetRace(candidate, pawn.def))
                {
                    rejectionReason = "precise compatibility rule rejected the gene for target Race";
                    return false;
                }

                GD_GeneCandidateKind kind = GD_WorldGenePool.Classify(candidate);
                bool knownForTarget = GD_WorldGenePool.IsKnownForRace(candidate, pawn.def, snapshot);
                bool sameRaceObserved = snapshot != null && snapshot.WasObservedOnRace(candidate, pawn.def);
                if (GD_SettingsAccess.Current.SameRaceOnly && !sameRaceObserved)
                {
                    rejectionReason = "same-race-only setting rejected cross-race candidate";
                    return false;
                }
                if (!sameRaceObserved && kind == GD_GeneCandidateKind.Standard && !GD_SettingsAccess.Current.AllowStandardCrossRace)
                {
                    rejectionReason = "standard cross-race setting disabled";
                    return false;
                }
                if (kind != GD_GeneCandidateKind.Standard
                    && !knownForTarget
                    && !(GD_SettingsAccess.Current.AllowSpecialCrossRace && !sameRaceObserved))
                {
                    rejectionReason = kind == GD_GeneCandidateKind.CustomGeneClass
                        ? "custom geneClass not observed or declared for target Race"
                        : "structural/appearance gene not observed or declared for target Race";
                    return false;
                }

                if (!GD_HarAdapter.CanHaveEndogene(candidate, pawn.def))
                {
                    rejectionReason = "HAR CanHaveGene rejected Endogene";
                    return false;
                }
            }

            List<Gene> currentGenes = pawn.genes.GenesListForReading;
            bool prerequisitePresent = candidate.prerequisite == null;
            for (int i = 0; i < currentGenes.Count; i++)
            {
                GeneDef existingDef = currentGenes[i]?.def;
                if (!AcceptsAgainstExisting(candidate, existingDef, ref prerequisitePresent, out rejectionReason))
                {
                    return false;
                }
            }

            if (!prerequisitePresent)
            {
                rejectionReason = "missing prerequisite " + candidate.prerequisite.defName;
                return false;
            }

            int metabolismAfter;
            bool disablesViolenceAfter;
            GD_MetabolismUtility.EvaluateAfterAddingEndogene(pawn, candidate, out metabolismAfter, out disablesViolenceAfter);
            if (!MetabolismInRange(metabolismAfter, out rejectionReason))
            {
                return false;
            }

            if (request.MustBeCapableOfViolence && disablesViolenceAfter)
            {
                rejectionReason = "would disable Violent work";
                return false;
            }

            rejectionReason = null;
            return true;
        }

        internal static bool AcceptsForBirth(
            GeneDef candidate,
            IList<GeneDef> inheritedEndogenes,
            out string rejectionReason)
        {
            if (candidate == null)
            {
                rejectionReason = "null candidate";
                return false;
            }

            if (IsBlacklisted(candidate, out rejectionReason))
            {
                return false;
            }

            if (inheritedEndogenes == null)
            {
                rejectionReason = "null inherited endogene list";
                return false;
            }

            // Phase 3 deliberately leaves the already accepted vanilla birth mutation path unchanged.
            if (!GD_WorldGenePool.IsMutationPoolGene(candidate))
            {
                rejectionReason = "not an eligible mutation Gene";
                return false;
            }

            bool prerequisitePresent = candidate.prerequisite == null;
            for (int i = 0; i < inheritedEndogenes.Count; i++)
            {
                GeneDef existingDef = inheritedEndogenes[i];
                if (!AcceptsAgainstExisting(candidate, existingDef, ref prerequisitePresent, out rejectionReason))
                {
                    return false;
                }
            }

            if (!prerequisitePresent)
            {
                rejectionReason = "missing prerequisite " + candidate.prerequisite.defName;
                return false;
            }

            int metabolismAfter;
            bool ignoredViolenceFlag;
            GD_MetabolismUtility.EvaluateAfterAddingEndogene(
                inheritedEndogenes,
                candidate,
                out metabolismAfter,
                out ignoredViolenceFlag);
            return MetabolismInRange(metabolismAfter, out rejectionReason);
        }

        private static bool AcceptsAgainstExisting(
            GeneDef candidate,
            GeneDef existingDef,
            ref bool prerequisitePresent,
            out string rejectionReason)
        {
            if (existingDef == null)
            {
                rejectionReason = null;
                return true;
            }

            if (existingDef == candidate)
            {
                rejectionReason = "duplicate gene";
                return false;
            }

            if (candidate.ConflictsWith(existingDef) || existingDef.ConflictsWith(candidate))
            {
                rejectionReason = "conflicts with " + existingDef.defName;
                return false;
            }

            if (existingDef == candidate.prerequisite)
            {
                prerequisitePresent = true;
            }

            rejectionReason = null;
            return true;
        }

        private static bool MetabolismInRange(int metabolismAfter, out string rejectionReason)
        {
            IntRange allowedRange = GeneTuning.BiostatRange;
            if (metabolismAfter < allowedRange.min || metabolismAfter > allowedRange.max)
            {
                rejectionReason = "metabolism would be " + metabolismAfter;
                return false;
            }

            rejectionReason = null;
            return true;
        }

        private static bool IsBlacklisted(GeneDef candidate, out string rejectionReason)
        {
            GD_Settings settings = GD_SettingsAccess.Current;
            if (settings.BlacklistedGenes != null && settings.BlacklistedGenes.Contains(candidate.defName))
            {
                rejectionReason = "gene blacklisted by defName: " + candidate.defName;
                return true;
            }

            if (settings.BlacklistedGeneCategories != null && candidate.displayCategory != null)
            {
                if (settings.BlacklistedGeneCategories.Contains(candidate.displayCategory.defName))
                {
                    rejectionReason = "gene category blacklisted: " + candidate.displayCategory.defName;
                    return true;
                }
            }

            rejectionReason = null;
            return false;
        }
    }
}

