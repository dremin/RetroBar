using System;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ManagedShell.Common.Enums;
using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using ManagedShell.ShellFolders;

namespace ManagedShell.UWPInterop
{
    public class StoreApp : IEquatable<StoreApp>
    {
        public readonly string AppUserModelId;
        public string DisplayName;
        public string EntryPoint;
        public string HostId;
        public string IconColor;

        public string SmallIconPath;
        public string MediumIconPath;
        public string LargeIconPath;
        public string ExtraLargeIconPath;
        public string JumboIconPath;

        public StoreApp(string appUserModelId)
        {
            AppUserModelId = appUserModelId;
        }

        private ImageSource GetShellItemImageSource(IconSize size)
        {
            ImageSource img;
            ShellItem item = new ShellItem("shell:appsfolder\\" + AppUserModelId);
            item.AllowAsync = false;

            switch (size)
            {
                case IconSize.Small:
                    img = item.SmallIcon;
                    break;
                case IconSize.ExtraLarge:
                    img = item.ExtraLargeIcon;
                    break;
                case IconSize.Jumbo:
                    img = item.JumboIcon;
                    break;
                default:
                    img = item.LargeIcon;
                    break;
            }

            item.Dispose();

            if (img != null)
            {
                return img;
            }

            return IconImageConverter.GetDefaultIcon();
        }

        public ImageSource GetIconImageSource(IconSize size)
        {
            string iconPath;

            switch (size)
            {
                case IconSize.Small:
                    iconPath = SmallIconPath;
                    break;
                case IconSize.Medium:
                    iconPath = MediumIconPath;
                    break;
                case IconSize.ExtraLarge:
                    iconPath = ExtraLargeIconPath;
                    break;
                case IconSize.Jumbo:
                    iconPath = JumboIconPath;
                    break;
                default:
                    iconPath = LargeIconPath;
                    break;
            }

            if (string.IsNullOrEmpty(iconPath))
            {
                return GetShellItemImageSource(size);
            }

            try
            {
                BitmapImage img = new BitmapImage();
                img.BeginInit();
                img.UriSource = new Uri(iconPath, UriKind.Absolute);
                img.CacheOption = BitmapCacheOption.OnLoad;
                img.EndInit();
                img.Freeze();

                return img;
            }
            catch (Exception e)
            {
                ShellLogger.Debug($"StoreApp: Unable to load icon by path for {DisplayName}: {e.Message}");
                return GetShellItemImageSource(size);
            }
        }

        #region IEquitable
        public bool Equals(StoreApp other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return AppUserModelId == other.AppUserModelId;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((StoreApp) obj);
        }

        public override int GetHashCode()
        {
            return AppUserModelId.GetHashCode();
        }
        #endregion
    }
}
