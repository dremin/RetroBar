using ManagedShell.Common.Logging;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Forms;


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
            // Handle loaded event (setup texts etc)
            this.Loaded += PropertiesWindow_Loaded;
        }

        private void PropertiesWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Initialize color texts
// TODO: maybe change color texts to the actual colors?
            SetCurrentColorText("TaskbarBackground", CurrentColorText_TaskbarBackground);
            SetCurrentColorText("TaskbarVerticalBackground", CurrentColorText_TaskbarVerticalBackground);
            SetCurrentColorText("TaskbarTopBorder", CurrentColorText_TaskbarTopBorder);
            SetCurrentColorText("TaskbarTopInnerBorder", CurrentColorText_TaskbarTopInnerBorder);
            SetCurrentColorText("TaskbarVerticalTopBorder", CurrentColorText_TaskbarVerticalTopBorder);
            SetCurrentColorText("TaskbarVerticalTopInnerBorder", CurrentColorText_TaskbarVerticalTopInnerBorder);
            SetCurrentColorText("TaskbarBottomBorder", CurrentColorText_TaskbarBottomBorder);
            SetCurrentColorText("TaskbarBottomInnerBorder", CurrentColorText_TaskbarBottomInnerBorder);
            SetCurrentColorText("TaskbarVerticalBottomBorder", CurrentColorText_TaskbarVerticalBottomBorder);
            SetCurrentColorText("TaskbarVerticalBottomInnerBorder", CurrentColorText_TaskbarVerticalBottomInnerBorder);
        }

        private void ThemeCustomizationsEnabled_CheckBox_OnChecked(object sender, RoutedEventArgs e)
        {
// TODO: enable theme customizations
        }

        private void ThemeCustomizationsEnabled_CheckBox_OnUnChecked(object sender, RoutedEventArgs e)
        {
// TODO: disable theme customizations
        }

        private void PickColorButton_Click_TaskbarBackground(object sender, RoutedEventArgs e)
        {
            ChangeResourceColor("TaskbarBackground", CurrentColorText_TaskbarBackground);
        }

        private void PickColorButton_Click_TaskbarVerticalBackground(object sender, RoutedEventArgs e)
        {
            ChangeResourceColor("TaskbarVerticalBackground", CurrentColorText_TaskbarVerticalBackground);
        }

        private void PickColorButton_Click_TaskbarTopBorder(object sender, RoutedEventArgs e)
        {
            ChangeResourceColor("TaskbarTopBorder", CurrentColorText_TaskbarTopBorder);
        }

        private void PickColorButton_Click_TaskbarTopInnerBorder(object sender, RoutedEventArgs e)
        {
            ChangeResourceColor("TaskbarTopInnerBorder", CurrentColorText_TaskbarTopInnerBorder);
        }

        private void PickColorButton_Click_TaskbarVerticalTopBorder(object sender, RoutedEventArgs e)
        {
            ChangeResourceColor("TaskbarVerticalTopBorder", CurrentColorText_TaskbarVerticalTopBorder);
        }

        private void PickColorButton_Click_TaskbarVerticalTopInnerBorder(object sender, RoutedEventArgs e)
        {
            ChangeResourceColor("TaskbarVerticalTopInnerBorder", CurrentColorText_TaskbarVerticalTopInnerBorder);
        }

        private void PickColorButton_Click_TaskbarBottomBorder(object sender, RoutedEventArgs e)
        {
            ChangeResourceColor("TaskbarBottomBorder", CurrentColorText_TaskbarBottomBorder);
        }

        private void PickColorButton_Click_TaskbarBottomInnerBorder(object sender, RoutedEventArgs e)
        {
            ChangeResourceColor("TaskbarBottomInnerBorder", CurrentColorText_TaskbarBottomInnerBorder);
        }

        private void PickColorButton_Click_TaskbarVerticalBottomBorder(object sender, RoutedEventArgs e)
        {
            ChangeResourceColor("TaskbarVerticalBottomBorder", CurrentColorText_TaskbarVerticalBottomBorder);
        }

        private void PickColorButton_Click_TaskbarVerticalBottomInnerBorder(object sender, RoutedEventArgs e)
        {
            ChangeResourceColor("TaskbarVerticalBottomInnerBorder", CurrentColorText_TaskbarVerticalBottomInnerBorder);
        }

        private static void ChangeResourceColor(string resourceName, System.Windows.Controls.TextBlock textBlock)
        {
            if (_resourceDictionary.Contains(resourceName))
            {
                if(IsSupportedColorBrush(resourceName))
                {
                    ColorDialog colorDialog = new();
                    colorDialog.AllowFullOpen = false;  // Restrict to only predefined colors by disabling custom color selection
                    colorDialog.ShowHelp = false;       // Hide the help button
                        if (colorDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                        {
                            System.Drawing.Color newColor = colorDialog.Color;

                            if(IsSolidColorBrush(resourceName))
                            {
                                SetResourceSolidColorBrush(resourceName, newColor);
                            }
                            else if(IsLinearGradientBrush(resourceName))
                            {
                                SetResourceLinearGradientBrush(resourceName, newColor);
                            }

                            SetCurrentColorText(resourceName, textBlock);
                        }
                }
                else
                {
                    ShellLogger.Debug($"Resource {resourceName} has unknown brush.");
                }
            }
            else
            {
                textBlock.Text = "";
                ShellLogger.Debug($"Resource {resourceName} not found.");
            }
        }

        private static void SetCurrentColorText(string resourceName, System.Windows.Controls.TextBlock textBlock)
        {
            if (_resourceDictionary.Contains(resourceName))
            {
                if(IsSupportedColorBrush(resourceName))
                {
                    System.Windows.Media.Brush currentColor = GetCurrentResourceColor(resourceName);
                    textBlock.Text = $"Current {resourceName} Color: {GetColorNameFromBrush(currentColor)}";
                }
            }
        }

        private static bool IsSupportedColorBrush(string resourceName)
        {
            if (_resourceDictionary.Contains(resourceName))
            {
                object brush = _resourceDictionary[resourceName];

                // If the brush is null
                if (brush == null) { return false; }

                // Supported brushes
                if (brush is SolidColorBrush    ) { return true; }
                if (brush is LinearGradientBrush) { return true; }
            }

            // If the brush is not supported, or the resource is not found
            return false; 
        }

        // Method to check if a resource is a SolidColorBrush
        private static bool IsSolidColorBrush(string resourceName)
        {
            var resources = System.Windows.Application.Current.Resources;
            if (resources.Contains(resourceName))
            {
                var resource = resources[resourceName];
                return resource is SolidColorBrush;
            }
            return false;
        }

        // Method to check if a resource is a LinearGradientBrush
        private static bool IsLinearGradientBrush(string resourceName)
        {
            var resources = System.Windows.Application.Current.Resources;
            if (resources.Contains(resourceName))
            {
                var resource = resources[resourceName];
                return resource is LinearGradientBrush;
            }
            return false;
        }

        // Method to get the current brush resource (SolidColorBrush or LinearGradientBrush)
        private static System.Windows.Media.Brush GetCurrentResourceColor(string resourceName)
        {
            var resources = System.Windows.Application.Current.Resources;
            if (resources.Contains(resourceName))
            {
                var resource = resources[resourceName];
                if (resource is SolidColorBrush || resource is LinearGradientBrush)
                {
                    return resource as System.Windows.Media.Brush;
                }
            }
            return null; // Return null if the resource is not found or not a valid brush type
        }

        // Method to set the color for a SolidColorBrush resource
        private static void SetResourceSolidColorBrush(string resourceName, System.Drawing.Color drawingColor)
        {
            var resources = System.Windows.Application.Current.Resources;
            if (resources.Contains(resourceName))
            {
                // Convert System.Drawing.Color to System.Windows.Media.Color
                System.Windows.Media.Color convertedColor = System.Windows.Media.Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);

                // Set the new SolidColorBrush to the resource
                resources[resourceName] = new SolidColorBrush(convertedColor);
            }
        }

        // Method to set the color for a LinearGradientBrush resource
        private static void SetResourceLinearGradientBrush(string resourceName, System.Drawing.Color drawingColor)
        {
            var resources = System.Windows.Application.Current.Resources;
    
            if (resources.Contains(resourceName))
            {
                var existingBrush = resources[resourceName] as LinearGradientBrush;

                if (existingBrush != null)
                {
                    // Convert System.Drawing.Color to System.Windows.Media.Color
                    System.Windows.Media.Color convertedColor = System.Windows.Media.Color.FromArgb(drawingColor.A, drawingColor.R, drawingColor.G, drawingColor.B);

                    // Create a new LinearGradientBrush with the same properties as the existing one
                    LinearGradientBrush newBrush = new LinearGradientBrush();

// TODO: this is a temporary loop, we will set all gradientstops to same color for now, update gradient stops separately later
                    foreach (var gradientStop in existingBrush.GradientStops)
                    {
                        newBrush.GradientStops.Add(new GradientStop(convertedColor, gradientStop.Offset));
                    }

                    // If there are no gradient stops, create a default gradient stop (with the new color)
                    if (newBrush.GradientStops.Count == 0)
                    {
                        newBrush.GradientStops.Add(new GradientStop(convertedColor, 0.0));
                    }

                    // Assign the new LinearGradientBrush to the resource
                    resources[resourceName] = newBrush;
                }
            }
        }

        // Method to get the color name from a Brush, accepting SolidColorBrush and LinearGradientBrush
        private static string GetColorNameFromBrush(System.Windows.Media.Brush brush)
        {
            if (brush == null)
            {
                return "Unknown"; // If the brush is null, return "Unknown"
            }

            // Check for SolidColorBrush
            if (brush is SolidColorBrush solidColorBrush)
            {
                System.Windows.Media.Color color = solidColorBrush.Color;

                // Check for predefined color names in System.Windows.Media.Colors
                if (IsColorMatch(color, Colors.Black)) return "Black";
                if (IsColorMatch(color, Colors.White)) return "White";
                if (IsColorMatch(color, Colors.Red)) return "Red";
                if (IsColorMatch(color, Colors.Green)) return "Green";
                if (IsColorMatch(color, Colors.Blue)) return "Blue";
                if (IsColorMatch(color, Colors.Yellow)) return "Yellow";
                if (IsColorMatch(color, Colors.Cyan)) return "Cyan";
                if (IsColorMatch(color, Colors.Magenta)) return "Magenta";
                if (IsColorMatch(color, Colors.Gray)) return "Gray";
                if (IsColorMatch(color, Colors.Orange)) return "Orange";
                if (IsColorMatch(color, Colors.Purple)) return "Purple";
                if (IsColorMatch(color, Colors.Brown)) return "Brown";
                if (IsColorMatch(color, Colors.Pink)) return "Pink";

                // Return RGB if not a predefined color
                return $"RGB({color.R}, {color.G}, {color.B})";
            }

            // Check for LinearGradientBrush
            if (brush is LinearGradientBrush linearGradientBrush)
            {
                // Get the first gradient stop (as a simple representative color)
                if (linearGradientBrush.GradientStops.Count > 0)
                {
                    System.Windows.Media.Color color = linearGradientBrush.GradientStops[0].Color;

                    // Check for predefined color names in System.Windows.Media.Colors
                    if (IsColorMatch(color, Colors.Black)) return "Black";
                    if (IsColorMatch(color, Colors.White)) return "White";
                    if (IsColorMatch(color, Colors.Red)) return "Red";
                    if (IsColorMatch(color, Colors.Green)) return "Green";
                    if (IsColorMatch(color, Colors.Blue)) return "Blue";
                    if (IsColorMatch(color, Colors.Yellow)) return "Yellow";
                    if (IsColorMatch(color, Colors.Cyan)) return "Cyan";
                    if (IsColorMatch(color, Colors.Magenta)) return "Magenta";
                    if (IsColorMatch(color, Colors.Gray)) return "Gray";
                    if (IsColorMatch(color, Colors.Orange)) return "Orange";
                    if (IsColorMatch(color, Colors.Purple)) return "Purple";
                    if (IsColorMatch(color, Colors.Brown)) return "Brown";
                    if (IsColorMatch(color, Colors.Pink)) return "Pink";

                    // Return RGB if not a predefined color
                    return $"RGB({color.R}, {color.G}, {color.B})";
                }
// TODO: Fallback for gradient brushes with multiple stops
                return "Gradient (multiple colors)";
            }

            return "Unknown"; // If the brush is not a SolidColorBrush or LinearGradientBrush
        }

        // Helper method to compare two colors (to handle potential rounding issues)
        private static bool IsColorMatch(System.Windows.Media.Color color1, System.Windows.Media.Color color2)
        {
            return color1.R == color2.R && color1.G == color2.G && color1.B == color2.B && color1.A == color2.A;
        }
    }
}
