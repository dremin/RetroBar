using RetroBar.Utilities;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for ToolbarButton.xaml
    /// </summary>
    public partial class ToolbarButton : UserControl
    {
        public static readonly DependencyProperty IsOverflowItemProperty = DependencyProperty.Register(
            nameof(IsOverflowItem), typeof(bool), typeof(ToolbarButton),
            new PropertyMetadata(false, OnIsOverflowItemChanged));

        /// <summary>
        /// True when this button is being shown as a row inside the Quick Launch overflow
        /// flyout (icon + label) rather than as a plain icon on the toolbar itself.
        /// </summary>
        public bool IsOverflowItem
        {
            get => (bool)GetValue(IsOverflowItemProperty);
            set => SetValue(IsOverflowItemProperty, value);
        }

        public ToolbarButton()
        {
            InitializeComponent();

            setIconBinding();
            ApplyOverflowMode();
        }

        private static void OnIsOverflowItemChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as ToolbarButton)?.ApplyOverflowMode();
        }

        private void ApplyOverflowMode()
        {
            InnerButton.SetResourceReference(StyleProperty, IsOverflowItem ? "ToolbarOverflowItem" : "ToolbarButton");
            ToolbarLabel.Visibility = IsOverflowItem ? Visibility.Visible : Visibility.Collapsed;
        }

        private void setIconBinding()
        {
            string bindingPath = "SmallIcon";
            bool useLargeIcons = Settings.Instance.TaskbarScale > 1 || (Application.Current.FindResource("UseLargeIcons") as bool? ?? false);

            if (useLargeIcons)
            {
                bindingPath = "LargeIcon";
            }

            Binding iconBinding = new Binding(bindingPath);
            iconBinding.Mode = BindingMode.OneWay;
            ToolbarIcon.SetBinding(Image.SourceProperty, iconBinding);
        }
    }
}
