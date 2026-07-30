using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorld;
using Verse;

namespace GeneticDiversity
{
    internal static class GD_FrdAdapter
    {
        internal const string PackageId = "yyyyy.mixedpeoplesfactions";
        internal const string HarmonyId = "yyyyy.factionracediversity";
        internal const string CulturalDiversityPackageId = "yyyyy.factionculturaldiversity";

        private static readonly object SyncRoot = new object();
        private static bool resolved;
        private static bool adapterFailed;
        private static bool warningLogged;
        private static Type raceRegistryType;
        private static Type xenotypeServiceType;
        private static Type modType;
        private static Type settingsType;
        private static PropertyInfo humanlikeRacesProperty;
        private static MethodInfo getKindsMethod;
        private static MethodInfo getAllowedXenotypesMethod;
        private static MethodInfo isSafeForRaceMethod;
        private static FieldInfo settingsField;
        private static MethodInfo getFactionSettingsMethod;

        internal static bool IsModLoaded => IsPackageLoaded(PackageId);
        internal static bool IsCulturalDiversityLoaded => IsPackageLoaded(CulturalDiversityPackageId);
        internal static bool AdapterFailed { get { EnsureResolved(); return adapterFailed; } }
        internal static bool IsAvailable { get { EnsureResolved(); return IsModLoaded && !adapterFailed; } }

        internal static string StatusLabel
        {
            get
            {
                EnsureResolved();
                if (!IsModLoaded) return "FRD未加载，使用阶段3逻辑";
                return adapterFailed ? "FRD已加载但反射适配失败，已安全降级" : "FRD可选反射适配可用";
            }
        }

        internal static string BuildStatusReport()
        {
            EnsureResolved();
            int raceCount = 0;
            if (IsAvailable)
            {
                try
                {
                    IEnumerable races = humanlikeRacesProperty.GetValue(null, null) as IEnumerable;
                    if (races != null)
                    {
                        foreach (object ignored in races) raceCount++;
                    }
                }
                catch (Exception exception)
                {
                    Fail("读取 FRD_RaceRegistry.HumanlikeRaces", exception);
                }
            }

            return StatusLabel
                + "；FRD种族登记=" + raceCount
                + "；FCD=" + (IsCulturalDiversityLoaded ? "已加载（保持独立）" : "未加载（不影响基因逻辑）")
                + "；程序集硬引用=" + (HasForbiddenAssemblyReference() ? "异常" : "无") + "。";
        }

        internal static List<GD_WeightedGene> GetConfiguredGenePrior(FactionDef faction, ThingDef race, float totalWeight)
        {
            List<GD_WeightedGene> result = new List<GD_WeightedGene>();
            if (faction == null || race == null || totalWeight <= 0f || !IsAvailable || !ModsConfig.BiotechActive)
            {
                return result;
            }

            try
            {
                object settings = settingsField.GetValue(null);
                if (settings == null)
                {
                    return result;
                }

                object factionSettings = getFactionSettingsMethod.Invoke(settings, new object[] { faction.defName });
                if (factionSettings == null || !RaceWeightAllowsTarget(factionSettings, race))
                {
                    return result;
                }

                IDictionary weights;
                bool enabled;
                if (!TryGetRaceXenotypeWeights(factionSettings, race, out enabled, out weights) || !enabled || weights == null)
                {
                    return result;
                }

                Dictionary<GeneDef, float> raw = new Dictionary<GeneDef, float>();
                float activeXenotypeWeight = 0f;
                foreach (DictionaryEntry entry in weights)
                {
                    string defName = entry.Key as string;
                    float configuredWeight;
                    if (defName.NullOrEmpty() || !TryPositiveFloat(entry.Value, out configuredWeight))
                    {
                        continue;
                    }

                    XenotypeDef xenotype = defName.Equals("Baseliner", StringComparison.OrdinalIgnoreCase)
                        ? XenotypeDefOf.Baseliner
                        : DefDatabase<XenotypeDef>.GetNamedSilentFail(defName);
                    if (xenotype == null || !xenotype.inheritable || xenotype.genes.NullOrEmpty() || !IsSafeForRace(xenotype, race))
                    {
                        continue;
                    }

                    List<GeneDef> safeGenes = xenotype.genes
                        .Where(gene => gene != null
                            && GD_CompatibilityRegistry.CanUseForTargetRace(gene, race)
                            && GD_HarAdapter.CanHaveEndogene(gene, race, false))
                        .Distinct()
                        .ToList();
                    if (safeGenes.Count == 0)
                    {
                        continue;
                    }

                    activeXenotypeWeight += configuredWeight;
                    float perGene = configuredWeight / safeGenes.Count;
                    for (int i = 0; i < safeGenes.Count; i++)
                    {
                        GeneDef gene = safeGenes[i];
                        raw[gene] = raw.TryGetValue(gene, out float old) ? old + perGene : perGene;
                    }
                }

                if (activeXenotypeWeight <= 0f || raw.Count == 0)
                {
                    return result;
                }

                float rawTotal = raw.Values.Sum();
                if (rawTotal <= 0f)
                {
                    return result;
                }

                return raw.OrderBy(pair => pair.Key.defName)
                    .Select(pair => new GD_WeightedGene(pair.Key, totalWeight * pair.Value / rawTotal, true))
                    .ToList();
            }
            catch (Exception exception)
            {
                Fail("读取 FRD 阵营/种族异种类型权重", exception);
                return result;
            }
        }

