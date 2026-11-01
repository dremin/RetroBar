using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RetroBar.Controls
{
    /// <summary>
    /// A small integer up/down box: type a number, use the arrow buttons, the Up/Down keys or the mouse wheel.
    /// </summary>
    public partial class NumericSpinner : UserControl
    {
        public static readonly DependencyProperty ValueProperty = DependencyProperty.Register(nameof(Value), typeof(int), typeof(NumericSpinner),
            new FrameworkPropertyMetadata(1, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnValueChanged, CoerceValue));

        public int Value
        {
            get => (int)GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(nameof(Minimum), typeof(int), typeof(NumericSpinner),
            new PropertyMetadata(1, OnLimitChanged));

        public int Minimum
        {
            get => (int)GetValue(MinimumProperty);
            set => SetValue(MinimumProperty, value);
        }

        public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(int), typeof(NumericSpinner),
            new PropertyMetadata(int.MaxValue, OnLimitChanged));

        public int Maximum
        {
            get => (int)GetValue(MaximumProperty);
            set => SetValue(MaximumProperty, value);
        }

        // Raised when the user changes the value (not when it is set from code).
        public event EventHandler ValueChangedByUser;

        private bool _isUserChange;

        public NumericSpinner()
        {
            InitializeComponent();
            UpdateDisplay();
        }

        private static object CoerceValue(DependencyObject sender, object baseValue)
        {
            NumericSpinner spinner = (NumericSpinner)sender;
            return Math.Max(spinner.Minimum, Math.Min(spinner.Maximum, (int)baseValue));
        }

        private static void OnValueChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            NumericSpinner spinner = (NumericSpinner)sender;
            spinner.UpdateDisplay();

            if (spinner._isUserChange)
            {
                spinner.ValueChangedByUser?.Invoke(spinner, EventArgs.Empty);
            }
        }

        private static void OnLimitChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            NumericSpinner spinner = (NumericSpinner)sender;
            spinner.CoerceValue(ValueProperty);
            spinner.UpdateDisplay();
        }

        private bool _isUpdatingDisplay;

        private void UpdateDisplay()
        {
            UpButton.IsEnabled = Value < Maximum;
            DownButton.IsEnabled = Value > Minimum;

            // Don't rewrite the text while the user is typing a valid value (it would move the caret)
            if (ValueTextBox.IsKeyboardFocused && int.TryParse(ValueTextBox.Text, out int typed) && typed == Value)
            {
                return;
            }

            _isUpdatingDisplay = true;
            ValueTextBox.Text = Value.ToString();
            _isUpdatingDisplay = false;
        }

        private void SetByUser(int value)
        {
            _isUserChange = true;
            Value = value;
            _isUserChange = false;
            UpdateDisplay();
        }

        private void CommitText()
        {
            if (int.TryParse(ValueTextBox.Text, out int typed))
            {
                SetByUser(typed);
            }
            else
            {
                UpdateDisplay();
            }
        }

        private void UpButton_Click(object sender, RoutedEventArgs e)
        {
            SetByUser(Value + 1);
        }

        private void DownButton_Click(object sender, RoutedEventArgs e)
        {
            SetByUser(Value - 1);
        }

        private void ValueTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            foreach (char c in e.Text)
            {
                if (!char.IsDigit(c))
                {
                    e.Handled = true;
                    return;
                }
            }
        }

        private void ValueTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch (e.Key)
            {
                case Key.Up:
                    SetByUser(Value + 1);
                    e.Handled = true;
                    break;
                case Key.Down:
                    SetByUser(Value - 1);
                    e.Handled = true;
                    break;
                case Key.Enter:
                    // Keep Enter in the box: it must not press the dialog's default button
                    CommitText();
                    e.Handled = true;
                    break;
            }
        }

        // Apply a typed number right away, so the taskbar previews it while typing
        private void ValueTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingDisplay || !ValueTextBox.IsKeyboardFocused)
            {
                return;
            }

            if (int.TryParse(ValueTextBox.Text, out int typed) && typed >= Minimum && typed <= Maximum && typed != Value)
            {
                _isUserChange = true;
                Value = typed;
                _isUserChange = false;
            }
        }

        private void ValueTextBox_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            SetByUser(Value + (e.Delta > 0 ? 1 : -1));
            e.Handled = true;
        }

        private void ValueTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            CommitText();
        }
    }
}
