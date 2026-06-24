using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SlotGame
{
    /// <summary>
    /// The central controller for the slot machine. It ties everything together:
    /// holds the symbol definitions, owns the RNG, drives the reels, applies bets,
    /// evaluates payouts, and reports results to the UI.
    ///
    /// Flow of a single spin:
    ///   1. Validate the player can afford the bet, then deduct it.
    ///   2. RNG pre-decides each reel's final symbol.
    ///   3. Reels animate and land on those symbols.
    ///   4. PayoutEvaluator checks the result; winnings are credited.
    ///   5. UI is notified of the outcome.
    /// </summary>
    public class SlotMachine : MonoBehaviour
    {
        [Header("Symbol Definitions")]
        [Tooltip("All symbols available on this machine (Cherry, Bell, BAR, Seven).")]
        [SerializeField] private List<SymbolData> symbols = new List<SymbolData>();

        [Header("Reels (left to right)")]
        [SerializeField] private Reel[] reels;

        [Header("Spin Staggering")]
        [Tooltip("Delay added per reel so they stop one after another, left to right.")]
        [SerializeField] private float perReelStopStagger = 0.35f;

        [Header("Economy")]
        [SerializeField] private int startingCoins = 50;

        [Header("Debug / Testing")]
        [Tooltip("If set, the RNG uses this fixed seed for reproducible spins. " +
                 "Leave at -1 for true randomness.")]
        [SerializeField] private int fixedSeed = -1;

        // --- Events the UI subscribes to. ---
        public event System.Action<int> OnCoinsChanged;          // new coin balance
        public event System.Action OnSpinStarted;                // a spin began
        public event System.Action<SpinResult> OnSpinCompleted;  // spin finished + result

        public int Coins { get; private set; }
        public bool IsSpinning { get; private set; }

        private SlotRng _rng;

        private void Awake()
        {
            _rng = new SlotRng(fixedSeed >= 0 ? fixedSeed : (int?)null);

            // Hand each reel the pool of symbols it may display.
            foreach (var reel in reels)
                reel.Initialize(symbols);

            Coins = startingCoins;
        }

        private void Start()
        {
            // Fire once so UI shows the opening balance.
            OnCoinsChanged?.Invoke(Coins);
        }

        /// <summary>
        /// Attempts to spin with the given bet. Returns false (and does nothing)
        /// if a spin is already running or the player can't afford the bet.
        /// </summary>
        public bool TrySpin(int bet)
        {
            if (IsSpinning)
                return false;

            if (bet <= 0 || bet > Coins)
            {
                Debug.Log($"Cannot bet {bet}: balance is {Coins}.");
                return false;
            }

            StartCoroutine(SpinRoutine(bet));
            return true;
        }

        private IEnumerator SpinRoutine(int bet)
        {
            IsSpinning = true;

            // 1. Take the bet up front.
            ChangeCoins(-bet);
            OnSpinStarted?.Invoke();

            // 2. RNG decides every reel's outcome before any animation plays.
            var results = new SymbolData[reels.Length];
            for (int i = 0; i < reels.Length; i++)
                results[i] = _rng.PickWeighted(symbols);

            // 3. Start all reels at once; stagger their *stop* via increasing delay.
            var spinCoroutines = new List<Coroutine>();
            for (int i = 0; i < reels.Length; i++)
            {
                float stopDelay = i * perReelStopStagger;
                spinCoroutines.Add(StartCoroutine(reels[i].Spin(results[i], stopDelay)));
            }

            // 4. Wait until every reel has fully stopped.
            foreach (var co in spinCoroutines)
                yield return co;

            // 5. Evaluate the line and pay out.
            var reelSymbols = new List<SymbolData>(results);
            SpinResult result = PayoutEvaluator.Evaluate(reelSymbols, bet);

            if (result.IsWin)
                ChangeCoins(result.Payout);

            IsSpinning = false;
            OnSpinCompleted?.Invoke(result);
        }

        /// <summary>Adjusts the coin balance and notifies listeners.</summary>
        private void ChangeCoins(int delta)
        {
            Coins += delta;
            OnCoinsChanged?.Invoke(Coins);
        }

        /// <summary>Adds coins from outside (e.g. a top-up button). Useful when the player busts.</summary>
        public void AddCoins(int amount)
        {
            if (amount <= 0) return;
            ChangeCoins(amount);
        }
    }
}