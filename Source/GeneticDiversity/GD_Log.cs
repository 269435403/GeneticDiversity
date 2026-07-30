using Verse;

namespace GeneticDiversity
{
    internal static class GD_Log
    {
        private const string Prefix = "[Mutation and Hererity] ";

        internal static void Message(string text)
        {
            Log.Message(Prefix + text);
        }

        internal static void Warning(string text)
        {
            Log.Warning(Prefix + text);
        }

        internal static void Error(string text)
        {
            Log.Error(Prefix + text);
        }
    }
}
