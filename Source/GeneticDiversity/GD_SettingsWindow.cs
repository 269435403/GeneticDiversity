using System.Collections.Generic;
using System.Linq;
using RimWorld;
using UnityEngine;
using Verse;

namespace GeneticDiversity
{
    internal static class GD_SettingsWindow
    {
        private static Vector2 scrollPosition = Vector2.zero;

        internal static void Draw(Rect inRect, GD_Settings settings)
        {
            settings.Normalize();
            Rect viewRect = new Rect(0f, 0f, inRect.width - 20f, 1200f);
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
            Listing_Standard listing = new Listing_Standard();
            listing.Begin(viewRect);
            
            listing.Label("GD.Settings.Section.Basic".Translate());
            listing.GapLine();
            listing.CheckboxLabeled("GD.Settings.Enabled.Label".Translate(), ref settings.Enabled, "GD.Settings.Enabled.Tooltip".Translate());
            listing.CheckboxLabeled("GD.Settings.AffectPlayerFaction".Translate(), ref settings.AffectPlayerFaction);
            listing.CheckboxLabeled("GD.Settings.AffectNpcFactions".Translate(), ref settings.AffectNpcFactions);
            
            listing.Gap();
            listing.Label("GD.Settings.GlobalIntensity".Translate(GD_Settings.IntensityLabel(settings.Intensity)));
            listing.Label("GD.Settings.IntensityDistribution".Translate(GD_Settings.IntensityDistribution(settings.Intensity)), -1f);
            settings.Intensity = (GD_DiversityIntensity)Mathf.RoundToInt(listing.Slider((int)settings.Intensity, 0, 2));
            
            listing.Gap();
            listing.Label("GD.Settings.MutationRatio".Translate(settings.MutationRatio.ToString("P0")));
            settings.MutationRatio = listing.Slider(settings.MutationRatio, 0f, 1f);
            
            listing.Label("GD.Settings.BirthMutationChance".Translate(settings.BirthMutationChance.ToString("P0")));
            settings.BirthMutationChance = listing.Slider(settings.BirthMutationChance, 0f, 1f);
            
            listing.Gap();
            listing.CheckboxLabeled("GD.Settings.AllowStandardCrossRace".Translate(), ref settings.AllowStandardCrossRace);
            listing.CheckboxLabeled("GD.Settings.AllowSpecialCrossRace".Translate(), ref settings.AllowSpecialCrossRace);
            listing.CheckboxLabeled("GD.Settings.SameRaceOnly".Translate(), ref settings.SameRaceOnly);
            listing.CheckboxLabeled("GD.Settings.AllowNonInheritableXenotypeMutation".Translate(), ref settings.AllowNonInheritableXenotypeMutation);
            listing.CheckboxLabeled("GD.Settings.AllowArchiteMutation".Translate(), ref settings.AllowArchiteMutation);
            listing.CheckboxLabeled("GD.Settings.VerboseLogging".Translate(), ref settings.VerboseLogging);
            
            listing.GapLine();
            listing.Label("GD.Settings.Blacklist.Summary".Translate(settings.BlacklistedGenes.Count, settings.BlacklistedGeneCategories.Count));
            
            if (listing.ButtonText("GD.Settings.Blacklist.ManageGenes".Translate()))
            {
                OpenGeneBlacklistMenu(settings);
            }
            
            if (listing.ButtonText("GD.Settings.Blacklist.ManageCategories".Translate()))
            {
                OpenCategoryBlacklistMenu(settings);
            }
            
            listing.GapLine();
            listing.Label("GD.Settings.Overrides.Summary".Translate(settings.FactionIntensityOverrides.Count, settings.RaceIntensityOverrides.Count));
            Pawn selectedPawn = null;
            if (Current.ProgramState == ProgramState.Playing && Find.CurrentMap != null)
            {
                selectedPawn = Find.Selector.SingleSelectedThing as Pawn;
            }
            if (selectedPawn != null)
            {
                if (listing.ButtonText("GD.Settings.Overrides.SetSelectedRace".Translate()))
                {
                    OpenRaceOverrideMenu(settings, selectedPawn);
                }
                if (selectedPawn.Faction != null && listing.ButtonText("GD.Settings.Overrides.SetSelectedFaction".Translate()))
                {
                    OpenFactionOverrideMenu(settings, selectedPawn);
                }
            }
            if (listing.ButtonText("GD.Settings.Overrides.ClearAll".Translate()))
            {
                settings.FactionIntensityOverrides.Clear();
                settings.RaceIntensityOverrides.Clear();
            }
            if (listing.ButtonText("GD.Settings.RefreshCache".Translate()))
            {
                GD_SettingsAccess.ApplyChanged(force: true);
                GD_WorldGenePool.RefreshNow();
            }
            if (listing.ButtonText("GD.Settings.OpenStatistics".Translate()))
            {
                Find.WindowStack.Add(new GD_StatisticsWindow());
            }
            settings.Normalize();
            GD_SettingsAccess.ApplyChanged();
            listing.End();
            Widgets.EndScrollView();
        }

