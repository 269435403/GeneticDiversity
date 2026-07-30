using System.Collections.Generic;
using System.Linq;
using System.Text;
using LudeonTK;
using RimWorld;
using Verse;

namespace GeneticDiversity
{
    internal static class GD_DebugActions
    {
        private const string Category = "基因多样性";

        [DebugAction(Category, "清空基因池缓存", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        private static void ClearGenePoolCache()
        {
            GD_WorldGenePool.ClearCache();
            Messages.Message("基因多样性：缓存已清空；下次生成角色时重建。", MessageTypeDefOf.TaskCompletion);
        }

        [DebugAction(Category, "立即刷新基因池缓存", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        private static void RefreshGenePoolCache()
        {
            GD_GenePoolSnapshot snapshot = GD_WorldGenePool.RefreshNow();
            Messages.Message("基因多样性：缓存已刷新，Race " + snapshot.ScannedRaceCount + "，系谱观察 " + snapshot.Observations.Count + "。", MessageTypeDefOf.TaskCompletion);
        }

        [DebugAction(Category, "只读统计当前地图成年人物", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void ReportCurrentMapAdultHumanlikes()
        {
            Map map = Find.CurrentMap;
            if (map == null)
            {
                return;
            }

            List<Pawn> pawns = map.mapPawns.AllPawnsSpawned
                .Where(pawn => pawn != null
                    && !pawn.Dead
                    && pawn.RaceProps != null
                    && pawn.RaceProps.Humanlike
                    && pawn.DevelopmentalStage.Adult()
                    && pawn.genes != null)
                .ToList();

            Dictionary<GeneDef, int> endogeneFrequency = new Dictionary<GeneDef, int>();
            Dictionary<string, int> raceFrequency = new Dictionary<string, int>();
            Dictionary<string, int> xenotypeFrequency = new Dictionary<string, int>();
            Dictionary<int, int> endogeneCountDistribution = new Dictionary<int, int>();

            for (int i = 0; i < pawns.Count; i++)
            {
                Pawn pawn = pawns[i];
                Increment(raceFrequency, pawn.def?.defName ?? "UnknownRace");
                Increment(xenotypeFrequency, pawn.genes.Xenotype?.defName ?? "Custom/Unknown");
                Increment(endogeneCountDistribution, pawn.genes.Endogenes.Count);

                for (int j = 0; j < pawn.genes.Endogenes.Count; j++)
                {
                    GeneDef gene = pawn.genes.Endogenes[j]?.def;
                    if (gene != null)
                    {
                        Increment(endogeneFrequency, gene);
                    }
                }
            }

            GD_GenePoolSnapshot snapshot = GD_WorldGenePool.Current;
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("当前地图成年人物只读统计（不修改任何人物）：");
            builder.AppendLine("  地图=" + (map.info?.parent?.LabelCap ?? map.ToString()) + "，成年人物数量=" + pawns.Count + "。");
            builder.AppendLine("  种族=" + FormatTop(raceFrequency, 30) + "。");
            builder.AppendLine("  异种类型=" + FormatTop(xenotypeFrequency, 30) + "。");
            builder.AppendLine("  系谱基因数量分布=" + FormatTop(endogeneCountDistribution, 20) + "。");
            builder.AppendLine("  最常见系谱基因=" + FormatGeneTop(endogeneFrequency, 40) + "。");
            builder.AppendLine("  " + GD_WorldGenePool.FormatSummary(snapshot, "cached pool"));
            builder.AppendLine(GD_Diagnostics.BuildReport());
            GD_Log.Message("\n" + builder.ToString().TrimEnd());
            Messages.Message("只读统计完成：当前地图共有 " + pawns.Count + " 名成年人物；没有修改人物或存档。详细结果已写入日志。", MessageTypeDefOf.NeutralEvent);
        }

        [DebugAction(Category, "只读查看第三阶段HAR适配器报告", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        private static void ReportHarAdapter()
        {
            List<ThingDef> harRaces = DefDatabase<ThingDef>.AllDefsListForReading
                .Where(def => def?.race != null && def.race.Humanlike && GD_HarAdapter.IsHarRace(def))
                .OrderBy(def => def.defName)
                .ToList();

            string statusChinese = !GD_HarAdapter.IsAvailable
                ? "未检测到 Humanoid Alien Races（HAR）"
                : GD_HarAdapter.AdapterFailed
                    ? "检测到 HAR，但兼容适配失败"
                    : "检测到 HAR，兼容适配正常";
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("第三阶段 HAR 适配器只读报告（不调用种族固有基因随机选择，不修改游戏状态）：");
            builder.AppendLine("  状态=" + statusChinese
                + "，检测到的 HAR 人形异族种族定义=" + harRaces.Count
                + "，已缓存种族检查=" + GD_HarAdapter.CachedRaceCount + "。");

            for (int i = 0; i < harRaces.Count; i++)
            {
                ThingDef race = harRaces[i];
                IReadOnlyCollection<GeneDef> raceGenes = GD_HarAdapter.GetPossibleRaceGenes(race);
                IReadOnlyList<XenotypeDef> xenotypes = GD_HarAdapter.GetExplicitXenotypes(race);
                builder.AppendLine("  " + race.defName
                    + "：静态可能种族固有基因=" + raceGenes.Count
                    + " [" + string.Join(", ", raceGenes.Take(12).Select(gene => gene.defName)) + "]"
                    + "，明确允许的异种类型=" + xenotypes.Count
                    + " [" + string.Join(", ", xenotypes.Take(12).Select(xenotype => xenotype.defName)) + "].");
            }

            builder.AppendLine(GD_Diagnostics.BuildReport());
            GD_Log.Message("\n" + builder.ToString().TrimEnd());
            Messages.Message("只读报告完成：" + statusChinese + "，检测到 " + harRaces.Count + " 个 HAR 人形异族种族定义；未修改人物或存档。", MessageTypeDefOf.NeutralEvent);
        }

        [DebugAction(Category, "只读查看世界系谱池按种族摘要", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        private static void ReportWorldGenePoolByRace()
        {
            GD_GenePoolSnapshot snapshot = GD_WorldGenePool.Current;
            StringBuilder builder = new StringBuilder();
            builder.AppendLine("当前世界系谱池按种族和阵营的只读摘要（不修改任何人物）：");
            builder.AppendLine("  " + GD_WorldGenePool.FormatSummary(snapshot, "当前快照"));

            List<IGrouping<ThingDef, GD_GeneObservation>> raceGroups = snapshot.Observations
                .Where(observation => observation?.Race != null)
                .GroupBy(observation => observation.Race)
                .OrderBy(group => group.Key.defName)
                .ToList();
            for (int i = 0; i < raceGroups.Count; i++)
            {
                IGrouping<ThingDef, GD_GeneObservation> group = raceGroups[i];
                int total = group.Sum(observation => observation.Count);
                int factions = group.Select(observation => observation.Faction).Distinct().Count();
                string topGenes = string.Join(", ", group
                    .GroupBy(observation => observation.Gene)
                    .Select(geneGroup => new { Gene = geneGroup.Key, Count = geneGroup.Sum(observation => observation.Count) })
                    .OrderByDescending(entry => entry.Count)
                    .ThenBy(entry => entry.Gene.defName)
                    .Take(20)
                    .Select(entry => entry.Gene.defName + "=" + entry.Count));
                builder.AppendLine("  " + group.Key.defName
                    + "：系谱基因记录=" + total
                    + "，阵营分组=" + factions
                    + "，最常见基因=" + (topGenes.NullOrEmpty() ? "无" : topGenes) + "。");
            }

            GD_Log.Message("\n" + builder.ToString().TrimEnd());
            Messages.Message("只读摘要完成：已按种族和阵营统计当前世界系谱基因；没有修改人物或存档。", MessageTypeDefOf.NeutralEvent);
        }

        [DebugAction(Category, "只读查看第二阶段出生诊断", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        private static void ReportBirthMutationDiagnostics()
        {
            GD_Log.Message("\n" + GD_Diagnostics.BuildReport());
            Messages.Message("基因多样性：阶段2聚合诊断已写入日志；未修改任何 Pawn。", MessageTypeDefOf.NeutralEvent);
        }


        private static void Increment<TKey>(Dictionary<TKey, int> dictionary, TKey key)
        {
            int count;
            dictionary.TryGetValue(key, out count);
            dictionary[key] = count + 1;
        }

        private static string FormatTop<TKey>(Dictionary<TKey, int> dictionary, int limit)
        {
            if (dictionary.Count == 0)
            {
                return "none";
            }

            return string.Join(", ", dictionary.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key.ToString()).Take(limit).Select(pair => pair.Key + "=" + pair.Value));
        }

        private static string FormatGeneTop(Dictionary<GeneDef, int> dictionary, int limit)
        {
            if (dictionary.Count == 0)
            {
                return "none";
            }

            return string.Join(", ", dictionary.OrderByDescending(pair => pair.Value).ThenBy(pair => pair.Key.defName).Take(limit).Select(pair => pair.Key.defName + "=" + pair.Value));
        }
        [DebugAction(Category, "\u6253\u5f00\u9636\u6bb56\u7edf\u8ba1\u9875\u9762", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        private static void OpenPhase6Statistics()
        {
            Find.WindowStack.Add(new GD_StatisticsWindow());
        }

        [DebugAction(Category, "\u65e7\u6863\u8865\u5145\uff1a\u4ec5\u9009\u4e2d\u4eba\u7269", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        private static void OldSaveSelected()
        {
            GD_OldSaveTool.OpenSelected();
        }

        [DebugAction(Category, "\u65e7\u6863\u8865\u5145\uff1a\u4ec5\u73a9\u5bb6\u9635\u8425", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        private static void OldSavePlayerFaction()
        {
            GD_OldSaveTool.OpenPlayerFaction();
        }

        [DebugAction(Category, "\u65e7\u6863\u8865\u5145\uff1a\u4ec5\u5f53\u524d\u5730\u56fe", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.PlayingOnMap)]
        private static void OldSaveCurrentMap()
        {
            GD_OldSaveTool.OpenCurrentMap();
        }

        [DebugAction(Category, "\u65e7\u6863\u8865\u5145\uff1a\u5168\u4e16\u754c\uff08\u5f3a\u8b66\u544a\uff09", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        private static void OldSaveWorld()
        {
            GD_OldSaveTool.OpenWorld();
        }

        [DebugAction(Category, "\u67e5\u770b\u65e7\u6863\u5df2\u5904\u7406\u6570\u91cf", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        private static void OldSaveStatus()
        {
            Messages.Message(GD_OldSaveTool.GetStatus(), MessageTypeDefOf.NeutralEvent);
        }

        [DebugAction(Category, "\u6e05\u9664\u65e7\u6863\u5df2\u5904\u7406\u8bb0\u5f55", actionType = DebugActionType.Action, allowedGameStates = AllowedGameStates.Playing)]
        private static void OldSaveClearProcessed()
        {
            GD_OldSaveTool.ClearProcessed();
        }

    }
}
