using System.Collections.Generic;

namespace SlotGame
{
    /// <summary>
    /// How many symbols lined up, for the UI / messaging to react to.
    /// </summary>
    public enum WinTier
    {
        None,       // no match
        Partial,    // two of a kind -> half payout
        Full        // all reels match -> full payout
    }

    /// <summary>
    /// Result of evaluating a single spin's symbols against the win rules.
    /// </summary>
    public struct SpinResult
    {
        public bool IsWin;                // true for any payout (partial or full)
        public int Payout;                // total coins won (0 if no win)
        public SymbolType? WinningSymbol; // which symbol triggered the win, if any
        public WinTier Tier;              // None / Partial / Full
    }

    /// <summary>
    /// Decides whether a set of reel results is a win and how much it pays.
    /// Kept free of any Unity / MonoBehaviour dependency so the rules can be
    /// unit-tested in isolation and reasoned about clearly.
    ///
    /// Rules:
    ///   - ALL reels share a symbol  -> FULL win,    payout = bet * multiplier.
    ///   - Exactly TWO reels match    -> PARTIAL win, payout = half of the full payout.
    ///   - Otherwise                  -> no win.
    /// "Half" is rounded down so payouts are always whole coins.
    /// </summary>
    public static class PayoutEvaluator
    {
        public static SpinResult Evaluate(IReadOnlyList<SymbolData> reelSymbols, int bet)
        {
            var result = new SpinResult
            {
                IsWin = false,
                Payout = 0,
                WinningSymbol = null,
                Tier = WinTier.None
            };

            if (reelSymbols == null || reelSymbols.Count == 0)
                return result;

            // Count how many times each symbol type appears across the reels,
            // and remember a representative SymbolData for each type (for its multiplier).
            var counts = new Dictionary<SymbolType, int>();
            var sampleByType = new Dictionary<SymbolType, SymbolData>();
            foreach (var symbol in reelSymbols)
            {
                if (symbol == null) continue;
                counts.TryGetValue(symbol.type, out int c);
                counts[symbol.type] = c + 1;
                sampleByType[symbol.type] = symbol;
            }

            // Find the symbol that appears most often.
            SymbolType bestType = default;
            int bestCount = 0;
            foreach (var pair in counts)
            {
                if (pair.Value > bestCount)
                {
                    bestCount = pair.Value;
                    bestType = pair.Key;
                }
            }

            int totalReels = reelSymbols.Count;
            int fullPayout = bet * sampleByType[bestType].payoutMultiplier;

            if (bestCount == totalReels)
            {
                // Every reel matched -> full win.
                result.IsWin = true;
                result.Tier = WinTier.Full;
                result.WinningSymbol = bestType;
                result.Payout = fullPayout;
            }
            else if (bestCount == 2)
            {
                // Exactly two matched -> consolation: refund half the staked bet
                // (rounded down, min 1 coin). Note the machine already deducted the
                // full bet at spin start, so net effect of a two-match is losing half
                // the bet rather than all of it. e.g. bet 10 -> +5 here -> net -5.
                result.IsWin = true;
                result.Tier = WinTier.Partial;
                result.WinningSymbol = bestType;
                result.Payout = Max(1, bet / 2);
            }
            // else: highest count is 1 (all different) -> no win, defaults stand.

            return result;
        }

        /// <summary>Tiny local helper so this class needs no Unity / System.Math dependency.</summary>
        private static int Max(int a, int b) => a > b ? a : b;
    }
}