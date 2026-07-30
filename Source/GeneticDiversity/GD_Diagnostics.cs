using System.Collections.Generic;
using System.Linq;
using System.Text;
using Verse;

namespace GeneticDiversity
{
    internal static class GD_Diagnostics
    {
        private static readonly Dictionary<string, long> SkipReasons = new Dictionary<string, long>();
        private static readonly Dictionary<string, long> RejectionReasons = new Dictionary<string, long>();
        private static readonly Dictionary<string, long> BirthSkipReasons = new Dictionary<string, long>();
        private static readonly Dictionary<string, long> BirthRejectionReasons = new Dictionary<string, long>();
        private static readonly Dictionary<string, long> BirthExhaustionReasons = new Dictionary<string, long>();
        private static readonly Dictionary<string, long> AddedTargetRaces = new Dictionary<string, long>();
        private static readonly Dictionary<string, long> EligibleTargetRaces = new Dictionary<string, long>();

        private static long patchCalls;
        private static long eligiblePawns;
        private static long eligibleHumanPawns;
        private static long eligibleHarPawns;
        private static long rolledSlots;
        private static long commonAdded;
        private static long mutationAdded;
        private static long sameRaceCommonAdded;
        private static long crossRaceCommonAdded;
        private static long defFallbackCommonAdded;
        private static long addGeneNull;
        private static long commonExhausted;
        private static long mutationExhausted;
        private static long commonEmpty;
        private static long mutationEmpty;
        private static long harRejected;
        private static long harAdapterFailures;
        private static long harRaceReadFailures;
        private static long recoveryFallbackAttempts;
        private static long recoveryFallbackAdded;

        private static long birthPatchCalls;
        private static long birthEligibleCalls;
        private static long birthNullGenesSkipped;
        private static long birthChanceRolls;
        private static long birthChanceHits;
        private static long birthChanceMisses;
        private static long birthMutationAdded;
        private static long birthMutationExhausted;

        internal static void RecordPatchCall()
        {
            patchCalls++;
        }

        internal static void RecordSkipped(string reason)
        {
            Increment(SkipReasons, reason);
        }

        internal static void RecordEligiblePawn(int slots, ThingDef race, bool harRace)
        {
            eligiblePawns++;
            rolledSlots += slots;
            Increment(EligibleTargetRaces, race?.defName ?? "unknown race");
            if (harRace)
            {
                eligibleHarPawns++;
            }
            else
            {
                eligibleHumanPawns++;
            }
        }

        internal static void RecordCandidateRejected(string reason)
        {
            Increment(RejectionReasons, reason);
        }

        internal static void RecordAddGeneNull(GeneDef gene)
        {
            addGeneNull++;
        }

        internal static void RecordGeneAdded(
            GD_GeneSource source,
            GeneDef gene,
            ThingDef targetRace,
            bool sameRaceObserved,
            bool defFallback)
        {
            if (source == GD_GeneSource.Common)
            {
                commonAdded++;
                if (defFallback)
                {
                    defFallbackCommonAdded++;
                }
                else if (sameRaceObserved)
                {
                    sameRaceCommonAdded++;
                }
                else
                {
                    crossRaceCommonAdded++;
                }
            }
            else
            {
                mutationAdded++;
            }

            Increment(AddedTargetRaces, targetRace?.defName ?? "unknown race");
        }

        internal static void RecordSlotExhausted(GD_GeneSource source)
        {
            if (source == GD_GeneSource.Common)
            {
                commonExhausted++;
            }
            else
            {
                mutationExhausted++;
            }
        }

        internal static void RecordEmptySource(GD_GeneSource source)
        {
            if (source == GD_GeneSource.Common)
            {
                commonEmpty++;
            }
            else
            {
                mutationEmpty++;
            }
        }

        internal static long GenerationPatchCalls => patchCalls;
        internal static long GenerationEligiblePawns => eligiblePawns;
        internal static long GenerationRolledSlots => rolledSlots;
        internal static long GenerationAddedTotal => commonAdded + mutationAdded;
        internal static long RecoveryFallbackAttempts => recoveryFallbackAttempts;
        internal static long RecoveryFallbackAdded => recoveryFallbackAdded;

        internal static void RecordRecoveryFallbackAttempt()
        {
            recoveryFallbackAttempts++;
        }

        internal static void RecordRecoveryFallbackAdded()
        {
            recoveryFallbackAdded++;
        }

        internal static void RecordHarRejected()
        {
            harRejected++;
        }

        internal static void RecordHarAdapterFailure()
        {
            harAdapterFailures++;
        }

