namespace Examples.Desktop
{
    using System;
    using System.Drawing;
    using System.Windows;

    internal static class Helper
    {
        /// <summary>
        /// Converts from WPF BitmapSource type to Windows forms (GDI+) Bitmap. 
        /// </summary>
        public static Bitmap ToBitmap(this System.Windows.Media.Imaging.BitmapSource source)
        {
            var bmp = new Bitmap(source.PixelWidth, source.PixelHeight, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            var data = bmp.LockBits(
                new Rectangle(System.Drawing.Point.Empty, bmp.Size),
                System.Drawing.Imaging.ImageLockMode.WriteOnly, System.Drawing.Imaging.PixelFormat.Format32bppPArgb);

            source.CopyPixels(Int32Rect.Empty, data.Scan0, data.Height * data.Stride, data.Stride);

            bmp.UnlockBits(data);

            return bmp;

            /* This code might be "better" for different formats but it requires unsafe code.          
            System.Drawing.Bitmap bitmap = null;

                        int width = source.PixelWidth;
                        int height = source.PixelHeight;
                        int stride = width * ((source.Format.BitsPerPixel + 7) / 8);

                        byte[] bits = new byte[height * stride];

                        source.CopyPixels(bits, stride, 0);

                        unsafe
                        {
                            fixed (byte* pB = bits)
                            {
                                IntPtr ptr = new IntPtr(pB);

                                bitmap = new System.Drawing.Bitmap(width, height, stride, System.Drawing.Imaging.PixelFormat.Format32bppPArgb, ptr);
                            }
                        }

                        return bitmap; */
        }

        [System.Runtime.InteropServices.DllImport("user32.dll")]
        public static extern bool DestroyIcon(IntPtr handle);
    }
}
