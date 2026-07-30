using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GeneticDiversity
{
    public sealed class GD_RaceGeneCompatibilityDef : Def
    {
        public List<string> sourcePackageIds = new List<string>();
        public string raceDefName;
        public List<string> nativeXenotypeDefNames = new List<string>();
        public List<string> nativeGeneDefNames = new List<string>();
        public List<string> excludedGeneDefNames = new List<string>();
        public List<string> sameRaceOnlyGeneDefNames = new List<string>();
        public List<string> crossRaceSafeGeneDefNames = new List<string>();
        public List<string> excludedPawnKindDefNames = new List<string>();
        public bool sameRaceOnlySourceCustomGenes;
        public bool sameRaceOnlySourceStructuralGenes;

        internal ThingDef ResolvedRace { get; private set; }
        internal List<XenotypeDef> ResolvedNativeXenotypes { get; private set; } = new List<XenotypeDef>();
        internal HashSet<GeneDef> ResolvedNativeGenes { get; private set; } = new HashSet<GeneDef>();
        internal HashSet<GeneDef> ResolvedExcludedGenes { get; private set; } = new HashSet<GeneDef>();
        internal HashSet<GeneDef> ResolvedSameRaceOnlyGenes { get; private set; } = new HashSet<GeneDef>();
        internal HashSet<GeneDef> ResolvedCrossRaceSafeGenes { get; private set; } = new HashSet<GeneDef>();
        internal HashSet<PawnKindDef> ResolvedExcludedPawnKinds { get; private set; } = new HashSet<PawnKindDef>();

        public override void ResolveReferences()
        {
            base.ResolveReferences();
            ResolvedRace = raceDefName.NullOrEmpty() ? null : DefDatabase<ThingDef>.GetNamedSilentFail(raceDefName);
            ResolvedNativeXenotypes = ResolveDefs<XenotypeDef>(nativeXenotypeDefNames);
            ResolvedNativeGenes = new HashSet<GeneDef>(ResolveDefs<GeneDef>(nativeGeneDefNames));
            ResolvedExcludedGenes = new HashSet<GeneDef>(ResolveDefs<GeneDef>(excludedGeneDefNames));
            ResolvedSameRaceOnlyGenes = new HashSet<GeneDef>(ResolveDefs<GeneDef>(sameRaceOnlyGeneDefNames));
            ResolvedCrossRaceSafeGenes = new HashSet<GeneDef>(ResolveDefs<GeneDef>(crossRaceSafeGeneDefNames));
            ResolvedExcludedPawnKinds = new HashSet<PawnKindDef>(ResolveDefs<PawnKindDef>(excludedPawnKindDefNames));
        }

        public override IEnumerable<string> ConfigErrors()
        {
            foreach (string error in base.ConfigErrors())
            {
                yield return error;
            }

            if (sourcePackageIds.NullOrEmpty())
            {
                yield return defName + " must declare at least one sourcePackageId.";
            }

            if (raceDefName.NullOrEmpty())
            {
                yield return defName + " must declare raceDefName.";
            }
        }

        private static List<T> ResolveDefs<T>(List<string> defNames) where T : Def
        {
            if (defNames.NullOrEmpty())
            {
                return new List<T>();
            }

            return defNames
                .Where(name => !name.NullOrEmpty())
                .Select(DefDatabase<T>.GetNamedSilentFail)
                .Where(def => def != null)
                .Distinct()
                .ToList();
        }
    }
}
