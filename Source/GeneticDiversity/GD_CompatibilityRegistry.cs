using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GeneticDiversity
{
    internal sealed class GD_CompatibilityRule
    {
        internal readonly GD_RaceGeneCompatibilityDef Def;
        internal readonly ThingDef Race;
        internal readonly HashSet<string> SourcePackageIds;
        internal readonly HashSet<XenotypeDef> NativeXenotypes;
        internal readonly HashSet<GeneDef> ExplicitNativeGenes;
        internal readonly HashSet<GeneDef> NativeGenes;
        internal readonly HashSet<GeneDef> ExcludedGenes;
        internal readonly HashSet<GeneDef> SameRaceOnlyGenes;
        internal readonly HashSet<GeneDef> CrossRaceSafeGenes;
        internal readonly HashSet<PawnKindDef> ExcludedPawnKinds;

        internal GD_CompatibilityRule(GD_RaceGeneCompatibilityDef def)
        {
            Def = def;
            Race = def.ResolvedRace;
            SourcePackageIds = new HashSet<string>(def.sourcePackageIds ?? new List<string>(), StringComparer.OrdinalIgnoreCase);
            NativeXenotypes = new HashSet<XenotypeDef>(def.ResolvedNativeXenotypes.Where(xenotype => xenotype != null && xenotype.inheritable));
            ExplicitNativeGenes = new HashSet<GeneDef>(def.ResolvedNativeGenes);
            NativeGenes = new HashSet<GeneDef>(ExplicitNativeGenes);
            ExcludedGenes = new HashSet<GeneDef>(def.ResolvedExcludedGenes);
            SameRaceOnlyGenes = new HashSet<GeneDef>(def.ResolvedSameRaceOnlyGenes);
            CrossRaceSafeGenes = new HashSet<GeneDef>(def.ResolvedCrossRaceSafeGenes);
            ExcludedPawnKinds = new HashSet<PawnKindDef>(def.ResolvedExcludedPawnKinds);

            foreach (XenotypeDef xenotype in NativeXenotypes)
            {
                if (!xenotype.genes.NullOrEmpty())
                {
                    NativeGenes.UnionWith(xenotype.genes.Where(gene => gene != null));
                }
            }
        }
    }

    internal static class GD_CompatibilityRegistry
    {
        internal const string KiiroPackageId = "Ancot.KiiroRaceGenePatch";
        internal const string MiliraPackageId = "Ancot.MiliraRaceGenePatch";
        internal const string WolfeinPackageId = "Ancot.WolfeinRaceGenePatch";
        internal const string RatkinExpandedPackageId = "EoralMilk.RatkinGeneExpanded";
        internal const string OaRatkinPackageId = "OARK.RatkinFaction.GeneExpand";
        internal const string EponaPackageId = "Epona.EponaDynasticRise";
        internal const string NivarianPackageId = "keeptpa.NivarianRace";
        internal const string NewRatkinPlusPackageId = "Solaris.RatkinRaceMod";
        internal const string OberoniaAureaPackageId = "OARK.RatkinFaction.OberoniaAurea";
        internal const string KiiroOrangeCatPackageId = "ZuoYao.KiiroOrangeCat";
        internal const string KiiroMaineCoonPackageId = "ZuoYao.KiiroMaineCoon";
        internal const string KiiroSiamesePackageId = "ZuoYao.KiiroSiamese";
        internal const string KiiroRagdollPackageId = "ZuoYao.KiiroRagdoll";
        internal const string DragonianPackageId = "RooAndGloomy.DragonianRaceMod";
        internal const string MoeLotlPackageId = "HenTaiLoliTeam.Axolotl";
        internal const string YuranPackageId = "RooAndGloomy.YuranRaceMod";
        internal const string MihoPackageId = "miho.fortifiedoutremer";

        internal static readonly string[] KnownSourcePackageIds =
        {
            KiiroPackageId,
            MiliraPackageId,
            WolfeinPackageId,
            RatkinExpandedPackageId,
            OaRatkinPackageId,
            EponaPackageId,
            NivarianPackageId,
            NewRatkinPlusPackageId,
            OberoniaAureaPackageId,
            KiiroOrangeCatPackageId,
            KiiroMaineCoonPackageId,
            KiiroSiamesePackageId,
            KiiroRagdollPackageId,
            DragonianPackageId,
            MoeLotlPackageId,
            YuranPackageId,
            MihoPackageId
        };

        private static readonly Dictionary<ThingDef, List<GD_CompatibilityRule>> RulesByRace = new Dictionary<ThingDef, List<GD_CompatibilityRule>>();
        private static readonly List<GD_CompatibilityRule> ActiveRules = new List<GD_CompatibilityRule>();
        private static readonly Dictionary<GeneDef, HashSet<ThingDef>> SameRaceOwnersByGene = new Dictionary<GeneDef, HashSet<ThingDef>>();
        private static readonly Dictionary<GeneDef, HashSet<ThingDef>> CrossRaceTargetsByGene = new Dictionary<GeneDef, HashSet<ThingDef>>();
        private static readonly HashSet<string> LoadedPackageIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        private static bool initialized;

        internal static int ActiveRuleCount
        {
            get
            {
                EnsureInitialized();
                return ActiveRules.Count;
            }
        }

        internal static void Initialize()
        {
            initialized = true;
            RulesByRace.Clear();
            ActiveRules.Clear();
            SameRaceOwnersByGene.Clear();
            CrossRaceTargetsByGene.Clear();
            LoadedPackageIds.Clear();

            foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading)
            {
                if (mod != null && !mod.PackageId.NullOrEmpty())
                {
                    LoadedPackageIds.Add(mod.PackageId);
                }
            }

            List<GD_RaceGeneCompatibilityDef> defs = DefDatabase<GD_RaceGeneCompatibilityDef>.AllDefsListForReading;
            for (int i = 0; i < defs.Count; i++)
            {
                GD_RaceGeneCompatibilityDef def = defs[i];
                if (def == null || def.ResolvedRace == null || !AllPackagesLoaded(def.sourcePackageIds))
                {
                    continue;
                }

                GD_CompatibilityRule rule = new GD_CompatibilityRule(def);
                ActiveRules.Add(rule);
                List<GD_CompatibilityRule> raceRules;
                if (!RulesByRace.TryGetValue(rule.Race, out raceRules))
                {
                    raceRules = new List<GD_CompatibilityRule>();
                    RulesByRace[rule.Race] = raceRules;
                }
                raceRules.Add(rule);

                foreach (GeneDef gene in rule.SameRaceOnlyGenes)
                {
                    AddOwner(SameRaceOwnersByGene, gene, rule.Race);
                }

                foreach (GeneDef gene in rule.CrossRaceSafeGenes)
                {
                    AddOwner(CrossRaceTargetsByGene, gene, rule.Race);
                }

                foreach (GeneDef gene in rule.NativeGenes)
                {
                    if (GD_WorldGenePool.Classify(gene) != GD_GeneCandidateKind.Standard)
                    {
                        AddOwner(SameRaceOwnersByGene, gene, rule.Race);
                    }
                }

                if (def.sameRaceOnlySourceCustomGenes || def.sameRaceOnlySourceStructuralGenes)
                {
                    List<GeneDef> allGenes = DefDatabase<GeneDef>.AllDefsListForReading;
                    for (int geneIndex = 0; geneIndex < allGenes.Count; geneIndex++)
                    {
                        GeneDef gene = allGenes[geneIndex];
                        if (!IsFromSourcePackage(gene, rule.SourcePackageIds))
                        {
                            continue;
                        }

                        GD_GeneCandidateKind kind = GD_WorldGenePool.Classify(gene);
                        bool customSameRace = def.sameRaceOnlySourceCustomGenes && kind == GD_GeneCandidateKind.CustomGeneClass;
                        bool structuralSameRace = def.sameRaceOnlySourceStructuralGenes && kind == GD_GeneCandidateKind.StructuralOrAppearance;
                        if (customSameRace || structuralSameRace)
                        {
                            AddOwner(SameRaceOwnersByGene, gene, rule.Race);
                        }
                    }
                }
            }
        }

        internal static void ClearCaches()
        {
            initialized = false;
            RulesByRace.Clear();
            ActiveRules.Clear();
            SameRaceOwnersByGene.Clear();
            CrossRaceTargetsByGene.Clear();
            LoadedPackageIds.Clear();
        }

        internal static bool IsSourceLoaded(string packageId)
        {
            EnsureInitialized();
            return !packageId.NullOrEmpty() && LoadedPackageIds.Contains(packageId);
        }

        internal static bool HasExactRule(ThingDef race)
        {
            EnsureInitialized();
            return race != null && RulesByRace.ContainsKey(race);
        }

        internal static IReadOnlyCollection<GeneDef> GetNativeGenes(ThingDef race)
        {
            return GetRules(race).SelectMany(rule => rule.NativeGenes).Distinct().ToList();
        }

        internal static IReadOnlyCollection<XenotypeDef> GetNativeXenotypes(ThingDef race)
        {
            return GetRules(race).SelectMany(rule => rule.NativeXenotypes).Distinct().ToList();
        }
        internal static IReadOnlyCollection<GeneDef> GetExplicitNativeGenes(ThingDef race)
        {
            return GetRules(race).SelectMany(rule => rule.ExplicitNativeGenes).Distinct().ToList();
        }

        internal static bool IsKnownForRace(GeneDef gene, ThingDef race)
        {
            return gene != null && GetRules(race).Any(rule => rule.NativeGenes.Contains(gene));
        }

        internal static bool IsExcludedForRace(GeneDef gene, ThingDef race)
        {
            return gene != null && GetRules(race).Any(rule => rule.ExcludedGenes.Contains(gene));
        }

        internal static bool IsPawnKindExcluded(PawnKindDef pawnKind, ThingDef race)
        {
            return pawnKind != null && GetRules(race).Any(rule => rule.ExcludedPawnKinds.Contains(pawnKind));
        }

        internal static bool CanUseForTargetRace(GeneDef gene, ThingDef targetRace)
        {
            if (gene == null || targetRace == null || IsExcludedForRace(gene, targetRace))
            {
                return false;
            }

            EnsureInitialized();
            HashSet<ThingDef> explicitTargets;
            if (CrossRaceTargetsByGene.TryGetValue(gene, out explicitTargets) && explicitTargets.Contains(targetRace))
            {
                return true;
            }

            HashSet<ThingDef> owners;
            return !SameRaceOwnersByGene.TryGetValue(gene, out owners) || owners.Contains(targetRace);
        }

        internal static string BuildStatusReport()
        {
            EnsureInitialized();
            List<string> parts = new List<string>();
            for (int i = 0; i < KnownSourcePackageIds.Length; i++)
            {
                string packageId = KnownSourcePackageIds[i];
                int rules = ActiveRules.Count(rule => rule.SourcePackageIds.Contains(packageId));
                string loadStatus = (LoadedPackageIds.Contains(packageId)
                    ? "GD.Compatibility.Loaded"
                    : "GD.Compatibility.NotLoaded").Translate().ToString();
                parts.Add("GD.Compatibility.StatusEntry".Translate(packageId, loadStatus, rules).ToString());
            }
            return string.Join("GD.Common.StatusSeparator".Translate().ToString(), parts);
        }

        internal static List<GD_CompatibilityRule> GetActiveRulesForSource(string packageId)
        {
            EnsureInitialized();
            return ActiveRules
                .Where(rule => rule.SourcePackageIds.Contains(packageId))
                .OrderBy(rule => rule.Race.defName)
                .ThenBy(rule => rule.Def.defName)
                .ToList();
        }

        private static List<GD_CompatibilityRule> GetRules(ThingDef race)
        {
            EnsureInitialized();
            List<GD_CompatibilityRule> rules;
            return race != null && RulesByRace.TryGetValue(race, out rules) ? rules : new List<GD_CompatibilityRule>();
        }

        private static void EnsureInitialized()
        {
            if (!initialized)
            {
                Initialize();
            }
        }

        private static bool AllPackagesLoaded(List<string> packageIds)
        {
            return !packageIds.NullOrEmpty() && packageIds.All(packageId => LoadedPackageIds.Contains(packageId));
        }

        private static bool IsFromSourcePackage(GeneDef gene, HashSet<string> packageIds)
        {
            return gene?.modContentPack != null
                && !gene.modContentPack.PackageId.NullOrEmpty()
                && packageIds.Contains(gene.modContentPack.PackageId);
        }

        private static void AddOwner(Dictionary<GeneDef, HashSet<ThingDef>> dictionary, GeneDef gene, ThingDef race)
        {
            if (gene == null || race == null)
            {
                return;
            }

            HashSet<ThingDef> owners;
            if (!dictionary.TryGetValue(gene, out owners))
            {
                owners = new HashSet<ThingDef>();
                dictionary[gene] = owners;
            }
            owners.Add(race);
        }
    }
}

