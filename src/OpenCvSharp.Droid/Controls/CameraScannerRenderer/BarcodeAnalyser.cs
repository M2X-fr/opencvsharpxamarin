using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Camera.Core;
using OpenCvSharp.Aruco;
using OpenCvSharp.Droid.Services;
using OpenCvSharp.XamarinForms.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xamarin.Forms;
using static Android.Graphics.Bitmap;
using droid = Android;

namespace OpenCvSharp.Droid.Controls.CameraScannerRenderer
{

    public class ArucoAnalyzer : Java.Lang.Object, ImageAnalysis.IAnalyzer
    {
        private const string TAG = "OpenCvSharp.Droid";
        private readonly Action<Bitmap> overlayAction;
        private readonly Func<string,string> qrCheckHandler;

        public float Zoom { get; set; }

        public ArucoAnalyzer(Action<Bitmap> callback, Func<string, string> qrCheckHandler = null) //LumaListener listener)
        {
            this.overlayAction = callback;
            this.qrCheckHandler = qrCheckHandler;
        }
        public static Bitmap RotateBitmap(Bitmap source, float angle)
        {
            Matrix matrix = new Matrix();
            matrix.PostRotate(angle);
            return Bitmap.CreateBitmap(source, 0, 0, source.Width, source.Height, matrix, true);
        }
        public void Analyze(IImageProxy imageProxy)
        {

            try
            {

                var buffer = imageProxy.GetPlanes()[0].Buffer; // We get the luminance plane only, since we
                                                          // want to binarize it and we don't wanna take color into consideration.
                var bytes = new byte[buffer.Capacity()];
                buffer.Get(bytes);

                var d = yuv420toNV21(imageProxy.Image);
                var b = RotateBitmap(getBitmap(d, imageProxy), 90);

                using var imagetmp = new Mat();

                LoadMatImage.BitmapToMat(b, imagetmp);
                using var image = imagetmp.CvtColor(ColorConversionCodes.RGBA2GRAY);
                using var outputImage = new Mat(new Size(image.Width, image.Height), MatType.CV_8UC4,new Scalar(0,0,0,0));// Mat.Zeros(new Size(image.Width, image.Height), MatType.CV_8UC4);// new Mat(new Size(image.Width,image.Height),MatType.CV_8UC4);
                 //using var outputImage = image.CvtColor(ColorConversionCodes.GRAY2RGB);;
                using var dict = CvAruco.GetPredefinedDictionary(PredefinedDictionaryName.Dict6X6_250);

                var param = DetectorParameters.Create();
                CvAruco.DetectMarkers(image, dict, out var corners, out var ids, param, out var rejectedImgPoints);

                var color = new Scalar(255, 255, 0, 255);

                // Draw markers custom
                for (var i = 0; i < corners.Length; ++i)
                {

                    // Draw contour
                    for (var j = 0; j < corners[i].Length; ++j)
                    {
                        var next = (j + 1) % corners[i].Length;
                        outputImage.Line( corners[i][j].ToPoint(), corners[i][next].ToPoint(), color, 2);
                            
                    }
                }

                if(corners.Length > 0)
                {
                    outputImage.Rectangle(new Rect((int)corners[0][0].X - 3, (int)corners[0][0].Y - 3, 6, 6), new Scalar(255, 0, 0, 255), 2);
                }


                //CvAruco.DrawDetectedMarkers(outputImage, corners, ids, new Scalar(255, 0, 0));

                Bitmap bitmap = Bitmap.CreateBitmap(image.Width, image.Height, Config.Argb8888);
                LoadMatImage.MatToBitmap(outputImage, bitmap);

               /* AssetManager assets = Plugin.CurrentActivity.CrossCurrentActivity.Current.AppContext.Assets;// Xam.Forms.Forms.Context.Assets;
                Bitmap bitmap = null;
                using (Stream sr = assets.Open(DateTime.Now.Second % 10 > 5 ? "markers_6x6_250.png" : "aruco_markers_photo.jpg"))
                {
                    bitmap = BitmapFactory.DecodeStream(sr);
                }*/

                overlayAction?.Invoke(bitmap);
                
            }
            catch (Exception ex)
            {

            }


            imageProxy.Close();

        }

        private byte[] yuv420toNV21(droid.Media.Image image)
        {
            var crop = image.CropRect;
            var format = image.Format;
            var width = crop.Width();
            var height = crop.Height();
            var planes = image.GetPlanes();
            var data = new byte[(width * height * ImageFormat.GetBitsPerPixel(format) / 8)];
            var rowData = new byte[planes[0].RowStride];
            var channelOffset = 0;
            var outputStride = 1;



            for (int i = 0; i < planes.Length; i++)
            {
                switch (i)
                {
                    case 0:
                        channelOffset = 0;
                        outputStride = 1;
                        break;
                    case 1:
                        channelOffset = width * height + 1;
                        outputStride = 2;
                        break;
                    case 2:
                        channelOffset = width * height;
                        outputStride = 2;
                        break;
                    default:
                        break;
                }
                var buffer = planes[i].Buffer;
                var rowStride = planes[i].RowStride;
                var pixelStride = planes[i].PixelStride;
                var shift = i == 0 ? 0 : 1;
                var w = width >> shift;
                var h = height >> shift;
                buffer.Position(rowStride * (crop.Top >> shift) + pixelStride * (crop.Left >> shift));

                for (int row = 0; row < h; row++)
                {
                    int length;
                    if (pixelStride == 1 && outputStride == 1)
                    {
                        length = w;
                        buffer.Get(data, channelOffset, length);
                        channelOffset += length;
                    }
                    else
                    {
                        length = (w - 1) * pixelStride + 1;
                        buffer.Get(rowData, 0, length);
                        for (int col = 0; col < w; col++)
                        {
                            data[channelOffset] = rowData[col * pixelStride];
                            channelOffset += outputStride;
                        }
                    }
                    if (row < h - 1)
                    {
                        buffer.Position(buffer.Position() + rowStride - length);
                    }
                }
            }
            return data;
        }


        private Bitmap getBitmap(byte[] data, IImageProxy metadata)
        {

            var image = new YuvImage(data, ImageFormatType.Nv21, metadata.Width, metadata.Height, null);
            var stream = new System.IO.MemoryStream();
            image.CompressToJpeg(
                new droid.Graphics.Rect(0, 0, metadata.Width, metadata.Height),
                100,
                stream
            );
            var bmp = BitmapFactory.DecodeByteArray(stream.ToArray(), 0, (int)stream.Length);
            stream.Close();
            return bmp;
        }

        private byte[] getBitmapData(byte[] data, IImageProxy metadata)
        {

            var image = new YuvImage(data, ImageFormatType.Nv21, metadata.Width, metadata.Height, null);
            var stream = new System.IO.MemoryStream();
            image.CompressToJpeg(
                new droid.Graphics.Rect(0, 0, metadata.Width, metadata.Height),
                100,
                stream
            );
            var datar = stream.ToArray();
            stream.Close();
            return datar;
        }
    }
}