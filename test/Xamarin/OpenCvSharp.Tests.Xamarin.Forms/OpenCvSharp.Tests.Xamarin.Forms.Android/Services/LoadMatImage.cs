using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xamarin.Forms;
using static Android.Graphics.Bitmap;
using Xam = Xamarin; 

[assembly: Xamarin.Forms.Dependency(typeof(OpenCvSharp.Tests.Xamarin.Forms.Droid.Services.LoadMatImage))]
namespace OpenCvSharp.Tests.Xamarin.Forms.Droid.Services
{
    public class LoadMatImage : OpenCvSharp.Tests.Xamarin.Forms.Services.ILoadMatImage
    {
        public ImageSource GetImageSourceFromMat(Mat image, int width, int height)
        {
            byte[] data = null;
            Bitmap bitmap = Bitmap.CreateBitmap(width, height, Config.Argb8888);
            OpenCvSharp.Tests.Xamarin.Forms.Droid.MainActivity.MatToBitmap(image, bitmap);
            
            using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
            {
                bitmap.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);

                data = stream.ToArray();
            }
            return ImageSource.FromStream(() => new MemoryStream(data));
        }

        public Mat LoadMatFromResource(string path)
        {
            AssetManager assets = Xam.Forms.Forms.Context.Assets;
            Bitmap bitmap = null;
            var image = new Mat();
            using (Stream sr = assets.Open(path))
            {
                bitmap = BitmapFactory.DecodeStream(sr);
            }

            OpenCvSharp.Tests.Xamarin.Forms.Droid.MainActivity.BitmapToMat(bitmap,image);

            return image;
        }
        public ImageSource LoadImageFromResource(string path)
        {
            AssetManager assets = Xam.Forms.Forms.Context.Assets;
            Bitmap bitmap = null;
            byte[] image;
            using (Stream sr = assets.Open(path))
            {
                bitmap = BitmapFactory.DecodeStream(sr);
            }
            using (System.IO.MemoryStream stream = new System.IO.MemoryStream())
            {
                bitmap.Compress(Bitmap.CompressFormat.Jpeg, 100, stream);

                image = stream.ToArray();
            }

            return ImageSource.FromStream(() => new MemoryStream(image));
        }

    }
}