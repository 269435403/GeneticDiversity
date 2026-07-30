using System;
using System.Collections.Generic;
using HarmonyLib;
using RimWorld;
using Verse;

namespace GeneticDiversity
{
    [HarmonyPatch(
        typeof(PregnancyUtility),
        nameof(PregnancyUtility.ApplyBirthOutcome),
        new Type[]
        {
            typeof(RitualOutcomePossibility), typeof(float), typeof(Precept_Ritual),
            typeof(List<GeneDef>), typeof(Pawn), typeof(Thing), typeof(Pawn),
            typeof(Pawn), typeof(LordJob_Ritual), typeof(RitualRoleAssignments), typeof(bool)
        })]
    internal static class GD_Patch_PregnancyUtility_ApplyBirthOutcome
    {
        [HarmonyPrefix]
        [HarmonyPriority(Priority.Last)]
        private static void Prefix(ref List<GeneDef> genes, Pawn geneticMother, Pawn father)
        {
            GD_Diagnostics.RecordBirthPatchCall();
            GD_BirthMutationUtility.TryApply(ref genes, geneticMother, father);
        }
    }
}