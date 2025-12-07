using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Media;
using ManagedShell.Common.Enums;
using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using ManagedShell.Interop;
using ManagedShell.ShellFolders.Enums;
using ManagedShell.ShellFolders.Interfaces;

namespace ManagedShell.ShellFolders
{
    public class ShellItem : INotifyPropertyChanged, IDisposable
    {
        protected IShellItem _shellItem;

        private bool _smallIconLoading;
        private bool _largeIconLoading;
        private bool _extraLargeIconLoading;
        private bool _jumboIconLoading;

        #region Properties

        public bool Loaded => _shellItem != null;

        public bool AllowAsync = true;
        

        private bool? _isFileSystem;

        public bool IsFileSystem
        {
            get
            {
                if (_isFileSystem == null)
                {
                    _isFileSystem = ((Attributes & SFGAO.FILESYSTEM) != 0);
                }
                
                return (bool)_isFileSystem;
            }
        }

        private bool? _isNavigableFolder;

        public bool IsNavigableFolder
        {
            get
            {
                if (_isNavigableFolder == null)
                {
                    _isNavigableFolder = ((Attributes & SFGAO.FOLDER) != 0);
                }

                return (bool)_isNavigableFolder;
            }
        }

        private bool? _isFolder;

        public bool IsFolder
        {
            get
            {
                if (_isFolder == null)
                {
                    _isFolder = ((Attributes & SFGAO.FOLDER) != 0 && (Attributes & SFGAO.STREAM) == 0);
                }

                return (bool)_isFolder;
            }
        }

        protected ShellItem _parentItem;

        public ShellItem ParentItem
        {
            get
            {
                if (_parentItem == null)
                {
                    _parentItem = new ShellItem(GetParentShellItem());
                }

                return _parentItem;
            }
        }

        protected IntPtr _absolutePidl;
        
        public IntPtr AbsolutePidl
        {
            get
            {
                if (_absolutePidl == IntPtr.Zero)
                {
                    _absolutePidl = GetAbsolutePidl();
                }

                return _absolutePidl;
            }
            protected set
            {
                _absolutePidl = value;
            }
        }

        private IntPtr _relativePidl;

        public IntPtr RelativePidl
        {
            get
            {
                if (_relativePidl == IntPtr.Zero)
                {
                    GetParentAndItem();
                }

                return _relativePidl;
            }
        }

        private string _path;
        
        public string Path
        {
            get
            {
                if (_path == null)
                {
                    _path = GetDisplayName(SIGDN.DESKTOPABSOLUTEPARSING);
                }

                return _path;
            }
        }

        private string _fileName;
        
        public string FileName
        {
            get
            {
                if (_fileName == null)
                {
                    _fileName = GetDisplayName(SIGDN.PARENTRELATIVEPARSING);
                }

                return _fileName;
            }
        }

        private string _displayName;
        
        public string DisplayName
        {
            get
            {
                if (_displayName == null)
                {
                    _displayName = GetDisplayName(SIGDN.NORMALDISPLAY);
                }

                return _displayName;
            }
        }

        private SFGAO _attributes = 0;
        
        public SFGAO Attributes
        {
            get
            {
                if (_attributes == 0)
                {
                    _attributes = GetAttributes();
                }

                return _attributes;
            }
        }

        private ImageSource _smallIcon;

        public ImageSource SmallIcon
        {
            get
            {
                if (_smallIcon == null && !_smallIconLoading)
                {
                    if (AllowAsync)
                    {
                        _smallIconLoading = true;

                        Task.Factory.StartNew(() =>
                        {
                            SmallIcon = GetDisplayIcon(IconSize.Small);
                            SmallIcon?.Freeze();
                            _smallIconLoading = false;
                        }, CancellationToken.None, TaskCreationOptions.None, IconHelper.IconScheduler);
                    }
                    else
                    {
                        _smallIcon = GetDisplayIcon(IconSize.Small);
                        _smallIcon?.Freeze();
                    }
                }

                return _smallIcon;
            }
            private set
            {
                _smallIcon = value;
                OnPropertyChanged();
            }
        }

        private ImageSource _largeIcon;

        public ImageSource LargeIcon
        {
            get
            {
                if (_largeIcon == null && !_largeIconLoading)
                {
                    if (AllowAsync)
                    {
                        _largeIconLoading = true;

                        Task.Factory.StartNew(() =>
                        {
                            LargeIcon = GetDisplayIcon(IconSize.Large);
                            LargeIcon?.Freeze();
                            _largeIconLoading = false;
                        }, CancellationToken.None, TaskCreationOptions.None, IconHelper.IconScheduler);
                    }
                    else
                    {
                        _largeIcon = GetDisplayIcon(IconSize.Large);
                        _largeIcon.Freeze();
                    }
                }

                return _largeIcon;
            }
            private set
            {
                _largeIcon = value;
                OnPropertyChanged();
            }
        }

        private ImageSource _extraLargeIcon;

