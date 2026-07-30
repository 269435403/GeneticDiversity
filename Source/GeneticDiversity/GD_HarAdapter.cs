using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace GeneticDiversity
{
    internal static class GD_HarAdapter
    {
        private const string RestrictionTypeName = "AlienRace.RaceRestrictionSettings";
        private const string AlienRaceDefTypeName = "AlienRace.ThingDef_AlienRace";

        private static readonly Dictionary<ThingDef, bool> HarRaceCache = new Dictionary<ThingDef, bool>();
        private static readonly Dictionary<ThingDef, IReadOnlyList<XenotypeDef>> ExplicitXenotypesCache = new Dictionary<ThingDef, IReadOnlyList<XenotypeDef>>();
        private static readonly Dictionary<ThingDef, IReadOnlyCollection<GeneDef>> PossibleRaceGenesCache = new Dictionary<ThingDef, IReadOnlyCollection<GeneDef>>();
        private static readonly Dictionary<ThingDef, Dictionary<GeneDef, bool>> CanHaveGeneCache = new Dictionary<ThingDef, Dictionary<GeneDef, bool>>();
        private static readonly Dictionary<ThingDef, Dictionary<XenotypeDef, bool>> CanUseXenotypeCache = new Dictionary<ThingDef, Dictionary<XenotypeDef, bool>>();
        private static readonly HashSet<ThingDef> RaceReadFailuresLogged = new HashSet<ThingDef>();

        private static bool initialized;
        private static bool available;
        private static bool adapterFailed;
        private static bool failureLogged;
        private static Type alienRaceDefType;
        private static Func<GeneDef, ThingDef, bool, bool> canHaveGene;
        private static Func<XenotypeDef, ThingDef, bool> canUseXenotype;

        internal static bool IsAvailable
        {
            get
            {
                EnsureInitialized();
                return available;
            }
        }

        internal static bool AdapterFailed
        {
            get
            {
                EnsureInitialized();
                return adapterFailed;
            }
        }

        internal static string StatusLabel
        {
            get
            {
                EnsureInitialized();
                if (!available)
                {
                    return "not detected";
                }

                return adapterFailed ? "detected, adapter failed" : "detected, adapter ready";
            }
        }

        internal static int CachedRaceCount => HarRaceCache.Count;

        internal static bool IsHarRace(ThingDef race)
        {
            if (race == null)
            {
                return false;
            }

            EnsureInitialized();
            if (!available || adapterFailed || alienRaceDefType == null)
            {
                return false;
            }

            bool cached;
            if (HarRaceCache.TryGetValue(race, out cached))
            {
                return cached;
            }

            bool result = alienRaceDefType.IsAssignableFrom(race.GetType());
            HarRaceCache[race] = result;
            return result;
        }

        internal static bool CanHaveEndogene(GeneDef gene, ThingDef race, bool recordDiagnostics = true)
        {
            if (gene == null || race == null)
            {
                return false;
            }

            EnsureInitialized();
            if (!available || adapterFailed || canHaveGene == null)
            {
                return ReferenceEquals(race, ThingDefOf.Human);
            }

            Dictionary<GeneDef, bool> raceCache;
            if (!CanHaveGeneCache.TryGetValue(race, out raceCache))
            {
                raceCache = new Dictionary<GeneDef, bool>();
                CanHaveGeneCache[race] = raceCache;
            }

            bool accepted;
            if (raceCache.TryGetValue(gene, out accepted))
            {
                if (!accepted && recordDiagnostics)
                {
                    GD_Diagnostics.RecordHarRejected();
                }
                return accepted;
            }

            try
            {
                accepted = canHaveGene(gene, race, false);
                raceCache[gene] = accepted;
                if (!accepted && recordDiagnostics)
                {
                    GD_Diagnostics.RecordHarRejected();
                }
                return accepted;
            }
            catch (Exception exception)
            {
                FailAdapter(exception);
                return ReferenceEquals(race, ThingDefOf.Human);
            }
        }

        internal static bool CanUseXenotype(XenotypeDef xenotype, ThingDef race)
        {
            if (xenotype == null || race == null)
            {
                return false;
            }

            EnsureInitialized();
            if (!available || adapterFailed || canUseXenotype == null)
            {
                return ReferenceEquals(race, ThingDefOf.Human);
            }

            Dictionary<XenotypeDef, bool> raceCache;
            if (!CanUseXenotypeCache.TryGetValue(race, out raceCache))
            {
                raceCache = new Dictionary<XenotypeDef, bool>();
                CanUseXenotypeCache[race] = raceCache;
            }

            bool accepted;
            if (raceCache.TryGetValue(xenotype, out accepted))
            {
                return accepted;
            }

            try
            {
                accepted = canUseXenotype(xenotype, race);
                raceCache[xenotype] = accepted;
                return accepted;
            }
            catch (Exception exception)
            {
                FailAdapter(exception);
                return ReferenceEquals(race, ThingDefOf.Human);
            }
        }

        internal static IReadOnlyList<XenotypeDef> GetExplicitXenotypes(ThingDef race)
        {
            if (!CanReadRaceSettings(race))
            {
                return Array.Empty<XenotypeDef>();
            }

            IReadOnlyList<XenotypeDef> cached;
            if (ExplicitXenotypesCache.TryGetValue(race, out cached))
            {
                return cached;
            }

            List<XenotypeDef> result = new List<XenotypeDef>();
            try
            {
                object alienRace = GetMemberValue(race, "alienRace");
                object restriction = GetMemberValue(alienRace, "raceRestriction");
                AddDefs(result, GetMemberValue(restriction, "xenotypeList"));
                AddDefs(result, GetMemberValue(restriction, "whiteXenotypeList"));

                HashSet<XenotypeDef> blacklisted = new HashSet<XenotypeDef>();
                AddDefs(blacklisted, GetMemberValue(restriction, "blackXenotypeList"));
                result.RemoveAll(xenotype => xenotype == null
                    || blacklisted.Contains(xenotype)
                    || !CanUseXenotype(xenotype, race));
            }
            catch (Exception exception)
            {
                LogRaceReadFailure(race, exception);
                result.Clear();
            }

            IReadOnlyList<XenotypeDef> distinct = result.Distinct().OrderBy(xenotype => xenotype.defName).ToList();
            ExplicitXenotypesCache[race] = distinct;
            return distinct;
        }

        internal static IReadOnlyCollection<GeneDef> GetPossibleRaceGenes(ThingDef race)
        {
            if (!CanReadRaceSettings(race))
            {
                return Array.Empty<GeneDef>();
            }

            IReadOnlyCollection<GeneDef> cached;
            if (PossibleRaceGenesCache.TryGetValue(race, out cached))
            {
                return cached;
            }

            HashSet<GeneDef> result = new HashSet<GeneDef>();
            try
            {
                object alienRace = GetMemberValue(race, "alienRace");
                object generalSettings = GetMemberValue(alienRace, "generalSettings");
                object raceGenes = GetMemberValue(generalSettings, "raceGenes");
                CollectChanceEntries(raceGenes, result, new HashSet<object>());
            }
            catch (Exception exception)
            {
                LogRaceReadFailure(race, exception);
                result.Clear();
            }

            IReadOnlyCollection<GeneDef> ordered = result.Where(gene => gene != null).OrderBy(gene => gene.defName).ToList();
            PossibleRaceGenesCache[race] = ordered;
            return ordered;
        }

        internal static void ClearCaches()
        {
            HarRaceCache.Clear();
            ExplicitXenotypesCache.Clear();
            PossibleRaceGenesCache.Clear();
            CanHaveGeneCache.Clear();
            CanUseXenotypeCache.Clear();
            RaceReadFailuresLogged.Clear();
        }

        private static bool CanReadRaceSettings(ThingDef race)
        {
            EnsureInitialized();
            return race != null && available && !adapterFailed && IsHarRace(race);
        }

        private static void CollectChanceEntries(object source, ISet<GeneDef> target, ISet<object> visited)
        {
            if (source == null || target == null || visited == null)
            {
                return;
            }

            if (source is string || !visited.Add(source))
            {
                return;
            }

            if (source is GeneDef directGene)
            {
                target.Add(directGene);
                return;
            }

            if (source is IEnumerable enumerable)
            {
                foreach (object item in enumerable)
                {
                    CollectChanceEntries(item, target, visited);
                }
                return;
            }

            // Mirror the static possibility boundary without consuming Rand. A zero chance entry
            // can never be selected; count limits options but does not suppress a direct entry.
            object chanceValue = GetMemberValue(source, "chance");
            if (chanceValue != null && Convert.ToSingle(chanceValue) <= 0f)
            {
                return;
            }

            object entry = GetMemberValue(source, "entry");
            if (entry is GeneDef gene)
            {
                target.Add(gene);
            }

            // Read fields only. Never call AlienChanceEntry.Select/Approved: those consume Rand
            // and mutate HAR's shuffledOptions cache. Any option can be chosen when count > 0.
            object countValue = GetMemberValue(source, "count");
            if (countValue == null || Convert.ToInt32(countValue) > 0)
            {
                CollectChanceEntries(GetMemberValue(source, "options"), target, visited);
            }
        }

        private static object GetMemberValue(object instance, string name)
        {
            if (instance == null || string.IsNullOrEmpty(name))
            {
                return null;
            }

            Type type = instance.GetType();
            FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
            {
                return field.GetValue(instance);
            }

            PropertyInfo property = type.GetProperty(name, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return property?.GetValue(instance, null);
        }

        private static void AddDefs<TDef>(ICollection<TDef> target, object source) where TDef : Def
        {
            if (target == null || !(source is IEnumerable enumerable))
            {
                return;
            }

            foreach (object item in enumerable)
            {
                if (item is TDef def && def != null && !target.Contains(def))
                {
                    target.Add(def);
                }
            }
        }

        private static void EnsureInitialized()
        {
            if (initialized)
            {
                return;
            }

            initialized = true;
            try
            {
                Type restrictionType = null;
                Assembly[] assemblies = AppDomain.CurrentDomain.GetAssemblies();
                for (int i = 0; i < assemblies.Length; i++)
                {
                    if (restrictionType == null)
                    {
                        restrictionType = assemblies[i].GetType(RestrictionTypeName, false);
                    }
                    if (alienRaceDefType == null)
                    {
                        alienRaceDefType = assemblies[i].GetType(AlienRaceDefTypeName, false);
                    }
                    if (restrictionType != null && alienRaceDefType != null)
                    {
                        break;
                    }
                }

                available = restrictionType != null || alienRaceDefType != null;
                if (!available)
                {
                    return;
                }

                if (restrictionType == null || alienRaceDefType == null)
                {
                    FailAdapter(null);
                    return;
                }

                MethodInfo canHaveGeneMethod = restrictionType.GetMethod(
                    "CanHaveGene",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(GeneDef), typeof(ThingDef), typeof(bool) },
                    null);
                MethodInfo canUseXenotypeMethod = restrictionType.GetMethod(
                    "CanUseXenotype",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    new[] { typeof(XenotypeDef), typeof(ThingDef) },
                    null);

                if (canHaveGeneMethod == null
                    || canHaveGeneMethod.ReturnType != typeof(bool)
                    || canUseXenotypeMethod == null
                    || canUseXenotypeMethod.ReturnType != typeof(bool))
                {
                    FailAdapter(null);
                    return;
                }

                canHaveGene = (Func<GeneDef, ThingDef, bool, bool>)canHaveGeneMethod.CreateDelegate(typeof(Func<GeneDef, ThingDef, bool, bool>));
                canUseXenotype = (Func<XenotypeDef, ThingDef, bool>)canUseXenotypeMethod.CreateDelegate(typeof(Func<XenotypeDef, ThingDef, bool>));
            }
            catch (Exception exception)
            {
                available = true;
                FailAdapter(exception);
            }
        }

        private static void FailAdapter(Exception exception)
        {
            bool firstFailure = !adapterFailed;
            adapterFailed = true;
            if (firstFailure)
            {
                GD_Diagnostics.RecordHarAdapterFailure();
            }

            if (failureLogged)
            {
                return;
            }

            failureLogged = true;
            string detail = exception == null
                ? "required HAR 1.6 types or method signatures were not found"
                : exception.GetType().Name + ": " + exception.Message;
            GD_Log.Warning("HAR compatibility adapter failed (" + detail + "). Vanilla Human behavior remains enabled; non-Human races are skipped conservatively.");
        }

        private static void LogRaceReadFailure(ThingDef race, Exception exception)
        {
            if (race == null || !RaceReadFailuresLogged.Add(race))
            {
                return;
            }

            GD_Diagnostics.RecordHarRaceReadFailure();
            string detail = exception == null ? "unknown error" : exception.GetType().Name + ": " + exception.Message;
            GD_Log.Warning("Could not read HAR fallback data for race " + race.defName + " (" + detail + "). Current-world observations remain usable; Def fallback for this race is skipped.");
        }
    }
}



