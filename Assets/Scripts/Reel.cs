using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace SlotGame
{
    /// <summary>
    /// A single vertical reel that scrolls like a real slot machine. A column of
    /// stacked symbol cells slides downward continuously; when a cell drops below the
    /// bottom of the strip it wraps back to the top and is given a fresh sprite, creating
    /// an endless treadmill. To stop, the reel decelerates smoothly and snaps the chosen
    /// result symbol into the centre window.
    ///
    /// REQUIRED SCENE SETUP (per reel):
    ///   Reel_X                  (RectTransform, has RectMask2D + this Reel script)
    ///     ├─ Panel              (Image, glass backing slot-machine5_X)  [optional, behind]
    ///     └─ Cell_0 .. Cell_N   (Image each; assign to `cells`, ordered top -> bottom)
    ///
    /// Use 4 or 5 cells. The RectMask2D on Reel_X crops cells entering / leaving the
    /// window so only about one symbol is visible at a time.
    /// </summary>
    public class Reel : MonoBehaviour
    {
        [Header("Cells (assign top to bottom)")]
        [Tooltip("Stacked symbol Images that scroll. Use 4-5. Order them top-to-bottom.")]
        [SerializeField] private Image[] cells;

        [Tooltip("Vertical gap between adjacent cells, in UI units. Set this to the reel " +
                 "window height so exactly one symbol shows at a time.")]
        [SerializeField] private float cellSpacing = 150f;

        [Header("Spin Motion")]
        [Tooltip("Scroll speed during the main spin, in UI units per second.")]
        [SerializeField] private float spinSpeed = 1800f;

        [Tooltip("Seconds the reel scrolls at full speed before it starts stopping.")]
        [SerializeField] private float spinDuration = 1.0f;

        [Tooltip("Seconds taken to decelerate and settle onto the result symbol.")]
        [SerializeField] private float stopDuration = 0.6f;

        /// <summary>The symbol this reel finally landed on. Read after the spin completes.</summary>
        public SymbolData ResultSymbol { get; private set; }

        /// <summary>True while this reel is mid-spin.</summary>
        public bool IsSpinning { get; private set; }

        private IReadOnlyList<SymbolData> _symbolPool;

        // Highest (top) Y a cell occupies and lowest (bottom) Y, used for wrapping.
        private float _topY;
        private float _bottomY;

        /// <summary>Supplies the pool of symbols this reel can display. Called once at setup.</summary>
        public void Initialize(IReadOnlyList<SymbolData> symbolPool)
        {
            _symbolPool = symbolPool;
            LayOutCells();
            FillWithRandomSprites();
        }

        /// <summary>
        /// Positions cells in an evenly spaced vertical column centred on the reel,
        /// and records the top/bottom bounds used for wrap-around.
        /// </summary>
        private void LayOutCells()
        {
            int count = cells.Length;
            // Centre the column: e.g. 5 cells -> offsets +2,+1,0,-1,-2 times spacing.
            float half = (count - 1) * 0.5f;
            for (int i = 0; i < count; i++)
            {
                float y = (half - i) * cellSpacing;
                var rt = cells[i].rectTransform;
                Vector2 pos = rt.anchoredPosition;
                pos.y = y;
                rt.anchoredPosition = pos;
            }

            _topY = half * cellSpacing;                 // highest cell centre
            _bottomY = -half * cellSpacing;             // lowest cell centre
        }

        private void FillWithRandomSprites()
        {
            foreach (var cell in cells)
                cell.sprite = RandomSymbol().sprite;
        }

        /// <summary>
        /// Spins the reel and lands on <paramref name="result"/>.
        /// <paramref name="startDelay"/> staggers reels so they stop one after another.
        /// </summary>
        public IEnumerator Spin(SymbolData result, float startDelay)
        {
            if (_symbolPool == null || _symbolPool.Count == 0)
            {
                Debug.LogError($"Reel '{name}' has no symbol pool. Did you call Initialize()?");
                yield break;
            }

            IsSpinning = true;
            ResultSymbol = result;

            if (startDelay > 0f)
                yield return new WaitForSeconds(startDelay);

            // --- Phase 1: constant-speed scroll. ---
            float elapsed = 0f;
            while (elapsed < spinDuration)
            {
                Step(spinSpeed * Time.deltaTime);
                elapsed += Time.deltaTime;
                yield return null;
            }

            // --- Phase 2: ease out to a stop. ---
            float t = 0f;
            while (t < stopDuration)
            {
                float k = 1f - (t / stopDuration); // 1 -> 0
                float easedSpeed = spinSpeed * k * k; // quadratic ease-out
                Step(easedSpeed * Time.deltaTime);
                t += Time.deltaTime;
                yield return null;
            }

            // --- Phase 3: lock the result into the centre. ---
            SnapResultToCenter();
            IsSpinning = false;
        }

        /// <summary>
        /// Moves every cell down by <paramref name="delta"/>. Any cell that passes below
        /// the bottom bound wraps to just above the top cell and gets a new random sprite.
        /// </summary>
        private void Step(float delta)
        {
            for (int i = 0; i < cells.Length; i++)
            {
                var rt = cells[i].rectTransform;
                Vector2 pos = rt.anchoredPosition;
                pos.y -= delta;

                // Wrapped below the strip? Lift it back above the current top.
                if (pos.y < _bottomY - (cellSpacing * 0.5f))
                {
                    pos.y += cells.Length * cellSpacing;
                    cells[i].sprite = RandomSymbol().sprite;
                }

                rt.anchoredPosition = pos;
            }
        }

        /// <summary>
        /// Finds the cell nearest the centre (y == 0), assigns it the result sprite, and
        /// shifts the whole column by the leftover offset so it sits perfectly centred.
        /// Neighbour cells get fresh sprites so the column reads naturally around the result.
        /// </summary>
        private void SnapResultToCenter()
        {
            int nearest = 0;
            float bestDist = float.MaxValue;
            for (int i = 0; i < cells.Length; i++)
            {
                float dist = Mathf.Abs(cells[i].rectTransform.anchoredPosition.y);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    nearest = i;
                }
            }

            cells[nearest].sprite = ResultSymbol.sprite;

            // Snap-correct so the chosen cell is exactly centred (removes sub-pixel drift).
            float correction = cells[nearest].rectTransform.anchoredPosition.y;
            for (int i = 0; i < cells.Length; i++)
            {
                var rt = cells[i].rectTransform;
                Vector2 pos = rt.anchoredPosition;
                pos.y -= correction;
                rt.anchoredPosition = pos;
            }
        }

        private SymbolData RandomSymbol()
        {
            return _symbolPool[Random.Range(0, _symbolPool.Count)];
        }
    }
} 