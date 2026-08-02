using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GeneticDiversity
{
    internal enum GD_DiversityIntensity
    {
        Conservative = 0,
        Standard = 1,
        High = 2
    }

    internal sealed class GD_Settings : ModSettings
    {
        internal bool Enabled = true;
        internal bool AffectPlayerFaction = true;
        internal bool AffectNpcFactions = true;
        internal GD_DiversityIntensity Intensity = GD_DiversityIntensity.Standard;
        internal float MutationRatio = 0.10f;
        internal float BirthMutationChance = 0.10f;
        internal bool AllowStandardCrossRace = true;
        internal bool AllowSpecialCrossRace = false;
        internal bool VerboseLogging = false;
        internal bool SameRaceOnly = false;
        internal bool AllowNonInheritableXenotypeMutation = true;
        internal bool AllowArchiteMutation = false;
        internal HashSet<string> BlacklistedGenes = new HashSet<string>();
        internal HashSet<string> BlacklistedGeneCategories = new HashSet<string>();
        internal Dictionary<string, int> FactionIntensityOverrides = new Dictionary<string, int>();
        internal Dictionary<string, int> RaceIntensityOverrides = new Dictionary<string, int>();

        internal static GD_Settings CreateDefaults()
        {
            return new GD_Settings();
        }

        internal GD_DiversityIntensity GetEffectiveIntensity(Pawn pawn)
        {
            return GetEffectiveIntensity(pawn, default(PawnGenerationRequest));
        }

        internal GD_DiversityIntensity GetEffectiveIntensity(Pawn pawn, PawnGenerationRequest request)
        {
            string raceName = pawn?.def?.defName;
            if (!raceName.NullOrEmpty() && RaceIntensityOverrides.TryGetValue(raceName, out int raceOverride))
            {
                return (GD_DiversityIntensity)Math.Min(2, Math.Max(0, raceOverride));
            }

            FactionDef faction = pawn?.Faction?.def ?? request.Faction?.def;
            string factionName = faction?.defName;
            if (!factionName.NullOrEmpty() && FactionIntensityOverrides.TryGetValue(factionName, out int factionOverride))
            {
                return (GD_DiversityIntensity)Math.Min(2, Math.Max(0, factionOverride));
            }

            return Intensity;
        }

        internal int RollVariationSlotCount(Pawn pawn = null)
        {
            return RollVariationSlotCount(pawn, default(PawnGenerationRequest));
        }

        internal int RollVariationSlotCount(Pawn pawn, PawnGenerationRequest request)
        {
            GD_DiversityIntensity effective = GetEffectiveIntensity(pawn, request);
            float roll = Rand.Value;
            switch (effective)
            {
                case GD_DiversityIntensity.Conservative:
                    return roll < 0.45f ? 0 : roll < 0.90f ? 1 : roll < 0.99f ? 2 : 3;
                case GD_DiversityIntensity.High:
                    return roll < 0.05f ? 0 : roll < 0.40f ? 1 : roll < 0.80f ? 2 : 3;
                default:
                    // Keep the accepted phase 1-5 distribution unchanged.
                    return roll < 0.20f ? 0 : roll < 0.75f ? 1 : roll < 0.95f ? 2 : 3;
            }
        }

        internal static string IntensityLabel(int value)
        {
            switch (value)
            {
                case 0: return "GD.Settings.Intensity.Conservative".Translate().ToString();
                case 1: return "GD.Settings.Intensity.Standard".Translate().ToString();
                case 2: return "GD.Settings.Intensity.High".Translate().ToString();
                default: return "GD.Settings.Intensity.UseGlobal".Translate().ToString();
            }
        }

        internal static string IntensityLabel(GD_DiversityIntensity value)
        {
            return IntensityLabel((int)value);
        }

        internal static string IntensityDistribution(GD_DiversityIntensity value)
        {
            switch (value)
            {
                case GD_DiversityIntensity.Conservative:
                    return "GD.Settings.IntensityDistribution.Conservative".Translate().ToString();
                case GD_DiversityIntensity.High:
                    return "GD.Settings.IntensityDistribution.High".Translate().ToString();
                default: // Standard
                    return "GD.Settings.IntensityDistribution.Standard".Translate().ToString();
            }
        }

        internal bool SettingsAffectPawn(Pawn pawn, PawnGenerationRequest request)
        {
            if (!Enabled)
            {
                return false;
            }

            Faction faction = pawn?.Faction ?? request.Faction;
            bool isPlayerFaction = faction?.def != null && faction.def.isPlayer;
            return isPlayerFaction ? AffectPlayerFaction : AffectNpcFactions;
        }

        internal bool SettingsAffectPawn(Pawn pawn)
        {
            return SettingsAffectPawn(pawn, default(PawnGenerationRequest));
        }

        internal void Normalize()
        {
            MutationRatio = Math.Max(0f, Math.Min(1f, MutationRatio));
            BirthMutationChance = Math.Max(0f, Math.Min(1f, BirthMutationChance));
            Intensity = (GD_DiversityIntensity)Math.Min(2, Math.Max(0, (int)Intensity));
            if (FactionIntensityOverrides == null)
            {
                FactionIntensityOverrides = new Dictionary<string, int>();
            }
            if (RaceIntensityOverrides == null)
            {
                RaceIntensityOverrides = new Dictionary<string, int>();
            }
            if (BlacklistedGenes == null)
            {
                BlacklistedGenes = new HashSet<string>();
            }
            if (BlacklistedGeneCategories == null)
            {
                BlacklistedGeneCategories = new HashSet<string>();
            }
            FactionIntensityOverrides = FactionIntensityOverrides
                .Where(pair => !pair.Key.NullOrEmpty() && pair.Value >= 0 && pair.Value <= 2)
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            RaceIntensityOverrides = RaceIntensityOverrides
                .Where(pair => !pair.Key.NullOrEmpty() && pair.Value >= 0 && pair.Value <= 2)
                .ToDictionary(pair => pair.Key, pair => pair.Value);
            BlacklistedGenes.RemoveWhere(defName => defName.NullOrEmpty());
            BlacklistedGeneCategories.RemoveWhere(defName => defName.NullOrEmpty());
        }

        public override void ExposeData()
        {
            Scribe_Values.Look(ref Enabled, "enabled", true);
            Scribe_Values.Look(ref AffectPlayerFaction, "affectPlayerFaction", true);
            Scribe_Values.Look(ref AffectNpcFactions, "affectNpcFactions", true);
            Scribe_Values.Look(ref Intensity, "intensity", GD_DiversityIntensity.Standard);
            Scribe_Values.Look(ref MutationRatio, "mutationRatio", 0.10f);
            Scribe_Values.Look(ref BirthMutationChance, "birthMutationChance", 0.10f);
            Scribe_Values.Look(ref AllowStandardCrossRace, "allowStandardCrossRace", true);
            Scribe_Values.Look(ref AllowSpecialCrossRace, "allowSpecialCrossRace", false);
            Scribe_Values.Look(ref VerboseLogging, "verboseLogging", false);
            Scribe_Values.Look(ref SameRaceOnly, "sameRaceOnly", false);
            Scribe_Values.Look(ref AllowNonInheritableXenotypeMutation, "allowNonInheritableXenotypeMutation", true);
            Scribe_Values.Look(ref AllowArchiteMutation, "allowArchiteMutation", false);
            Scribe_Collections.Look(ref BlacklistedGenes, "blacklistedGenes", LookMode.Value);
            Scribe_Collections.Look(ref BlacklistedGeneCategories, "blacklistedGeneCategories", LookMode.Value);
            Scribe_Collections.Look(ref FactionIntensityOverrides, "factionIntensityOverrides", LookMode.Value, LookMode.Value);
            Scribe_Collections.Look(ref RaceIntensityOverrides, "raceIntensityOverrides", LookMode.Value, LookMode.Value);
            if (Scribe.mode == LoadSaveMode.PostLoadInit)
            {
                Normalize();
            }
        }
    }

    internal static class GD_SettingsAccess
    {
        private static int lastFingerprint = int.MinValue;

        internal static GD_Settings Current
        {
            get
            {
                if (GD_Mod.Instance != null)
                {
                    return GD_Mod.Instance.Settings;
                }
                return GD_Settings.CreateDefaults();
            }
        }

        internal static int Fingerprint(GD_Settings settings)
        {
            if (settings == null) return 0;
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + settings.Enabled.GetHashCode();
                hash = hash * 31 + settings.AffectPlayerFaction.GetHashCode();
                hash = hash * 31 + settings.AffectNpcFactions.GetHashCode();
                hash = hash * 31 + (int)settings.Intensity;
                hash = hash * 31 + settings.MutationRatio.GetHashCode();
                hash = hash * 31 + settings.BirthMutationChance.GetHashCode();
                hash = hash * 31 + settings.AllowStandardCrossRace.GetHashCode();
                hash = hash * 31 + settings.AllowSpecialCrossRace.GetHashCode();
                hash = hash * 31 + settings.VerboseLogging.GetHashCode();
                hash = hash * 31 + settings.SameRaceOnly.GetHashCode();
                hash = hash * 31 + settings.AllowNonInheritableXenotypeMutation.GetHashCode();
                hash = hash * 31 + settings.AllowArchiteMutation.GetHashCode();
                foreach (string defName in settings.BlacklistedGenes.OrderBy(s => s))
                {
                    hash = hash * 31 + defName.GetHashCode();
                }
                foreach (string defName in settings.BlacklistedGeneCategories.OrderBy(s => s))
                {
                    hash = hash * 31 + defName.GetHashCode();
                }
                foreach (KeyValuePair<string, int> pair in settings.FactionIntensityOverrides.OrderBy(pair => pair.Key))
                {
                    hash = hash * 31 + pair.Key.GetHashCode();
                    hash = hash * 31 + pair.Value;
                }
                foreach (KeyValuePair<string, int> pair in settings.RaceIntensityOverrides.OrderBy(pair => pair.Key))
                {
                    hash = hash * 31 + pair.Key.GetHashCode();
                    hash = hash * 31 + pair.Value;
                }
                return hash;
            }
        }

        internal static void ApplyChanged(bool force = false, bool logChange = true)
        {
            GD_Settings settings = Current;
            settings.Normalize();
            int fingerprint = Fingerprint(settings);
            if (force || fingerprint != lastFingerprint)
            {
                lastFingerprint = fingerprint;
                GD_WorldGenePool.ClearCache(logMessage: logChange);
                if (logChange && LanguageDatabase.activeLanguage != null)
                {
                    GD_Log.Message("GD.Settings.CacheInvalidated".Translate().ToString());
                }
            }
        }
    }
}
