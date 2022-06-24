using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace OpenCvSharp.XamarinForms.Services
{
    public interface ILoadMatImage
    {
        Mat LoadMatFromResource(string path);
        Mat LoadMatFromData(byte[] data);

        ImageSource GetImageSourceFromMat(Mat image, int width, int height);

        ImageSource LoadImageFromResource(string path);
    }
}
