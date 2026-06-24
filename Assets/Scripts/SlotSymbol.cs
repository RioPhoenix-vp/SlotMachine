using UnityEngine;

namespace SlotGame
{
    /// <summary>
    /// The set of symbols that can appear on a reel.
    /// The order here is purely identity; visual + payout data live in <see cref="SymbolData"/>.
    /// </summary>
    public enum SymbolType
    {
        Cherry,
        Bell,
        Bar,
        Seven
    }

    /// <summary>
    /// Designer-editable definition of a single slot symbol:
    /// its sprite, its type, and the multiplier paid when three of a kind line up.
    /// Created via the asset menu so each symbol is a reusable ScriptableObject.
    /// </summary>
    [CreateAssetMenu(fileName = "SymbolData", menuName = "SlotGame/Symbol Data", order = 0)]
    public class SymbolData : ScriptableObject
    {
        [Tooltip("Logical identity of this symbol.")]
        public SymbolType type;

        [Tooltip("Sprite shown on the reel for this symbol.")]
        public Sprite sprite;

        [Tooltip("Payout multiplier applied to the bet when three of this symbol line up.")]
        public int payoutMultiplier = 1;

        [Tooltip("Relative likelihood of this symbol landing. Higher = more common. " +
                 "Rare/high-value symbols should use a smaller weight.")]
        [Min(0.0001f)]
        public float spawnWeight = 1f;
    }
}