        public ImageSource ExtraLargeIcon
        {
            get
            {
                if (_extraLargeIcon == null && !_extraLargeIconLoading)
                {
                    if (AllowAsync)
                    {
                        _extraLargeIconLoading = true;

                        Task.Factory.StartNew(() =>
                        {
                            ExtraLargeIcon = GetDisplayIcon(IconSize.ExtraLarge);
                            ExtraLargeIcon?.Freeze();
                            _extraLargeIconLoading = false;
                        }, CancellationToken.None, TaskCreationOptions.None, IconHelper.IconScheduler);
                    }
                    else
                    {
                        _extraLargeIcon = GetDisplayIcon(IconSize.ExtraLarge);
                        _extraLargeIcon?.Freeze();
                    }
                }

                return _extraLargeIcon;
            }
            private set
            {
                _extraLargeIcon = value;
                OnPropertyChanged();
            }
        }

        private ImageSource _jumboIcon;

        public ImageSource JumboIcon
        {
            get
            {
                if (_jumboIcon == null && !_jumboIconLoading)
                {
                    if (AllowAsync)
                    {
                        _jumboIconLoading = true;

                        Task.Factory.StartNew(() =>
                        {
                            JumboIcon = GetDisplayIcon(IconSize.Jumbo);
                            JumboIcon?.Freeze();
                            _jumboIconLoading = false;
                        }, CancellationToken.None, TaskCreationOptions.None, IconHelper.IconScheduler);
                    }
                    else
                    {
                        _jumboIcon = GetDisplayIcon(IconSize.Jumbo);
                        _jumboIcon?.Freeze();
                    }
                }

                return _jumboIcon;
            }
            private set
            {
                _jumboIcon = value;
                OnPropertyChanged();
            }
        }
        #endregion

        public ShellItem(IShellItem shellItem)
        {
            _shellItem = shellItem;
        }

        public ShellItem(string parsingName)
        {
            _shellItem = GetShellItem(parsingName);
        }

        public ShellItem(IntPtr parentPidl, IShellFolder parentShellFolder, IntPtr relativePidl, bool isAsync = false)
        {
            _relativePidl = relativePidl;
            
            _shellItem = GetShellItem(parentPidl, parentShellFolder, _relativePidl);
        }

        public void Refresh(bool newPath = false)
        {
            _displayName = null;
            if (newPath)
            {
                _fileName = null;
                _path = null;
            }
            
            _attributes = 0;
            _isFileSystem = null;
            _isFolder = null;
            
            _smallIcon = null;
            _largeIcon = null;
            _extraLargeIcon = null;
            _jumboIcon = null;

            OnPropertyChanged("DisplayName");
            if (newPath)
            {
                OnPropertyChanged("FileName");
                OnPropertyChanged("Path");
            }

            OnPropertyChanged("Attributes");
            OnPropertyChanged("IsFileSystem");
            OnPropertyChanged("IsFolder");
            
            OnPropertyChanged("SmallIcon");
            OnPropertyChanged("LargeIcon");
            OnPropertyChanged("ExtraLargeIcon");
            OnPropertyChanged("JumboIcon");
        }

        public override string ToString()
        {
            return $"{DisplayName} ({Path})";
        }

        #region Retrieve interfaces
        private IShellItem GetParentShellItem()
        {
            IShellItem parent = null;

            try
            {
                if (_shellItem?.GetParent(out parent) != NativeMethods.S_OK)
                {
                    parent = null;
                }
            }
            catch (Exception e)
            {
                ShellLogger.Error($"ShellItem: Unable to get parent shell item: {e.Message}");

                // Fall back to the root shell item via empty string
                try
                {
                    parent = GetShellItem(string.Empty);
                }
                catch (Exception exception)
                {
                    ShellLogger.Error($"ShellItem: Unable to get fallback parent shell item: {exception.Message}");
                }
            }

            return parent;
        }

        protected IShellItem GetShellItem(string parsingName)
        {
            try
            {
                Interop.SHCreateItemFromParsingName(parsingName, IntPtr.Zero, typeof(IShellItem).GUID, out IShellItem ppv);
                return ppv;
            }
            catch (Exception e)
            {
                ShellLogger.Error($"ShellItem: Unable to get shell item for {parsingName}: {e.Message}");
                return null;
            }
        }

        private IShellItem GetShellItem(IntPtr parentPidl, IShellFolder parentShellFolder, IntPtr relativePidl)
        {
            try
            {
                Interop.SHCreateItemWithParent(parentPidl, parentShellFolder, relativePidl, typeof(IShellItem).GUID, out IShellItem ppv);
                return ppv;
            }
            catch (Exception e)
            {
                ShellLogger.Error($"ShellItem: Unable to get shell item for {relativePidl}: {e.Message}");
                return null;
            }
        }

        private IShellItemImageFactory GetImageFactory(IntPtr absolutePidl)
        {
            if (_shellItem == null)
            {
                return null;
            }
            
            try
            {
                Interop.SHCreateItemFromIDList(absolutePidl, typeof(IShellItemImageFactory).GUID, out IShellItemImageFactory ppv);
                return ppv;
            }
            catch (Exception e)
            {
                ShellLogger.Error($"ShellItem: Unable to get shell item image factory for {absolutePidl}: {e.Message}");
                return null;
            }
        }

