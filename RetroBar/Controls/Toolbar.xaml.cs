using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using ManagedShell.ShellFolders;
using ManagedShell.ShellFolders.Enums;
using RetroBar.Utilities;

namespace RetroBar.Controls
{
    /// <summary>
    /// Interaction logic for Toolbar.xaml
    /// </summary>
    public partial class Toolbar : UserControl
    {
        private bool _ignoreNextUpdate;
        private bool _isLoaded;
        private OverflowPanel _overflowPanel;
        private double _pendingQuickLaunchSize;

        // Floor for the draggable Quick Launch size, so the gripper can't be dragged down to
        // nothing (or negative) and effectively strand the toolbar.
        private const double MinQuickLaunchSize = 20;

        // Every loaded Toolbar (one per monitor) registers itself here so the Properties
        // window's "Reset Width" button - which has no direct reference of its own to any
        // Toolbar - can ask one of them to measure real icons for GetEraDefaultQuickLaunchSize.
        // Quick Launch's contents and theme are shared across monitors, so any one entry
        // gives the same answer; this is read-only from outside and never drives layout by
        // itself, so it can't reintroduce the old auto-relayout bugs.
        private static readonly List<Toolbar> _liveToolbars = new List<Toolbar>();

        // Classic per-theme Quick Launch look ("XP shows about 3 icons", "Aero shows about
        // 2") when you press Reset Width, so you don't have to hand-drag the gripper back
        // to that exact spot every time. Falls back to the XP-era count if a theme doesn't
        // define its own.
        private const int DefaultVisibleQuickLaunchItemsFallback = 3;

        private enum MenuItem : uint
        {
            OpenParentFolder = CommonContextMenuItem.Paste + 1,
            ShowOnQuickLaunch
        }

        public static DependencyProperty PathProperty = DependencyProperty.Register(nameof(Path), typeof(string), typeof(Toolbar), new PropertyMetadata(OnPathChanged));

        public string Path
        {
            get => (string)GetValue(PathProperty);
            set
            {
                SetValue(PathProperty, value);
                SetupFolder(value);
            }
        }

        private static DependencyProperty FolderProperty = DependencyProperty.Register(nameof(Folder), typeof(ShellFolder), typeof(Toolbar));

        public static DependencyProperty HostProperty = DependencyProperty.Register(nameof(Host), typeof(Taskbar), typeof(Toolbar), new PropertyMetadata(HostChangedCallback));

        public Taskbar Host
        {
            get { return (Taskbar)GetValue(HostProperty); }
            set { SetValue(HostProperty, value); }
        }

        public ToolbarDropHandler DropHandler { get; set; }

        private ShellFolder Folder
        {
            get => (ShellFolder)GetValue(FolderProperty);
            set
            {
                SetValue(FolderProperty, value);
                SetItemsSource();
            }
        }

        public Toolbar()
        {
            DropHandler = new ToolbarDropHandler(this);

            InitializeComponent();

            // OverflowMenu (a real ContextMenu, see Toolbar.xaml) can't be wired to
            // OverflowToggle by an ElementName binding from inside XAML - its content lives
            // outside the normal name-lookup tree until it's already open. So the two are
            // linked by hand here instead: OverflowToggle checking/unchecking opens and
            // closes the menu, and the menu closing itself (StaysOpen="False", e.g. clicking
            // elsewhere) un-checks the button to match.
            OverflowMenu.PlacementTarget = OverflowToggle;
            OverflowToggle.Checked += (s, e) => OverflowMenu.IsOpen = true;
            OverflowToggle.Unchecked += (s, e) => OverflowMenu.IsOpen = false;
            OverflowMenu.Closed += (s, e) => OverflowToggle.IsChecked = false;
        }

