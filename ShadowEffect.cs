using PaintDotNet.IndirectUI;
using PaintDotNet.PropertySystem;
using PaintDotNet.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Resources;

namespace PaintDotNet.Effects
{
    /// <summary>
    /// Configurable effect for Paint.NET which creates a shadow of the source image
    /// </summary>
    public sealed class ShadowEffect : PropertyBasedEffect
    {
        /// <summary>
        /// The user displayed name of the effect
        /// </summary>
        public static string StaticName
        {
            get
            {
                return resources.GetString("ShadowEffect.Name");
            }
        }

        /// <summary>
        /// The user displayed icon of the effect
        /// </summary>
        public static Image StaticImage
        {
            get
            {
                return (Image)resources.GetObject("Icons.ShadowEffect.bmp");
            }
        }

        #region Static Private Fields

        private static ResourceManager resources = new ResourceManager(typeof(ShadowEffect));
        private static int rowsPerBlurRadius = 5;

        #endregion Static Private Fields

        #region Constructors

        /// <summary>
        /// Creates an instance of the <see cref="ShadowEffect"/> class
        /// </summary>
        public ShadowEffect()
            : base(StaticName,
                  StaticImage,
                  "Object",
                  EffectFlags.Configurable)
        {
            this.blurEffect = new GaussianBlurEffect();
        }

        #endregion Constructors

        #region Members

        /// <summary>
        /// </summary>
        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> propsBuilder = new List<Property>()
            {
                new Int32Property("ShadowEffect.Alpha", 115, 0, 255),
                new DoubleProperty("ShadowEffect.ShadowAngle", 45, 0, 180),
                new DoubleProperty("ShadowEffect.ShadowDepthAngle", 45, 0, 90),
                new BooleanProperty("ShadowEffect.OriginalImage", true)
            };

            return new PropertyCollection(propsBuilder);
        }

        /// <summary>
        /// Configure the user interface of the effect.
        /// You may change the default control type of your properties or
        /// modify/suppress the default texts in the controls.
        /// </summary>
        protected override ControlInfo OnCreateConfigUI(PropertyCollection props)
        {
            ControlInfo configUI = CreateDefaultConfigUI(props);

            // Change DisplayName (default is the PropertyNames identifier)
            configUI.SetPropertyControlValue("ShadowEffect.Alpha", ControlInfoPropertyNames.DisplayName, resources.GetString("ShadowEffect.AlphaAmountLabel"));
            configUI.SetPropertyControlValue("ShadowEffect.Alpha", ControlInfoPropertyNames.ControlColors, new ColorBgra[] { ColorBgra.White, ColorBgra.Black });
            configUI.SetPropertyControlValue("ShadowEffect.ShadowAngle", ControlInfoPropertyNames.DisplayName, resources.GetString("ShadowEffect.ShadowAngle"));
            configUI.SetPropertyControlType("ShadowEffect.ShadowAngle", PropertyControlType.AngleChooser);
            configUI.SetPropertyControlValue("ShadowEffect.ShadowDepthAngle", ControlInfoPropertyNames.DisplayName, resources.GetString("ShadowEffect.ShadowDepthAngle"));
            configUI.SetPropertyControlType("ShadowEffect.ShadowDepthAngle", PropertyControlType.AngleChooser);
            configUI.SetPropertyControlValue("ShadowEffect.OriginalImage", ControlInfoPropertyNames.DisplayName, string.Empty);
            configUI.SetPropertyControlValue("ShadowEffect.OriginalImage", ControlInfoPropertyNames.Description, "Keep original image");

            return configUI;
        }

        private GaussianBlurEffect blurEffect;

        // ----------------------------------------------------------------------
        /// <summary>
        /// Render an area defined by a list of rectangles
        /// This function may be called multiple times to render the area of
        ///  the selection on the active layer
        /// </summary>
        protected override void OnRender(Rectangle[] rois, int startIndex, int length)
        {
            for (int i = startIndex; i < startIndex + length; ++i)
            {
                RenderRectangle(DstArgs.Surface, SrcArgs.Surface, rois[i]);
            }
        }

