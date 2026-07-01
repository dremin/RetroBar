using System;
using System.Windows.Controls;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for InputLanguageMenuItem.xaml
    /// </summary>
    public partial class InputLanguageMenuItem : UserControl
    {
        public string Code { get; set; }
        public string DisplayName { get; set; }

        public InputLanguageMenuItem(string code, string displayName)
        {
            Code = code;
            DisplayName = displayName;
            DataContext = this;
            InitializeComponent();
        }
    }
}