        private void Settings_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(Settings.QuickLaunchOrder))
            {
                if (_ignoreNextUpdate)
                {
                    _ignoreNextUpdate = false;
                    return;
                }

                Refresh();
            }
            else if (e.PropertyName == nameof(Settings.TaskbarScale))
            {
                Refresh();
            }
            else if (e.PropertyName == nameof(Settings.QuickLaunchWidth))
            {
                if (Settings.Instance.QuickLaunchWidth == null)
                {
                    // Cleared back to automatic (the Properties "reset width" button, or
                    // another monitor's toolbar doing the same). Just let it size itself to
                    // however many icons actually fit the real available taskbar space - the
                    // same thing real Windows does with a never-dragged Quick Launch. There
                    // used to be an extra step here that forced it down to a small fixed icon
                    // count (2-3) as a simulated "default", but that didn't match real
                    // Windows (which shows everything that fits, and only starts trimming
                    // once space actually runs out or you drag it narrower yourself) and was
                    // the source of every Width-vs-on-screen-size desync bug this had - by
                    // introducing an extra size to compute and re-apply after the auto-size
                    // pass, instead of just leaving it to auto-size once and be done.
                    ApplyQuickLaunchSize(null);
                }
                else
                {
                    // The size was changed (e.g. by dragging the gripper on another
                    // monitor's toolbar, since this setting is shared); match it here too.
                    ApplyQuickLaunchSize(Settings.Instance.QuickLaunchWidth);
                }
            }
            else if (e.PropertyName == nameof(Settings.LockTaskbar))
            {
                // The chevron's own visibility doesn't depend on lock state (it shows any
                // time there's overflow, see UpdateOverflowButtonVisibility) - only the
                // gripper does, and its Visibility is bound directly to this setting in XAML.
                // When Quick Launch is auto-sized (the normal, never-dragged case), that
                // gripper appearing/disappearing already changes how much room is left over
                // for Quick Launch to auto-fill, in the same DockPanel - so it naturally ends
                // up showing one fewer icon while unlocked, matching real XP, with no extra
                // code needed here. A manually-dragged width stays exactly as dragged either
                // way, which is a small departure from real XP's exact behavior there, but a
                // simple, predictable one.
                UpdateOverflowButtonVisibility();
            }
            else if (e.PropertyName == nameof(Settings.EnableQuickLaunchOverflow))
            {
                // Order matters: recompute the reserved chevron space first, since hiding
                // the button (below) doesn't by itself change how much room OverflowPanel
                // sets aside for it.
                UpdateOverflowButtonSize();
                UpdateOverflowButtonVisibility();
            }
        }

        private void Refresh()
        {
            if (Folder == null)
            {
                return;
            }

            ListCollectionView cvs = (ListCollectionView)CollectionViewSource.GetDefaultView(Folder.Files);
            cvs.Refresh();
        }

        private void SetupFolder(string path)
        {
            Folder?.Dispose();
            Folder = new ShellFolder(Environment.ExpandEnvironmentVariables(path), IntPtr.Zero, true);
        }

        private void UnloadFolder()
        {
            Folder?.Dispose();
            Folder = null;
        }

        private void SetItemsSource()
        {
            if (Folder != null)
            {
                ToolbarItems.ItemsSource = Folder.Files;
                ListCollectionView cvs = (ListCollectionView)CollectionViewSource.GetDefaultView(Folder.Files);
                cvs.CustomSort = new ToolbarSorter(this);
            }
        }

        public void SaveItemOrder()
        {
            List<string> itemPaths = new List<string>();

            foreach (ShellFile file in ((ListCollectionView)CollectionViewSource.GetDefaultView(Folder.Files)).OfType<ShellFile>())
            {
                itemPaths.Add(file.Path);
            }

            // small optimization, only other toolbars with this folder need to reload when the setting is saved.
            _ignoreNextUpdate = true;

            Settings.Instance.QuickLaunchOrder = itemPaths;
        }

        /// <summary>
        /// Moves a Quick Launch icon to sit immediately before/after another icon in the
        /// one shared order that both the visible toolbar and the chevron's overflow
        /// flyout are views over, or all the way to the end of that order when there's no
        /// specific target icon (e.g. dropped on empty flyout space, or directly on the
        /// chevron button before it even opened) - matching real XP, where dragging an
        /// icon past the last one that fits is what sends it into overflow, rather than
        /// pinning it there through any separate persisted flag.
        /// </summary>
        public void MoveQuickLaunchItem(ShellFile draggedFile, ShellFile targetFile, bool insertAfterTarget)
        {
            if (Folder == null || draggedFile == null || ReferenceEquals(draggedFile, targetFile))
            {
                return;
            }

            if (!TryGetQuickLaunchOrderWithout(draggedFile, out List<ShellFile> order))
            {
                return;
            }

            int insertIndex = targetFile != null ? order.IndexOf(targetFile) : -1;

            if (insertIndex < 0)
            {
                insertIndex = order.Count;
            }
            else if (insertAfterTarget)
            {
                insertIndex++;
            }

            order.Insert(insertIndex, draggedFile);

            ApplyQuickLaunchOrder(order);
        }

        /// <summary>
        /// Moves a Quick Launch icon to the very front of the shared order, so it's as
        /// visible as it can be. This is the click-based way back onto the toolbar from
        /// the overflow flyout's own right-click menu ("Show on Quick Launch") - dragging
        /// an icon out of that open flyout isn't reliable, since native Windows menus
        /// have their own built-in mouse handling that fights this drag library on that
        /// side (dragging an icon onto the chevron to send it into overflow, the other
        /// direction, doesn't have this problem - see ToolbarDropHandler).
        /// </summary>
        public void MoveQuickLaunchItemToFront(ShellFile file)
        {
            if (Folder == null || file == null)
            {
                return;
            }

            if (!TryGetQuickLaunchOrderWithout(file, out List<ShellFile> order))
            {
                return;
            }

            order.Insert(0, file);

            ApplyQuickLaunchOrder(order);
        }

        private bool TryGetQuickLaunchOrderWithout(ShellFile file, out List<ShellFile> order)
        {
            order = ((ListCollectionView)CollectionViewSource.GetDefaultView(Folder.Files))
                .OfType<ShellFile>()
                .ToList();

            return order.Remove(file);
        }

        private void ApplyQuickLaunchOrder(List<ShellFile> order)
        {
            // small optimization, only other toolbars with this folder need to reload when the setting is saved.
            _ignoreNextUpdate = true;

            Settings.Instance.QuickLaunchOrder = order.Select(file => file.Path).ToList();

            Refresh();
        }

        public void AddToSource(StringCollection filesToAdd)
        {
            string sourcePath = Environment.ExpandEnvironmentVariables(Path);

            foreach (string itemPath in filesToAdd)
            {
                // Create shortcut to each dragged file
                try
                {
                    string destinationFileName = System.IO.Path.GetFileNameWithoutExtension(itemPath);
                    string destinationPath = System.IO.Path.Combine(sourcePath, destinationFileName + ".lnk");
                    int dupCount = 0;

                    while (ShellHelper.Exists(destinationPath))
                    {
                        dupCount++;

                        destinationPath = System.IO.Path.Combine(sourcePath, $"{destinationFileName} ({dupCount}).lnk");
                    }

                    ShellLinkHelper.CreateAndSave(itemPath, destinationPath);
                }
                catch (Exception e)
                {
                    ShellLogger.Error($"Toolbar: Unable to save shortcut to {itemPath}", e);
                }
            }
        }

        #region Events
        private static void OnPathChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Toolbar toolbar)
            {
                toolbar.SetupFolder((string)e.NewValue);
            }
        }

        private void ToolbarIcon_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ToolbarButton icon = sender as ToolbarButton;
            if (icon == null)
            {
                return;
            }

            Mouse.Capture(null);
            HandleIconLeftClick(icon.DataContext as ShellFile, false, e);
        }

        private void ToolbarIcon_OnPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ToolbarButton icon = sender as ToolbarButton;
            if (icon == null)
            {
                return;
            }

            HandleIconRightClick(icon.DataContext as ShellFile, e);
        }

        // Overflow rows are real MenuItems now (see Toolbar.xaml), not ToolbarButtons, so they
        // need their own pair of handlers - but the actual launch/right-click logic underneath
        // is identical either way, hence the shared HandleIconLeftClick/RightClick below.
        private void OverflowMenuItem_OnPreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Controls.MenuItem item = sender as System.Windows.Controls.MenuItem;
            if (item == null)
            {
                return;
            }

            Mouse.Capture(null);
            HandleIconLeftClick(item.DataContext as ShellFile, true, e);
        }

        private void OverflowMenuItem_OnPreviewMouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            System.Windows.Controls.MenuItem item = sender as System.Windows.Controls.MenuItem;
            if (item == null)
            {
                return;
            }

            HandleIconRightClick(item.DataContext as ShellFile, e);
        }

        private void HandleIconLeftClick(ShellFile file, bool isOverflowItem, MouseButtonEventArgs e)
        {
            if (file == null || string.IsNullOrWhiteSpace(file.Path))
            {
                return;
            }

            if (InvokeContextMenu(file, false))
            {
                e.Handled = true;

                if (isOverflowItem)
                {
                    // Launching an item from the overflow flyout should close it, same as
                    // a classic Quick Launch overflow menu closing after you pick something.
                    OverflowToggle.IsChecked = false;
                }
            }
        }

        private void HandleIconRightClick(ShellFile file, MouseButtonEventArgs e)
        {
            if (InvokeContextMenu(file, true))
            {
                e.Handled = true;
            }
        }

        private void UserControl_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (e.NewValue is bool visible)
            {
                if (visible)
                {
                    if (Folder != null)
                    {
                        return;
                    }

                    SetupFolder(Path);
                }
                else
                {
                    UnloadFolder();
                }
            }
        }

        private void Toolbar_TaskbarHotkeyPressed(object sender, HotkeyManager.TaskbarHotkeyEventArgs e)
        {
            if (Settings.Instance.WinNumHotkeysAction == WinNumHotkeysOption.InvokeQuickLaunch && Host.Screen.Primary)
            {
                try
                {
                    ListCollectionView items = (ListCollectionView)CollectionViewSource.GetDefaultView(Folder.Files);

                    bool exists = items.MoveCurrentToPosition(e.index);

                    if (exists) InvokeContextMenu((ShellFile)items.CurrentItem, false);

                }
                catch (ArgumentOutOfRangeException) { }
            }
        }

        private void OverflowPanel_OnLoaded(object sender, RoutedEventArgs e)
        {
            if (_overflowPanel != null)
            {
                _overflowPanel.OverflowChanged -= OverflowPanel_OnOverflowChanged;
            }

            _overflowPanel = sender as OverflowPanel;

            if (_overflowPanel == null)
            {
                return;
            }

            _overflowPanel.OverflowChanged += OverflowPanel_OnOverflowChanged;
            OverflowMenu.ItemsSource = _overflowPanel.OverflowItems;
            UpdateOverflowButtonSize();
            UpdateOverflowButtonVisibility();
        }

        private void OverflowPanel_OnOverflowChanged(object sender, EventArgs e)
        {
            UpdateOverflowButtonVisibility();
        }

        private void OverflowToggle_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateOverflowButtonSize();
        }

        private void UpdateOverflowButtonSize()
        {
            if (_overflowPanel == null)
            {
                return;
            }

            if (!Settings.Instance.EnableQuickLaunchOverflow)
            {
                // The chevron never shows in this mode (see UpdateOverflowButtonVisibility),
                // so don't reserve any room for it either - otherwise icons that don't fit
                // would leave a blank gap exactly where an invisible chevron would have gone,
                // instead of just being plainly clipped like RetroBar always used to do.
                _overflowPanel.OverflowButtonSize = 0;
                return;
            }

            // Read the explicit Width/Height each theme's ToolbarOverflowButton style sets,
            // rather than ActualWidth/ActualHeight: the button stays Collapsed (and so at
            // 0x0 actual size) right up until we know overflow exists, but we need to know
            // how much room to reserve for it before that decision can be made.
            bool vertical = _overflowPanel.Orientation == Orientation.Vertical;
            double size = vertical ? OverflowToggle.Height : OverflowToggle.Width;

            if (!double.IsNaN(size) && size > 0)
            {
                _overflowPanel.OverflowButtonSize = size;
            }
        }

        private void UpdateOverflowButtonVisibility()
        {
            // Real XP shows the chevron any time there's overflow, locked or not - it's
            // independent of the gripper (confirmed by a real XP screenshot showing both
            // the gripper and the chevron at once while unlocked). OverflowPanel reserves
            // room for the chevron whenever anything overflows regardless of whether this
            // button is shown, so hiding it here while unlocked used to leave that reserved
            // space blank instead of drawing the button that's actually holding it open.
            //
            // The EnableQuickLaunchOverflow setting is a separate, coarser override: when
            // it's off, the chevron/popup never appear at all (icons that don't fit are just
            // clipped, like RetroBar always did before this feature existed), even though the
            // gripper still works and Quick Launch is still resizable.
            bool showButton = Settings.Instance.EnableQuickLaunchOverflow && _overflowPanel != null && _overflowPanel.HasOverflow;

            OverflowToggle.Visibility = showButton ? Visibility.Visible : Visibility.Collapsed;

            if (!showButton && OverflowToggle.IsChecked == true)
            {
                OverflowToggle.IsChecked = false;
            }
        }

        // Applies a Quick Launch size (in DIPs, along whichever axis matches the taskbar's
        // current orientation) to the icon area, or clears it back to auto-size when null.
        // This only affects the live visual layout; callers are responsible for persisting
        // the value to Settings if it should stick around.
        private void ApplyQuickLaunchSize(double? size)
        {
            bool vertical = Host != null && Host.Orientation == Orientation.Vertical;

            if (size == null)
            {
                ToolbarItems.Width = double.NaN;
                ToolbarItems.Height = double.NaN;
                return;
            }

            if (vertical)
            {
                ToolbarItems.Height = size.Value;
                ToolbarItems.Width = double.NaN;
            }
            else
            {
                ToolbarItems.Width = size.Value;
                ToolbarItems.Height = double.NaN;
            }
        }

        private double GetCurrentQuickLaunchSize()
        {
            bool vertical = Host != null && Host.Orientation == Orientation.Vertical;
            double actual = vertical ? ToolbarItems.ActualHeight : ToolbarItems.ActualWidth;

            return actual > 0 ? actual : MinQuickLaunchSize;
        }

        // Quick Launch's own gripper (in Toolbar.xaml) is purely decorative and never
        // drags - real Windows resizes Quick Launch from the task list's gripper, which
        // sits on Quick Launch's other side. TaskList calls these instead of handling
        // its Thumb's drag events locally, since Quick Launch's actual size lives here.
        public void BeginQuickLaunchResize()
        {
            _pendingQuickLaunchSize = GetCurrentQuickLaunchSize();
        }

        public void UpdateQuickLaunchResize(double delta)
        {
            _pendingQuickLaunchSize = Math.Max(MinQuickLaunchSize, _pendingQuickLaunchSize + delta);
            ApplyQuickLaunchSize(ConstrainQuickLaunchSize(_pendingQuickLaunchSize));
        }

        public void CommitQuickLaunchResize()
        {
            // Commit the dragged-to size just once, rather than writing (and re-serializing
            // settings to disk) on every DragDelta tick. Same clamp as the live preview
            // during the drag, so nothing changes when the drag ends.
            Settings.Instance.QuickLaunchWidth = ConstrainQuickLaunchSize(_pendingQuickLaunchSize);
        }

        // Dragging the gripper is plain 1:1 free-form, same as real Windows - the applied
        // width always matches how far you've actually dragged, with no snapping to
        // icon-count steps and no early/magnetic reveals. Either of those would make the
        // displayed width jump away from wherever the mouse actually is, which feels like
        // a stutter mid-drag. This also no longer stops early once every icon is already
        // visible - real rebar bands let you keep dragging a band wider than its own
        // content (leaving dead space inside it), squashing whatever band comes after it
        // instead of refusing to grow. Here, that's the task list, which already has its
        // own reactive shrink-to-fit logic (TaskList.SetTaskButtonWidth, wired to its
        // SizeChanged event) that kicks in automatically as it's given less room, all the
        // way down to a minimum button width and then a scrollbar - so nothing else needs
        // to change for the squash to work, just the floor stays.
        private double ConstrainQuickLaunchSize(double rawSize)
        {
            return Math.Max(MinQuickLaunchSize, rawSize);
        }

        private static int GetDefaultVisibleQuickLaunchItems()
        {
            return Application.Current.TryFindResource("QuickLaunchDefaultVisibleItems") as int? ?? DefaultVisibleQuickLaunchItemsFallback;
        }

        // Measures how wide this theme's classic default icon count actually renders right
        // now (real icon sizes, current DPI/scale, all included), rather than assuming a
        // fixed pixel number that would drift as soon as any of those change. Returns null
        // if this Toolbar doesn't have anything loaded/measured to go on, so the caller can
        // fall back to another monitor's Toolbar, or to plain auto-size as a last resort.
        //
        // This is only ever called once, in direct response to a button press (see
        // PropertiesWindow.ResetQuickLaunchWidth_OnClick) - never wired into Loaded,
        // OverflowChanged, or lock-state events the way an earlier version of this was.
        // That repeated re-triggering (fighting the panel's own live auto-sizing on every
        // layout pass) was what caused the Width/ActualWidth desync bugs this taskbar used
        // to have, not the idea of a default icon count itself.
        private double? GetEraDefaultQuickLaunchSize()
        {
            if (_overflowPanel == null || _overflowPanel.Children.Count == 0)
            {
                return null;
            }

            bool vertical = Host != null && Host.Orientation == Orientation.Vertical;
            int count = Math.Min(GetDefaultVisibleQuickLaunchItems(), _overflowPanel.Children.Count);
            double total = 0;

            for (int i = 0; i < count; i++)
            {
                Size desired = _overflowPanel.Children[i].DesiredSize;
                total += vertical ? desired.Height : desired.Width;
            }

            if (_overflowPanel.Children.Count > count)
            {
                // There are more icons than the default count shows, so the chevron is
                // about to reserve space on this same row - without padding for it here,
                // that reservation would squeeze out the very last icon this was
                // supposed to fit, showing one fewer than intended.
                total += _overflowPanel.OverflowButtonSize;
            }

            return Math.Max(MinQuickLaunchSize, total);
        }

        // Entry point for the Properties window, which has no reference of its own to any
        // Toolbar (Quick Launch's width setting is shared across every monitor's taskbar, so
        // it only needs one of them to answer). Returns null if no loaded Toolbar could
        // measure anything yet, in which case the caller should fall back to plain auto-size.
        public static double? GetAnyEraDefaultQuickLaunchSize()
        {
            foreach (Toolbar toolbar in _liveToolbars)
            {
                double? size = toolbar.GetEraDefaultQuickLaunchSize();

                if (size != null)
                {
                    return size;
                }
            }

            return null;
        }
        #endregion

        #region Context menu
        private ShellMenuCommandBuilder GetFileCommandBuilder(ShellFile file)
        {
            if (file == null)
            {
                return new ShellMenuCommandBuilder();
            }

            ShellMenuCommandBuilder builder = new ShellMenuCommandBuilder();

            // TryFindResource (not FindResource) on purpose - a missing string resource here used to
            // throw ResourceReferenceKeyNotFoundException and crash RetroBar entirely on every single
            // Quick Launch click, since this runs on the click path before the item even launches. A
            // blank/fallback label is a far smaller problem than that, so every lookup here is guarded.
            builder.AddSeparator();

            if (_overflowPanel != null && _overflowPanel.OverflowItems.Contains(file))
            {
                // Only offered for icons currently sitting in the overflow flyout - an
                // exact, click-based way back onto the visible bar, since dragging one
                // out of the open flyout isn't reliable (see MoveQuickLaunchItemToFront).
                builder.AddCommand(new ShellMenuCommand
                {
                    Flags = MFT.BYCOMMAND,
                    Label = TryFindResource("show_on_quick_launch") as string ?? "Show on Quick Launch",
                    UID = (uint)MenuItem.ShowOnQuickLaunch
                });
            }

            builder.AddCommand(new ShellMenuCommand
            {
                Flags = MFT.BYCOMMAND,
                Label = TryFindResource("open_folder") as string ?? "Open Folder",
                UID = (uint)MenuItem.OpenParentFolder
            });

            return builder;
        }

        private bool InvokeContextMenu(ShellFile file, bool isInteractive)
        {
            if (file == null)
            {
                return false;
            }

            var _ = new ShellItemContextMenu(new ShellItem[] { file }, Folder, IntPtr.Zero, HandleFileAction, isInteractive, false, new ShellMenuCommandBuilder(), GetFileCommandBuilder(file));
            return true;
        }

        private bool HandleFileAction(string action, ShellItem[] items, bool allFolders)
        {
            if (action == ((uint)MenuItem.OpenParentFolder).ToString())
            {
                ShellHelper.StartProcess(Folder.Path);
                return true;
            }

            if (action == ((uint)MenuItem.ShowOnQuickLaunch).ToString())
            {
                if (items?.Length > 0 && items[0] is ShellFile file)
                {
                    MoveQuickLaunchItemToFront(file);
                }

                return true;
            }

            return false;
        }
        #endregion

        private void Initialize()
        {
            if (!_isLoaded && Host != null)
            {
                Settings.Instance.PropertyChanged += Settings_PropertyChanged;
                Host.hotkeyManager.TaskbarHotkeyPressed += Toolbar_TaskbarHotkeyPressed;

                ApplyQuickLaunchSize(Settings.Instance.QuickLaunchWidth);

                _isLoaded = true;
            }
        }

        private void UserControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (!_liveToolbars.Contains(this))
            {
                _liveToolbars.Add(this);
            }

            Initialize();
        }

        private void UserControl_Unloaded(object sender, RoutedEventArgs e)
        {
            _liveToolbars.Remove(this);

            Settings.Instance.PropertyChanged -= Settings_PropertyChanged;
            if (Host != null)
            {
                Host.hotkeyManager.TaskbarHotkeyPressed -= Toolbar_TaskbarHotkeyPressed;
            }

            _isLoaded = false;
        }

        private static void HostChangedCallback(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            if (sender is Toolbar toolbar && e.OldValue == null && e.NewValue != null)
            {
                toolbar.Initialize();
            }
        }
    }
}
