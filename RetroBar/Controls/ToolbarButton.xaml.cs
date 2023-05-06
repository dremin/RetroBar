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
        public ToolbarButton()
        {
            InitializeComponent();

            setIconBinding();
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
