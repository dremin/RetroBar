using System.ComponentModel;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using ManagedShell.Common.Enums;
using ManagedShell.Common.Helpers;

namespace ManagedShell.Common.SupportingClasses
{
    public class SearchResult : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string Path { get; set; }
        public string PathDisplay { get; set; }
        public string DateModified { get; set; }

        public ImageSource Icon
        {
            get
            {
                if (icon == null && !_iconLoading)
                {
                    _iconLoading = true;

                    Task.Factory.StartNew(() =>
                    {
                        string iconPath = Path.Substring(Path.IndexOf(':') + 1).Replace("/", "\\");
                        Icon = IconImageConverter.GetImageFromAssociatedIcon(iconPath, IconSize.Large);
                        _iconLoading = false;
                    }, CancellationToken.None, TaskCreationOptions.None, IconHelper.IconScheduler);
                }

                return icon;
            }
            set
            {
                icon = value;
                OnPropertyChanged("Icon");
            }
        }

        private bool _iconLoading = false;
        private ImageSource icon { get; set; }

        #region INotifyPropertyChanged Members

        /// <summary>
        /// This Event is raised whenever a property of this object has changed. Necesary to sync state when binding.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        [DebuggerNonUserCode]
        private void OnPropertyChanged(string propName)
        {
            if (this.PropertyChanged != null)
            {
                this.PropertyChanged(this, new PropertyChangedEventArgs(propName));
            }
        }
        #endregion
    }
}
