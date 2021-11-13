using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace RetroBar
{
    public partial class SystemTheme : ResourceDictionary
    {
        private static void InvalidateMeasureRecursively(UIElement main)
        {
            if (main != null)
            {
                main.InvalidateMeasure();

                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(main); i++)
                {
                    if (VisualTreeHelper.GetChild(main, i) is UIElement sub)
                    {
                        InvalidateMeasureRecursively(sub);
                    }
                }
            }
        }

        private void ContextMenu_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender != null)
            {
                ContextMenu contextMenu = (ContextMenu)sender;
                if (contextMenu != null)
                {
                    InvalidateMeasureRecursively(contextMenu);
                }
            }
        }
    }
}