        internal static List<GD_WeightedGene> BuildSyntheticPriorForTests(ThingDef race, XenotypeDef xenotype, float totalWeight)
        {
            if (race == null || xenotype == null || totalWeight <= 0f || !IsAvailable || !xenotype.inheritable || xenotype.genes.NullOrEmpty() || !IsSafeForRace(xenotype, race))
            {
                return new List<GD_WeightedGene>();
            }

            List<GeneDef> genes = xenotype.genes
                .Where(gene => gene != null
                    && GD_CompatibilityRegistry.CanUseForTargetRace(gene, race)
                    && GD_HarAdapter.CanHaveEndogene(gene, race, false))
                .Distinct()
                .OrderBy(gene => gene.defName)
                .ToList();
            if (genes.Count == 0)
            {
                return new List<GD_WeightedGene>();
            }
            float perGene = totalWeight / genes.Count;
            return genes.Select(gene => new GD_WeightedGene(gene, perGene, true)).ToList();
        }

        internal static bool IsRegisteredRace(ThingDef race)
        {
            if (race == null || !IsAvailable) return false;
            try
            {
                IEnumerable races = humanlikeRacesProperty.GetValue(null, null) as IEnumerable;
                if (races == null) return false;
                foreach (object item in races)
                {
                    if (ReferenceEquals(item, race)) return true;
                }
                return false;
            }
            catch (Exception exception)
            {
                Fail("核对 FRD 人形种族登记", exception);
                return false;
            }
        }

        internal static int GetRegisteredKindCount(ThingDef race)
        {
            if (race == null || !IsAvailable) return 0;
            try
            {
                IEnumerable kinds = getKindsMethod.Invoke(null, new object[] { race }) as IEnumerable;
                if (kinds == null) return 0;
                int count = 0;
                foreach (object ignored in kinds) count++;
                return count;
            }
            catch (Exception exception)
            {
                Fail("读取 FRD_RaceRegistry.GetKinds", exception);
                return 0;
            }
        }

        internal static int GetAllowedXenotypeCount(ThingDef race)
        {
            if (race == null || !IsAvailable) return 0;
            try
            {
                IEnumerable xenotypes = getAllowedXenotypesMethod.Invoke(null, new object[] { race }) as IEnumerable;
                if (xenotypes == null) return 0;
                int count = 0;
                foreach (object ignored in xenotypes) count++;
                return count;
            }
            catch (Exception exception)
            {
                Fail("读取 FRD_XenotypeService.GetAllowedXenotypes", exception);
                return 0;
            }
        }

        internal static bool HasForbiddenAssemblyReference()
        {
            return typeof(GD_FrdAdapter).Assembly.GetReferencedAssemblies().Any(name =>
                name.Name.Equals("MixedPeoplesFactions", StringComparison.OrdinalIgnoreCase)
                || name.Name.Equals("FactionCulturalDiversity", StringComparison.OrdinalIgnoreCase));
        }

        internal static void ClearCaches()
        {
            lock (SyncRoot)
            {
                resolved = false;
                adapterFailed = false;
                warningLogged = false;
                raceRegistryType = null;
                xenotypeServiceType = null;
                modType = null;
                settingsType = null;
                humanlikeRacesProperty = null;
                getKindsMethod = null;
                getAllowedXenotypesMethod = null;
                isSafeForRaceMethod = null;
                settingsField = null;
                getFactionSettingsMethod = null;
            }
        }

