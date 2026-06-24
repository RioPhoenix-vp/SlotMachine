using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace SlotGame
{
    /// <summary>
    /// Animates the pull-lever on the side of the machine. Two sprites are used:
    /// an "up" (resting) sprite and a "down" (pulled) sprite. On click the lever
    /// snaps to the down sprite, holds briefly, then returns to the up sprite —
    /// and on the way down it fires the spin via the connected <see cref="SlotUIManager"/>.
    ///
    /// Using two sprites this way is the classic, cheap way to fake a lever pull
    /// without a full skeletal/keyframe animation.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public class LeverAnimator : MonoBehaviour, IPointerDownHandler
    {
        [Header("Lever Sprites")]
        [Tooltip("Lever in the resting / up position (slot-machine2).")]
        [SerializeField] private Sprite leverUpSprite;

        [Tooltip("Lever in the pulled / down position (slot-machine3).")]
        [SerializeField] private Sprite leverDownSprite;

        [Header("Timing")]
        [Tooltip("How long the lever stays in the 'down' position before springing back, in seconds.")]
        [SerializeField] private float holdDownDuration = 0.5f;

        [Header("Spin Hook")]
        [Tooltip("The UI manager whose spin is triggered when the lever is pulled. " +
                 "Leave empty if you only want the visual animation.")]
        [SerializeField] private SlotUIManager uiManager;

        private Image _leverImage;
        private bool _isAnimating;

        private void Awake()
        {
            _leverImage = GetComponent<Image>();
            // Start in the resting position.
            if (leverUpSprite != null)
                _leverImage.sprite = leverUpSprite;
        }

        /// <summary>Fired by Unity's event system when the lever graphic is clicked/tapped.</summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            Pull();
        }

        /// <summary>
        /// Public so it can also be triggered from elsewhere (e.g. a keyboard shortcut).
        /// Ignores the input if the lever is already mid-animation.
        /// </summary>
        public void Pull()
        {
            if (_isAnimating)
                return;

            StartCoroutine(PullRoutine());
        }

        private IEnumerator PullRoutine()
        {
            _isAnimating = true;

            // 1. Swing down.
            if (leverDownSprite != null)
                _leverImage.sprite = leverDownSprite;

            // 2. Trigger the spin at the moment of the pull.
            if (uiManager != null)
                uiManager.RequestSpin();

            // 3. Hold in the down position.
            yield return new WaitForSeconds(holdDownDuration);

            // 4. Spring back up.
            if (leverUpSprite != null)
                _leverImage.sprite = leverUpSprite;

            _isAnimating = false;
        }
    }
}