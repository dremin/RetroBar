using RetroBar.Utilities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms;
using System.Windows.Media;
using System.Xml.Linq;


// TODO: Add localization
// TODO: Add better support for linear gradient colors
// TODO: Clean and optimize code


namespace RetroBar
{
    public partial class PropertiesWindow : Window
    {
        private static readonly ResourceDictionary _resourceDictionary = System.Windows.Application.Current.Resources;
        private bool _hasInitialized = false;

        private static readonly string CustomizationsFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "RetroBar", "ThemeCustomizations.json"
        );

        protected override void OnInitialized(EventArgs e)
        {
            base.OnInitialized(e);

            InitializeComponent();
            SetupEventHandlers();
        }

        private void SetupEventHandlers()
        {
            Loaded += PropertiesWindow_Loaded;
        }

        private void PropertiesWindow_Loaded(object sender, RoutedEventArgs e)
        {
            PopulateResourcesList();
            EnableCustomizationControls(Settings.Instance.CustomizeThemeEnabled);

            if (Settings.Instance.CustomizeThemeEnabled)
            {
                if (ResourcesList.SelectedItem is string selectedKey)
                {
                    UpdateSelectedResourceDetails(selectedKey);
                }
            }

            _hasInitialized = true;
        }

        private void ThemeCustomizationsEnabled_CheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
            if(!_hasInitialized){return;}

            EnableCustomizationControls(true);
            _settingsCustomizeThemeEnabled = true;

            if (ResourcesList.SelectedItem is string selectedKey)
            {
                UpdateSelectedResourceDetails(selectedKey);
            }

