using System.Linq;
using System.Text;
using UnityEngine;
using Verse;

namespace GeneticDiversity
{
    internal sealed class GD_StatisticsWindow : Window
    {
        private Vector2 scrollPosition;

        public GD_StatisticsWindow()
        {
            doCloseX = true;
            doCloseButton = true;
            forcePause = false;
        }

        public override Vector2 InitialSize => new Vector2(900f, 700f);

        public override void DoWindowContents(Rect inRect)
        {
            GD_GenePoolSnapshot snapshot = GD_WorldGenePool.Current;
            StringBuilder text = new StringBuilder();
            text.AppendLine("GD.Statistics.Title".Translate().ToString());
            text.AppendLine("GD.Statistics.ScannedPawns".Translate(snapshot.ScannedPawnCount).ToString());
            text.AppendLine("GD.Statistics.ScannedRaces".Translate(snapshot.ScannedRaceCount).ToString());
            text.AppendLine("GD.Statistics.ObservationRecords".Translate(snapshot.Observations.Count).ToString());
            text.AppendLine("GD.Statistics.EndogeneEntries".Translate(snapshot.CountedEndogeneEntries).ToString());
            text.AppendLine("GD.Statistics.CacheRefreshDuration".Translate(GD_WorldGenePool.LastRefreshMilliseconds).ToString());
            int exactCompatibilityHits = snapshot.Observations.Count(x => x != null && x.Race != null && x.Gene != null && GD_CompatibilityRegistry.HasExactRule(x.Race) && GD_CompatibilityRegistry.CanUseForTargetRace(x.Gene, x.Race));
            int frdPriorHits = snapshot.Observations.Count(x => x != null && x.Race != null && x.Faction != null && GD_FrdAdapter.GetConfiguredGenePrior(x.Faction, x.Race, 0.5f).Any(y => y != null && y.Gene == x.Gene));
            text.AppendLine("GD.Statistics.ExactCompatibilityHits".Translate(exactCompatibilityHits).ToString());
            text.AppendLine("GD.Statistics.FrdPriorHits".Translate(frdPriorHits).ToString());
            text.AppendLine("GD.Statistics.CompatibilityStatus".Translate(GD_CompatibilityRegistry.BuildStatusReport()).ToString());
            text.AppendLine("GD.Statistics.FrdPriorStatus".Translate(GD_FrdAdapter.BuildStatusReport()).ToString());
            text.AppendLine("GD.Statistics.OldSaveProcessed".Translate(GD_OldSaveWorldComponent.GetProcessedCount()).ToString());
            text.AppendLine();
            text.AppendLine("GD.Statistics.CommonGenesByRace".Translate().ToString());
            foreach (var group in snapshot.Observations.Where(x => x != null && x.Race != null && x.Gene != null).GroupBy(x => x.Race).OrderBy(x => x.Key.LabelCap.RawText))
            {
                string separator = "GD.Common.ListSeparator".Translate().ToString();
                string genes = string.Join(separator, group.GroupBy(x => x.Gene)
                    .Select(x => new { Gene = x.Key, Count = x.Sum(y => y.Count) })
                    .OrderByDescending(x => x.Count)
                    .ThenBy(x => x.Gene.LabelCap.RawText)
                    .Take(12)
                    .Select(x => "GD.Statistics.GeneCountEntry".Translate(x.Gene.LabelCap, x.Count).ToString()));
                text.AppendLine("GD.Statistics.IndentedEntry".Translate(group.Key.LabelCap, genes.NullOrEmpty() ? "GD.Common.None".Translate() : genes).ToString());
            }
            text.AppendLine();
            text.AppendLine("GD.Statistics.GenesBySourceMod".Translate().ToString());
            foreach (var group in snapshot.CommonGenes.Where(x => x?.Gene != null).GroupBy(x => x.Gene.modContentPack?.Name ?? "GD.Common.Unknown".Translate().ToString()).OrderByDescending(x => x.Count()).ThenBy(x => x.Key))
            {
                text.AppendLine("GD.Statistics.IndentedEntry".Translate(group.Key, group.Count()).ToString());
            }
            text.AppendLine();
            text.AppendLine(GD_Diagnostics.BuildStatisticsSummary());

            Rect viewRect = new Rect(0f, 0f, inRect.width - 24f, Mathf.Max(inRect.height, 1400f));
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
            Widgets.Label(viewRect, text.ToString());
            Widgets.EndScrollView();
        }
    }
}