        /// <summary>
        /// Creates the shadow of the source image
        /// </summary>
        /// <param name="dst">Describes the destination surface.</param>
        /// <param name="src">Describes the source surface.</param>
        /// <param name="rect">The rectangle that describes the region of interest.</param>
        /// 

        private BinaryPixelOp normalOp = LayerBlendModeUtil.CreateCompositionOp(LayerBlendMode.Normal);

        private unsafe void RenderRectangle(Surface dst, Surface src, Rectangle rect)
        {
            double shadowFactor = (double)(Token.GetProperty<Int32Property>("ShadowEffect.Alpha").Value) / 255.0;

            // The blurring algorithm was stolen directly from the BlurEffect code.  I couldn't 
            // use it directly because the source image must be transformed prior to applying 
            // the blur effect. Also, I gradually increase the blur radius from one end
            // of the shadow to the other, which the blur effect code doesn't support either.
            if (rect.Height >= 1 && rect.Width >= 1)
            {
                // For each row in the rectangle
                for (int y = rect.Top; y < rect.Bottom; ++y)
                {
                    double radius = invertedYcoordinate(y, src.Height) / (double)rowsPerBlurRadius;
                    int[] w = CreateGaussianBlurRow(radius);
                    int wlen = w.Length;
                    int r = (wlen - 1) / 2;
                    long[] waSums = new long[wlen];
                    long[] aSums = new long[wlen];
                    long waSum = 0;
                    long aSum = 0;
                    ColorBgra* dstPtr = dst.GetPointAddressUnchecked(rect.Left, y);

                    // For each item in the gaussian blur row
                    for (int wx = 0; wx < wlen; ++wx)
                    {
                        int srcX = rect.Left + wx - r;
                        waSums[wx] = 0;
                        aSums[wx] = 0;

                        if (srcX >= 0 && srcX < src.Width)
                        {
                            for (int wy = 0; wy < wlen; ++wy)
                            {
                                int srcY = y + wy - r;

                                if (srcY >= 0 && srcY < src.Height)
                                {
                                    ColorBgra c = getShadowPixel(srcX, srcY, src, shadowFactor, (int)Token.GetProperty<DoubleProperty>("ShadowEffect.ShadowAngle").Value, (int)Token.GetProperty<DoubleProperty>("ShadowEffect.ShadowDepthAngle").Value);
                                    int wp = w[wy];

                                    waSums[wx] += wp;
                                    aSums[wx] += wp * c.A;
                                }
                            }

                            int wwx = w[wx];
                            waSum += wwx * waSums[wx];
                            aSum += wwx * aSums[wx];
                        }
                    }

                    if (waSum == 0)
                        dstPtr->Bgra = 0;
                    else
                        dstPtr->Bgra = ColorBgra.BgraToUInt32(0, 0, 0, (int)(aSum / waSum));

                    ++dstPtr;

                    for (int x = rect.Left + 1; x < rect.Right; ++x)
                    {
                        ColorBgra OrginalImage = src[x, y];

                        for (int i = 0; i < wlen - 1; ++i)
                        {
                            waSums[i] = waSums[i + 1];
                            aSums[i] = aSums[i + 1];
                        }

                        waSum = 0;
                        aSum = 0;

                        int wx;
                        for (wx = 0; wx < wlen - 1; ++wx)
                        {
                            long wwx = (long)w[wx];
                            waSum += wwx * waSums[wx];
                            aSum += wwx * aSums[wx];
                        }

                        wx = wlen - 1;

                        waSums[wx] = 0;
                        aSums[wx] = 0;

                        int srcX = x + wx - r;

                        if (srcX >= 0 && srcX < src.Width)
                        {
                            for (int wy = 0; wy < wlen; ++wy)
                            {
                                int srcY = y + wy - r;

                                if (srcY >= 0 && srcY < src.Height)
                                {
                                    ColorBgra c = getShadowPixel(srcX, srcY, src, shadowFactor, (int)Token.GetProperty<DoubleProperty>("ShadowEffect.ShadowAngle").Value, (int)Token.GetProperty<DoubleProperty>("ShadowEffect.ShadowDepthAngle").Value);
                                    int wp = w[wy];

                                    waSums[wx] += wp;
                                    aSums[wx] += wp * (long)c.A;
                                }
                            }

                            int wr = w[wx];
                            waSum += (long)wr * waSums[wx];
                            aSum += (long)wr * aSums[wx];
                        }

                        ColorBgra Shadow = new ColorBgra();
                        if (waSum == 0)
                        {
                            Shadow = ColorBgra.FromBgra(0, 0, 0, 0);
                        }
                        else
                        {
                            Shadow = ColorBgra.FromBgra(0, 0, 0, (byte)(aSum / waSum));
                        }

                        if (Token.GetProperty<BooleanProperty>("ShadowEffect.OriginalImage").Value)
                        {
                            dstPtr->Bgra = (uint)normalOp.Apply(Shadow, OrginalImage);
                        }
                        else
                        {
                            dstPtr->Bgra = (uint)Shadow;
                        }

                        ++dstPtr;
                    }
                }
            }
        }