            LoadThemeCustomizations();
        }

        private void ThemeCustomizationsEnabled_CheckBox_OnUnChecked(object sender, RoutedEventArgs e)
        {
            if(!_hasInitialized){return;}

            EnableCustomizationControls(false);
            _settingsCustomizeThemeEnabled = false;

            RestoreResourceColor("BalloonCloseButtonBackgroundHover", ReadOriginalResourceColor("BalloonCloseButtonBackgroundHover"));
            RestoreResourceColor("BalloonCloseButtonBackgroundPressed", ReadOriginalResourceColor("BalloonCloseButtonBackgroundPressed"));
            RestoreResourceColor("BalloonCloseButtonForegroundHover", ReadOriginalResourceColor("BalloonCloseButtonForegroundHover"));
            RestoreResourceColor("BalloonCloseButtonForegroundPressed", ReadOriginalResourceColor("BalloonCloseButtonForegroundPressed"));
            RestoreResourceColor("BalloonCloseButtonInactiveForeground", ReadOriginalResourceColor("BalloonCloseButtonInactiveForeground"));
            RestoreResourceColor("BalloonCloseButtonInnerBorderHover", ReadOriginalResourceColor("BalloonCloseButtonInnerBorderHover"));
            RestoreResourceColor("BalloonCloseButtonInnerBorderPressed", ReadOriginalResourceColor("BalloonCloseButtonInnerBorderPressed"));
            RestoreResourceColor("BalloonCloseButtonOuterBorder", ReadOriginalResourceColor("BalloonCloseButtonOuterBorder"));
            RestoreResourceColor("ButtonActiveForeground", ReadOriginalResourceColor("ButtonActiveForeground"));
            RestoreResourceColor("ButtonFlashingForeground", ReadOriginalResourceColor("ButtonFlashingForeground"));
            RestoreResourceColor("ButtonForeground", ReadOriginalResourceColor("ButtonForeground"));
            RestoreResourceColor("ButtonPressedForeground", ReadOriginalResourceColor("ButtonPressedForeground"));
            RestoreResourceColor("ClockForeground", ReadOriginalResourceColor("ClockForeground"));
            RestoreResourceColor("InputLanguageBackground", ReadOriginalResourceColor("InputLanguageBackground"));
            RestoreResourceColor("InputLanguageForeground", ReadOriginalResourceColor("InputLanguageForeground"));
            RestoreResourceColor("ItemButtonForeground", ReadOriginalResourceColor("ItemButtonForeground"));
            RestoreResourceColor("TaskbarBackground", ReadOriginalResourceColor("TaskbarBackground"));
            RestoreResourceColor("TaskbarBottomBorder", ReadOriginalResourceColor("TaskbarBottomBorder"));
            RestoreResourceColor("TaskbarBottomInnerBorder", ReadOriginalResourceColor("TaskbarBottomInnerBorder"));
            RestoreResourceColor("TaskbarTopBorder", ReadOriginalResourceColor("TaskbarTopBorder"));
            RestoreResourceColor("TaskbarTopInnerBorder", ReadOriginalResourceColor("TaskbarTopInnerBorder"));
            RestoreResourceColor("TaskbarVerticalBackground", ReadOriginalResourceColor("TaskbarVerticalBackground"));
            RestoreResourceColor("TaskbarVerticalBottomBorder", ReadOriginalResourceColor("TaskbarVerticalBottomBorder"));
            RestoreResourceColor("TaskbarVerticalBottomInnerBorder", ReadOriginalResourceColor("TaskbarVerticalBottomInnerBorder"));
            RestoreResourceColor("TaskbarVerticalTopBorder", ReadOriginalResourceColor("TaskbarVerticalTopBorder"));
            RestoreResourceColor("TaskbarVerticalTopInnerBorder", ReadOriginalResourceColor("TaskbarVerticalTopInnerBorder"));
            RestoreResourceColor("TaskbarWindowBackground", ReadOriginalResourceColor("TaskbarWindowBackground"));
            RestoreResourceColor("TaskButtonBackground", ReadOriginalResourceColor("TaskButtonBackground"));
            RestoreResourceColor("TaskButtonBackgroundActive", ReadOriginalResourceColor("TaskButtonBackgroundActive"));
            RestoreResourceColor("TaskButtonBackgroundActiveHover", ReadOriginalResourceColor("TaskButtonBackgroundActiveHover"));
            RestoreResourceColor("TaskButtonBackgroundFlashing", ReadOriginalResourceColor("TaskButtonBackgroundFlashing"));
            RestoreResourceColor("TaskButtonBackgroundHover", ReadOriginalResourceColor("TaskButtonBackgroundHover"));
            RestoreResourceColor("TaskButtonInnerBorder", ReadOriginalResourceColor("TaskButtonInnerBorder"));
            RestoreResourceColor("TaskButtonInnerBorderActive", ReadOriginalResourceColor("TaskButtonInnerBorderActive"));
            RestoreResourceColor("TaskButtonInnerBorderFlashing", ReadOriginalResourceColor("TaskButtonInnerBorderFlashing"));
            RestoreResourceColor("TaskButtonInnerBorderHover", ReadOriginalResourceColor("TaskButtonInnerBorderHover"));
            RestoreResourceColor("TaskButtonInnerBottomLeftBorder", ReadOriginalResourceColor("TaskButtonInnerBottomLeftBorder"));
            RestoreResourceColor("TaskButtonInnerBottomLeftBorderActive", ReadOriginalResourceColor("TaskButtonInnerBottomLeftBorderActive"));
            RestoreResourceColor("TaskButtonInnerBottomLeftBorderFlashing", ReadOriginalResourceColor("TaskButtonInnerBottomLeftBorderFlashing"));
            RestoreResourceColor("TaskButtonInnerBottomLeftBorderHover", ReadOriginalResourceColor("TaskButtonInnerBottomLeftBorderHover"));
            RestoreResourceColor("TaskButtonInnerTopRightBorder", ReadOriginalResourceColor("TaskButtonInnerTopRightBorder"));
            RestoreResourceColor("TaskButtonInnerTopRightBorderActive", ReadOriginalResourceColor("TaskButtonInnerTopRightBorderActive"));
            RestoreResourceColor("TaskButtonInnerTopRightBorderFlashing", ReadOriginalResourceColor("TaskButtonInnerTopRightBorderFlashing"));
            RestoreResourceColor("TaskButtonInnerTopRightBorderHover", ReadOriginalResourceColor("TaskButtonInnerTopRightBorderHover"));
            RestoreResourceColor("TaskButtonOuterBorder", ReadOriginalResourceColor("TaskButtonOuterBorder"));
            RestoreResourceColor("TaskButtonOuterBorderActive", ReadOriginalResourceColor("TaskButtonOuterBorderActive"));
            RestoreResourceColor("TaskButtonOuterBorderFlashing", ReadOriginalResourceColor("TaskButtonOuterBorderFlashing"));
            RestoreResourceColor("TaskButtonOuterBorderHover", ReadOriginalResourceColor("TaskButtonOuterBorderHover"));
            RestoreResourceColor("TaskButtonThumbnailBackground", ReadOriginalResourceColor("TaskButtonThumbnailBackground"));
            RestoreResourceColor("TaskButtonThumbnailBorder", ReadOriginalResourceColor("TaskButtonThumbnailBorder"));
            RestoreResourceColor("TaskButtonThumbnailInnerBorder", ReadOriginalResourceColor("TaskButtonThumbnailInnerBorder"));
            RestoreResourceColor("TaskButtonThumbnailThumbBorder", ReadOriginalResourceColor("TaskButtonThumbnailThumbBorder"));
            RestoreResourceColor("TaskListScrollArrow", ReadOriginalResourceColor("TaskListScrollArrow"));
            RestoreResourceColor("TaskListScrollArrowHover", ReadOriginalResourceColor("TaskListScrollArrowHover"));
            RestoreResourceColor("TaskListScrollButtonBackground", ReadOriginalResourceColor("TaskListScrollButtonBackground"));
            RestoreResourceColor("TaskListScrollButtonBorder", ReadOriginalResourceColor("TaskListScrollButtonBorder"));
            RestoreResourceColor("TaskListScrollButtonInnerBorderHover", ReadOriginalResourceColor("TaskListScrollButtonInnerBorderHover"));
            RestoreResourceColor("TaskListScrollButtonInnerBorderPressed", ReadOriginalResourceColor("TaskListScrollButtonInnerBorderPressed"));
            RestoreResourceColor("TaskListScrollButtonOuterBorder", ReadOriginalResourceColor("TaskListScrollButtonOuterBorder"));
            RestoreResourceColor("ToolTip", ReadOriginalResourceColor("ToolTip"));
            RestoreResourceColor("ToolTipBackground", ReadOriginalResourceColor("ToolTipBackground"));
            RestoreResourceColor("ToolTipBalloonBottomBackground", ReadOriginalResourceColor("ToolTipBalloonBottomBackground"));
            RestoreResourceColor("ToolTipBalloonForeground", ReadOriginalResourceColor("ToolTipBalloonForeground"));
            RestoreResourceColor("ToolTipBorder", ReadOriginalResourceColor("ToolTipBorder"));
            RestoreResourceColor("ToolTipForeground", ReadOriginalResourceColor("ToolTipForeground"));
            RestoreResourceColor("ToolbarButtonBackgroundHover", ReadOriginalResourceColor("ToolbarButtonBackgroundHover"));
            RestoreResourceColor("ToolbarThumbFill", ReadOriginalResourceColor("ToolbarThumbFill"));
            RestoreResourceColor("TrayToggleArrowForeground", ReadOriginalResourceColor("TrayToggleArrowForeground"));
            RestoreResourceColor("TrayToggleArrowPressed", ReadOriginalResourceColor("TrayToggleArrowPressed"));
            RestoreResourceColor("TrayToggleBorder", ReadOriginalResourceColor("TrayToggleBorder"));
            RestoreResourceColor("TrayToggleHoverBackground", ReadOriginalResourceColor("TrayToggleHoverBackground"));
            RestoreResourceColor("TrayToggleOuterBorder", ReadOriginalResourceColor("TrayToggleOuterBorder"));
            RestoreResourceColor("TrayTogglePressedBackground", ReadOriginalResourceColor("TrayTogglePressedBackground"));
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
            if (ResourcesList.SelectedItem is string selectedKey)
            {
                RestoreResourceColor(selectedKey, ReadOriginalResourceColor(selectedKey));
                UpdateSelectedResourceDetails(selectedKey);
            }
        }

        private void ResourcesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(!_hasInitialized){return;}

            if (ResourcesList.SelectedItem is string selectedKey)
            {
                UpdateSelectedResourceDetails(selectedKey);
            }
        }

        private void PopulateResourcesList()
        {
            List<string> _brushKeys = [];

            // Add brush keys from the resource dictionary
            ListAddUnique(_brushKeys, "BalloonCloseButtonBackgroundHover");
            ListAddUnique(_brushKeys, "BalloonCloseButtonBackgroundPressed");
            ListAddUnique(_brushKeys, "BalloonCloseButtonForegroundHover");
            ListAddUnique(_brushKeys, "BalloonCloseButtonForegroundPressed");
            ListAddUnique(_brushKeys, "BalloonCloseButtonInactiveForeground");
            ListAddUnique(_brushKeys, "BalloonCloseButtonInnerBorderHover");
            ListAddUnique(_brushKeys, "BalloonCloseButtonInnerBorderPressed");
            ListAddUnique(_brushKeys, "BalloonCloseButtonOuterBorder");
            ListAddUnique(_brushKeys, "ButtonActiveForeground");
            ListAddUnique(_brushKeys, "ButtonFlashingForeground");
            ListAddUnique(_brushKeys, "ButtonForeground");
            ListAddUnique(_brushKeys, "ButtonPressedForeground");
            ListAddUnique(_brushKeys, "ClockForeground");
            ListAddUnique(_brushKeys, "InputLanguageBackground");
            ListAddUnique(_brushKeys, "InputLanguageForeground");
            ListAddUnique(_brushKeys, "ItemButtonForeground");
            ListAddUnique(_brushKeys, "TaskbarBackground");
            ListAddUnique(_brushKeys, "TaskbarBottomBorder");
            ListAddUnique(_brushKeys, "TaskbarBottomInnerBorder");
            ListAddUnique(_brushKeys, "TaskbarTopBorder");
            ListAddUnique(_brushKeys, "TaskbarTopInnerBorder");
            ListAddUnique(_brushKeys, "TaskbarVerticalBackground");
            ListAddUnique(_brushKeys, "TaskbarVerticalBottomBorder");
            ListAddUnique(_brushKeys, "TaskbarVerticalBottomInnerBorder");
            ListAddUnique(_brushKeys, "TaskbarVerticalTopBorder");
            ListAddUnique(_brushKeys, "TaskbarVerticalTopInnerBorder");
            ListAddUnique(_brushKeys, "TaskbarWindowBackground");
            ListAddUnique(_brushKeys, "TaskButtonBackground");
            ListAddUnique(_brushKeys, "TaskButtonBackgroundActive");
            ListAddUnique(_brushKeys, "TaskButtonBackgroundActiveHover");
            ListAddUnique(_brushKeys, "TaskButtonBackgroundFlashing");
            ListAddUnique(_brushKeys, "TaskButtonBackgroundHover");
            ListAddUnique(_brushKeys, "TaskButtonInnerBorder");
            ListAddUnique(_brushKeys, "TaskButtonInnerBorderActive");
            ListAddUnique(_brushKeys, "TaskButtonInnerBorderFlashing");
            ListAddUnique(_brushKeys, "TaskButtonInnerBorderHover");
            ListAddUnique(_brushKeys, "TaskButtonInnerBottomLeftBorder");
            ListAddUnique(_brushKeys, "TaskButtonInnerBottomLeftBorderActive");
            ListAddUnique(_brushKeys, "TaskButtonInnerBottomLeftBorderFlashing");
            ListAddUnique(_brushKeys, "TaskButtonInnerBottomLeftBorderHover");
            ListAddUnique(_brushKeys, "TaskButtonInnerTopRightBorder");
            ListAddUnique(_brushKeys, "TaskButtonInnerTopRightBorderActive");
            ListAddUnique(_brushKeys, "TaskButtonInnerTopRightBorderFlashing");
            ListAddUnique(_brushKeys, "TaskButtonInnerTopRightBorderHover");
            ListAddUnique(_brushKeys, "TaskButtonOuterBorder");
            ListAddUnique(_brushKeys, "TaskButtonOuterBorderActive");
            ListAddUnique(_brushKeys, "TaskButtonOuterBorderFlashing");
            ListAddUnique(_brushKeys, "TaskButtonOuterBorderHover");
            ListAddUnique(_brushKeys, "TaskButtonThumbnailBackground");
            ListAddUnique(_brushKeys, "TaskButtonThumbnailBorder");
            ListAddUnique(_brushKeys, "TaskButtonThumbnailInnerBorder");
            ListAddUnique(_brushKeys, "TaskButtonThumbnailThumbBorder");
            ListAddUnique(_brushKeys, "TaskListScrollArrow");
            ListAddUnique(_brushKeys, "TaskListScrollArrowHover");
            ListAddUnique(_brushKeys, "TaskListScrollButtonBackground");
            ListAddUnique(_brushKeys, "TaskListScrollButtonBorder");
            ListAddUnique(_brushKeys, "TaskListScrollButtonInnerBorderHover");
            ListAddUnique(_brushKeys, "TaskListScrollButtonInnerBorderPressed");
            ListAddUnique(_brushKeys, "TaskListScrollButtonOuterBorder");
            ListAddUnique(_brushKeys, "ToolTip");
            ListAddUnique(_brushKeys, "ToolTipBackground");
            ListAddUnique(_brushKeys, "ToolTipBalloonBottomBackground");
            ListAddUnique(_brushKeys, "ToolTipBalloonForeground");
            ListAddUnique(_brushKeys, "ToolTipBorder");
            ListAddUnique(_brushKeys, "ToolTipForeground");
            ListAddUnique(_brushKeys, "ToolbarButtonBackgroundHover");
            ListAddUnique(_brushKeys, "ToolbarThumbFill");
            ListAddUnique(_brushKeys, "TrayToggleArrowForeground");
            ListAddUnique(_brushKeys, "TrayToggleArrowPressed");
            ListAddUnique(_brushKeys, "TrayToggleBorder");
            ListAddUnique(_brushKeys, "TrayToggleHoverBackground");
            ListAddUnique(_brushKeys, "TrayToggleOuterBorder");
            ListAddUnique(_brushKeys, "TrayTogglePressedBackground");

            // Bind the list of brush keys to the ListBox
            ResourcesList.ItemsSource = _brushKeys;
        }

        private void EnableCustomizationControls(bool isEnabled)
        {
            ResourcesList.IsEnabled = isEnabled;
            ChangeColorButton.IsEnabled = isEnabled;
            ResetColorButton.IsEnabled = isEnabled;
            SelectedColorBox.Fill = isEnabled ? Brushes.Transparent : Brushes.Gray;
            HexColorText.Text = isEnabled ? string.Empty : "";
        }

        private bool _settingsCustomizeThemeEnabled
        {
            get => Settings.Instance.CustomizeThemeEnabled;
            set
            {
                if (Settings.Instance.CustomizeThemeEnabled != value)
                {
                    Settings.Instance.CustomizeThemeEnabled = value;
                    Settings.Instance.PropertyChanged += Settings_PropertyChanged;
                }
            }
        }

        private static void SaveThemeCustomizations(string resourceName, string color, string brushType)
        {
            var newCustomization = new { resourceName, color, brushType };

            Dictionary<string, object> customizations = new Dictionary<string, object>();

            if (File.Exists(CustomizationsFilePath))
            {
                string json = File.ReadAllText(CustomizationsFilePath);
                customizations = JsonSerializer.Deserialize<Dictionary<string, object>>(json) ?? new Dictionary<string, object>();
            }

            customizations[resourceName] = newCustomization;

            var options = new JsonSerializerOptions { WriteIndented = true };
            string updatedJson = JsonSerializer.Serialize(customizations, options);

            string directory = Path.GetDirectoryName(CustomizationsFilePath);
            if (!Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllText(CustomizationsFilePath, updatedJson);
        }

        public static void LoadThemeCustomizations()
        {
            if (!File.Exists(CustomizationsFilePath))
            {
                return; // No customizations file, do nothing
            }

            string json = File.ReadAllText(CustomizationsFilePath);
            var customizations = JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, string>>>(json);

            if (customizations == null)
            {
                return; // No customizations found in the file, do nothing
            }

            foreach (var customization in customizations)
            {
                string resourceName = customization.Key;
                var customizationData = customization.Value;

                if (customizationData.ContainsKey("color") && customizationData.ContainsKey("brushType"))
                {
                    string color = customizationData["color"];
                    string brushType = customizationData["brushType"];

                    if (_resourceDictionary.Contains(resourceName) && IsSupportedColorBrush(resourceName))
                    {
                        if (brushType == "SolidColorBrush")
                        {
                            Color solidColor = (Color)ColorConverter.ConvertFromString(color);
                            _resourceDictionary[resourceName] = new SolidColorBrush(solidColor);
                        }
                        else if (brushType == "LinearGradientBrush")
                        {
// TODO: add better support for linear gradient colors
                            Color gradientColor = (Color)ColorConverter.ConvertFromString(color);
                            if (_resourceDictionary[resourceName] is LinearGradientBrush gradientBrush)
                            {
                                LinearGradientBrush newBrush = new();
                                foreach (GradientStop stop in gradientBrush.GradientStops)
                                {
                                    newBrush.GradientStops.Add(new GradientStop(gradientColor, stop.Offset));
                                }
                                _resourceDictionary[resourceName] = newBrush;
                            }
                        }
                    }
                }
            }
        }

        public static string ReadOriginalResourceColor(string resourceKey)
        {
            string themeName = Settings.Instance.Theme;

            string themeFilePath = GetThemeFilePath(themeName);
            if (themeFilePath == null || !File.Exists(themeFilePath))
            {
                return null;
            }

            try
            {
                XDocument themeXaml = XDocument.Load(themeFilePath);

                XNamespace xmlns = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
                XNamespace xNs = "http://schemas.microsoft.com/winfx/2006/xaml";

                var resourceElement = themeXaml.Descendants(xmlns + "LinearGradientBrush")
                                               .FirstOrDefault(e => (string)e.Attribute(xNs + "Key") == resourceKey) ??
                                      themeXaml.Descendants(xmlns + "SolidColorBrush")
                                               .FirstOrDefault(e => (string)e.Attribute(xNs + "Key") == resourceKey);

                if (resourceElement != null)
                {
                    string color = null;

                    if (resourceElement.Name.LocalName == "LinearGradientBrush")
                    {
// TODO: add better support for linear gradient colors
                        // Return gradient stop color
                        //var firstGradientStop = resourceElement.Descendants(xmlns + "GradientStop").FirstOrDefault();
                        var firstGradientStop = resourceElement.Descendants(xmlns + "GradientStop").LastOrDefault();
                        if (firstGradientStop != null)
                        {
                            color = ((string)firstGradientStop.Attribute("Color")).TrimStart('#');
                        }
                    }
                    else if (resourceElement.Name.LocalName == "SolidColorBrush")
                    {
                        color = ((string)resourceElement.Attribute("Color")).TrimStart('#');
                    }

                    if (!string.IsNullOrEmpty(color))
                    {
                        if(color.ToUpper() == "TRANSPARENT"){ return null; } // Skip "Transparent" values

                        color = "#" + color; // Add the # back

                        return color.ToUpper();
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Error reading the theme file: " + ex.Message);
            }

            return null;
        }

        private static string GetThemeFilePath(string themeName)
        {
            string appPath = AppDomain.CurrentDomain.BaseDirectory;
            string localAppDataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "RetroBar", "Themes");

            // Check if the theme exists in the app path or in the local app data
            string appThemeFilePath = Path.Combine(appPath, "Themes", themeName + ".xaml");
            string localAppDataThemeFilePath = Path.Combine(localAppDataPath, themeName + ".xaml");

            if (File.Exists(appThemeFilePath))
            {
                return appThemeFilePath;
            }
            else if (File.Exists(localAppDataThemeFilePath))
            {
                return localAppDataThemeFilePath;
            }

            return null;
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
                        SaveThemeCustomizations(resourceName, convertedColor.ToString(), "SolidColorBrush");
                    }
                    else if (_resourceDictionary[resourceName] is LinearGradientBrush gradientBrush)
                    {
// TODO: add better support for linear gradient colors
                        LinearGradientBrush newBrush = new();
                        foreach (GradientStop stop in gradientBrush.GradientStops)
                        {
                            newBrush.GradientStops.Add(new GradientStop(convertedColor, stop.Offset));
                        }
                        _resourceDictionary[resourceName] = newBrush;
                        SaveThemeCustomizations(resourceName, convertedColor.ToString(), "LinearGradientBrush");
                    }
                }
            }
        }

        private static void RestoreResourceColor(string resourceName, string color, bool saveToFile = false)
        {
            if(color == null){ return; }

            if (_resourceDictionary[resourceName] is SolidColorBrush)
            {
                Color solidColor = (Color)ColorConverter.ConvertFromString(color);
                _resourceDictionary[resourceName] = new SolidColorBrush(solidColor);
                if(saveToFile){ SaveThemeCustomizations(resourceName, color, "SolidColorBrush");}
            }
            else if (_resourceDictionary[resourceName] is LinearGradientBrush gradientBrush)
            {
                Color gradientColor = (Color)ColorConverter.ConvertFromString(color);
// TODO: add better support for linear gradient colors
                LinearGradientBrush newBrush = new();
                foreach (GradientStop stop in gradientBrush.GradientStops)
                {
                    newBrush.GradientStops.Add(new GradientStop(gradientColor, stop.Offset));
                }
                _resourceDictionary[resourceName] = newBrush;
                if(saveToFile){ SaveThemeCustomizations(resourceName, color, "LinearGradientBrush");}
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
