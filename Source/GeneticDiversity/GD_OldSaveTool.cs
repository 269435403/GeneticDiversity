using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace GeneticDiversity
{
    internal static class GD_OldSaveTool
    {
        internal static void OpenSelected()
        {
            Pawn pawn = Find.Selector?.SingleSelectedThing as Pawn;
            if (pawn == null)
            {
                Messages.Message("\u8bf7\u5148\u9009\u62e9\u4e00\u540d Pawn\u3002", MessageTypeDefOf.RejectInput);
                return;
            }
            ConfirmAndProcess(new List<Pawn> { pawn }, "\u9009\u4e2d Pawn");
        }

        internal static void OpenPlayerFaction()
        {
            ConfirmAndProcess(AllPawns().Where(p => p.Faction == Faction.OfPlayer).ToList(), "\u73a9\u5bb6\u9635\u8425");
        }

        internal static void OpenCurrentMap()
        {
            Map map = Find.CurrentMap;
            ConfirmAndProcess(map == null ? new List<Pawn>() : map.mapPawns.AllPawnsSpawned.ToList(), "\u5f53\u524d\u5730\u56fe");
        }

        internal static void OpenWorld()
        {
            List<Pawn> pawns = AllPawns().ToList();
            Dialog_MessageBox.CreateConfirmation(
                "\u8b66\u544a\uff1a\u5168\u4e16\u754c\u65e7\u6863\u8865\u5145\u4f1a\u4fee\u6539\u53ef\u590d\u73b0\u7684\u5f53\u524d\u4eba\u7269\u57fa\u56e0\u3002\u8bf7\u5148\u53e6\u5b58\u6d4b\u8bd5\u6863\u3002\n\u5904\u7406\u8303\u56f4\uff1a" + pawns.Count + "\u4eba\u3002", delegate { Process(pawns); }, true);
        }

        internal static void ClearProcessed()
        {
            GD_OldSaveWorldComponent component = GD_OldSaveWorldComponent.Current;
            if (component == null)
            {
                return;
            }
            Dialog_MessageBox.CreateConfirmation("\u786e\u5b9a\u6e05\u9664\u65e7\u6863\u5df2\u5904\u7406\u8bb0\u5f55\uff1f\u6e05\u9664\u540e\u5de5\u5177\u4ecd\u53ef\u5728\u4e0b\u6b21\u64cd\u4f5c\u4e2d\u590d\u65b0\u5904\u7406\u3002", component.ClearProcessed, true);
        }

        internal static string GetStatus()
        {
            return "\u65e7\u6863\u5df2\u5904\u7406\uff1a" + GD_OldSaveWorldComponent.GetProcessedCount();
        }

        private static void ConfirmAndProcess(List<Pawn> pawns, string scope)
        {
            Dialog_MessageBox.CreateConfirmation(
                "\u8bf7\u5148\u53e6\u5b58\u6d4b\u8bd5\u6863\u3002\n\u8303\u56f4\uff1a" + scope + "\uff0c\u5019\u9009 Pawn=" + pawns.Count + "\u3002\n\u53ea\u4f1a\u590d\u7528\u6b63\u5f0f\u5019\u9009\u4e0e\u5b89\u5168\u6821\u9a8c\uff0c\u4e0d\u4f1a\u76f4\u63a5\u4fee\u6539\u5b58\u6863 XML\u3002", delegate { Process(pawns); }, true);
        }

        private static void Process(List<Pawn> pawns)
        {
            GD_OldSaveWorldComponent component = GD_OldSaveWorldComponent.Current;
            if (component == null)
            {
                Messages.Message("\u65e0\u6cd5\u627e\u5230\u4e16\u754c\u5904\u7406\u8bb0\u5f55\u7ec4\u4ef6\u3002", MessageTypeDefOf.RejectInput);
                return;
            }

            int changed = 0;
            int skipped = 0;
            int processed = 0;
            GD_Settings settings = GD_SettingsAccess.Current;
            if (!settings.Enabled)
            {
                Messages.Message("当前设置已关闭 Mutation and Hererity，未执行旧档补充。", MessageTypeDefOf.RejectInput);
                return;
            }
            foreach (Pawn pawn in pawns.Where(x => x != null).Distinct())
            {
                if (!IsEligible(pawn) || component.HasProcessed(pawn))
                {
                    skipped++;
                    continue;
                }
                GD_GenePoolSnapshot snapshot = GD_WorldGenePool.Current;
                int slots = settings.RollVariationSlotCount(pawn);
                int added = GD_GeneSelector.AddVariations(pawn, default(PawnGenerationRequest), snapshot, slots);
                component.MarkProcessed(pawn);
                processed++;
                changed += added;
            }
            Messages.Message("\u65e7\u6863\u8865\u5145\u5b8c\u6210\uff1a\u672c\u6b21\u8bb0\u5f55=" + processed + "\uff0c\u65b0\u589e\u57fa\u56e0=" + changed + "\uff0c\u8df3\u8fc7=" + skipped + "\uff0c\u7d2f\u8ba1\u5df2\u5904\u7406=" + GD_OldSaveWorldComponent.GetProcessedCount() + "\u3002", MessageTypeDefOf.TaskCompletion);
        }

        private static bool IsEligible(Pawn pawn)
        {
            return pawn != null
                && !pawn.Dead
                && pawn.genes != null
                && pawn.RaceProps != null
                && pawn.RaceProps.Humanlike
                && !pawn.DevelopmentalStage.Baby()
                && !pawn.DevelopmentalStage.Newborn()
                && !pawn.IsQuestLodger();
        }

        private static IEnumerable<Pawn> AllPawns()
        {
            if (Find.WorldPawns != null)
            {
                foreach (Pawn pawn in Find.WorldPawns.AllPawnsAliveOrDead)
                {
                    yield return pawn;
                }
            }
            foreach (Map map in Find.Maps ?? Enumerable.Empty<Map>())
            {
                foreach (Pawn pawn in map.mapPawns.AllPawnsSpawned)
                {
                    yield return pawn;
                }
            }
        }
    }
}