        internal static void RecordHarRaceReadFailure()
        {
            harRaceReadFailures++;
        }

        internal static void RecordBirthPatchCall()
        {
            birthPatchCalls++;
        }

        internal static void RecordBirthSkipped(string reason)
        {
            if (reason == "null genes")
            {
                birthNullGenesSkipped++;
            }

            Increment(BirthSkipReasons, reason);
        }

        internal static void RecordBirthEligible()
        {
            birthEligibleCalls++;
        }

        internal static void RecordBirthChanceRoll()
        {
            birthChanceRolls++;
        }

        internal static void RecordBirthChanceHit()
        {
            birthChanceHits++;
        }

        internal static void RecordBirthChanceMiss()
        {
            birthChanceMisses++;
        }

        internal static void RecordBirthCandidateRejected(string reason)
        {
            Increment(BirthRejectionReasons, reason);
        }

        internal static void RecordBirthMutationAdded(GeneDef gene)
        {
            birthMutationAdded++;
        }

        internal static void RecordBirthMutationExhausted(string reason)
        {
            birthMutationExhausted++;
            Increment(BirthExhaustionReasons, reason);
        }

        internal static string BuildReport()
        {
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("Generation diagnostics:");
            builder.AppendLine("  patchCalls=" + patchCalls
                + ", eligiblePawns=" + eligiblePawns
                + ", eligibleHuman=" + eligibleHumanPawns
                + ", eligibleHAR=" + eligibleHarPawns
                + ", rolledSlots=" + rolledSlots
                + ", commonAdded=" + commonAdded
                + ", mutationAdded=" + mutationAdded + ".");
            builder.AppendLine("  sameRaceCommonAdded=" + sameRaceCommonAdded
                + ", crossRaceCommonAdded=" + crossRaceCommonAdded
                + ", defFallbackCommonAdded=" + defFallbackCommonAdded + ".");
            builder.AppendLine("  addGeneNull=" + addGeneNull
                + ", commonExhausted=" + commonExhausted
                + ", mutationExhausted=" + mutationExhausted
                + ", commonEmpty=" + commonEmpty
                + ", mutationEmpty=" + mutationEmpty + ".");
            builder.AppendLine("  recoveryFallbackAttempts=" + recoveryFallbackAttempts
                + ", recoveryFallbackAdded=" + recoveryFallbackAdded + ".");
            builder.AppendLine("  HAR status=" + GD_HarAdapter.StatusLabel
                + ", canHaveGeneRejected=" + harRejected
                + ", adapterFailures=" + harAdapterFailures
                + ", raceFallbackReadFailures=" + harRaceReadFailures + ".");
            AppendTop(builder, "  eligible target races", EligibleTargetRaces);
            AppendTop(builder, "  added target races", AddedTargetRaces);
            AppendTop(builder, "  skips", SkipReasons);
            AppendTop(builder, "  candidate rejections", RejectionReasons);

            builder.AppendLine("Birth mutation diagnostics:");
            builder.AppendLine("  patchCalls=" + birthPatchCalls
                + ", eligibleCalls=" + birthEligibleCalls
                + ", nullGenesSkipped=" + birthNullGenesSkipped
                + ", chanceRolls=" + birthChanceRolls
                + ", chanceHits=" + birthChanceHits
                + ", chanceMisses=" + birthChanceMisses + ".");
            builder.AppendLine("  mutationAdded=" + birthMutationAdded
                + ", candidateExhausted=" + birthMutationExhausted + ".");
            AppendTop(builder, "  birth skips", BirthSkipReasons);
            AppendTop(builder, "  birth candidate rejections", BirthRejectionReasons);
            AppendTop(builder, "  birth exhaustion", BirthExhaustionReasons);
            return builder.ToString().TrimEnd();
        }

        private static void Increment(Dictionary<string, long> dictionary, string key)
        {
            if (key.NullOrEmpty())
            {
                key = "unspecified";
            }

            long count;
            dictionary.TryGetValue(key, out count);
            dictionary[key] = count + 1;
        }

        private static void AppendTop(StringBuilder builder, string label, Dictionary<string, long> dictionary)
        {
            if (dictionary.Count == 0)
            {
                builder.AppendLine(label + ": none.");
                return;
            }

            builder.AppendLine(label + ": " + string.Join(", ", dictionary
                .OrderByDescending(pair => pair.Value)
                .ThenBy(pair => pair.Key)
                .Take(12)
                .Select(pair => pair.Key + "=" + pair.Value)) + ".");
        }
    }
}

