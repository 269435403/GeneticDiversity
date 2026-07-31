using System.Collections.Generic;
using System.Linq;
using RimWorld.Planet;
using Verse;

namespace GeneticDiversity
{
    internal sealed class GD_OldSaveWorldComponent : WorldComponent
    {
        private HashSet<int> processedPawnIds = new HashSet<int>();

        public GD_OldSaveWorldComponent(World world) : base(world)
        {
        }

        internal bool HasProcessed(Pawn pawn)
        {
            return pawn != null && processedPawnIds != null && processedPawnIds.Contains(pawn.thingIDNumber);
        }

        internal bool MarkProcessed(Pawn pawn)
        {
            if (processedPawnIds == null)
            {
                processedPawnIds = new HashSet<int>();
            }
            return pawn != null && processedPawnIds.Add(pawn.thingIDNumber);
        }

        internal void ClearProcessed()
        {
            if (processedPawnIds != null)
            {
                processedPawnIds.Clear();
            }
            else
            {
                processedPawnIds = new HashSet<int>();
            }
        }

        internal static GD_OldSaveWorldComponent Current
        {
            get
            {
                return Find.World?.GetComponent<GD_OldSaveWorldComponent>();
            }
        }

        internal static int GetProcessedCount()
        {
            return Current?.processedPawnIds?.Count ?? 0;
        }

        public override void ExposeData()
        {
            base.ExposeData();
            Scribe_Collections.Look(ref processedPawnIds, "processedPawnIds", LookMode.Value);
            if (processedPawnIds == null)
            {
                processedPawnIds = new HashSet<int>();
            }
        }
    }
}
