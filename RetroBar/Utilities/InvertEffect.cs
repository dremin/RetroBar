using ManagedShell.Common.Logging;
using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Effects;

namespace RetroBar.Utilities
{
    // Courtesy of https://stackoverflow.com/a/45096471
    public class InvertEffect : ShaderEffect
    {
        private static readonly PixelShader _shader =
            new PixelShader { UriSource = new Uri("pack://application:,,,/Resources/shader_invert.ps") };

        public InvertEffect()
        {
            PixelShader = _shader;
            PixelShader.InvalidPixelShaderEncountered += PixelShader_InvalidPixelShaderEncountered;
            UpdateShaderValue(InputProperty);
        }

        private void PixelShader_InvalidPixelShaderEncountered(object sender, EventArgs e)
        {
            ShellLogger.Error("InvertEffect: The given pixel shader is not valid.");
        }

        public Brush Input
        {
            get { return (Brush)GetValue(InputProperty); }
            set { SetValue(InputProperty, value); }
        }

        public static readonly DependencyProperty InputProperty =
            RegisterPixelShaderSamplerProperty("Input", typeof(InvertEffect), 0);
    }
}
