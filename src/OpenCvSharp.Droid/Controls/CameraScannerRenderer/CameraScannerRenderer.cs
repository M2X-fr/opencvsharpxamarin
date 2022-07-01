using System;
using System.ComponentModel;
using AndroidX.Fragment.App;
//using Android.Support.V4.App;
using Android.Content;
using Android.Views;
using Android.Widget;
using Xamarin.Forms;
using Xamarin.Forms.Platform.Android;
using Xamarin.Forms.Platform.Android.FastRenderers;
using droid = Android;
using Android.App;
using AndroidX.ConstraintLayout.Widget;
using AndroidX.Camera.Lifecycle;
using Plugin.CurrentActivity;
using AndroidX.Camera.Core;
using AndroidX.Camera.View;
using AndroidX.Core.Content;
using static AndroidX.Camera.Core.ImageCapture;
using Android.Graphics;
using OpenCvSharp.Droid.Controls.CameraScannerRenderer;
using OpenCvSharp.XamarinForms.Controls;
using OpenCvSharp.Droid;
using Android.Runtime;
using OpenCvSharp.XamarinForms.Services;
using OpenCvSharp.Droid.Services;
using static Android.Graphics.Bitmap;
using Android.Content.Res;
using System.IO;
using static Android.Hardware.Display.DisplayManager;
using Android.Hardware.Display;
using Android.Hardware;

[assembly: ExportRenderer(typeof(CameraScanner), typeof(CameraScannerRenderer))]
namespace OpenCvSharp.Droid.Controls.CameraScannerRenderer
{
    class MyOrientationListner : Java.Lang.Object, ISensorEventListener
    {
        public event EventHandler OrientationChanged;

        public void OnAccuracyChanged(Sensor sensor, SensorStatus accuracy)
        {
        }

