using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

#pragma warning disable 1591

namespace Seren.PaintDotNet.Effects
{
    public static class ShadowEffectProperties
    {
        /// <summary>
        /// Maxium value for shadow opacity.  Equals a black shadow.
        /// </summary>
        public const int MaxOpacity = 255;

        public const string Angle = "ShadowEffect.ShadowAngle";
        public const string DepthAngle = "ShadowEffect.ShadowDepthAngle";
        public const string Opacity = "ShadowEffect.Alpha";
        public const string KeepOriginalImage = "ShadowEffect.OriginalImage";
        public const string DiffusionFactor = "ShadowEffect.Diffusion";
    }
}

#pragma warning restore 1591