        private static void EnsureResolved()
        {
            if (resolved) return;
            lock (SyncRoot)
            {
                if (resolved) return;
                resolved = true;
                if (!IsModLoaded) return;

                try
                {
                    raceRegistryType = GenTypes.GetTypeInAnyAssembly("MixedPeoplesFactions.FRD_RaceRegistry");
                    xenotypeServiceType = GenTypes.GetTypeInAnyAssembly("MixedPeoplesFactions.FRD_XenotypeService");
                    modType = GenTypes.GetTypeInAnyAssembly("MixedPeoplesFactions.MPF_Mod");
                    settingsType = GenTypes.GetTypeInAnyAssembly("MixedPeoplesFactions.MPF_Settings");
                    if (raceRegistryType == null || xenotypeServiceType == null || modType == null || settingsType == null)
                    {
                        throw new MissingMemberException("FRD public integration types are incomplete.");
                    }

                    humanlikeRacesProperty = raceRegistryType.GetProperty("HumanlikeRaces", BindingFlags.Public | BindingFlags.Static);
                    getKindsMethod = raceRegistryType.GetMethod("GetKinds", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(ThingDef) }, null);
                    getAllowedXenotypesMethod = xenotypeServiceType.GetMethod("GetAllowedXenotypes", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(ThingDef) }, null);
                    isSafeForRaceMethod = xenotypeServiceType.GetMethod("IsSafeForRace", BindingFlags.Public | BindingFlags.Static, null, new[] { typeof(XenotypeDef), typeof(ThingDef), typeof(bool) }, null);
                    settingsField = modType.GetField("Settings", BindingFlags.Public | BindingFlags.Static);
                    getFactionSettingsMethod = settingsType.GetMethod("GetFactionSettings", BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(string) }, null);
                    if (humanlikeRacesProperty == null || getKindsMethod == null || getAllowedXenotypesMethod == null
                        || isSafeForRaceMethod == null || settingsField == null || getFactionSettingsMethod == null)
                    {
                        throw new MissingMemberException("FRD public integration members are incomplete.");
                    }
                }
                catch (Exception exception)
                {
                    Fail("解析 FRD 公共只读 Integration API", exception);
                }
            }
        }

        private static bool RaceWeightAllowsTarget(object factionSettings, ThingDef race)
        {
            Type type = factionSettings.GetType();
            bool raceOverrideEnabled = ReadBool(type.GetField("raceOverrideEnabled", BindingFlags.Public | BindingFlags.Instance), factionSettings);
            if (!raceOverrideEnabled)
            {
                return true;
            }

            IDictionary raceWeights = type.GetField("raceWeights", BindingFlags.Public | BindingFlags.Instance)?.GetValue(factionSettings) as IDictionary;
            if (raceWeights == null || race == null)
            {
                return false;
            }

            return TryPositiveFloat(raceWeights[race.defName], out float configuredWeight) && configuredWeight > 0f;
        }

        private static bool TryGetRaceXenotypeWeights(object factionSettings, ThingDef race, out bool enabled, out IDictionary weights)
        {
            enabled = false;
            weights = null;
            Type type = factionSettings.GetType();
            FieldInfo byRaceField = type.GetField("xenotypeSettingsByRace", BindingFlags.Public | BindingFlags.Instance);
            IDictionary byRace = byRaceField?.GetValue(factionSettings) as IDictionary;
            object raceSettings = byRace != null ? byRace[race.defName] : null;
            if (raceSettings != null)
            {
                Type raceSettingsType = raceSettings.GetType();
                enabled = ReadBool(raceSettingsType.GetField("overrideEnabled", BindingFlags.Public | BindingFlags.Instance), raceSettings);
                weights = raceSettingsType.GetField("weights", BindingFlags.Public | BindingFlags.Instance)?.GetValue(raceSettings) as IDictionary;
                return true;
            }

            if (race == ThingDefOf.Human)
            {
                enabled = ReadBool(type.GetField("xenotypeOverrideEnabled", BindingFlags.Public | BindingFlags.Instance), factionSettings);
                weights = type.GetField("xenotypeWeights", BindingFlags.Public | BindingFlags.Instance)?.GetValue(factionSettings) as IDictionary;
                return true;
            }

            return false;
        }

        private static bool IsSafeForRace(XenotypeDef xenotype, ThingDef race)
        {
            return (bool)isSafeForRaceMethod.Invoke(null, new object[] { xenotype, race, false });
        }

        private static bool ReadBool(FieldInfo field, object owner)
        {
            return field != null && field.GetValue(owner) is bool value && value;
        }

        private static bool TryPositiveFloat(object value, out float result)
        {
            try
            {
                result = Convert.ToSingle(value);
                return result > 0f && !float.IsNaN(result) && !float.IsInfinity(result);
            }
            catch
            {
                result = 0f;
                return false;
            }
        }

        private static bool IsPackageLoaded(string packageId)
        {
            return LoadedModManager.RunningModsListForReading.Any(mod =>
                mod != null && !mod.PackageId.NullOrEmpty() && mod.PackageId.Equals(packageId, StringComparison.OrdinalIgnoreCase));
        }

        private static void Fail(string operation, Exception exception)
        {
            adapterFailed = true;
            if (warningLogged) return;
            warningLogged = true;
            GD_Log.Warning("阶段5 FRD 可选适配在“" + operation + "”时失败，已安全退回阶段3逻辑且不会阻止人物生成。" + exception.GetType().Name + ": " + exception.Message);
        }
    }
}

