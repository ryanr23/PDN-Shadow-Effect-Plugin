using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using PaintDotNet;
using PaintDotNet.Effects;
using PaintDotNet.PropertySystem;
using PaintDotNet.Data;

namespace Seren.PaintDotNet.Effects
{
    public class ShadowEffectConfiguration
    {
        public static ShadowEffectConfiguration FromToken(PropertyBasedEffectConfigToken token)
        {
            return new ShadowEffectConfiguration
            {
                Angle = (int)token.GetProperty<DoubleProperty>(ShadowEffectProperties.Angle).Value,
                DepthAngle = (int)token.GetProperty<DoubleProperty>(ShadowEffectProperties.DepthAngle).Value,
                Opacity = (double)(token.GetProperty<Int32Property>(ShadowEffectProperties.Opacity).Value),
                KeepOriginalImage = token.GetProperty<BooleanProperty>(ShadowEffectProperties.KeepOriginalImage).Value,
                DiffusionFactor = (int)token.GetProperty<Int32Property>(ShadowEffectProperties.DiffusionFactor).Value
            };
        }

        public int Angle { get; private set; }
        public int DepthAngle { get; private set; }
        public double Opacity { get; private set; }
        public bool KeepOriginalImage { get; private set; }
        public int DiffusionFactor { get; private set; }

        private ShadowEffectConfiguration() { }
    }
}
