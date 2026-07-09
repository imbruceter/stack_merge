namespace StackMerge
{
    public static class StackMergeSpriteTags
    {
        public const string Chips = "<sprite name=\"chips\" tint=1>";
        public const string Insight = "<sprite name=\"insight\" tint=1>";
        public const string Token = "<sprite name=\"token\" tint=1>";

        private const string LegacyChips = "<sprite name=\"chips\">";
        private const string LegacyInsight = "<sprite name=\"insight\">";
        private const string LegacyToken = "<sprite name=\"token\">";
        private const string CompactChips = "<sprite name=\"chips\"tint=1>";
        private const string CompactInsight = "<sprite name=\"insight\"tint=1>";
        private const string CompactToken = "<sprite name=\"token\"tint=1>";

        public static string ApplyTint(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value
                .Replace(CompactChips, Chips)
                .Replace(CompactInsight, Insight)
                .Replace(CompactToken, Token)
                .Replace(LegacyChips, Chips)
                .Replace(LegacyInsight, Insight)
                .Replace(LegacyToken, Token);
        }

        public static string RemoveTint(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return value;
            }

            return value
                .Replace(Chips, LegacyChips)
                .Replace(Insight, LegacyInsight)
                .Replace(Token, LegacyToken)
                .Replace(CompactChips, LegacyChips)
                .Replace(CompactInsight, LegacyInsight)
                .Replace(CompactToken, LegacyToken);
        }
    }
}
