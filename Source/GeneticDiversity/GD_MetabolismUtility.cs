using System.Collections.Generic;
using RimWorld;
using Verse;

namespace GeneticDiversity
{
    internal static class GD_MetabolismUtility
    {
        internal static void EvaluateAfterAddingEndogene(Pawn pawn, GeneDef candidate, out int metabolism, out bool disablesViolence)
        {
            List<GeneDefWithType> typedGenes = new List<GeneDefWithType>(
                pawn.genes.Xenogenes.Count + pawn.genes.Endogenes.Count + 1);

            for (int i = 0; i < pawn.genes.Xenogenes.Count; i++)
            {
                Gene gene = pawn.genes.Xenogenes[i];
                if (gene?.def != null)
                {
                    typedGenes.Add(new GeneDefWithType(gene.def, xenogene: true));
                }
            }

            for (int i = 0; i < pawn.genes.Endogenes.Count; i++)
            {
                Gene gene = pawn.genes.Endogenes[i];
                if (gene?.def != null)
                {
                    typedGenes.Add(new GeneDefWithType(gene.def, xenogene: false));
                }
            }

            typedGenes.Add(new GeneDefWithType(candidate, xenogene: false));
            EvaluateTypedGenes(typedGenes, out metabolism, out disablesViolence);
        }

        internal static void EvaluateAfterAddingEndogene(
            IList<GeneDef> endogenes,
            GeneDef candidate,
            out int metabolism,
            out bool disablesViolence)
        {
            int existingCount = endogenes?.Count ?? 0;
            List<GeneDefWithType> typedGenes = new List<GeneDefWithType>(existingCount + 1);

            if (endogenes != null)
            {
                for (int i = 0; i < endogenes.Count; i++)
                {
                    GeneDef gene = endogenes[i];
                    if (gene != null)
                    {
                        typedGenes.Add(new GeneDefWithType(gene, xenogene: false));
                    }
                }
            }

            if (candidate != null)
            {
                typedGenes.Add(new GeneDefWithType(candidate, xenogene: false));
            }

            EvaluateTypedGenes(typedGenes, out metabolism, out disablesViolence);
        }

        private static void EvaluateTypedGenes(
            List<GeneDefWithType> typedGenes,
            out int metabolism,
            out bool disablesViolence)
        {
            metabolism = 0;
            disablesViolence = false;
            List<GeneDef> nonOverridden = typedGenes.NonOverriddenGenes();
            for (int i = 0; i < nonOverridden.Count; i++)
            {
                GeneDef gene = nonOverridden[i];
                metabolism += gene.biostatMet;
                if ((gene.disabledWorkTags & WorkTags.Violent) != WorkTags.None)
                {
                    disablesViolence = true;
                }
            }
        }
    }
}