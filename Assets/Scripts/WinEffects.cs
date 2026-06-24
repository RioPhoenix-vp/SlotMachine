using System.Collections;
using UnityEngine;

namespace SlotGame
{
    /// <summary>
    /// Plays celebratory feedback when the player lands a FULL win (three of a kind):
    /// a particle burst and an optional sound. It listens to the machine's
    /// <see cref="SlotMachine.OnSpinCompleted"/> event and reacts only to
    /// <see cref="WinTier.Full"/>, so partial (two-match) wins stay quiet.
    ///
    /// The particle GameObject stays DISABLED until a win, so nothing renders or
    /// pre-warms beforehand. On a win it is enabled, played, and then disabled again
    /// once the burst has finished.
    ///
    /// Keeping this as its own component means the celebration is fully decoupled
    /// from game logic — you can swap, disable, or restyle the effect without
    /// touching the machine or the payout rules.
    /// </summary>
    public class WinEffects : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The machine whose spin results drive the celebration.")]
        [SerializeField] private SlotMachine slotMachine;

        [Header("Particles")]
        [Tooltip("Particle System to burst on a three-of-a-kind win. Its GameObject " +
                 "should start DISABLED in the scene; this script enables it on a win. " +
                 "Set 'Play On Awake' OFF and 'Looping' OFF.")]
        [SerializeField] private ParticleSystem winParticles;

        [Header("Audio (optional)")]
        [Tooltip("Optional. Sound played on a full win.")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip winClip;

        [Header("Options")]
        [Tooltip("If true, partial (two-match) wins also play a smaller version. " +
                 "Left off by default so only the jackpot celebrates.")]
        [SerializeField] private bool celebratePartialToo = false;

        private void Awake()
        {
            // Ensure the particle object begins hidden regardless of how it was left
            // in the scene, so it never shows before the first win.
            if (winParticles != null)
                winParticles.gameObject.SetActive(false);
        }

        private void OnEnable()
        {
            if (slotMachine != null)
                slotMachine.OnSpinCompleted += HandleSpinCompleted;
        }

        private void OnDisable()
        {
            if (slotMachine != null)
                slotMachine.OnSpinCompleted -= HandleSpinCompleted;
        }

        private void HandleSpinCompleted(SpinResult result)
        {
            bool shouldCelebrate =
                result.Tier == WinTier.Full ||
                (celebratePartialToo && result.Tier == WinTier.Partial);

            if (shouldCelebrate)
                Play();
        }

        /// <summary>Enables the particle object, fires the burst + sound, then hides it again.</summary>
        private void Play()
        {
            if (winParticles != null)
            {
                winParticles.gameObject.SetActive(true);
                winParticles.Clear(true);
                winParticles.Play();
                StartCoroutine(DisableWhenFinished());
            }

            if (audioSource != null && winClip != null)
                audioSource.PlayOneShot(winClip);
        }

        /// <summary>Waits for the burst to fully play out, then disables the object again.</summary>
        private IEnumerator DisableWhenFinished()
        {
            // Wait while the system is still emitting or has live particles on screen.
            yield return new WaitWhile(() => winParticles != null && winParticles.IsAlive(true));

            if (winParticles != null)
                winParticles.gameObject.SetActive(false);
        }
    }
}