﻿namespace SqualrCore.Source.Utils
{
    using DataStructures;
    using SqualrCore.Source.Analytics;
    using SqualrCore.Source.Output;
    using SqualrCore.Source.Utils.Extensions;
    using Svg;
    using System;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// Static class for useful image utilities.
    /// </summary>
    public static class ImageUtils
    {
        /// <summary>
        /// Cached bitmap mappings stored by this utility.
        /// </summary>
        private static TTLCache<String, Bitmap> bitmapCache = new TTLCache<String, Bitmap>();

        /// <summary>
        /// Loads an image from the given uri.
        /// </summary>
        /// <param name="uri">The uri specifying from where to load the image.</param>
        /// <returns>The bitmap image loaded from the given uri.</returns>
        public static BitmapImage LoadImage(String uri)
        {
            BitmapImage bitmapImage = new BitmapImage();

            bitmapImage.BeginInit();
            bitmapImage.UriSource = new Uri(uri);
            bitmapImage.EndInit();
            bitmapImage.Freeze();

            return bitmapImage;
        }

        public static void SnapShotPng(UIElement source, String destination, Int32 zoom)
        {
            try
            {
                Double actualHeight = source.RenderSize.Height;
                Double actualWidth = source.RenderSize.Width;

                Double renderHeight = actualHeight * zoom;
                Double renderWidth = actualWidth * zoom;

                RenderTargetBitmap renderTarget = new RenderTargetBitmap((Int32)renderWidth, (Int32)renderHeight, 96, 96, PixelFormats.Pbgra32);
                VisualBrush sourceBrush = new VisualBrush(source);

                DrawingVisual drawingVisual = new DrawingVisual();
                DrawingContext drawingContext = drawingVisual.RenderOpen();

                using (drawingContext)
                {
                    drawingContext.PushTransform(new ScaleTransform(zoom, zoom));
                    drawingContext.DrawRectangle(sourceBrush, null, new Rect(new System.Windows.Point(0, 0), new System.Windows.Point(actualWidth, actualHeight)));
                }

                renderTarget.Render(drawingVisual);

                PngBitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(renderTarget));

                using (FileStream stream = new FileStream(destination, FileMode.Create, FileAccess.Write))
                {
                    encoder.Save(stream);
                }
            }
            catch (Exception ex)
            {
                OutputViewModel.GetInstance().Log(OutputViewModel.LogLevel.Error, "Unable to export image", ex);
                AnalyticsService.GetInstance().SendEvent(AnalyticsService.AnalyticsAction.General, ex);
            }
        }

        /// <summary>
        /// Converts a <see cref="BitmapImage"/> to a <see cref="Bitmap"/>.
        /// </summary>
        /// <param name="bitmapImage">The bitmap image to convert.</param>
        /// <returns>The resulting bitmap.</returns>
        public static Bitmap BitmapImageToBitmap(BitmapImage bitmapImage)
        {
            String uri = bitmapImage?.UriSource?.AbsoluteUri;

            if (ImageUtils.bitmapCache.Contains(uri))
            {
                Bitmap result;

                if (ImageUtils.bitmapCache.TryGetValue(uri, out result))
                {
                    return result;
                }
            }

            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapImage));
                enc.Save(outStream);
                Bitmap bitmap = new Bitmap(outStream);
                bitmap?.MakeTransparent();
                ImageUtils.bitmapCache.Add(uri, bitmap);

                return new Bitmap(bitmap);
            }
        }

        public static BitmapImage BitmapToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, ImageFormat.Png);
                memory.Position = 0;

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
                bitmapImage.Freeze();

                return bitmapImage;
            }
        }

        /// <summary>
        /// Loads an svg image from the given path.
        /// </summary>
        /// <param name="uri">The path specifying from where to load the svg image.</param>
        /// <returns>The bitmap image loaded from the given path.</returns>
        public static Bitmap LoadSvg(String base64, Int32 width, Int32 height, System.Drawing.Color color)
        {
            using (MemoryStream memoryStream = new MemoryStream(Convert.FromBase64String(base64)))
            {
                SvgDocument svgDoc = SvgDocument.Open<SvgDocument>(memoryStream);

                svgDoc.Children.Select(child => child)
                    .Where(child => child.Fill as SvgColourServer != null && (child.Fill as SvgColourServer).Colour == System.Drawing.Color.White)
                    .ForEach(child => child.Fill = new SvgColourServer(color));

                svgDoc.Children.Select(child => child)
                    .Where(child => child.Fill as SvgColourServer != null && (child.Fill as SvgColourServer).Colour == System.Drawing.Color.Black)
                    .ForEach(child => child.Fill = new SvgColourServer(System.Drawing.Color.Transparent));

                svgDoc.Width = width;
                svgDoc.Height = height;

                return new Bitmap(svgDoc.Draw());
            }
        }
    }
    //// End class
}
//// End namespace