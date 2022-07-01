using System;

using Android.App;
using Android.Content.PM;
using Android.Runtime;
using Android.OS;
using Android.Graphics;
using xam = Xamarin;

namespace OpenCvSharp.Tests.Xamarin.Forms.Droid
{
    [Activity(Label = "OpenCvSharp.Tests.Xamarin.Forms", Icon = "@mipmap/icon", Theme = "@style/MainTheme", MainLauncher = true, ConfigurationChanges = ConfigChanges.ScreenSize | ConfigChanges.Orientation | ConfigChanges.UiMode | ConfigChanges.ScreenLayout | ConfigChanges.SmallestScreenSize )]
    public class MainActivity : global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity
    {
        protected override void OnCreate(Bundle savedInstanceState)
        {
            base.OnCreate(savedInstanceState);

            Plugin.CurrentActivity.CrossCurrentActivity.Current.Init(this,savedInstanceState);
            xam.Essentials.Platform.Init(this, savedInstanceState);
            global::Xamarin.Forms.Forms.Init(this, savedInstanceState);
            LoadApplication(new App());
        }
        public override void OnRequestPermissionsResult(int requestCode, string[] permissions, [GeneratedEnum] Android.Content.PM.Permission[] grantResults)
        {
            xam.Essentials.Platform.OnRequestPermissionsResult(requestCode, permissions, grantResults);

            base.OnRequestPermissionsResult(requestCode, permissions, grantResults);
        }
        /*
        // Method adapted from the original OpenCV convert at https://github.com/opencv/opencv/blob/b39cd06249213220e802bb64260727711d9fc98c/modules/java/generator/src/cpp/utils.cpp
        ///<summary>
        ///This function converts an image in the OpenCV Mat representation to the Android Bitmap.
        ///The input Mat object has to be of the types 'CV_8UC1' (gray-scale), 'CV_8UC3' (RGB) or 'CV_8UC4' (RGBA).
        ///The output Bitmap object has to be of the same size as the input Mat and of the types 'ARGB_8888' or 'RGB_565'.
        ///This function throws an exception if the conversion fails.
        ///</summary>
        ///<param name="srcImage">srcImage is a valid input Mat object of types 'CV_8UC1', 'CV_8UC3' or 'CV_8UC4'</param>
        ///<param name="dstImage">dstImage is a valid Bitmap object of the same size as the Mat and of type 'ARGB_8888' or 'RGB_565'</param>
        ///<param name="needPremultiplyAlpha">premultiplyAlpha is a flag, that determines, whether the Mat needs to be converted to alpha premultiplied format (like Android keeps 'ARGB_8888' bitmaps); the flag is ignored for 'RGB_565' bitmaps.</param>
        public static void MatToBitmap(Mat srcImage, Bitmap dstImage, bool needPremultiplyAlpha = false)
        {
            var bitmapInfo = dstImage.GetBitmapInfo();
            var bitmapPixels = dstImage.LockPixels();

            if (bitmapInfo.Format != Format.Rgba8888 && bitmapInfo.Format != Format.Rgb565)
                throw new Exception("Invalid Bitmap Format: It is of format " + bitmapInfo.Format.ToString() + " and is expected Rgba8888 or Rgb565");
            if (srcImage.Dims != 2)
                throw new Exception("The source image has " + srcImage.Dims + " dimensions, while it is expected 2");
            if (srcImage.Cols != bitmapInfo.Width || srcImage.Rows != bitmapInfo.Height)
                throw new Exception("The source image and the output Bitmap don't have the same amount of rows and columns");
            if (srcImage.Type() != MatType.CV_8UC1 && srcImage.Type() != MatType.CV_8UC3 && srcImage.Type() != MatType.CV_8UC4)
                throw new Exception("The source image has the type " + srcImage.Type().ToString() + ", while it is expected CV_8UC1, CV_8UC3 or CV_8UC4");
            if (bitmapPixels == null)
                throw new Exception("Can't lock the output bitmap");

            if (bitmapInfo.Format == Format.Rgba8888)
            {
                Mat tmp = new Mat((int)bitmapInfo.Height, (int)bitmapInfo.Width, MatType.CV_8UC4, bitmapPixels);
                if (srcImage.Type() == MatType.CV_8UC1)
                {
                    Android.Util.Log.Info("MatToBitmap", "CV_8UC1 -> RGBA_8888");
                    Cv2.CvtColor(srcImage, tmp, ColorConversionCodes.GRAY2RGBA);
                }
                else if (srcImage.Type() == MatType.CV_8UC3)
                {
                    Android.Util.Log.Info("MatToBitmap", "CV_8UC3 -> RGBA_8888");
                    Cv2.CvtColor(srcImage, tmp, ColorConversionCodes.RGB2RGBA);
                }
                else if (srcImage.Type() == MatType.CV_8UC4)
                {
                    Android.Util.Log.Info("MatToBitmap", "CV_8UC4 -> RGBA_8888");
                    if (needPremultiplyAlpha)
                        Cv2.CvtColor(srcImage, tmp, ColorConversionCodes.RGBA2mRGBA);
                    else
                        srcImage.CopyTo(tmp);
                }
            }
            else
            {
                // info.format == ANDROID_BITMAP_FORMAT_RGB_565
                Mat tmp = new Mat((int)bitmapInfo.Height, (int)bitmapInfo.Width, MatType.CV_8UC2, bitmapPixels);
                if (srcImage.Type() == MatType.CV_8UC1)
                {
                    Android.Util.Log.Info("MatToBitmap", "CV_8UC1 -> RGB_565");
                    Cv2.CvtColor(srcImage, tmp, ColorConversionCodes.GRAY2BGR565);
                }
                else if (srcImage.Type() == MatType.CV_8UC3)
                {
                    Android.Util.Log.Info("MatToBitmap", "CV_8UC3 -> RGB_565");
                    Cv2.CvtColor(srcImage, tmp, ColorConversionCodes.RGB2BGR565);
                }
                else if (srcImage.Type() == MatType.CV_8UC4)
                {
                    Android.Util.Log.Info("MatToBitmap", "CV_8UC4 -> RGB_565");
                    Cv2.CvtColor(srcImage, tmp, ColorConversionCodes.RGBA2BGR565);
                }
            }
            dstImage.UnlockPixels();
            return;
        }

        // Method adapted from the original OpenCV convert at https://github.com/opencv/opencv/blob/b39cd06249213220e802bb64260727711d9fc98c/modules/java/generator/src/cpp/utils.cpp
        ///<summary>
        ///This function converts an Android Bitmap image to the OpenCV Mat.
        ///'ARGB_8888' and 'RGB_565' input Bitmap formats are supported.
        ///The output Mat is always created of the same size as the input Bitmap and of the 'CV_8UC4' type,it keeps the image in RGBA format.
        ///This function throws an exception if the conversion fails.
        ///</summary>
        ///<param name="srcImage">srcImage is a valid input Bitmap object of the type 'ARGB_8888' or 'RGB_565'</param>
        ///<param name="dstImage">dstImage is a valid output Mat object, it will be reallocated if needed, so it may be empty.</param>
        ///<param name="needUnPremultiplyAlpha">unPremultiplyAlpha is a flag, that determines, whether the bitmap needs to be converted from alpha premultiplied format (like Android keeps 'ARGB_8888' ones) to regular one; this flag is ignored for 'RGB_565' bitmaps.</param>
        public static void BitmapToMat(Bitmap srcImage, Mat dstImage, bool needUnPremultiplyAlpha = false)
        {
            var bitmapInfo = srcImage.GetBitmapInfo();
            var bitmapPixels = srcImage.LockPixels();

            if (bitmapInfo.Format != Format.Rgba8888 && bitmapInfo.Format != Format.Rgb565)
                throw new Exception("Invalid Bitmap Format: It is of format " + bitmapInfo.Format.ToString() + " and is expected Rgba8888 or Rgb565");
            if (bitmapPixels == null)
                throw new Exception("Can't lock the source bitmap");

            dstImage.Create((int)bitmapInfo.Height, (int)bitmapInfo.Width, MatType.CV_8UC4);

            if (bitmapInfo.Format == Format.Rgba8888)
            {
                Android.Util.Log.Info("nBitmapToMat", "RGBA_8888 -> CV_8UC4");
                Mat tmp = new Mat((int)bitmapInfo.Height, (int)bitmapInfo.Width, MatType.CV_8UC4, bitmapPixels);
                if (needUnPremultiplyAlpha)
                    Cv2.CvtColor(tmp, dstImage, ColorConversionCodes.mRGBA2RGBA);
                else
                    tmp.CopyTo(dstImage);
            }
            else
            {
                // info.format == ANDROID_BITMAP_FORMAT_RGB_565
                Android.Util.Log.Info("nBitmapToMat", "RGB_565 -> CV_8UC4");
                Mat tmp = new Mat((int)bitmapInfo.Height, (int)bitmapInfo.Width, MatType.CV_8UC2, bitmapPixels);
                Cv2.CvtColor(tmp, dstImage, ColorConversionCodes.BGR5652RGBA);
            }

            srcImage.UnlockPixels();
            return;
        }*/
    }

    
}