        #endregion Members

        #region Private Methods

        /// <summary>
        /// Calculates the value of this pixel for the shadow of the image defined by inSrcSurface.
        /// The pixel value is before any blurring has been applied to the shadow.
        /// </summary>
        /// <param name="shadowX">The x coordinate of the shadow pixel to retreive</param>
        /// <param name="shadowY">Tye y coordinate of the shadow pixel to retreive</param>
        /// <param name="inSrcSurface">The surface from which to calculate the shadow</param>
        /// <param name="inShadowFactor">The factor by which to calculate the alpha for the shadow</param>
        /// <param name="theta1">The angle of the shadow (left to right) in degrees</param>
        /// <param name="theta2">The angle of the depth of the shadow (front to back) in degrees</param>
        /// <returns></returns>
        private ColorBgra getShadowPixel(int shadowX, int shadowY, Surface inSrcSurface, double inShadowFactor, int theta1, int theta2)
        {
            Point src = new Point(0, 0);
            src.X = (int)(shadowX - (invertedYcoordinate(shadowY, inSrcSurface.Height) / Math.Tan(degreesToRadians(theta1))));
            src.Y = invertedYcoordinate((int)(invertedYcoordinate(shadowY, inSrcSurface.Height) * 90.0 / theta2), inSrcSurface.Height);

            if (inSrcSurface.Bounds.Contains(src))
            {
                return ColorBgra.FromBgra(0, 0, 0, (byte)(inSrcSurface[src].A * inShadowFactor));
            }

            return ColorBgra.Transparent;
        }

        private double degreesToRadians(double inDegrees)
        {
            return inDegrees * (Math.PI / 180);
        }

        /// <summary>
        /// Invert a y coordinate so that the bottom left corner of the coordinate system is (0,0)
        /// </summary>
        /// <param name="inOriginalY">Original y coordinate</param>
        /// <param name="inImageHeight">Height of image on which we are operating</param>
        /// <returns>The y coordinate in the inverted coordinate system</returns>
        private int invertedYcoordinate(int inOriginalY, int inImageHeight)
        {
            return -1 * (inOriginalY - inImageHeight);
        }

        private int[] CreateGaussianBlurRow(double amount)
        {
            int size = (int)(1 + (Math.Ceiling(amount) * 2));
            int[] weights = new int[size];

            for (int i = (int)Math.Ceiling(amount); i >= 0; --i, --amount)
            {
                weights[i] = (int)(16 * (amount + 1.0));
                weights[weights.Length - i - 1] = weights[i];
            }

            return weights;
        }

        #endregion Private Methods
    }
}