        private static void OpenGeneBlacklistMenu(GD_Settings settings)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            
            List<GeneDef> allGenes = DefDatabase<GeneDef>.AllDefsListForReading
                .Where(g => g != null && !g.defName.NullOrEmpty())
                .OrderBy(g => g.LabelCap.RawText)
                .ToList();

            foreach (GeneDef gene in allGenes)
            {
                bool isBlacklisted = settings.BlacklistedGenes.Contains(gene.defName);
                string label = isBlacklisted
                    ? "GD.Settings.Blacklist.DisabledEntry".Translate(gene.LabelCap).ToString()
                    : gene.LabelCap.ToString();
                
                options.Add(new FloatMenuOption(label, delegate
                {
                    if (isBlacklisted)
                    {
                        settings.BlacklistedGenes.Remove(gene.defName);
                    }
                    else
                    {
                        settings.BlacklistedGenes.Add(gene.defName);
                    }
                    GD_SettingsAccess.ApplyChanged();
                }));
            }

            if (settings.BlacklistedGenes.Count > 0)
            {
                options.Insert(0, new FloatMenuOption("GD.Settings.Blacklist.ClearGenes".Translate(), delegate
                {
                    settings.BlacklistedGenes.Clear();
                    GD_SettingsAccess.ApplyChanged();
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void OpenCategoryBlacklistMenu(GD_Settings settings)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            
            List<GeneCategoryDef> allCategories = DefDatabase<GeneCategoryDef>.AllDefsListForReading
                .Where(c => c != null && !c.defName.NullOrEmpty())
                .OrderBy(c => c.LabelCap.RawText)
                .ToList();

            foreach (GeneCategoryDef category in allCategories)
            {
                bool isBlacklisted = settings.BlacklistedGeneCategories.Contains(category.defName);
                int geneCount = DefDatabase<GeneDef>.AllDefsListForReading.Count(g => g.displayCategory == category);
                string label = (isBlacklisted
                    ? "GD.Settings.Blacklist.DisabledCategoryEntry"
                    : "GD.Settings.Blacklist.CategoryEntry").Translate(category.LabelCap, geneCount).ToString();
                
                options.Add(new FloatMenuOption(label, delegate
                {
                    if (isBlacklisted)
                    {
                        settings.BlacklistedGeneCategories.Remove(category.defName);
                    }
                    else
                    {
                        settings.BlacklistedGeneCategories.Add(category.defName);
                    }
                    GD_SettingsAccess.ApplyChanged();
                }));
            }

            if (settings.BlacklistedGeneCategories.Count > 0)
            {
                options.Insert(0, new FloatMenuOption("GD.Settings.Blacklist.ClearCategories".Translate(), delegate
                {
                    settings.BlacklistedGeneCategories.Clear();
                    GD_SettingsAccess.ApplyChanged();
                }));
            }

            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void OpenRaceOverrideMenu(GD_Settings settings, Pawn pawn)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            for (int value = 0; value <= 2; value++)
            {
                int captured = value;
                options.Add(new FloatMenuOption(GD_Settings.IntensityLabel(value), delegate
                {
                    settings.RaceIntensityOverrides[pawn.def.defName] = captured;
                    GD_SettingsAccess.ApplyChanged();
                }));
            }
            options.Add(new FloatMenuOption("GD.Settings.Overrides.RemoveRace".Translate(), delegate
            {
                settings.RaceIntensityOverrides.Remove(pawn.def.defName);
                GD_SettingsAccess.ApplyChanged();
            }));
            Find.WindowStack.Add(new FloatMenu(options));
        }

        private static void OpenFactionOverrideMenu(GD_Settings settings, Pawn pawn)
        {
            List<FloatMenuOption> options = new List<FloatMenuOption>();
            for (int value = 0; value <= 2; value++)
            {
                int captured = value;
                options.Add(new FloatMenuOption(GD_Settings.IntensityLabel(value), delegate
                {
                    settings.FactionIntensityOverrides[pawn.Faction.def.defName] = captured;
                    GD_SettingsAccess.ApplyChanged();
                }));
            }
            options.Add(new FloatMenuOption("GD.Settings.Overrides.RemoveFaction".Translate(), delegate
            {
                settings.FactionIntensityOverrides.Remove(pawn.Faction.def.defName);
                GD_SettingsAccess.ApplyChanged();
            }));
            Find.WindowStack.Add(new FloatMenu(options));
        }

    }
}