        private void GetParentAndItem()
        {
            IParentAndItem pni = _shellItem as IParentAndItem;

            if (pni == null)
            {
                return;
            }

            if (pni.GetParentAndItem(out IntPtr parentAbsolutePidl, out IShellFolder parentFolder, out _relativePidl) != NativeMethods.S_OK)
            {
                ShellLogger.Error($"ShellItem: Unable to get shell item parent for {Path}");
            }

            if (parentAbsolutePidl != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(parentAbsolutePidl);
            }

            if (parentFolder != null)
            {
                // Other ShellItems may reference this IShellFolder so don't set refcount to 0
                Marshal.ReleaseComObject(parentFolder);
            }
        }
        #endregion

        #region Retrieve properties
        private IntPtr GetAbsolutePidl()
        {
            IntPtr pidl = IntPtr.Zero;

            try
            {
                if (_shellItem != null)
                {
                    Interop.SHGetIDListFromObject(_shellItem, out pidl);
                }
            }
            catch (Exception e)
            {
                ShellLogger.Error($"ShellItem: Unable to get absolute pidl: {e.Message}");
            }

            return pidl;
        }

        private SFGAO GetAttributes()
        {
            SFGAO attrs = 0;

            if (_shellItem?.GetAttributes(SFGAO.FILESYSTEM | SFGAO.FOLDER | SFGAO.HIDDEN | SFGAO.STREAM, out attrs) !=
                NativeMethods.S_OK)
            {
                attrs = 0;
            }

            return attrs;
        }

        private string GetDisplayName(SIGDN purpose)
        {
            IntPtr hString = IntPtr.Zero;
            string name = string.Empty;

            try
            {
                if (_shellItem?.GetDisplayName(purpose, out hString) == NativeMethods.S_OK)
                {
                    if (hString != IntPtr.Zero)
                    {
                        name = Marshal.PtrToStringAuto(hString);
                    }
                }
            }
            catch (Exception e)
            {
                ShellLogger.Error($"ShellItem: Unable to get {purpose} display name: {e.Message}");
            }
            finally
            {
                Marshal.FreeCoTaskMem(hString);
            }

            return name;
        }

        private ImageSource GetDisplayIcon(IconSize size)
        {
            ImageSource icon = null;
            IShellItemImageFactory imageFactory = GetImageFactory(AbsolutePidl);

            if (imageFactory == null)
            {
                icon = IconImageConverter.GetDefaultIcon();
            }
            else
            {
                try
                {
                    int iconPoints = IconHelper.GetSize(size);
                    SIZE imageSize = new SIZE { cx = Convert.ToInt32(iconPoints * DpiHelper.DpiScale), cy = Convert.ToInt32(iconPoints * DpiHelper.DpiScale) };

                    IntPtr hBitmap = IntPtr.Zero;
                    SIIGBF flags = 0;

                    if (size == IconSize.Small)
                    {
                        // for 16pt icons, thumbnails are too small
                        flags = SIIGBF.ICONONLY;
                    }

                    if (imageFactory?.GetImage(imageSize, flags, out hBitmap) == NativeMethods.S_OK)
                    {
                        if (hBitmap != IntPtr.Zero)
                        {
                            icon = IconImageConverter.GetImageFromHBitmap(hBitmap);
                        }
                    }
                }
                catch (Exception e)
                {
                    ShellLogger.Error($"ShellItem: Unable to get icon from ShellItemImageFactory: {e.Message}");
                }
                finally
                {
                    Marshal.FinalReleaseComObject(imageFactory);
                }
            }

            if (icon == null)
            {
                // Fall back to SHGetFileInfo
                icon = IconImageConverter.GetImageFromAssociatedIcon(AbsolutePidl, size);
            }

            if (icon == null)
            {
                icon = IconImageConverter.GetDefaultIcon();
            }
            
            return icon;
        }
        #endregion

        #region Modify item
        public void Rename(string newName)
        {
            string newFilePathName = System.IO.Path.GetDirectoryName(Path) + "\\" + newName;
            
            if (newFilePathName != Path)
            {
                if (IsFolder)
                    Directory.Move(Path, newFilePathName);
                else
                    File.Move(Path, newFilePathName);
            }
        }
        #endregion

        #region IDisposable
        public void Dispose()
        {
            if (_shellItem != null)
            {
                Marshal.FinalReleaseComObject(_shellItem);
                _shellItem = null;
            }

            if (_absolutePidl != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(_absolutePidl);
                _absolutePidl = IntPtr.Zero;
            }

            if (_relativePidl != IntPtr.Zero)
            {
                Marshal.FreeCoTaskMem(_relativePidl);
                _relativePidl = IntPtr.Zero;
            }
        }
        #endregion

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
        #endregion
    }
}
