using System.Collections.Generic;
using RimWorld;
using Verse;

namespace GeneticDiversity
{
    internal static class GD_BirthMutationUtility
    {
        internal const float MutationChance = 0.10f;
        private const int MaxCandidateAttempts = 20;

        internal static void TryApply(ref List<GeneDef> genes, Pawn geneticMother, Pawn father)
        {
            string skipReason;
            if (!GD_SettingsAccess.Current.Enabled)
            {
                GD_Diagnostics.RecordBirthSkipped("mod disabled");
                return;
            }

            if (!IsEligible(genes, geneticMother, father, out skipReason))
            {
                GD_Diagnostics.RecordBirthSkipped(skipReason);
                return;
            }

            GD_Diagnostics.RecordBirthEligible();
            GD_Diagnostics.RecordBirthChanceRoll();
            if (!Rand.Chance(GD_SettingsAccess.Current.BirthMutationChance))
            {
                GD_Diagnostics.RecordBirthChanceMiss();
                return;
            }

            GD_Diagnostics.RecordBirthChanceHit();
            List<GeneDef> mutatedGenes;
            GeneDef addedGene;
            string failureReason;
            if (!TryBuildMutatedCopy(
                    genes,
                    GD_WorldGenePool.Current,
                    out mutatedGenes,
                    out addedGene,
                    out failureReason,
                    recordDiagnostics: true))
            {
                GD_Diagnostics.RecordBirthMutationExhausted(failureReason);
                return;
            }

            genes = mutatedGenes;
            GD_Diagnostics.RecordBirthMutationAdded(addedGene);
        }

        internal static bool TryBuildMutatedCopy(
            IList<GeneDef> sourceGenes,
            GD_GenePoolSnapshot snapshot,
            out List<GeneDef> mutatedGenes,
            out GeneDef addedGene,
            out string failureReason,
            bool recordDiagnostics)
        {
            mutatedGenes = null;
            addedGene = null;

            if (sourceGenes == null)
            {
                failureReason = "null source genes";
                return false;
            }

            List<GD_WeightedGene> pool = snapshot?.MutationGenes;
            if (pool == null || pool.Count == 0)
            {
                failureReason = "empty mutation pool";
                return false;
            }

            HashSet<GeneDef> attempted = new HashSet<GeneDef>();
            HashSet<GeneDef> noAddGeneRejections = new HashSet<GeneDef>();
            for (int attempt = 0; attempt < MaxCandidateAttempts; attempt++)
            {
                GeneDef candidate;
                if (!GD_GeneSelector.TrySelectWeighted(pool, attempted, noAddGeneRejections, out candidate))
                {
                    failureReason = "no untried mutation candidate";
                    return false;
                }

                attempted.Add(candidate);
                string rejectionReason;
                if (!GD_GeneSafetyValidator.AcceptsForBirth(candidate, sourceGenes, out rejectionReason))
                {
                    if (recordDiagnostics)
                    {
                        GD_Diagnostics.RecordBirthCandidateRejected(rejectionReason);
                    }
                    continue;
                }

                // Never mutate the pregnancy/embryo/multiple-birth source list. The replacement
                // argument is assigned only after a legal candidate has been found.
                mutatedGenes = new List<GeneDef>(sourceGenes) { candidate };
                addedGene = candidate;
                failureReason = null;
                return true;
            }

            failureReason = "all attempted mutation candidates rejected";
            return false;
        }

        private static bool IsEligible(
            List<GeneDef> genes,
            Pawn geneticMother,
            Pawn father,
            out string reason)
        {
            if (!ModsConfig.BiotechActive)
            {
                reason = "Biotech inactive";
                return false;
            }

            if (genes == null)
            {
                reason = "null genes";
                return false;
            }

            if (!IsVanillaHuman(geneticMother))
            {
                reason = "non-vanilla Human genetic mother";
                return false;
            }

            if (father != null && !IsVanillaHuman(father))
            {
                reason = "non-vanilla Human father";
                return false;
            }

            reason = null;
            return true;
        }

        private static bool IsVanillaHuman(Pawn pawn)
        {
            return pawn != null
                && pawn.def == ThingDefOf.Human
                && pawn.RaceProps != null
                && pawn.RaceProps.Humanlike;
        }
    }
}