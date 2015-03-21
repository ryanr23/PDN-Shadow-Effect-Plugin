
using PaintDotNet.PropertySystem;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Resources;

namespace PaintDotNet.Effects
{
	/// <summary>
	/// Configurable effect for Paint.NET which creates a shadow of the source image
	/// </summary>
    public class ShadowEffect : PropertyBasedEffect
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
		public ShadowEffect() : base(StaticName,
									StaticImage,
                                    "Shadow Effect",
                                    EffectFlags.Configurable)
		{
            this.blurEffect = new GaussianBlurEffect();
		}

		#endregion Constructors

		#region Members

        protected override PropertyCollection OnCreatePropertyCollection()
        {
            List<Property> propsBuilder = new List<Property>()
            {
                new Int32Property(resources.GetString("ShadowEffect.AlphaAmountLabel"), 115, 0, 255),
                new Int32Property(resources.GetString("ShadowEffect.ShadowAngle"), 45, 0, 180),
                new Int32Property(resources.GetString("ShadowEffect.ShadowDepthAngle"), 45, 0, 90)
            };

            return new PropertyCollection(propsBuilder);
        } 

        private GaussianBlurEffect blurEffect;

        // ----------------------------------------------------------------------
        /// <summary>
        /// Called after the token of the effect changed.
        /// This method is used to read all values of the token to instance variables.
        /// These instance variables are then used to render the surface.
        /// </summary>
        protected override void OnSetRenderInfo(PropertyBasedEffectConfigToken effectToken, RenderArgs dstArgs, RenderArgs srcArgs)
        {
            // Read the current settings of the properties
            //propInt32Slider = effectToken.GetProperty<Int32Property>(PropertyNames.Int32Slider).Value;

            base.OnSetRenderInfo(effectToken, dstArgs, srcArgs);
        } /* OnSetRenderInfo */

        // ----------------------------------------------------------------------
        /// <summary>
        /// Render an area defined by a list of rectangles
        /// This function may be called multiple times to render the area of
        //  the selection on the active layer
        /// </summary>
        protected override void OnRender(Rectangle[] rois, int startIndex, int length)
        {
            for (int i = startIndex; i < startIndex + length; ++i)
            {
                RenderRectangle(DstArgs, SrcArgs, rois[i]);
            }
        }

        /// <summary>
        /// Creates the shadow of the source image
        /// </summary>
        /// <param name="dstArgs">Describes the destination surface.</param>
        /// <param name="srcArgs">Describes the source surface.</param>
        /// <param name="rect">The rectangle that describes the region of interest.</param>
        /// 
        protected unsafe void RenderRectangle(PaintDotNet.RenderArgs dstArgs, PaintDotNet.RenderArgs srcArgs, Rectangle rect)
        {
            // amount1 = alpha of shadow
            // amount2 = left to right angle in degrees
            // amount3 = front to back angle in degrees
            //ThreeAmountsConfigToken token = (ThreeAmountsConfigToken)properties;
            double shadowFactor = (double)(Token.GetProperty<Int32Property>("ShadowEffect.Alpha").Value) / 255.0;

            // The blurring algorithm was stolen directly from the BlurEffect code.  I couldn't 
            // use it directly because the source image must be transformed prior to applying 
            // the blur effect. Also, I gradually increase the blur radius from one end
            // of the shadow to the other, which the blur effect code doesn't support either.
            Surface dst = dstArgs.Surface;
            Surface src = srcArgs.Surface;

            if (rect.Height >= 1 && rect.Width >= 1)
            {
                // For each row in the rectangle
                for (int y = rect.Top; y < rect.Bottom; ++y)
                {
                    double radius = invertedYcoordinate(y, srcArgs.Surface.Height) / (double)rowsPerBlurRadius;
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
                                    ColorBgra c = getShadowPixel(srcX, srcY, src, shadowFactor, Token.GetProperty<Int32Property>("ShadowEffect.ShadowAngle").Value, Token.GetProperty<Int32Property>("ShadowEffect.ShadowDepthAngle").Value);
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
                                    ColorBgra c = getShadowPixel(srcX, srcY, src, shadowFactor, Token.GetProperty<Int32Property>("ShadowEffect.ShadowAngle").Value, Token.GetProperty<Int32Property>("ShadowEffect.ShadowDepthAngle").Value);
                                    int wp = w[wy];

                                    waSums[wx] += wp;
                                    aSums[wx] += wp * (long)c.A;
                                }
                            }

                            int wr = w[wx];
                            waSum += (long)wr * waSums[wx];
                            aSum += (long)wr * aSums[wx];
                        }

                        if (waSum == 0)
                            dstPtr->Bgra = 0;
                        else
                            dstPtr->Bgra = ColorBgra.BgraToUInt32(0, 0, 0, (int)(aSum / waSum));

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
            // (-destY + height) = (-srcY + height) * theta2 / 90; ( up and down angle )
            // destX = srcX + (destY / tan( theta1 )) (left to right angle 0-90)
            // srcX = destX - (destY / tan( theta1 ))
            // srcY = -1 * ((-destY + height) * (90/theta2)) + height;
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

            for (int i = (int)Math.Ceiling(amount); i >= 0; --i, --amount )
            {
                weights[i] = (int)(16 * (amount + 1.0));
                weights[weights.Length - i - 1] = weights[i];
            }

            return weights;
        }

		#endregion Private Methods
	}
}