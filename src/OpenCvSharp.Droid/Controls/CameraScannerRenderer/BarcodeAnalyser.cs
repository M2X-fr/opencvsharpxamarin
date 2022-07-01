using Android.App;
using Android.Content;
using Android.Content.Res;
using Android.Graphics;
using Android.OS;
using Android.Runtime;
using Android.Views;
using Android.Widget;
using AndroidX.Camera.Core;
using AndroidX.Camera.Core.Internal.Utils;
using OpenCvSharp.Aruco;
using OpenCvSharp.Droid.Services;
using OpenCvSharp.XamarinForms.Controls;
using OpenCvSharp.XamarinForms.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
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
        private readonly Action<ArucoData[]> OnSnapshotReady;
        public float Zoom { get; set; }
        private readonly Queue<QueueItem<ArucoData[]>> _workerQueue = new Queue<QueueItem<ArucoData[]>>();

        private List<(ArucoData data, DateTime time)> tempArucoData = new List<(ArucoData, DateTime)>();

        public ArucoAnalyzer(Action<Bitmap> callback, Action<ArucoData[]> onSnapshotReady = null) //LumaListener listener)
        {
            this.overlayAction = callback;
            this.OnSnapshotReady = onSnapshotReady;
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
                var data = ImageUtil.ImageToJpegByteArray(imageProxy);
                var imageBitmap = BitmapFactory.DecodeByteArray(data, 0, data.Length);
                imageBitmap = RotateBitmap(imageBitmap, 90);

                Mat rgba = new Mat();
                LoadMatImage.BitmapToMat(imageBitmap, rgba);

                Mat rgb = new Mat();
                rgb = rgba.CvtColor(ColorConversionCodes.RGBA2RGB);

                Mat gray = new Mat();
                using var outputImage = rgb;//.CvtColor(ColorConversionCodes.RGBA2GRAY);

                using var dict = CvAruco.GetPredefinedDictionary(PredefinedDictionaryName.Dict4X4_100);

                var param = DetectorParameters.Create();
                param.MinCornerDistanceRate = 0.11;

                CvAruco.DetectMarkers(outputImage, dict, out var corners, out var ids, param, out var rejectedImgPoints);

                if (corners.Length > 0)
                {
                    ArucoData[] t = new ArucoData[corners.Length];
                    /// Pour chacun des points déterminer l'orientation
                    /// 

                    for (int i = 0; i < corners.Length; i++)
                    {
                        var item = corners[i];
                        int orient = GetPointOrientation(item);
                        t[i] = new ArucoData() { Id = ids[i], Angle = orient };
                    }

                    QueuedBackgroundWorker.QueueWorkItem<ArucoData[], ArucoData[]>(
                        _workerQueue,
                        t,
                        a =>
                        {
                            OnSnapshotReady(a.Argument);
                            return a.Argument;
                        },
                        a =>
                        {

                        });
                }

                CvAruco.DrawDetectedMarkers(rgb, corners, ids, new Scalar(255, 0, 0));

                Bitmap bitmap = Bitmap.CreateBitmap(rgb.Width, rgb.Height, Config.Argb8888);
                LoadMatImage.MatToBitmap(outputImage, bitmap);

                overlayAction?.Invoke(bitmap);
            }
            catch (Exception ex)
            {

            }

            imageProxy.Close();
        }

        private int GetPointOrientation(Point2f[] item)
        {
            int result = 0;

            var dx = item[0].X - item[2].X;
            var dy = item[0].Y - item[2].Y;

            if (dx > 0)
            {
                if (dy > 0)
                {
                    return 180;
                }
                else if (dy < 0)
                {
                    return 270;
                }
            }
            else if (dx < 0)
            {
                if (dy > 0)
                {
                    return 90;
                }
                else if (dy < 0)
                {
                    return 0;
                }
            }
            {

            }

            return result;
        }
    }

    public static class QueuedBackgroundWorker
    {
        public static void QueueWorkItem<Tin, Tout>(
            Queue<QueueItem<Tin>> queue,
            Tin inputArgument,
            Func<DoWorkArgument<Tin>, Tout> doWork,
            Action<WorkerResult<Tout>> workerCompleted)
        {
            if (queue == null) throw new ArgumentNullException("queue");

            BackgroundWorker bw = new BackgroundWorker();
            bw.WorkerReportsProgress = false;
            bw.WorkerSupportsCancellation = false;
            bw.DoWork += (sender, args) =>
            {
                if (doWork != null)
                {
                    args.Result = doWork(new DoWorkArgument<Tin>((Tin)args.Argument));
                }
            };
            bw.RunWorkerCompleted += (sender, args) =>
            {
                if (workerCompleted != null)
                {
                    workerCompleted(new WorkerResult<Tout>((Tout)args.Result, args.Error));
                }
                queue.Dequeue();
                if (queue.Count > 0)
                {
                    QueueItem<Tin> nextItem = queue.Peek(); // as QueueItem<T>;
                    nextItem.BackgroundWorker.RunWorkerAsync(nextItem.Argument);
                }
            };

            queue.Enqueue(new QueueItem<Tin>(bw, inputArgument));
            if (queue.Count == 1)
            {
                QueueItem<Tin> nextItem = queue.Peek() as QueueItem<Tin>;
                nextItem.BackgroundWorker.RunWorkerAsync(nextItem.Argument);
            }
        }
    }

    public class DoWorkArgument<T>
    {
        public DoWorkArgument(T argument)
        {
            this.Argument = argument;
        }

        public T Argument { get; private set; }
    }

    public class WorkerResult<T>
    {
        public WorkerResult(T result, Exception error)
        {
            this.Result = result;
            this.Error = error;
        }

        public T Result { get; private set; }

        public Exception Error { get; private set; }
    }

    public class QueueItem<T>
    {
        public QueueItem(BackgroundWorker backgroundWorker, T argument)
        {
            this.BackgroundWorker = backgroundWorker;
            this.Argument = argument;
        }

        public T Argument { get; private set; }

        public BackgroundWorker BackgroundWorker { get; private set; }
    }
}