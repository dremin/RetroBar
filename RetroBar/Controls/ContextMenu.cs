using ManagedShell.Common.Helpers;
using System.Windows;
using System.Windows.Controls;

namespace RetroBar.Controls
{
    public partial class ContextMenu : System.Windows.Controls.ContextMenu
    {
        public ContextMenu()
        {
            SetResourceReference(StyleProperty, typeof(System.Windows.Controls.ContextMenu));
        }

        protected override void OnOpened(RoutedEventArgs e)
        {
            base.OnOpened(e);
            SoundHelper.PlaySystemSound(".Default", "MenuPopup");

            foreach (var item in Items)
            {
                if (item is MenuItem menuItem)
                {
                    menuItem.Click -= MenuItem_Click;
                    menuItem.Click += MenuItem_Click;

                    menuItem.SubmenuOpened -= MenuItem_SubmenuOpened;
                    menuItem.SubmenuOpened += MenuItem_SubmenuOpened;
                }
            }
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (!IsOpen)
            {
                SoundHelper.PlaySystemSound(".Default", "MenuCommand");
            }
        }

        private void MenuItem_SubmenuOpened(object sender, RoutedEventArgs e)
        {
            SoundHelper.PlaySystemSound(".Default", "MenuPopup");
        }
    }
}