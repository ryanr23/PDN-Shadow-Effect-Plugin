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
    /// <summary>
    /// 
    /// </summary>
    public class ShadowEffectConfiguration
    {
        /// <summary>
        /// Factory method for creating a ShadowEffectConfiguration object from the effect configuration token.
        /// </summary>
        /// <param name="token">The input token</param>
        /// <returns></returns>
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

        /// <summary>
        /// Left to right angle of the casted shadow.  0 is all the way to the right, 90 is straight back, 180 is all the way to the left.
        /// </summary>
        public int Angle { get; private set; }

        /// <summary>
        /// Front to back angle of the source light.  
        /// </summary>
        public int DepthAngle { get; private set; }

        /// <summary>
        /// Darkness or intensity of the shadow.
        /// </summary>
        public double Opacity { get; private set; }

        /// <summary>
        /// Should the original image be maintained or destroyed when generating the shadow effect? 
        /// </summary>
        public bool KeepOriginalImage { get; private set; }

        /// <summary>
        /// How much diffusion should be present in the shadow.  This effects how fuzzy the edge of the shadow is 
        /// the farther it is from the source image.  
        /// </summary>
        public int DiffusionFactor { get; private set; }

        private ShadowEffectConfiguration() { }
    }
}
