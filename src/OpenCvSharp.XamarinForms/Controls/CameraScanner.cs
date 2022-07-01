using System;
using System.Collections.Generic;
using System.Text;
using Xamarin.Forms;

namespace OpenCvSharp.XamarinForms.Controls
{
    public class ArucoData
    {
        public int Id { get; set; }
        public int Angle { get; set; }
    }


    public enum CameraScannerOptions
    {
        Rear,
        Front
    }

    public class IdsFoundedEventArgs : EventArgs    
    {
        public ArucoData[] Data { get; set; }
        public IdsFoundedEventArgs(ArucoData[] data) {
            this.Data = data;
        }
    }

    public class CameraScanner : View
    {
        public event EventHandler<IdsFoundedEventArgs>? IdsFounded;
        public event EventHandler? SnapshotRequested;
        public event EventHandler? SnapshotReady;

        #region options rear/front
        public static readonly BindableProperty CameraProperty = BindableProperty.Create(
            propertyName: "Camera",
            returnType: typeof(CameraScannerOptions),
            declaringType: typeof(CameraScanner),
            defaultValue: CameraScannerOptions.Rear);

        public CameraScannerOptions Camera
        {
            get { return (CameraScannerOptions)GetValue(CameraProperty); }
            set { SetValue(CameraProperty, value); }
        }
        #endregion

        #region snapshot
        public static readonly BindableProperty SnapshotProperty = BindableProperty.Create(
          propertyName: "Snapshot",
          returnType: typeof(byte[]),
          declaringType: typeof(CameraScanner),
          defaultValue: null,
          defaultBindingMode: BindingMode.OneWayToSource);

        public byte[] Snapshot
        {
            get { return (byte[])GetValue(SnapshotProperty); }
            set { SetValue(SnapshotProperty, value); }
        }

        public void RequestSnapshot()
        {
            SnapshotRequested?.Invoke(this, EventArgs.Empty);
        }

        public void OnSnapshotReady(byte[] data)
        {
            Snapshot = data;
            SnapshotReady?.Invoke(this, EventArgs.Empty);
        }

        public void OnSnapshotReady(ArucoData[]data)
        {
            IdsFounded?.Invoke(this, new IdsFoundedEventArgs(data));
        }
        

        #endregion

        #region barcode
        public static readonly BindableProperty BarecodeProperty = BindableProperty.Create(
            propertyName: "Barecode",
            returnType: typeof(string),
            declaringType: typeof(CameraScanner),
            defaultBindingMode:BindingMode.OneWayToSource,            
            defaultValue: null);

        public string Barecode
        {
            get { return (string)GetValue(BarecodeProperty); }
            set { SetValue(BarecodeProperty, value); }
        }
        #endregion

        public Func<string, string>? QrCheckHandler { get; set; } = null;

    }
}
