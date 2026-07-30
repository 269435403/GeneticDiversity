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
            
            listing.Label("\u57fa\u672c\u8bbe\u7f6e");
            listing.GapLine();
            listing.CheckboxLabeled("\u542f\u7528\u57fa\u56e0\u591a\u6837\u6027", ref settings.Enabled, "\u5173\u95ed\u540e\u4e0d\u4f1a\u5728\u65b0\u4eba\u7269\u4e0a\u8ffd\u52a0\u57fa\u56e0\u3002");
            listing.CheckboxLabeled("\u5f71\u54cd\u73a9\u5bb6\u9635\u8425\u65b0\u4eba\u7269", ref settings.AffectPlayerFaction);
            listing.CheckboxLabeled("\u5f71\u54cd NPC \u9635\u8423\u65b0\u4eba\u7269", ref settings.AffectNpcFactions);
            
            listing.Gap();
            listing.Label("\u5168\u5c40\u5f3a\u5ea6\uff1a" + GD_Settings.IntensityLabel(settings.Intensity));
            listing.Label("  \u2192 " + GD_Settings.IntensityDistribution(settings.Intensity), -1f);
            settings.Intensity = (GD_DiversityIntensity)Mathf.RoundToInt(listing.Slider((int)settings.Intensity, 0, 2));
            
            listing.Gap();
            listing.Label("\u7a81\u53d8\u6bd4\u4f8b\uff1a" + settings.MutationRatio.ToString("P0") + "\uff08\u6bcf\u4e2a\u4f4d\u70b9\u4ece\u7a81\u53d8\u6c60\u62bd\u53d6\u7684\u6982\u7387\uff09");
            settings.MutationRatio = listing.Slider(settings.MutationRatio, 0f, 1f);
            
            listing.Label("\u65b0\u751f\u513f\u7a81\u53d8\u6982\u7387\uff1a" + settings.BirthMutationChance.ToString("P0") + "\uff08\u6bcf\u6b21\u6700\u591a 1 \u4e2a\uff09");
            settings.BirthMutationChance = listing.Slider(settings.BirthMutationChance, 0f, 1f);
            
            listing.Gap();
            listing.CheckboxLabeled("\u5141\u8bb8\u666e\u901a\u529f\u80fd\u57fa\u56e0\u8de8\u79cd\u65cf", ref settings.AllowStandardCrossRace);
            listing.CheckboxLabeled("\u5141\u8bb8\u5916\u89c2/\u8eab\u4f53/\u81ea\u5b9a\u4e49\u57fa\u56e0\u8de8\u79cd\u65cf", ref settings.AllowSpecialCrossRace);
            listing.CheckboxLabeled("\u53ea\u4f7f\u7528\u540c\u79cd\u65cf\u57fa\u56e0\u6c60", ref settings.SameRaceOnly);
            listing.CheckboxLabeled("\u5141\u8bb8\u4e0d\u53ef\u9057\u4f20\u5f02\u79cd\u7c7b\u57fa\u56e0\u8fdb\u5165\u81ea\u7136\u7a81\u53d8", ref settings.AllowNonInheritableXenotypeMutation);
            listing.CheckboxLabeled("\u5141\u8bb8 Archite \u57fa\u56e0\u81ea\u7136\u7a81\u53d8", ref settings.AllowArchiteMutation);
            listing.CheckboxLabeled("\u8be6\u7ec6\u65e5\u5fd7", ref settings.VerboseLogging);
            
            listing.GapLine();
            listing.Label("\u57fa\u56e0\u9ed1\u540d\u5355\uff08" + settings.BlacklistedGenes.Count + " \u4e2a\u57fa\u56e0\uff0c" + settings.BlacklistedGeneCategories.Count + " \u4e2a\u7c7b\u522b\uff09");
            
            if (listing.ButtonText("\u7ba1\u7406\u9ed1\u540d\u5355\u57fa\u56e0"))
            {
                OpenGeneBlacklistMenu(settings);
            }
            
            if (listing.ButtonText("\u7ba1\u7406\u9ed1\u540d\u5355\u7c7b\u522b"))
            {
                OpenCategoryBlacklistMenu(settings);
            }
            
            listing.GapLine();
            listing.Label("\u9635\u8425\u8986\u76d6\uff1a" + settings.FactionIntensityOverrides.Count + " \u9879\uff1b\u79cd\u65cf\u8986\u76d6\uff1a" + settings.RaceIntensityOverrides.Count + " \u9879\u3002");
            Pawn selectedPawn = null;
            if (Current.ProgramState == ProgramState.Playing && Find.CurrentMap != null)
            {
                selectedPawn = Find.Selector.SingleSelectedThing as Pawn;
            }
            if (selectedPawn != null)
            {
                if (listing.ButtonText("\u4e3a\u5f53\u524d\u9009\u4e2d Pawn \u8bbe\u7f6e\u79cd\u65cf\u5f3a\u5ea6"))
                {
                    OpenRaceOverrideMenu(settings, selectedPawn);
                }
                if (selectedPawn.Faction != null && listing.ButtonText("\u4e3a\u5f53\u524d\u9009\u4e2d Pawn \u8bbe\u7f6e\u9635\u8425\u5f3a\u5ea6"))
                {
                    OpenFactionOverrideMenu(settings, selectedPawn);
                }
            }
            if (listing.ButtonText("\u6e05\u7a7a\u9635\u8423/\u79cd\u65cf\u5f3a\u5ea6\u8986\u76d6"))
            {
                settings.FactionIntensityOverrides.Clear();
                settings.RaceIntensityOverrides.Clear();
            }
            if (listing.ButtonText("\u7acb\u5373\u5237\u65b0\u57fa\u56e0\u6c60\u7f13\u5b58"))
            {
                GD_SettingsAccess.ApplyChanged(force: true);
                GD_WorldGenePool.RefreshNow();
            }
            if (listing.ButtonText("\u6253\u5f00\u7edf\u8ba1\u9875\u9762"))
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
                string label = gene.LabelCap + (isBlacklisted ? " [\u5df2\u7981\u7528]" : "");
                
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
                options.Insert(0, new FloatMenuOption("\u6e05\u7a7a\u6240\u6709\u9ed1\u540d\u5355\u57fa\u56e0", delegate
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
                string label = category.LabelCap + " (" + geneCount + " \u57fa\u56e0)" + (isBlacklisted ? " [\u5df2\u7981\u7528]" : "");
                
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
                options.Insert(0, new FloatMenuOption("\u6e05\u7a7a\u6240\u6709\u9ed1\u540d\u5355\u7c7b\u522b", delegate
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
            options.Add(new FloatMenuOption("\u79fb\u9664\u6b64\u79cd\u65cf\u8986\u76d6", delegate
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
            options.Add(new FloatMenuOption("\u79fb\u9664\u6b64\u9635\u8425\u8986\u76d6", delegate
            {
                settings.FactionIntensityOverrides.Remove(pawn.Faction.def.defName);
                GD_SettingsAccess.ApplyChanged();
            }));
            Find.WindowStack.Add(new FloatMenu(options));
        }

    }
}
