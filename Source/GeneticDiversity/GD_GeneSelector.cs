using System.Collections.Generic;
using RimWorld;
using Verse;

namespace GeneticDiversity
{
    internal enum GD_GeneSource
    {
        Common,
        Mutation
    }

    internal static class GD_GeneSelector
    {
        private const int MaxCandidateAttemptsPerSlot = 20;
        private const float CommonPoolChance = 0.90f;

        internal static int RollVariationSlotCount(Pawn pawn = null, PawnGenerationRequest request = default(PawnGenerationRequest))
        {
            return GD_SettingsAccess.Current.RollVariationSlotCount(pawn, request);
        }

        internal static int AddVariations(Pawn pawn, PawnGenerationRequest request, GD_GenePoolSnapshot snapshot, int slots)
        {
            int added = 0;
            HashSet<GeneDef> addGeneRejected = new HashSet<GeneDef>();
            List<GD_WeightedGene> commonPool = GD_WorldGenePool.GetCommonGenesFor(pawn, request, snapshot);
            List<GD_WeightedGene> recoveryFallbackPool = null;

            for (int slot = 0; slot < slots; slot++)
            {
                float mutationRatio = GD_SettingsAccess.Current.MutationRatio;
                GD_GeneSource source = Rand.Chance(1f - mutationRatio) ? GD_GeneSource.Common : GD_GeneSource.Mutation;
                if (source == GD_GeneSource.Common)
                {
                    if (TryAddOne(pawn, request, snapshot, source, commonPool, addGeneRejected, false))
                    {
                        added++;
                        continue;
                    }

                    if (recoveryFallbackPool == null)
                    {
                        recoveryFallbackPool = GD_WorldGenePool.GetRecoveryFallbackGenesFor(pawn, request, snapshot);
                    }
                    GD_Diagnostics.RecordRecoveryFallbackAttempt();
                    if (TryAddOne(pawn, request, snapshot, source, recoveryFallbackPool, addGeneRejected, true))
                    {
                        GD_Diagnostics.RecordRecoveryFallbackAdded();
                        added++;
                    }
                    continue;
                }

                if (TryAddOne(pawn, request, snapshot, source, snapshot?.MutationGenes, addGeneRejected, true))
                {
                    added++;
                }
            }

            return added;
        }

        private static bool TryAddOne(
            Pawn pawn,
            PawnGenerationRequest request,
            GD_GenePoolSnapshot snapshot,
            GD_GeneSource source,
            List<GD_WeightedGene> pool,
            HashSet<GeneDef> addGeneRejected,
            bool recordExhaustion)
        {
            if (pool == null || pool.Count == 0)
            {
                GD_Diagnostics.RecordEmptySource(source);
                return false;
            }

            HashSet<GeneDef> attemptedThisSlot = new HashSet<GeneDef>();
            for (int attempt = 0; attempt < MaxCandidateAttemptsPerSlot; attempt++)
            {
                GD_WeightedGene selectedEntry;
                if (!TrySelectWeightedEntry(pool, attemptedThisSlot, addGeneRejected, out selectedEntry))
                {
                    break;
                }

                GeneDef candidate = selectedEntry.Gene;
                attemptedThisSlot.Add(candidate);
                string rejectionReason;
                if (!GD_GeneSafetyValidator.Accepts(candidate, pawn, request, snapshot, out rejectionReason))
                {
                    GD_Diagnostics.RecordCandidateRejected(rejectionReason);
                    continue;
                }

                Gene addedGene = pawn.genes.AddGene(candidate, xenogene: false);
                if (addedGene == null)
                {
                    addGeneRejected.Add(candidate);
                    GD_Diagnostics.RecordAddGeneNull(candidate);
                    continue;
                }

                bool sameRaceObserved = snapshot != null && snapshot.WasObservedOnRace(candidate, pawn.def);
                GD_Diagnostics.RecordGeneAdded(source, candidate, pawn.def, sameRaceObserved, selectedEntry.DefFallback);
                if (GD_SettingsAccess.Current.VerboseLogging)
                {
                    GD_Log.Message("Added gene " + candidate.defName + " to " + pawn.LabelShort + " (" + source + ").");
                }
                return true;
            }

            if (recordExhaustion)
            {
                GD_Diagnostics.RecordSlotExhausted(source);
            }
            return false;
        }

        internal static bool TrySelectWeighted(
            List<GD_WeightedGene> pool,
            HashSet<GeneDef> attemptedThisSlot,
            HashSet<GeneDef> addGeneRejected,
            out GeneDef selected)
        {
            GD_WeightedGene selectedEntry;
            bool result = TrySelectWeightedEntry(pool, attemptedThisSlot, addGeneRejected, out selectedEntry);
            selected = selectedEntry?.Gene;
            return result;
        }

        private static bool TrySelectWeightedEntry(
            List<GD_WeightedGene> pool,
            HashSet<GeneDef> attemptedThisSlot,
            HashSet<GeneDef> addGeneRejected,
            out GD_WeightedGene selected)
        {
            float totalWeight = 0f;
            for (int i = 0; i < pool.Count; i++)
            {
                GD_WeightedGene entry = pool[i];
                if (entry?.Gene == null || entry.Weight <= 0f || attemptedThisSlot.Contains(entry.Gene) || addGeneRejected.Contains(entry.Gene))
                {
                    continue;
                }

                totalWeight += entry.Weight;
            }

            if (totalWeight <= 0f)
            {
                selected = null;
                return false;
            }

            float choice = Rand.Value * totalWeight;
            GD_WeightedGene lastEligible = null;
            for (int i = 0; i < pool.Count; i++)
            {
                GD_WeightedGene entry = pool[i];
                if (entry?.Gene == null || entry.Weight <= 0f || attemptedThisSlot.Contains(entry.Gene) || addGeneRejected.Contains(entry.Gene))
                {
                    continue;
                }

                lastEligible = entry;
                choice -= entry.Weight;
                if (choice <= 0f)
                {
                    selected = entry;
                    return true;
                }
            }

            selected = lastEligible;
            return selected != null;
        }
    }
}
