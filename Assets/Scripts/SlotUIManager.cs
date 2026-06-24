using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace SlotGame
{
    /// <summary>
    /// Bridges the player-facing UI to the <see cref="SlotMachine"/> logic.
    /// It listens to machine events to update text/coins and shows the win popup,
    /// and forwards button presses (spin, bet selection) back into the machine.
    ///
    /// This class deliberately holds NO game rules. It only displays state and
    /// relays input, keeping presentation separate from logic.
    /// </summary>
    public class SlotUIManager : MonoBehaviour
    {
        [Header("Machine Reference")]
        [SerializeField] private SlotMachine slotMachine;

        [Header("HUD")]
        [SerializeField] private TMP_Text coinsText;
        [SerializeField] private TMP_Text betText;
        [SerializeField] private TMP_Text messageText;

        [Header("Controls")]
        [Tooltip("Optional. Leave empty if you use only the pull-lever to spin.")]
        [SerializeField] private Button spinButton;
        [SerializeField] private Button increaseBetButton;
        [SerializeField] private Button decreaseBetButton;

        [Header("Win Popup")]
        [Tooltip("Root object of the win popup; toggled on/off.")]
        [SerializeField] private GameObject winPopup;
        [SerializeField] private TMP_Text winAmountText;
        [SerializeField] private Button winPopupCloseButton;

        [Header("Bet Settings")]
        [Tooltip("Selectable bet amounts the player can cycle through (e.g. 10, 50, 100).")]
        [SerializeField] private int[] betOptions = { 10, 50, 100 };

        private int _betIndex = 0;

        private int CurrentBet => betOptions[Mathf.Clamp(_betIndex, 0, betOptions.Length - 1)];

        private void OnEnable()
        {
            // Subscribe to machine state changes.
            slotMachine.OnCoinsChanged += HandleCoinsChanged;
            slotMachine.OnSpinStarted += HandleSpinStarted;
            slotMachine.OnSpinCompleted += HandleSpinCompleted;

            // Wire buttons. The spin button is optional — the pull-lever can drive
            // spins instead — so only hook it up if one is assigned.
            if (spinButton != null)
                spinButton.onClick.AddListener(HandleSpinPressed);
            increaseBetButton.onClick.AddListener(IncreaseBet);
            decreaseBetButton.onClick.AddListener(DecreaseBet);
            if (winPopupCloseButton != null)
                winPopupCloseButton.onClick.AddListener(HideWinPopup);
        }

        private void OnDisable()
        {
            slotMachine.OnCoinsChanged -= HandleCoinsChanged;
            slotMachine.OnSpinStarted -= HandleSpinStarted;
            slotMachine.OnSpinCompleted -= HandleSpinCompleted;

            if (spinButton != null)
                spinButton.onClick.RemoveListener(HandleSpinPressed);
            increaseBetButton.onClick.RemoveListener(IncreaseBet);
            decreaseBetButton.onClick.RemoveListener(DecreaseBet);
            if (winPopupCloseButton != null)
                winPopupCloseButton.onClick.RemoveListener(HideWinPopup);
        }

        private void Start()
        {
            if (winPopup != null) winPopup.SetActive(false);
            UpdateBetText();
            SetMessage("Place your bet and spin!");
        }

        // ---------- Input handlers ----------

        private void HandleSpinPressed()
        {
            RequestSpin();
        }

        /// <summary>
        /// Public spin entry point. Used by both the on-screen Spin button and the
        /// pull-lever (<see cref="LeverButton"/>), so all spin requests funnel through
        /// one place with the same affordability check and messaging.
        /// </summary>
        public void RequestSpin()
        {
            HideWinPopup();
            bool started = slotMachine.TrySpin(CurrentBet);
            if (!started && !slotMachine.IsSpinning)
                SetMessage("Not enough coins for that bet!");
        }

        private void IncreaseBet()
        {
            if (slotMachine.IsSpinning) return;
            _betIndex = Mathf.Min(_betIndex + 1, betOptions.Length - 1);
            UpdateBetText();
        }

        private void DecreaseBet()
        {
            if (slotMachine.IsSpinning) return;
            _betIndex = Mathf.Max(_betIndex - 1, 0);
            UpdateBetText();
        }

        // ---------- Machine event responses ----------

        private void HandleCoinsChanged(int coins)
        {
            if (coinsText != null) coinsText.text = coins.ToString();
        }

        private void HandleSpinStarted()
        {
            SetMessage("Spinning...");
            SetControlsInteractable(false);
        }

        private void HandleSpinCompleted(SpinResult result)
        {
            SetControlsInteractable(true);

            switch (result.Tier)
            {
                case WinTier.Full:
                    SetMessage($"JACKPOT! Three of a kind — you won {result.Payout} coins!");
                    ShowWinPopup(result.Payout);
                    break;

                case WinTier.Partial:
                    SetMessage($"Two of a kind! Half your bet back — {result.Payout} coins.");
                    ShowWinPopup(result.Payout);
                    break;

                default:
                    SetMessage("No match. Try again!");
                    break;
            }
        }

        // ---------- Helpers ----------

        private void SetControlsInteractable(bool interactable)
        {
            if (spinButton != null)
                spinButton.interactable = interactable;
            increaseBetButton.interactable = interactable;
            decreaseBetButton.interactable = interactable;
        }

        private void UpdateBetText()
        {
            if (betText != null) betText.text = $"Bet: {CurrentBet}";
        }

        private void SetMessage(string msg)
        {
            if (messageText != null) messageText.text = msg;
        }

        private void ShowWinPopup(int amount)
        {
            if (winPopup == null) return;
            if (winAmountText != null) winAmountText.text = $"+{amount}";
            winPopup.SetActive(true);
        }

        private void HideWinPopup()
        {
            if (winPopup != null) winPopup.SetActive(false);
        }
    }
}