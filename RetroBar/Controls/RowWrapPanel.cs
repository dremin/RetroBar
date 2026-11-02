using ManagedShell.AppBar;
using RetroBar.Utilities;
using System;
using System.Windows;
using System.Windows.Controls;

namespace RetroBar.Controls
{
    /// <summary>
    /// A horizontal WrapPanel that, on a multi-row horizontal taskbar, wraps after just enough columns
    /// to fit its items into the given number of rows. Items then fill row by row, left to right.
    /// It stays a WrapPanel so that drag and drop keeps treating it as a horizontal list.
    /// </summary>
    public class RowWrapPanel : WrapPanel
    {
        public static readonly DependencyProperty RowsProperty = DependencyProperty.Register(nameof(Rows), typeof(int), typeof(RowWrapPanel),
            new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.AffectsMeasure));

        public int Rows
        {
            get => (int)GetValue(RowsProperty);
            set => SetValue(RowsProperty, value);
        }

        protected override Size MeasureOverride(Size constraint)
        {
            bool horizontalTaskbar = Settings.Instance.Edge is AppBarEdge.Top or AppBarEdge.Bottom;

            if (Orientation != Orientation.Horizontal || !horizontalTaskbar || Rows < 2)
            {
                return base.MeasureOverride(constraint);
            }

            double maxItemWidth = 0;
            int visibleItems = 0;

            foreach (UIElement child in InternalChildren)
            {
                if (child == null || child.Visibility == Visibility.Collapsed)
                {
                    continue;
                }

                child.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                maxItemWidth = Math.Max(maxItemWidth, child.DesiredSize.Width);
                visibleItems++;
            }

            if (visibleItems == 0)
            {
                return base.MeasureOverride(constraint);
            }

            int columns = (int)Math.Ceiling((double)visibleItems / Rows);
            // Small tolerance so that rounding does not push the last item of a row onto the next one
            double width = Math.Min(constraint.Width, columns * maxItemWidth + 0.5);

            return base.MeasureOverride(new Size(width, constraint.Height));
        }
    }
}
