using System.Collections.Generic;
using UnityEngine;

namespace SlotGame
{
    /// <summary>
    /// Centralised random-number source for the slot machine.
    /// Keeping all randomness behind one class makes the game's fairness easy to
    /// audit, and lets us seed the generator for reproducible testing.
    /// </summary>
    public class SlotRng
    {
        private System.Random _random;

        /// <summary>
        /// Creates an RNG. Pass a fixed seed to make spins reproducible (useful for tests);
        /// leave null to seed from the system clock for genuine unpredictability.
        /// </summary>
        public SlotRng(int? seed = null)
        {
            _random = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        /// <summary>
        /// Picks one symbol from the supplied pool using each symbol's <see cref="SymbolData.spawnWeight"/>.
        /// This is a standard weighted roulette-wheel selection: symbols with larger weights
        /// occupy a larger slice of the [0, totalWeight) range and so land more often.
        /// </summary>
        public SymbolData PickWeighted(IReadOnlyList<SymbolData> pool)
        {
            if (pool == null || pool.Count == 0)
            {
                Debug.LogError("SlotRng.PickWeighted called with an empty symbol pool.");
                return null;
            }

            // Sum all weights to know the size of our selection range.
            float totalWeight = 0f;
            for (int i = 0; i < pool.Count; i++)
            {
                totalWeight += Mathf.Max(0f, pool[i].spawnWeight);
            }

            // Roll a value inside the total range, then walk the pool subtracting
            // each weight until the roll falls inside a symbol's slice.
            double roll = _random.NextDouble() * totalWeight;
            for (int i = 0; i < pool.Count; i++)
            {
                roll -= Mathf.Max(0f, pool[i].spawnWeight);
                if (roll <= 0d)
                    return pool[i];
            }

            // Floating-point edge case fallback: return the last symbol.
            return pool[pool.Count - 1];
        }
    }
}