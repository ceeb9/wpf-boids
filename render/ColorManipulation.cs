using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Media.Imaging;

namespace ColorManipulation
{
    class Conversions
    {
        public static BitmapSource CreateBitmapSourceFromGdiBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
                throw new ArgumentNullException("bitmap");

            var rect = new Rectangle(0, 0, bitmap.Width, bitmap.Height);

            var bitmapData = bitmap.LockBits(
                rect,
                ImageLockMode.ReadWrite,
                System.Drawing.Imaging.PixelFormat.Format32bppArgb);

            try
            {
                var size = (rect.Width * rect.Height) * 4;

                return BitmapSource.Create(
                    bitmap.Width,
                    bitmap.Height,
                    bitmap.HorizontalResolution,
                    bitmap.VerticalResolution,
                    System.Windows.Media.PixelFormats.Bgra32,
                    null,
                    bitmapData.Scan0,
                    size,
                    bitmapData.Stride);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }
        public static Color[,] ColorFromBmp(Bitmap bmp)
        {
            Color[,] output = new Color[bmp.Width, bmp.Height];
            for (var y = 0; y < output.GetLength(1); y++)
            {
                for (var x = 0; x < output.GetLength(0); x++)
                {
                    output[x, y] = bmp.GetPixel(x, y);
                }
            }
            return output;
        }
        public static Bitmap ColorToBmp(Color[,] input)
        {
            Bitmap bmp = new Bitmap(input.GetLength(0), input.GetLength(1));
            for (var y = 0; y < bmp.Height; y++)
            {
                for (var x = 0; x < bmp.Width; x++)
                {
                    bmp.SetPixel(x, y, input[x, y]);
                }
            }
            return bmp;
        }
        public static Color ColorFromHSV(double hue, double saturation, double value)
        {
            int hi = Convert.ToInt32(Math.Floor(hue / 60)) % 6;
            double f = hue / 60 - Math.Floor(hue / 60);

            value = value * 255;
            int v = Convert.ToInt32(value);
            int p = Convert.ToInt32(value * (1 - saturation));
            int q = Convert.ToInt32(value * (1 - f * saturation));
            int t = Convert.ToInt32(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return Color.FromArgb(255, v, t, p);
            else if (hi == 1)
                return Color.FromArgb(255, q, v, p);
            else if (hi == 2)
                return Color.FromArgb(255, p, v, t);
            else if (hi == 3)
                return Color.FromArgb(255, p, q, v);
            else if (hi == 4)
                return Color.FromArgb(255, t, p, v);
            else
                return Color.FromArgb(255, v, p, q);
        }
        public static short ColorToShort(Color input, bool fore) //works as intended ! no changes needed
        {
            int b = 0;
            int g = 0;
            int r = 0;
            int bright = 0;

            if (fore)
            {
                b = (input.B > 85) ? 1 : 0;
                g = (input.G > 85) ? 2 : 0;
                r = (input.R > 85) ? 4 : 0;
                if (input.B >= 170 || input.G >= 170 || input.R >= 170)
                {
                    bright = 8;
                }
            }
            else
            {
                b = (input.B > 85) ? 16 : 0;
                g = (input.G > 85) ? 32 : 0;
                r = (input.R > 85) ? 64 : 0;
                if (input.B >= 170 || input.G >= 170 || input.R >= 170)
                {
                    bright = 128;
                }
            }

            return (short)(b + g + r + bright);
        }
    }

    class Info
    {
        public class UnboundColor
        {
            public float R = 0;
            public float G = 0;
            public float B = 0;
        }
        public static Color[,] ColorTable(Bitmap bmp) //gets the color values from each pixel of the image (no issues)
        {
            Color[,] colorTable = new Color[bmp.Width, bmp.Height];
            //DITHERING
            //assign the default pixel values to the final value table
            for (int y = 0; y < bmp.Height; y++)
            {
                for (int x = 0; x < bmp.Width; x++)
                {
                    colorTable[x, y] = bmp.GetPixel(x, y);
                }
            }
            return colorTable;
        }
    }

    class Calculations
    {
        private static readonly int[] xPos = { 1, 2, -2, -1, 0, 1, 2, -2, -1, 0, 1, 2 };
        private static readonly int[] yPos = { 0, 0, 1, 1, 1, 1, 1, 2, 2, 2, 2, 2 };
        private static readonly float[] weight = { 5f / 32f, 3f / 32f, 2f / 32f, 4f / 32f, 5f / 32f, 4f / 32f, 2f / 32f, 0f / 32f, 2f / 32f, 3f / 32f, 2f / 32f, 0f / 32f };
        private static float redError;
        private static float greenError;
        private static float blueError;
        public static Color[,] ditherValues(Bitmap bmp)
        {
            Color[,] rawColor = Info.ColorTable(bmp);
            Info.UnboundColor[,] errorValues = new Info.UnboundColor[bmp.Width, bmp.Height];

            //initialize all the values in the array (so we can reference them)
            for (var y = 0; y < errorValues.GetLength(1); y++)
            {
                for (var x = 0; x < errorValues.GetLength(0); x++)
                {
                    errorValues[x, y] = new Info.UnboundColor();
                    errorValues[x, y].R = Convert.ToInt32(rawColor[x, y].R);
                    errorValues[x, y].G = Convert.ToInt32(rawColor[x, y].G);
                    errorValues[x, y].B = Convert.ToInt32(rawColor[x, y].B);
                }
            }

            //calculate the errors for each pixel and put them into errorValues
            for (var y = 0; y < errorValues.GetLength(1); y++)
            {
                for (var x = 0; x < errorValues.GetLength(0); x++)
                {
                    //find the errors, do it outside of loop so we only calculate it once per pixel
                    redError = Calculations.normError(errorValues[x, y].R);
                    greenError = Calculations.normError(errorValues[x, y].G);
                    blueError = Calculations.normError(errorValues[x, y].B);

                    //populate the table of errors
                    for (var i = 0; i < xPos.Length; i++)
                    {
                        if (x + xPos[i] < errorValues.GetLength(0) && x + xPos[i] >= 0 && y + yPos[i] < errorValues.GetLength(1))
                        {
                            errorValues[x + xPos[i], y + yPos[i]].R += redError * weight[i];
                            errorValues[x + xPos[i], y + yPos[i]].G += greenError * weight[i];
                            errorValues[x + xPos[i], y + yPos[i]].B += blueError * weight[i];
                        }
                    }

                    //set rawcolor (output table) to error values
                    //clamp the values to account for error in the calculation of error of large canvases
                    rawColor[x, y] = Color.FromArgb(Calculations.colorClamp((int)Math.Round(errorValues[x, y].R)),
                                                    Calculations.colorClamp((int)Math.Round(errorValues[x, y].G)),
                                                    Calculations.colorClamp((int)Math.Round(errorValues[x, y].B)));
                }
            }
            return rawColor;
        }
        public static int colorClamp(int input)
        {
            if (input > 255) return 255;
            if (input < 0) return 0;
            return input;
        }
        public static float normError(float inputValue)
        {

            if (inputValue > 170)
            {
                return inputValue - 255;
            }
            else if (inputValue < 85)
            {
                return inputValue;
            }
            else
            {
                if (inputValue <= 128)
                {
                    return inputValue - 128;
                }
                else if (inputValue <= 170)
                {
                    return 128 - inputValue;
                }
                else
                {
                    return inputValue;
                }
            }

        }
    }
}