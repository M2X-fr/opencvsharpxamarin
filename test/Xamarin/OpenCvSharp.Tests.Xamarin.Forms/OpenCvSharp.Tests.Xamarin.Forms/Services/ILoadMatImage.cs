using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace OpenCvSharp.Tests.Xamarin.Forms.Services
{
    public interface ILoadMatImage
    {
        Mat LoadMatFromResource(string path);

        ImageSource GetImageSourceFromMat(Mat image, int width, int height);

        ImageSource LoadImageFromResource(string path);
    }
}
