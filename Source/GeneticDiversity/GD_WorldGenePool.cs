using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using RimWorld;
using RimWorld.Planet;
using Verse;

namespace GeneticDiversity
{
    internal enum GD_GeneCandidateKind
    {
        Standard,
        CustomGeneClass,
        StructuralOrAppearance
    }

    internal sealed class GD_WeightedGene
    {
        internal readonly GeneDef Gene;
        internal readonly float Weight;
        internal readonly bool DefFallback;

        internal GD_WeightedGene(GeneDef gene, float weight, bool defFallback = false)
        {
            Gene = gene;
            Weight = weight;
            DefFallback = defFallback;
        }
    }

    internal sealed class GD_GeneObservation
    {
        internal readonly GeneDef Gene;
        internal readonly ThingDef Race;
        internal readonly FactionDef Faction;
        internal readonly int Count;
        internal readonly GD_GeneCandidateKind Kind;

        internal GD_GeneObservation(GeneDef gene, ThingDef race, FactionDef faction, int count)
        {
            Gene = gene;
            Race = race;
            Faction = faction;
            Count = count;
            Kind = GD_WorldGenePool.Classify(gene);
        }
    }


    internal struct GD_TargetPoolKey : IEquatable<GD_TargetPoolKey>
    {
        internal readonly ThingDef Race;
        internal readonly FactionDef Faction;

        internal GD_TargetPoolKey(ThingDef race, FactionDef faction)
        {
            Race = race;
            Faction = faction;
        }

        public bool Equals(GD_TargetPoolKey other)
        {
            return ReferenceEquals(Race, other.Race) && ReferenceEquals(Faction, other.Faction);
        }