        public void OnSensorChanged(SensorEvent e)
        {
            OrientationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    public class CameraScannerRenderer : ViewRenderer, ISurfaceHolderCallback
    {
        private static Type _resources = null;
        public static void InitializeRender(Type resourceType)
        {
            _resources = resourceType;
        }

        private static float _density = float.MinValue;

        ConstraintLayout _layout = null;

        OpenCvSharp.XamarinForms.Controls.CameraScanner _element = null;

        ProcessCameraProvider cameraProvider;

        ICamera _cam;
        ArucoAnalyzer _arucoAnalyzer;

        ImageCapture imageCapture;
        Java.Util.Concurrent.IExecutorService cameraExecutor;

        PreviewView viewFinder;
        AndroidX.Camera.Core.Preview preview;

        ImageView overlay;

        OrientationEventListener _camOrientation;

        bool _gererOrientation = false;
        string defaultRatio;

        SensorManager _sensorManager;
        MyOrientationListner _listener;

        public CameraScannerRenderer(Context ctx) : base(ctx)
        {

        }

        protected override void OnElementChanged(ElementChangedEventArgs<Xamarin.Forms.View> e)
        {
            base.OnElementChanged(e);

            _element = e.NewElement as OpenCvSharp.XamarinForms.Controls.CameraScanner;
            _element.SnapshotRequested += _element_SnapshotRequested;

            _density = Resources.DisplayMetrics.Density;

            if (Control == null)
            {
                var test = this.Context as ContextThemeWrapper;
                if (test != null)
                {
                    var activity = (test as Activity);
                    if (activity != null)
                        _layout = activity.LayoutInflater.Inflate(Resource.Layout.cameraScanner, this, false) as ConstraintLayout;
                    else
                        _layout = (test.BaseContext as Activity).LayoutInflater.Inflate(Resource.Layout.cameraScanner, this, false) as ConstraintLayout;
                }
                else
                    _layout = (this.Context as Activity).LayoutInflater.Inflate(Resource.Layout.cameraScanner, this, false) as ConstraintLayout;

                _sensorManager = CrossCurrentActivity.Current.Activity.GetSystemService(Context.SensorService) as SensorManager;

                var sensor = _sensorManager.GetDefaultSensor(SensorType.Orientation);
                _listener = new MyOrientationListner();
                _listener.OrientationChanged += OnOrientationChanged;
                _sensorManager.RegisterListener(_listener, sensor, SensorDelay.Normal);


                viewFinder = _layout.GetChildAt(0) as PreviewView;
                defaultRatio = (viewFinder.LayoutParameters as ConstraintLayout.LayoutParams).DimensionRatio;

                overlay = _layout.GetChildAt(1) as ImageView;

                SetNativeControl(_layout);


                StartCamera();

                cameraExecutor = Java.Util.Concurrent.Executors.NewSingleThreadExecutor();
            }

            if (e.OldElement != null)
            {
                if (cameraProvider != null)
                    cameraProvider.UnbindAll();
            }
        }

        private void OnOrientationChanged(object sender, EventArgs e)
        {
            //var test = (viewFinder.LayoutParameters as ConstraintLayout.LayoutParams).DimensionRatio;
            //if (_layout.Display != null && _layout.Display.Rotation == SurfaceOrientation.Rotation0 )
            //    (viewFinder.LayoutParameters as ConstraintLayout.LayoutParams).DimensionRatio = "3:4";
            //else
            //    (viewFinder.LayoutParameters as ConstraintLayout.LayoutParams).DimensionRatio = "4:3";
        }

        private void _element_SnapshotRequested(object sender, EventArgs e)
        {
            if (imageCapture != null)
            {
                TakePhoto();
            }
        }

        private void StartCamera()
        {
            var cameraProviderFuture = ProcessCameraProvider.GetInstance(CrossCurrentActivity.Current.Activity);

            cameraProviderFuture.AddListener(new Java.Lang.Runnable(() =>
            {
                // Used to bind the lifecycle of cameras to the lifecycle owner
                cameraProvider = (ProcessCameraProvider)cameraProviderFuture.Get();

                // Preview
                preview = new AndroidX.Camera.Core.Preview.Builder()
                    .SetTargetAspectRatio(AspectRatio.Ratio43)
                    .Build();
                //viewFinder.SetScaleType(PreviewView.ScaleType.FillCenter);
                preview.SetSurfaceProvider(viewFinder.SurfaceProvider);

                // Take Photo
                this.imageCapture = new ImageCapture.Builder()
                    .SetTargetAspectRatio(AspectRatio.Ratio43)
                    .Build();

                /*if (_gererOrientation)
                    (viewFinder.LayoutParameters as ConstraintLayout.LayoutParams).DimensionRatio = "4:3";
                else
                    (viewFinder.LayoutParameters as ConstraintLayout.LayoutParams).DimensionRatio = defaultRatio;*/

                _camOrientation = new CameraOrientationEventListener(Plugin.CurrentActivity.CrossCurrentActivity.Current.Activity, this.imageCapture);
                _camOrientation.Enable();
                if (_arucoAnalyzer == null)
                {
                    _arucoAnalyzer = new ArucoAnalyzer(image =>
                    {
                        DrawFocusRect(image);

                    }, _element.OnSnapshotReady);

                }
                var imageAnalyzerBarcode = new ImageAnalysis.Builder()
                        .SetTargetAspectRatio(AspectRatio.Ratio43)
                       .SetBackpressureStrategy(ImageAnalysis.StrategyKeepOnlyLatest)
                       .Build();
                imageAnalyzerBarcode.SetAnalyzer(cameraExecutor, _arucoAnalyzer);

                // Select back camera as a default, or front camera otherwise
                CameraSelector cameraSelector = null;
                if (cameraProvider.HasCamera(CameraSelector.DefaultBackCamera) == true)
                    cameraSelector = CameraSelector.DefaultBackCamera;
                else if (cameraProvider.HasCamera(CameraSelector.DefaultFrontCamera) == true)
                    cameraSelector = CameraSelector.DefaultFrontCamera;
                else
                    throw new System.Exception("Camera not found");

                try
                {
                    // Unbind use cases before rebinding
                    cameraProvider.UnbindAll();

                    // Bind use cases to camera
                    _cam = cameraProvider.BindToLifecycle(CrossCurrentActivity.Current.Activity as global::Xamarin.Forms.Platform.Android.FormsAppCompatActivity,
                        cameraSelector, preview, imageAnalyzerBarcode, imageCapture);

                    //var pinchListener = new CameraPinchGesture(_cam.CameraInfo, _cam.CameraControl);
                    // var tapListener = new CameraTapGestureDetector(viewFinder, _cam.CameraControl, cameraSelector, _cam.CameraInfo);
                    //var scaleGestureDetector = new ScaleGestureDetector(viewFinder.Context, pinchListener);
                    //var tapGestureDetector = new GestureDetector(viewFinder.Context, tapListener);
                    //viewFinder.SetOnTouchListener(new CameraTouchListener(scaleGestureDetector, tapGestureDetector));
                }
                catch (Exception exc)
                {

                }

            }), ContextCompat.GetMainExecutor(CrossCurrentActivity.Current.Activity)); //GetMainExecutor: returns an Executor that runs on the main thread.
        }

        private void DrawFocusRect(Bitmap image)
        {

            Device.BeginInvokeOnMainThread(() =>
            {
                if(viewFinder.Display != null)
                {
                    var test = viewFinder.Display.Rotation;
                    overlay.SetImageBitmap(image);
                }

            });

        }



        private void TakePhoto()
        {
            byte[] bytes;
            using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
            {
                viewFinder.Bitmap.Compress(Bitmap.CompressFormat.Jpeg, 100, ms);
                bytes = ms.ToArray();
            }

            _element.OnSnapshotReady(bytes);

            return;

            // Get a stable reference of the modifiable image capture use case   
            var imageCapture = this.imageCapture;
            if (imageCapture == null)
                return;

            /*Display d = CrossCurrentActivity.Current.Activity.displ();
            if (d != null)
            {
                iCapture.setTargetRotation(d.getRotation());
            }*/

            // Set up image capture listener, which is triggered after photo has been taken
            imageCapture.TakePicture(ContextCompat.GetMainExecutor(CrossCurrentActivity.Current.Activity), new ImageSaveCallback(_element));
        }

        protected override void Dispose(bool disposing)
        {
            if (cameraProvider != null)
                cameraProvider.UnbindAll();

            if (cameraExecutor != null)
                cameraExecutor.Shutdown();

            if (_camOrientation != null)
                _camOrientation.Disable();

            if (_element != null)
                _element.SnapshotRequested -= _element_SnapshotRequested;

            if (_listener != null)
            {
                _listener.OrientationChanged -= OnOrientationChanged;
                _listener.Dispose();
            }

            if (_sensorManager != null)
            {
                _sensorManager.UnregisterListener(_listener);
                _sensorManager.Dispose();
            }

            base.Dispose(disposing);
        }

        public void SurfaceChanged(ISurfaceHolder holder, [GeneratedEnum] Format format, int width, int height)
        {

        }

        public void SurfaceCreated(ISurfaceHolder holder)
        {

        }

        public void SurfaceDestroyed(ISurfaceHolder holder)
        {

        }
    }

    public class CameraPinchGesture : ScaleGestureDetector.SimpleOnScaleGestureListener
    {
        ICameraInfo _cameraInfo;
        ICameraControl _cameraControl;
        public CameraPinchGesture(ICameraInfo cameraInfo, ICameraControl cameraControl)
        {
            _cameraInfo = cameraInfo;
            _cameraControl = cameraControl;
        }

        public override bool OnScale(ScaleGestureDetector detector)
        {
            // Get the camera's current zoom ratio
            var zs = _cameraInfo.ZoomState?.Value as IZoomState;

            var currentZoomRatio = zs != null ? zs.ZoomRatio : 0f;

            // Get the pinch gesture's scaling factor 
            var delta = detector.ScaleFactor;

            // Update the camera's zoom ratio. This is an asynchronous operation that returns
            // a ListenableFuture, allowing you to listen to when the operation completes.
            _cameraControl.SetZoomRatio(currentZoomRatio * delta);

            // Return true, as the event was handled
            return true;
        }


    }

    public class CameraTapGestureDetector : GestureDetector.SimpleOnGestureListener
    {
        PreviewView _preview;
        ICameraControl _cameraControl;
        CameraSelector _cameraSelector;
        ICameraInfo _cameraInfo;
        public CameraTapGestureDetector(PreviewView preview, ICameraControl cameraControl, CameraSelector cameraSelector, ICameraInfo cameraInfo)
        {
            _cameraInfo = cameraInfo;
            _preview = preview;
            _cameraControl = cameraControl;
            _cameraSelector = cameraSelector;
        }

        public override bool OnSingleTapConfirmed(MotionEvent e)
        {
            // Get the MeteringPointFactory from PreviewView
            var factory = _preview.MeteringPointFactory;// CreateMeteringPointFactory(_cameraSelector);

            // Create a MeteringPoint from the tap coordinates
            var point = factory.CreatePoint(e.GetX(), e.GetY());

            // Create a MeteringAction from the MeteringPoint, you can configure it to specify the metering mode
            var action = new FocusMeteringAction.Builder(point).Build();

            // Trigger the focus and metering. The method returns a ListenableFuture since the operation
            // is asynchronous. You can use it get notified when the focus is successful or if it fails.
            _cameraControl.StartFocusAndMetering(action);

            return true;
        }

        public override bool OnDoubleTap(MotionEvent e)
        {
            // Get the camera's current zoom ratio
            var zs = _cameraInfo.ZoomState?.Value as IZoomState;

            var currentZoomRatio = zs != null ? zs.ZoomRatio : 0f;

            // Update the camera's zoom ratio. This is an asynchronous operation that returns
            // a ListenableFuture, allowing you to listen to when the operation completes.
            _cameraControl.SetLinearZoom(currentZoomRatio > 1f ? 0f : 1f);

            // Return true, as the event was handled
            return true;
        }
    }

    public class CameraTouchListener : Java.Lang.Object, droid.Views.View.IOnTouchListener
    {
        ScaleGestureDetector _pinchGesture;
        GestureDetector _cameraTapGestureDetector;
        public CameraTouchListener(ScaleGestureDetector pinchGesture, GestureDetector cameraTapGestureDetector)
        {
            _pinchGesture = pinchGesture;
            _cameraTapGestureDetector = cameraTapGestureDetector;
        }
        public bool OnTouch(droid.Views.View v, MotionEvent e)
        {
            _cameraTapGestureDetector.OnTouchEvent(e);
            _pinchGesture.OnTouchEvent(e);



            return true;
        }
    }

    public class CameraOrientationEventListener : OrientationEventListener
    {
        ImageCapture _imageCapture;

        public CameraOrientationEventListener(Context ctx, ImageCapture imageCapture) : base(ctx)
        {
            _imageCapture = imageCapture;
        }

        public override void OnOrientationChanged(int orientation)
        {
            // Monitors orientation values to determine the target rotation value
            int rotation = 0;

            if (orientation >= 45 && orientation < 135)
            {
                rotation = (int)SurfaceOrientation.Rotation270;
            }
            else if (orientation >= 135 && orientation < 225)
            {
                rotation = (int)SurfaceOrientation.Rotation180;
            }
            else if (orientation >= 225 && orientation < 315)
            {
                rotation = (int)SurfaceOrientation.Rotation90;
            }
            else
            {
                rotation = (int)SurfaceOrientation.Rotation0;
            }

            _imageCapture.TargetRotation = rotation;
        }
    }

    class ImageSaveCallback : ImageCapture.OnImageCapturedCallback
    {
        CameraScanner _element;

        public ImageSaveCallback(CameraScanner element)
        {
            _element = element;
        }

        public override void OnCaptureSuccess(IImageProxy image)
        {
            try
            {
                var buffer = image.GetPlanes()[0].Buffer; // We get the luminance plane only, since we
                                                          // want to binarize it and we don't wanna take color into consideration.
                var bytes = new byte[buffer.Capacity()];
                buffer.Get(bytes);

                //if(image.ImageInfo.RotationDegrees != 0)
                {
                    var b = BitmapFactory.DecodeByteArray(bytes, 0, bytes.Length);
                    Matrix m = new Matrix();
                    m.SetRotate(image.ImageInfo.RotationDegrees);
                    Bitmap rotatedBitmap = Bitmap.CreateBitmap(b, image.CropRect.Left, image.CropRect.Top, image.CropRect.Width(), image.CropRect.Height(), m, true);
                    using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                    {
                        rotatedBitmap.Compress(Bitmap.CompressFormat.Jpeg, 100, ms);
                        bytes = ms.ToArray();
                    }
                    rotatedBitmap.Dispose();
                    b.Dispose();
                }

                _element.OnSnapshotReady(bytes);
            }
            catch (Exception exception)
            {
                droid.Util.Log.Debug("KeepControl", "ImageSaveCallback error : " + exception.Message);
            }

            image.Close();
        }

        public override void OnError(ImageCaptureException exception)
        {
            droid.Util.Log.Debug("KeepControl", "ImageSaveCallback error : " + exception.Message);
        }
    }
}