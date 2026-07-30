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
            text.AppendLine("\u57fa\u56e0\u591a\u6837\u6027\uff1a\u9636\u6bb5 6 \u7edf\u8ba1\u9875\u9762");
            text.AppendLine("\u4e16\u754c\u626b\u63cf\u4eba\u7269\u6570\uff1a" + snapshot.ScannedPawnCount);
            text.AppendLine("\u79cd\u65cf\u6570\uff1a" + snapshot.ScannedRaceCount);
            text.AppendLine("\u7cfb\u8c31\u57fa\u56e0\u89c2\u5bdf\u8bb0\u5f55\uff1a" + snapshot.Observations.Count);
            text.AppendLine("\u7cfb\u8c31\u57fa\u56e0\u6761\u76ee\uff1a" + snapshot.CountedEndogeneEntries);
            text.AppendLine("\u7f13\u5b58\u5237\u65b0\u8017\u65f6\uff1a" + GD_WorldGenePool.LastRefreshMilliseconds + " ms");
            int exactCompatibilityHits = snapshot.Observations.Count(x => x != null && x.Race != null && x.Gene != null && GD_CompatibilityRegistry.HasExactRule(x.Race) && GD_CompatibilityRegistry.CanUseForTargetRace(x.Gene, x.Race));
            int frdPriorHits = snapshot.Observations.Count(x => x != null && x.Race != null && x.Faction != null && GD_FrdAdapter.GetConfiguredGenePrior(x.Faction, x.Race, 0.5f).Any(y => y != null && y.Gene == x.Gene));
            text.AppendLine("\u9636\u6bb5 4 \u7cbe\u786e\u517c\u5bb9\u547d\u4e2d\u6570\uff1a" + exactCompatibilityHits);
            text.AppendLine("FRD \u5148\u9a8c\u547d\u4e2d\u6570\uff1a" + frdPriorHits);
            text.AppendLine("\u56de\u9000\u4e0e\u7cbe\u786e\u517c\u5bb9\uff1a" + GD_CompatibilityRegistry.BuildStatusReport());
            text.AppendLine("FRD \u5148\u9a8c\uff1a" + GD_FrdAdapter.BuildStatusReport());
            text.AppendLine("\u5df2\u5904\u7406\u65e7\u6863\u4eba\u7269\uff1a" + GD_OldSaveWorldComponent.GetProcessedCount());
            text.AppendLine();
            text.AppendLine("\u6309\u79cd\u65cf\u5e38\u89c1\u57fa\u56e0\uff1a");
            foreach (var group in snapshot.Observations.Where(x => x != null && x.Race != null && x.Gene != null).GroupBy(x => x.Race).OrderBy(x => x.Key.defName))
            {
                string genes = string.Join(", ", group.GroupBy(x => x.Gene).Select(x => new { Gene = x.Key, Count = x.Sum(y => y.Count) }).OrderByDescending(x => x.Count).ThenBy(x => x.Gene.defName).Take(12).Select(x => x.Gene.defName + "=" + x.Count));
                text.AppendLine("  " + group.Key.defName + "?" + (genes.NullOrEmpty() ? "\u65e0" : genes));
            }
            text.AppendLine();
            text.AppendLine("\u6309\u6765\u6e90\u6a21\u7ec4\u57fa\u56e0\u6570\uff1a");
            foreach (var group in snapshot.CommonGenes.Where(x => x?.Gene != null).GroupBy(x => x.Gene.modContentPack?.Name ?? "\u672a\u77e5").OrderByDescending(x => x.Count()).ThenBy(x => x.Key))
            {
                text.AppendLine("  " + group.Key + "=" + group.Count());
            }
            text.AppendLine();
            text.AppendLine(GD_Diagnostics.BuildReport());

            Rect viewRect = new Rect(0f, 0f, inRect.width - 24f, Mathf.Max(inRect.height, 1400f));
            Widgets.BeginScrollView(inRect, ref scrollPosition, viewRect);
            Widgets.Label(viewRect, text.ToString());
            Widgets.EndScrollView();
        }
    }
}
