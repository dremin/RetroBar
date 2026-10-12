using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace RetroBar.Controls
{
    /// <summary>
    /// Lays out its children across a fixed number of rows (or columns, in a vertical
    /// orientation) - like the classic Explorer toolbar wrapping across however many
    /// rows the taskbar is set to - then hides whichever trailing children still don't
    /// fit even after using every row, exposing them so a caller can show an overflow
    /// ("chevron") button and a flyout listing what got hidden.
    /// </summary>
    public class OverflowPanel : Panel
    {
        public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
            nameof(Orientation), typeof(Orientation), typeof(OverflowPanel),
            new FrameworkPropertyMetadata(Orientation.Horizontal, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        public Orientation Orientation
        {
            get => (Orientation)GetValue(OrientationProperty);
            set => SetValue(OrientationProperty, value);
        }

        public static readonly DependencyProperty RowsProperty = DependencyProperty.Register(
            nameof(Rows), typeof(int), typeof(OverflowPanel),
            new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>
        /// How many rows (or, in a vertical orientation, columns) to wrap children
        /// across before anything that still doesn't fit is sent to the overflow
        /// chevron instead. Bind this to the taskbar's own row count so multi-row mode
        /// applies to Quick Launch the same way it applies to the task list.
        /// </summary>
        public int Rows
        {
            get => (int)GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        public static readonly DependencyProperty OverflowButtonSizeProperty = DependencyProperty.Register(
            nameof(OverflowButtonSize), typeof(double), typeof(OverflowPanel),
            new FrameworkPropertyMetadata(18d, FrameworkPropertyMetadataOptions.AffectsMeasure | FrameworkPropertyMetadataOptions.AffectsArrange));

        /// <summary>
        /// The space to reserve along the layout axis, on the last row only, for the
        /// overflow (chevron) button, so items don't lay out underneath it. Kept in
        /// sync with the button's actual rendered size by the code-behind, since that
        /// size varies per theme.
        /// </summary>
        public double OverflowButtonSize
        {
            get => (double)GetValue(OverflowButtonSizeProperty);
            set => SetValue(OverflowButtonSizeProperty, value);
        }

        public static readonly DependencyPropertyKey HasOverflowPropertyKey = DependencyProperty.RegisterReadOnly(
            nameof(HasOverflow), typeof(bool), typeof(OverflowPanel), new PropertyMetadata(false));

        public static readonly DependencyProperty HasOverflowProperty = HasOverflowPropertyKey.DependencyProperty;

        public bool HasOverflow => (bool)GetValue(HasOverflowProperty);

        /// <summary>
        /// The data items (e.g. ShellFile instances) belonging to children that did not
        /// fit on this pass. Bind a separate ItemsControl (in a Popup) to this.
        /// </summary>
        public ObservableCollection<object> OverflowItems { get; } = new ObservableCollection<object>();

        /// <summary>
        /// Raised after a layout pass in which HasOverflow or OverflowItems may have changed.
        /// </summary>
        public event EventHandler OverflowChanged;

        private readonly HashSet<UIElement> _visible = new HashSet<UIElement>();
        private readonly Dictionary<UIElement, int> _rowOf = new Dictionary<UIElement, int>();
        private readonly Dictionary<UIElement, double> _mainOffsetOf = new Dictionary<UIElement, double>();

        protected override Size MeasureOverride(Size availableSize)
        {
            bool vertical = Orientation == Orientation.Vertical;
            double limit = vertical ? availableSize.Height : availableSize.Width;
            bool limited = !double.IsInfinity(limit);
            int rows = Math.Max(1, Rows);

            int count = InternalChildren.Count;
            double[] sizes = new double[count];
            double crossAxisDesired = 0;

            Size childConstraint = new Size(double.PositiveInfinity, double.PositiveInfinity);

            for (int i = 0; i < count; i++)
            {
                UIElement child = InternalChildren[i];
                child.Measure(childConstraint);

                double cross = vertical ? child.DesiredSize.Width : child.DesiredSize.Height;
                if (cross > crossAxisDesired)
                {
                    crossAxisDesired = cross;
                }

                sizes[i] = vertical ? child.DesiredSize.Height : child.DesiredSize.Width;
            }

            // First pass: try to wrap everything across the available rows using the
            // full row budget on every row - no chevron space reserved yet, since one
            // might not even be needed.
            PackResult pass = Pack(count, sizes, rows, limit, limited, limit);

            if (pass.SpaceOverflowCount > 0)
            {
                // Something doesn't fit even after using every row - redo the pack,
                // this time leaving room on the last row for the chevron button, since
                // it's about to become visible and would otherwise steal space out
                // from under whatever we just laid out.
                double lastRowBudget = limited ? Math.Max(0, limit - OverflowButtonSize) : limit;
                pass = Pack(count, sizes, rows, limit, limited, lastRowBudget);
            }

            _visible.Clear();
            _rowOf.Clear();
            _mainOffsetOf.Clear();

            List<object> overflow = new List<object>();

            for (int i = 0; i < count; i++)
            {
                UIElement child = InternalChildren[i];

                if (pass.Overflow[i])
                {
                    object item = (child as FrameworkElement)?.DataContext;
                    if (item != null)
                    {
                        overflow.Add(item);
                    }
                    continue;
                }

                _visible.Add(child);
                _rowOf[child] = pass.RowOf[i];
                _mainOffsetOf[child] = pass.MainOffset[i];
            }

            SyncOverflowItems(overflow);

            bool hasOverflow = overflow.Count > 0;
            if (hasOverflow != HasOverflow)
            {
                SetValue(HasOverflowPropertyKey, hasOverflow);
            }

            OverflowChanged?.Invoke(this, EventArgs.Empty);

            double widestRow = MaxOf(pass.RowLengths);
            double desiredMain = limited ? Math.Min(widestRow, limit) : widestRow;
            double desiredCross = crossAxisDesired * rows;

            return vertical
                ? new Size(desiredCross, desiredMain)
                : new Size(desiredMain, desiredCross);
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            bool vertical = Orientation == Orientation.Vertical;
            int rows = Math.Max(1, Rows);
            double totalCross = vertical ? finalSize.Width : finalSize.Height;
            double rowCross = totalCross / rows;

            foreach (UIElement child in InternalChildren)
            {
                if (!_visible.Contains(child))
                {
                    child.Arrange(new Rect(0, 0, 0, 0));
                    continue;
                }

                int row = _rowOf[child];
                double mainOffset = _mainOffsetOf[child];
                double mainSize = vertical ? child.DesiredSize.Height : child.DesiredSize.Width;
                double crossOffset = row * rowCross;

                Rect rect = vertical
                    ? new Rect(crossOffset, mainOffset, rowCross, mainSize)
                    : new Rect(mainOffset, crossOffset, mainSize, rowCross);

                child.Arrange(rect);
            }

            return finalSize;
        }

        /// <summary>
        /// Greedily packs children left-to-right (top-to-bottom when vertical) into up
        /// to `rows` rows, moving to the next row whenever an item would overflow the
        /// current one. `lastRowLimit` lets the caller reserve chevron space on just the
        /// final row without shrinking the earlier ones. Anything that still doesn't fit
        /// once every row is full comes back with Overflow[i] set instead of a row
        /// assignment.
        /// </summary>
        private static PackResult Pack(int count, double[] sizes, int rows, double limit, bool limited, double lastRowLimit)
        {
            int[] rowOf = new int[count];
            double[] mainOffset = new double[count];
            bool[] overflow = new bool[count];
            double[] rowLengths = new double[rows];
            int spaceOverflowCount = 0;

            int row = 0;
            double running = 0;

            for (int i = 0; i < count; i++)
            {
                while (true)
                {
                    double rowBudget = limited ? (row == rows - 1 ? lastRowLimit : limit) : double.PositiveInfinity;
                    bool fits = !limited || running + sizes[i] <= rowBudget + 0.01;

                    if (fits)
                    {
                        rowOf[i] = row;
                        mainOffset[i] = running;
                        running += sizes[i];
                        rowLengths[row] = Math.Max(rowLengths[row], running);
                        break;
                    }

                    if (row < rows - 1 && running > 0)
                    {
                        // This row already has something in it and the item doesn't
                        // fit - try the next row before giving up on it.
                        row++;
                        running = 0;
                        continue;
                    }

                    // Either there's no row left to try, or this row is already empty
                    // and the item still doesn't fit on its own - it overflows.
                    overflow[i] = true;
                    spaceOverflowCount++;
                    break;
                }
            }

            return new PackResult
            {
                RowOf = rowOf,
                MainOffset = mainOffset,
                Overflow = overflow,
                RowLengths = rowLengths,
                SpaceOverflowCount = spaceOverflowCount
            };
        }

        private static double MaxOf(double[] values)
        {
            double max = 0;
            foreach (double value in values)
            {
                if (value > max)
                {
                    max = value;
                }
            }

            return max;
        }

        private struct PackResult
        {
            public int[] RowOf;
            public double[] MainOffset;
            public bool[] Overflow;
            public double[] RowLengths;
            public int SpaceOverflowCount;
        }

        private void SyncOverflowItems(List<object> overflow)
        {
            // Only touch the ObservableCollection when something actually changed, so an
            // open Popup bound to it doesn't needlessly reset selection/scroll position.
            if (overflow.Count == OverflowItems.Count)
            {
                bool same = true;
                for (int i = 0; i < overflow.Count; i++)
                {
                    if (!ReferenceEquals(overflow[i], OverflowItems[i]))
                    {
                        same = false;
                        break;
                    }
                }

                if (same)
                {
                    return;
                }
            }

            OverflowItems.Clear();
            foreach (object item in overflow)
            {
                OverflowItems.Add(item);
            }
        }
    }
}