        public override bool Equals(object obj)
        {
            return obj is GD_TargetPoolKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((Race != null ? Race.GetHashCode() : 0) * 397) ^ (Faction != null ? Faction.GetHashCode() : 0);
            }
        }
    }
    internal sealed class GD_GenePoolSnapshot
    {
        internal readonly List<GD_WeightedGene> CommonGenes;
        internal readonly List<GD_WeightedGene> MutationGenes;
        internal readonly List<GD_GeneObservation> Observations;
        internal readonly int ScannedPawnCount;
        internal readonly int CountedEndogeneEntries;
        internal readonly int ScannedRaceCount;
        internal readonly bool UsedXenotypeFallback;
        internal readonly bool HarAware;
        internal readonly int BuiltAtTick;
        private readonly Dictionary<ThingDef, HashSet<GeneDef>> observedGenesByRace = new Dictionary<ThingDef, HashSet<GeneDef>>();
        private readonly Dictionary<GD_TargetPoolKey, List<GD_WeightedGene>> targetCommonCache = new Dictionary<GD_TargetPoolKey, List<GD_WeightedGene>>();
        private readonly Dictionary<GD_TargetPoolKey, List<GD_WeightedGene>> targetRecoveryFallbackCache = new Dictionary<GD_TargetPoolKey, List<GD_WeightedGene>>();

        internal GD_GenePoolSnapshot(
            List<GD_WeightedGene> commonGenes,
            List<GD_WeightedGene> mutationGenes,
            List<GD_GeneObservation> observations,
            int scannedPawnCount,
            int countedEndogeneEntries,
            int scannedRaceCount,
            bool usedXenotypeFallback,
            bool harAware,
            int builtAtTick)
        {
            CommonGenes = commonGenes;
            MutationGenes = mutationGenes;
            Observations = observations;
            ScannedPawnCount = scannedPawnCount;
            CountedEndogeneEntries = countedEndogeneEntries;
            ScannedRaceCount = scannedRaceCount;
            UsedXenotypeFallback = usedXenotypeFallback;
            HarAware = harAware;
            BuiltAtTick = builtAtTick;

            for (int i = 0; i < Observations.Count; i++)
            {
                GD_GeneObservation observation = Observations[i];
                if (observation?.Race == null || observation.Gene == null)
                {
                    continue;
                }

                HashSet<GeneDef> genes;
                if (!observedGenesByRace.TryGetValue(observation.Race, out genes))
                {
                    genes = new HashSet<GeneDef>();
                    observedGenesByRace[observation.Race] = genes;
                }
                genes.Add(observation.Gene);
            }
        }

        internal bool WasObservedOnRace(GeneDef gene, ThingDef race)
        {
            HashSet<GeneDef> genes;
            return gene != null
                && race != null
                && observedGenesByRace.TryGetValue(race, out genes)
                && genes.Contains(gene);
        }

        internal bool HasObservationForRace(ThingDef race)
        {
            return race != null && observedGenesByRace.ContainsKey(race);
        }

        internal bool TryGetTargetCommon(ThingDef race, FactionDef faction, out List<GD_WeightedGene> genes)
        {
            return targetCommonCache.TryGetValue(new GD_TargetPoolKey(race, faction), out genes);
        }

        internal void CacheTargetCommon(ThingDef race, FactionDef faction, List<GD_WeightedGene> genes)
        {
            targetCommonCache[new GD_TargetPoolKey(race, faction)] = genes;
        }

        internal bool TryGetTargetRecoveryFallback(ThingDef race, FactionDef faction, out List<GD_WeightedGene> genes)
        {
            return targetRecoveryFallbackCache.TryGetValue(new GD_TargetPoolKey(race, faction), out genes);
        }

        internal void CacheTargetRecoveryFallback(ThingDef race, FactionDef faction, List<GD_WeightedGene> genes)
        {
            targetRecoveryFallbackCache[new GD_TargetPoolKey(race, faction)] = genes;
        }
    }

    internal struct GD_GeneObservationKey : IEquatable<GD_GeneObservationKey>
    {
        internal readonly GeneDef Gene;
        internal readonly ThingDef Race;
        internal readonly FactionDef Faction;

        internal GD_GeneObservationKey(GeneDef gene, ThingDef race, FactionDef faction)
        {
            Gene = gene;
            Race = race;
            Faction = faction;
        }

        public bool Equals(GD_GeneObservationKey other)
        {
            return ReferenceEquals(Gene, other.Gene)
                && ReferenceEquals(Race, other.Race)
                && ReferenceEquals(Faction, other.Faction);
        }

        public override bool Equals(object obj)
        {
            return obj is GD_GeneObservationKey other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hash = Gene != null ? Gene.GetHashCode() : 0;
                hash = (hash * 397) ^ (Race != null ? Race.GetHashCode() : 0);
                hash = (hash * 397) ^ (Faction != null ? Faction.GetHashCode() : 0);
                return hash;
            }
        }
    }

    internal static class GD_WorldGenePool
    {
        // Phase 3 keeps the accepted vanilla pool intact for HAR-absent behavior and birth mutation.
        // HAR-aware target pools are built from current-world Race/Faction observations on demand.
        private const int RefreshIntervalTicks = 60000;
        private const float DefFallbackWeight = 0.25f;
        private const float FrdConfiguredPriorWeight = 0.5f;
        private static GD_GenePoolSnapshot cachedSnapshot;
        private static World cachedWorld;
        private static long lastRefreshMilliseconds;

        internal static long LastRefreshMilliseconds => lastRefreshMilliseconds;

        internal static GD_GenePoolSnapshot Current
        {
            get
            {
                World world = Find.World;
                int tick = CurrentGameTick;
                if (cachedSnapshot == null || cachedWorld != world || TickExpired(cachedSnapshot.BuiltAtTick, tick))
                {
                    Stopwatch stopwatch = Stopwatch.StartNew();
                    cachedSnapshot = BuildSnapshot(tick);
                    cachedWorld = world;
                    stopwatch.Stop();
                    lastRefreshMilliseconds = stopwatch.ElapsedMilliseconds;
                }

                return cachedSnapshot;
            }
        }

        internal static void ClearCache(bool logMessage = true)
        {
            cachedSnapshot = null;
            cachedWorld = null;
            GD_HarAdapter.ClearCaches();
            GD_CompatibilityRegistry.ClearCaches();
            GD_FrdAdapter.ClearCaches();
            if (logMessage)
            {
                GD_Log.Message("Gene pool, HAR, precise compatibility, and FRD integration caches cleared. The next eligible pawn generation will rebuild them.");
            }
        }

        internal static GD_GenePoolSnapshot RefreshNow()
        {
            int tick = CurrentGameTick;
            Stopwatch stopwatch = Stopwatch.StartNew();
            cachedSnapshot = BuildSnapshot(tick);
            cachedWorld = Find.World;
            stopwatch.Stop();
            lastRefreshMilliseconds = stopwatch.ElapsedMilliseconds;
            GD_Log.Message(FormatSummary(cachedSnapshot, "Gene pool cache refreshed"));
            return cachedSnapshot;
        }

        internal static List<GD_WeightedGene> GetCommonGenesFor(Pawn target, PawnGenerationRequest request, GD_GenePoolSnapshot snapshot)
        {
            if (snapshot == null || target == null)
            {
                return new List<GD_WeightedGene>();
            }

            // This is the exact Phase 1 path when HAR is absent or its adapter failed before the snapshot was built.
            if (!snapshot.HarAware && !GD_FrdAdapter.IsAvailable)
            {
                return snapshot.CommonGenes;
            }

            ThingDef targetRace = target.def;
            FactionDef targetFaction = (target.Faction ?? request.Faction)?.def;
            return GetCommonGenesFor(targetRace, targetFaction, snapshot);
        }

        internal static List<GD_WeightedGene> GetCommonGenesFor(ThingDef targetRace, FactionDef targetFaction, GD_GenePoolSnapshot snapshot)
        {
            if (snapshot == null || targetRace == null)
            {
                return new List<GD_WeightedGene>();
            }

            if (!snapshot.HarAware && !GD_FrdAdapter.IsAvailable)
            {
                return snapshot.CommonGenes;
            }

            List<GD_WeightedGene> cachedTargetPool;
            if (snapshot.TryGetTargetCommon(targetRace, targetFaction, out cachedTargetPool))
            {
                return cachedTargetPool;
            }

            Dictionary<GeneDef, float> weights = new Dictionary<GeneDef, float>();
            HashSet<GeneDef> observedGenes = new HashSet<GeneDef>();
            HashSet<GeneDef> fallbackGenes = new HashSet<GeneDef>();
            bool hasTargetRaceSample = snapshot.HasObservationForRace(targetRace);

            for (int i = 0; i < snapshot.Observations.Count; i++)
            {
                GD_GeneObservation observation = snapshot.Observations[i];
                bool sameRace = observation.Race == targetRace;
                GD_Settings settings = GD_SettingsAccess.Current;
                if (!sameRace && settings.SameRaceOnly)
                {
                    continue;
                }
                if (!sameRace && observation.Kind == GD_GeneCandidateKind.Standard && !settings.AllowStandardCrossRace)
                {
                    continue;
                }
                if (!sameRace && observation.Kind != GD_GeneCandidateKind.Standard && !settings.AllowSpecialCrossRace)
                {
                    continue;
                }
                if (!GD_CompatibilityRegistry.CanUseForTargetRace(observation.Gene, targetRace))
                {
                    continue;
                }

                if (!sameRace && observation.Kind != GD_GeneCandidateKind.Standard)
                {
                    continue;
                }

                float multiplier;
                if (sameRace && targetFaction != null && observation.Faction == targetFaction)
                {
                    multiplier = 4f;
                }
                else if (sameRace)
                {
                    multiplier = 2f;
                }
                else
                {
                    multiplier = 1f;
                }

                observedGenes.Add(observation.Gene);
                AddWeight(weights, observation.Gene, observation.Count * multiplier);
            }

            if (!hasTargetRaceSample)
            {
                AddFrdConfiguredPrior(weights, fallbackGenes, targetFaction, targetRace);
                if (GD_HarAdapter.IsHarRace(targetRace))
                {
                    AddHarRaceFallback(weights, observedGenes, fallbackGenes, targetRace, DefFallbackWeight);
                }
                else if (targetRace == ThingDefOf.Human)
                {
                    AddVanillaTargetFallback(weights, observedGenes, fallbackGenes, snapshot.CommonGenes, DefFallbackWeight);
                }
            }

            if (weights.Count == 0)
            {
                if (GD_HarAdapter.IsHarRace(targetRace))
                {
                    AddHarRaceFallback(weights, observedGenes, fallbackGenes, targetRace, 1f);
                }
                else if (targetRace == ThingDefOf.Human)
                {
                    AddVanillaTargetFallback(weights, observedGenes, fallbackGenes, snapshot.CommonGenes, 1f);
                }
            }

            List<GD_WeightedGene> result = weights
                .Where(pair => pair.Key != null && pair.Value > 0f)
                .Select(pair => new GD_WeightedGene(
                    pair.Key,
                    pair.Value,
                    fallbackGenes.Contains(pair.Key) && !observedGenes.Contains(pair.Key)))
                .OrderBy(entry => entry.Gene.defName)
                .ToList();
            snapshot.CacheTargetCommon(targetRace, targetFaction, result);
            return result;
        }

        internal static List<GD_WeightedGene> GetRecoveryFallbackGenesFor(Pawn target, PawnGenerationRequest request, GD_GenePoolSnapshot snapshot)
        {
            if (target == null || snapshot == null)
            {
                return new List<GD_WeightedGene>();
            }

            ThingDef targetRace = target.def;
            FactionDef targetFaction = (target.Faction ?? request.Faction)?.def;
            List<GD_WeightedGene> cached;
            if (snapshot.TryGetTargetRecoveryFallback(targetRace, targetFaction, out cached))
            {
                return cached;
            }

            Dictionary<GeneDef, float> weights = new Dictionary<GeneDef, float>();
            HashSet<GeneDef> fallbackGenes = new HashSet<GeneDef>();
            AddFrdConfiguredPrior(weights, fallbackGenes, targetFaction, targetRace);

            if (GD_HarAdapter.IsHarRace(targetRace))
            {
                AddHarRaceFallback(weights, new HashSet<GeneDef>(), fallbackGenes, targetRace, DefFallbackWeight);
            }
            else if (targetRace == ThingDefOf.Human)
            {
                Dictionary<GeneDef, float> vanillaFallback = new Dictionary<GeneDef, float>();
                AddVanillaXenotypeFallback(vanillaFallback);
                foreach (KeyValuePair<GeneDef, float> pair in vanillaFallback)
                {
                    if (pair.Key != null && pair.Value > 0f)
                    {
                        AddWeight(weights, pair.Key, pair.Value * DefFallbackWeight);
                        fallbackGenes.Add(pair.Key);
                    }
                }
            }

            List<GD_WeightedGene> result = weights
                .Where(pair => pair.Key != null && pair.Value > 0f)
                .Select(pair => new GD_WeightedGene(pair.Key, pair.Value, true))
                .OrderBy(entry => entry.Gene.defName)
                .ToList();
            snapshot.CacheTargetRecoveryFallback(targetRace, targetFaction, result);
            return result;
        }

        private static void AddFrdConfiguredPrior(
            Dictionary<GeneDef, float> weights,
            HashSet<GeneDef> fallbackGenes,
            FactionDef targetFaction,
            ThingDef targetRace)
        {
            List<GD_WeightedGene> prior = GD_FrdAdapter.GetConfiguredGenePrior(targetFaction, targetRace, FrdConfiguredPriorWeight);
            for (int i = 0; i < prior.Count; i++)
            {
                GD_WeightedGene entry = prior[i];
                if (entry?.Gene == null || entry.Weight <= 0f)
                {
                    continue;
                }
                AddWeight(weights, entry.Gene, entry.Weight);
                fallbackGenes.Add(entry.Gene);
            }
        }

        internal static bool IsKnownForRace(GeneDef gene, ThingDef race, GD_GenePoolSnapshot snapshot)
        {
            if (gene == null || race == null)
            {
                return false;
            }

            if (snapshot != null && snapshot.WasObservedOnRace(gene, race))
            {
                return true;
            }

            if (GD_CompatibilityRegistry.IsKnownForRace(gene, race))
            {
                return true;
            }

            if (!GD_HarAdapter.IsHarRace(race))
            {
                return false;
            }

            if (GD_HarAdapter.GetPossibleRaceGenes(race).Contains(gene))
            {
                return true;
            }

            if (!GD_CompatibilityRegistry.HasExactRule(race))
            {
                IReadOnlyList<XenotypeDef> xenotypes = GD_HarAdapter.GetExplicitXenotypes(race);
                for (int i = 0; i < xenotypes.Count; i++)
                {
                    XenotypeDef xenotype = xenotypes[i];
                    if (xenotype != null && xenotype.inheritable && !xenotype.genes.NullOrEmpty() && xenotype.genes.Contains(gene))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        internal static string FormatSummary(GD_GenePoolSnapshot snapshot, string heading)
        {
            if (snapshot == null)
            {
                return heading + ": no snapshot.";
            }

            return heading
                + ": scannedHumanlikePawns=" + snapshot.ScannedPawnCount
                + ", scannedRaces=" + snapshot.ScannedRaceCount
                + ", countedEndogeneEntries=" + snapshot.CountedEndogeneEntries
                + ", vanillaCommonCandidates=" + snapshot.CommonGenes.Count
                + ", raceFactionObservations=" + snapshot.Observations.Count
                + ", mutationCandidates=" + snapshot.MutationGenes.Count
                + ", harAware=" + snapshot.HarAware
                + ", vanillaCommonSource=" + (snapshot.UsedXenotypeFallback ? "inheritable XenotypeDef fallback" : "current-world Human endogenes")
                + ", tick=" + snapshot.BuiltAtTick + ".";
        }

        internal static GD_GeneCandidateKind Classify(GeneDef gene)
        {
            if (gene == null)
            {
                return GD_GeneCandidateKind.StructuralOrAppearance;
            }

            if (gene.bodyType.HasValue
                || !gene.forcedHeadTypes.NullOrEmpty()
                || gene.forcedHair != null
                || gene.HasDefinedGraphicProperties)
            {
                return GD_GeneCandidateKind.StructuralOrAppearance;
            }

            return gene.geneClass == typeof(Gene)
                ? GD_GeneCandidateKind.Standard
                : GD_GeneCandidateKind.CustomGeneClass;
        }

        internal static bool IsWorldPoolGene(GeneDef gene)
        {
            return IsWorldPoolGene(gene, GD_SettingsAccess.Current.AllowArchiteMutation);
        }

        internal static bool IsWorldPoolGene(GeneDef gene, bool allowArchite)
        {
            if (gene == null)
            {
                return false;
            }

            if (gene.endogeneCategory == EndogeneCategory.HairColor || gene.endogeneCategory == EndogeneCategory.Melanin)
            {
                return false;
            }

            return gene != GeneDefOf.Inbred
                && (allowArchite || gene.biostatArc <= 0)
                && gene.canGenerateInGeneSet
                && gene.geneClass != null
                && typeof(Gene).IsAssignableFrom(gene.geneClass);
        }

        internal static bool IsMutationPoolGene(GeneDef gene)
        {
            if (!IsWorldPoolGene(gene, GD_SettingsAccess.Current.AllowArchiteMutation)
                || !IsOfficialDef(gene)
                || Classify(gene) != GD_GeneCandidateKind.Standard)
            {
                return false;
            }
            if (!GD_SettingsAccess.Current.AllowNonInheritableXenotypeMutation)
            {
                foreach (XenotypeDef xenotype in DefDatabase<XenotypeDef>.AllDefsListForReading)
                {
                    if (xenotype != null && !xenotype.inheritable && xenotype.genes != null && xenotype.genes.Contains(gene))
                    {
                        return false;
                    }
                }
            }
            return true;
        }

        internal static bool IsVanillaPoolGene(GeneDef gene)
        {
            return IsWorldPoolGene(gene, allowArchite: false)
                && IsOfficialDef(gene)
                && Classify(gene) == GD_GeneCandidateKind.Standard;
        }

        private static int CurrentGameTick => Find.TickManager != null ? Find.TickManager.TicksGame : -1;

        private static bool TickExpired(int builtAtTick, int currentTick)
        {
            if (builtAtTick < 0 || currentTick < 0)
            {
                return false;
            }

            return currentTick < builtAtTick || currentTick - builtAtTick >= RefreshIntervalTicks;
        }

        private static GD_GenePoolSnapshot BuildSnapshot(int tick)
        {
            bool harAware = GD_HarAdapter.IsAvailable && !GD_HarAdapter.AdapterFailed;
            Dictionary<GeneDef, float> vanillaCommonWeights = new Dictionary<GeneDef, float>();
            Dictionary<GD_GeneObservationKey, int> observationCounts = new Dictionary<GD_GeneObservationKey, int>();
            HashSet<ThingDef> scannedRaces = new HashSet<ThingDef>();
            int scannedPawnCount = 0;
            int countedEndogeneEntries = 0;

            if (Find.World != null)
            {
                List<Pawn> pawns = PawnsFinder.AllMapsWorldAndTemporary_Alive.ToList();
                HashSet<Pawn> visited = new HashSet<Pawn>();
                foreach (Pawn pawn in pawns)
                {
                    if (pawn == null || !visited.Add(pawn) || !IsEligibleWorldPawn(pawn, harAware))
                    {
                        continue;
                    }

                    scannedPawnCount++;
                    scannedRaces.Add(pawn.def);
                    FactionDef faction = pawn.Faction?.def;
                    List<Gene> endogenes = pawn.genes.Endogenes;
                    for (int i = 0; i < endogenes.Count; i++)
                    {
                        GeneDef gene = endogenes[i]?.def;
                        bool vanillaEligible = pawn.def == ThingDefOf.Human && IsVanillaPoolGene(gene);
                        bool worldEligible = harAware && IsWorldPoolGene(gene);
                        if (!vanillaEligible && !worldEligible)
                        {
                            continue;
                        }

                        countedEndogeneEntries++;
                        if (vanillaEligible)
                        {
                            AddWeight(vanillaCommonWeights, gene, 1f);
                        }

                        if (worldEligible)
                        {
                            GD_GeneObservationKey key = new GD_GeneObservationKey(gene, pawn.def, faction);
                            int count;
                            observationCounts.TryGetValue(key, out count);
                            observationCounts[key] = count + 1;
                        }
                    }
                }
            }

            bool usedFallback = vanillaCommonWeights.Count == 0;
            if (usedFallback)
            {
                AddVanillaXenotypeFallback(vanillaCommonWeights);
            }

            List<GD_WeightedGene> common = vanillaCommonWeights
                .Where(pair => pair.Key != null && pair.Value > 0f)
                .Select(pair => new GD_WeightedGene(pair.Key, pair.Value, usedFallback))
                .OrderBy(entry => entry.Gene.defName)
                .ToList();

            List<GD_GeneObservation> observations = observationCounts
                .Select(pair => new GD_GeneObservation(pair.Key.Gene, pair.Key.Race, pair.Key.Faction, pair.Value))
                .OrderBy(entry => entry.Race.defName)
                .ThenBy(entry => entry.Faction?.defName ?? string.Empty)
                .ThenBy(entry => entry.Gene.defName)
                .ToList();

            // Keep the Phase 2 mutation pool based on the accepted vanilla Human common pool.
            HashSet<GeneDef> vanillaCommonSet = new HashSet<GeneDef>(common.Select(entry => entry.Gene));
            List<GD_WeightedGene> mutation = DefDatabase<GeneDef>.AllDefsListForReading
                .Where(gene => IsMutationPoolGene(gene) && gene.selectionWeight > 0f && !vanillaCommonSet.Contains(gene))
                .Select(gene => new GD_WeightedGene(gene, gene.selectionWeight))
                .OrderBy(entry => entry.Gene.defName)
                .ToList();

            return new GD_GenePoolSnapshot(
                common,
                mutation,
                observations,
                scannedPawnCount,
                countedEndogeneEntries,
                scannedRaces.Count,
                usedFallback,
                harAware,
                tick);
        }

        private static void AddHarRaceFallback(
            Dictionary<GeneDef, float> weights,
            ISet<GeneDef> observedGenes,
            ISet<GeneDef> fallbackGenes,
            ThingDef race,
            float weight)
        {
            HashSet<GeneDef> added = new HashSet<GeneDef>();
            foreach (GeneDef gene in GD_HarAdapter.GetPossibleRaceGenes(race))
            {
                if (IsWorldPoolGene(gene)
                    && GD_CompatibilityRegistry.CanUseForTargetRace(gene, race)
                    && GD_HarAdapter.CanHaveEndogene(gene, race)
                    && added.Add(gene))
                {
                    AddFallbackWeight(weights, observedGenes, fallbackGenes, gene, weight);
                }
            }

            foreach (GeneDef gene in GD_CompatibilityRegistry.GetExplicitNativeGenes(race))
            {
                if (IsWorldPoolGene(gene)
                    && GD_CompatibilityRegistry.CanUseForTargetRace(gene, race)
                    && GD_HarAdapter.CanHaveEndogene(gene, race)
                    && added.Add(gene))
                {
                    AddFallbackWeight(weights, observedGenes, fallbackGenes, gene, weight);
                }
            }

            foreach (XenotypeDef xenotype in GD_CompatibilityRegistry.GetNativeXenotypes(race))
            {
                if (xenotype == null || !xenotype.inheritable || xenotype.genes.NullOrEmpty() || !GD_HarAdapter.CanUseXenotype(xenotype, race))
                {
                    continue;
                }

                for (int i = 0; i < xenotype.genes.Count; i++)
                {
                    GeneDef gene = xenotype.genes[i];
                    if (IsWorldPoolGene(gene)
                        && GD_CompatibilityRegistry.CanUseForTargetRace(gene, race)
                        && GD_HarAdapter.CanHaveEndogene(gene, race)
                        && added.Add(gene))
                    {
                        AddFallbackWeight(weights, observedGenes, fallbackGenes, gene, weight);
                    }
                }
            }

            if (!GD_CompatibilityRegistry.HasExactRule(race))
            {
                IReadOnlyList<XenotypeDef> xenotypes = GD_HarAdapter.GetExplicitXenotypes(race);
                for (int i = 0; i < xenotypes.Count; i++)
                {
                    XenotypeDef xenotype = xenotypes[i];
                    if (xenotype == null || !xenotype.inheritable || xenotype.genes.NullOrEmpty() || !GD_HarAdapter.CanUseXenotype(xenotype, race))
                    {
                        continue;
                    }

                    for (int j = 0; j < xenotype.genes.Count; j++)
                    {
                        GeneDef gene = xenotype.genes[j];
                        if (IsWorldPoolGene(gene)
                            && GD_CompatibilityRegistry.CanUseForTargetRace(gene, race)
                            && GD_HarAdapter.CanHaveEndogene(gene, race)
                            && added.Add(gene))
                        {
                            AddFallbackWeight(weights, observedGenes, fallbackGenes, gene, weight);
                        }
                    }
                }
            }
        }

        private static void AddVanillaTargetFallback(
            Dictionary<GeneDef, float> weights,
            ISet<GeneDef> observedGenes,
            ISet<GeneDef> fallbackGenes,
            List<GD_WeightedGene> vanillaCommon,
            float multiplier)
        {
            if (vanillaCommon == null)
            {
                return;
            }

            for (int i = 0; i < vanillaCommon.Count; i++)
            {
                GD_WeightedGene entry = vanillaCommon[i];
                if (entry?.Gene != null && entry.Weight > 0f)
                {
                    AddFallbackWeight(weights, observedGenes, fallbackGenes, entry.Gene, entry.Weight * multiplier);
                }
            }
        }

        private static void AddFallbackWeight(
            Dictionary<GeneDef, float> weights,
            ISet<GeneDef> observedGenes,
            ISet<GeneDef> fallbackGenes,
            GeneDef gene,
            float amount)
        {
            if (gene == null || amount <= 0f)
            {
                return;
            }

            if (!observedGenes.Contains(gene))
            {
                fallbackGenes.Add(gene);
            }
            AddWeight(weights, gene, amount);
        }

        private static void AddVanillaXenotypeFallback(Dictionary<GeneDef, float> weights)
        {
            List<XenotypeDef> xenotypes = DefDatabase<XenotypeDef>.AllDefsListForReading;
            for (int i = 0; i < xenotypes.Count; i++)
            {
                XenotypeDef xenotype = xenotypes[i];
                if (xenotype == null || !xenotype.inheritable || !IsOfficialDef(xenotype) || xenotype.genes.NullOrEmpty())
                {
                    continue;
                }

                HashSet<GeneDef> countedForXenotype = new HashSet<GeneDef>();
                for (int j = 0; j < xenotype.genes.Count; j++)
                {
                    GeneDef gene = xenotype.genes[j];
                    if (IsVanillaPoolGene(gene) && countedForXenotype.Add(gene))
                    {
                        AddWeight(weights, gene, 1f);
                    }
                }
            }
        }

        private static bool IsEligibleWorldPawn(Pawn pawn, bool harAware)
        {
            if (pawn.RaceProps == null || !pawn.RaceProps.Humanlike || pawn.genes == null)
            {
                return false;
            }

            return pawn.def == ThingDefOf.Human || harAware && GD_HarAdapter.IsHarRace(pawn.def);
        }

        private static bool IsOfficialDef(Def def)
        {
            return def?.modContentPack != null && def.modContentPack.IsOfficialMod;
        }

        private static void AddWeight(Dictionary<GeneDef, float> weights, GeneDef gene, float amount)
        {
            float existing;
            weights.TryGetValue(gene, out existing);
            weights[gene] = existing + amount;
        }
    }
}





