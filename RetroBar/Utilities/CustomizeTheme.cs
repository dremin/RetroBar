using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;


namespace RetroBar
{
    public partial class PropertiesWindow : Window
    {
        private static readonly ResourceDictionary _resourceDictionary = System.Windows.Application.Current.Resources;

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            ConfigureSettings();
            InitializeComponent();
            SetupEventHandlers();
        }

        private void ConfigureSettings()
        {
// TODO: load settings, and we also need to save the settings first
// TODO: enable\disable here
// TODO: add localization
        }

        private void SetupEventHandlers()
        {
            Loaded += PropertiesWindow_Loaded;
        }

        private void PropertiesWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateResourcesList();
        }

        private void ThemeCustomizationsEnabled_CheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
// TODO: enable theme customizations
        }

        private void ThemeCustomizationsEnabled_CheckBox_OnUnChecked(object sender, RoutedEventArgs e)
        {
// TODO: disable theme customizations
        }

        private void ChangeColorButton_Click(object sender, RoutedEventArgs e)
        {
            if (ResourcesList.SelectedItem is string selectedKey)
            {
                ChangeResourceColor(selectedKey);
                UpdateSelectedResourceDetails(selectedKey);
            }
        }

        private void ResetColorButton_Click(object sender, RoutedEventArgs e)
        {
// TODO: Get original color from theme
        }

        private void ResourcesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ResourcesList.SelectedItem is string selectedKey)
            {
                UpdateSelectedResourceDetails(selectedKey);
            }
        }

        private void PopulateResourcesList()
        {
            List<string> brushKeys = [];

            // Add brush keys from the resource dictionary
            ListAddUnique(brushKeys, "BalloonCloseButtonBackgroundHover");
            ListAddUnique(brushKeys, "BalloonCloseButtonBackgroundPressed");
            ListAddUnique(brushKeys, "BalloonCloseButtonForegroundHover");
            ListAddUnique(brushKeys, "BalloonCloseButtonForegroundPressed");
            ListAddUnique(brushKeys, "BalloonCloseButtonInactiveForeground");
            ListAddUnique(brushKeys, "BalloonCloseButtonInnerBorderHover");
            ListAddUnique(brushKeys, "BalloonCloseButtonInnerBorderPressed");
            ListAddUnique(brushKeys, "BalloonCloseButtonOuterBorder");
            ListAddUnique(brushKeys, "ButtonActiveForeground");
            ListAddUnique(brushKeys, "ButtonFlashingForeground");
            ListAddUnique(brushKeys, "ButtonForeground");
            ListAddUnique(brushKeys, "ButtonPressedForeground");
            ListAddUnique(brushKeys, "ClockForeground");
            ListAddUnique(brushKeys, "InputLanguageBackground");
            ListAddUnique(brushKeys, "InputLanguageForeground");
            ListAddUnique(brushKeys, "ItemButtonForeground");
            ListAddUnique(brushKeys, "TaskbarBackground");
            ListAddUnique(brushKeys, "TaskbarBottomBorder");
            ListAddUnique(brushKeys, "TaskbarBottomInnerBorder");
            ListAddUnique(brushKeys, "TaskbarTopBorder");
            ListAddUnique(brushKeys, "TaskbarTopInnerBorder");
            ListAddUnique(brushKeys, "TaskbarVerticalBackground");
            ListAddUnique(brushKeys, "TaskbarVerticalBottomBorder");
            ListAddUnique(brushKeys, "TaskbarVerticalBottomInnerBorder");
            ListAddUnique(brushKeys, "TaskbarVerticalTopBorder");
            ListAddUnique(brushKeys, "TaskbarVerticalTopInnerBorder");
            ListAddUnique(brushKeys, "TaskbarWindowBackground");
            ListAddUnique(brushKeys, "TaskButtonBackground");
            ListAddUnique(brushKeys, "TaskButtonBackgroundActive");
            ListAddUnique(brushKeys, "TaskButtonBackgroundActiveHover");
            ListAddUnique(brushKeys, "TaskButtonBackgroundFlashing");
            ListAddUnique(brushKeys, "TaskButtonBackgroundHover");
            ListAddUnique(brushKeys, "TaskButtonInnerBorder");
            ListAddUnique(brushKeys, "TaskButtonInnerBorderActive");
            ListAddUnique(brushKeys, "TaskButtonInnerBorderFlashing");
            ListAddUnique(brushKeys, "TaskButtonInnerBorderHover");
            ListAddUnique(brushKeys, "TaskButtonInnerBottomLeftBorder");
            ListAddUnique(brushKeys, "TaskButtonInnerBottomLeftBorderActive");
            ListAddUnique(brushKeys, "TaskButtonInnerBottomLeftBorderFlashing");
            ListAddUnique(brushKeys, "TaskButtonInnerBottomLeftBorderHover");
            ListAddUnique(brushKeys, "TaskButtonInnerTopRightBorder");
            ListAddUnique(brushKeys, "TaskButtonInnerTopRightBorderActive");
            ListAddUnique(brushKeys, "TaskButtonInnerTopRightBorderFlashing");
            ListAddUnique(brushKeys, "TaskButtonInnerTopRightBorderHover");
            ListAddUnique(brushKeys, "TaskButtonOuterBorder");
            ListAddUnique(brushKeys, "TaskButtonOuterBorderActive");
            ListAddUnique(brushKeys, "TaskButtonOuterBorderFlashing");
            ListAddUnique(brushKeys, "TaskButtonOuterBorderHover");
            ListAddUnique(brushKeys, "TaskButtonThumbnailBackground");
            ListAddUnique(brushKeys, "TaskButtonThumbnailBorder");
            ListAddUnique(brushKeys, "TaskButtonThumbnailInnerBorder");
            ListAddUnique(brushKeys, "TaskButtonThumbnailThumbBorder");
            ListAddUnique(brushKeys, "TaskListScrollArrow");
            ListAddUnique(brushKeys, "TaskListScrollArrowHover");
            ListAddUnique(brushKeys, "TaskListScrollButtonBackground");
            ListAddUnique(brushKeys, "TaskListScrollButtonBorder");
            ListAddUnique(brushKeys, "TaskListScrollButtonInnerBorderHover");
            ListAddUnique(brushKeys, "TaskListScrollButtonInnerBorderPressed");
            ListAddUnique(brushKeys, "TaskListScrollButtonOuterBorder");
            ListAddUnique(brushKeys, "ToolTip");
            ListAddUnique(brushKeys, "ToolTipBackground");
            ListAddUnique(brushKeys, "ToolTipBalloonBottomBackground");
            ListAddUnique(brushKeys, "ToolTipBalloonForeground");
            ListAddUnique(brushKeys, "ToolTipBorder");
            ListAddUnique(brushKeys, "ToolTipForeground");
            ListAddUnique(brushKeys, "ToolbarButtonBackgroundHover");
            ListAddUnique(brushKeys, "ToolbarThumbFill");
            ListAddUnique(brushKeys, "TrayToggleArrowForeground");
            ListAddUnique(brushKeys, "TrayToggleArrowPressed");
            ListAddUnique(brushKeys, "TrayToggleBorder");
            ListAddUnique(brushKeys, "TrayToggleHoverBackground");
            ListAddUnique(brushKeys, "TrayToggleOuterBorder");
            ListAddUnique(brushKeys, "TrayTogglePressedBackground");

            // Bind the list of brush keys to the ListBox
            ResourcesList.ItemsSource = brushKeys;
        }

        private static void ListAddUnique(List<string> list, string item)
        {
            if (!list.Contains(item))
            {
                list.Add(item);
            }
        }

        private static bool IsSupportedColorBrush(string resourceName)
        {
            if (_resourceDictionary.Contains(resourceName))
            {
                object brush = _resourceDictionary[resourceName];
                return brush is SolidColorBrush || brush is LinearGradientBrush;
            }

            return false;
        }

        private static Brush GetCurrentResourceColor(string resourceName)
        {
            return _resourceDictionary.Contains(resourceName) ? _resourceDictionary[resourceName] as Brush : null;
        }

        private static void ChangeResourceColor(string resourceName)
        {
            if (_resourceDictionary.Contains(resourceName) && IsSupportedColorBrush(resourceName))
            {
                using ColorDialog colorDialog = new()
                {
                    AllowFullOpen = true
                };

                if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    System.Drawing.Color newColor = colorDialog.Color;
                    Color convertedColor = Color.FromArgb(newColor.A, newColor.R, newColor.G, newColor.B);

                    if (_resourceDictionary[resourceName] is SolidColorBrush)
                    {
                        _resourceDictionary[resourceName] = new SolidColorBrush(convertedColor);
                    }
                    else if (_resourceDictionary[resourceName] is LinearGradientBrush gradientBrush)
                    {
                        LinearGradientBrush newBrush = new();
                        foreach (GradientStop stop in gradientBrush.GradientStops)
                        {
                            newBrush.GradientStops.Add(new GradientStop(convertedColor, stop.Offset));
                        }
                        _resourceDictionary[resourceName] = newBrush;
                    }
                }
            }
        }

        private void UpdateSelectedResourceDetails(string resourceName)
        {
            if (_resourceDictionary.Contains(resourceName) && IsSupportedColorBrush(resourceName))
            {
                Brush brush = GetCurrentResourceColor(resourceName);
                if (brush is SolidColorBrush solidColorBrush)
                {
                    SelectedColorBox.Fill = solidColorBrush;
                    HexColorText.Text = $"#{solidColorBrush.Color.R:X2}{solidColorBrush.Color.G:X2}{solidColorBrush.Color.B:X2}";
                }
                else if (brush is LinearGradientBrush gradientBrush)
                {
                    // Show the color of the first gradient stop
                    Color? firstStopColor = gradientBrush.GradientStops.FirstOrDefault()?.Color;
                    if (firstStopColor != null)
                    {
                        SelectedColorBox.Fill = new SolidColorBrush(firstStopColor.Value);
                        HexColorText.Text = $"#{firstStopColor.Value.R:X2}{firstStopColor.Value.G:X2}{firstStopColor.Value.B:X2}";
                    }
                }
            }
            else if (_resourceDictionary.Contains(resourceName) && !IsSupportedColorBrush(resourceName))
            {
                System.Diagnostics.Debug.WriteLine("Resource not supported brush: " + resourceName);
            }
        }
    }
}
