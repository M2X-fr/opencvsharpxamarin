using OpenCvSharp.Aruco;
using OpenCvSharp.Tests.Xamarin.Forms.Services;
using OpenCvSharp.XamarinForms.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Xamarin.Essentials;
using Xamarin.Forms;

namespace OpenCvSharp.Tests.Xamarin.Forms
{
    public partial class MainPage : ContentPage
    {
        public MainVM datas = new MainVM();

        public MainPage()
        {
            InitializeComponent();

            BindingContext = datas;
        }

        public void MarkerDataNotification(object sender, IdsFoundedEventArgs e)
        {
            this.Dispatcher.BeginInvokeOnMainThread(() =>
            {
                foreach (var item in e.Data)
                {
                    if (!datas.datasTemp.Any(Id => Id.Id == item.Id))
                        datas.datasTemp.Add(new ItemData(item.Id, item.Angle, DateTime.Now));
                    else
                    {
                        var t = datas.datasTemp.FirstOrDefault(i => i.Id == item.Id);
                        t.Date = DateTime.Now;
                        t.Angle = item.Angle;
                    }
                }

                datas.datasTemp = datas.datasTemp.Where(d=>(DateTime.Now - d.Date).TotalMilliseconds < 5 ).ToList();

                foreach (var item in datas.datasTemp)
                {
                    if (!datas.Datas.Any(Id => Id.Id == item.Id))
                        datas.Datas.Add(new ItemData(item.Id, item.Angle, DateTime.Now));
                    else
                    {
                        var t = datas.Datas.FirstOrDefault(i => i.Id == item.Id);
                        t.Date = DateTime.Now;
                        t.Angle = item.Angle;
                    }
                }
                OnPropertyChanged(nameof(datas.Datas));
            });
        }

        private void Button_Clicked(object sender, EventArgs e)
        {
            if(root.Children.Count == 0)
            {
                root.Children.Add(new OpenCvSharp.XamarinForms.Controls.CameraScanner());
            }
            else
            {
                root.Children.Clear();
            }
        }
    }

    public class MainVM : BindableObject
    {
        public List<ItemData> datasTemp = new List<ItemData>();
        public ObservableCollection<ItemData> datas = new ObservableCollection<ItemData>();
        public CameraScannerOptions CurrentCamera { get; set; } = CameraScannerOptions.Rear;
        public ICommand ChangeCameraCommand { get; set; }

        public ICommand ClearCommand { get; set; }

        public ObservableCollection<ItemData> Datas
        {
            get => datas;
            set => datas = value;
        }

        public MainVM()
        {
            ClearCommand = new Command(() =>
            {
                this.datasTemp.Clear();
                this.datas.Clear();

                OnPropertyChanged(nameof(Datas));

            });

            ChangeCameraCommand = new Command(() =>
            {
                if (CurrentCamera == CameraScannerOptions.Rear)
                {
                    CurrentCamera = CameraScannerOptions.Front;
                }
                else
                    CurrentCamera = CameraScannerOptions.Rear;
            });
        }
    }

    public class ItemData : INotifyPropertyChanged
    {

        public ItemData(int id, int angle, DateTime date)
        {
            Id = id;
            Angle = angle;
            Date = date;
        }
        private int id;

        public int Id
        {
            get { return id; }
            set { id = value;
                OnPropertyChanged(nameof(Id));
            }
        }


        private int angle;

        public int Angle
        {
            get { return angle; }
            set { angle = value;
                OnPropertyChanged(nameof(Angle));
            }
        }

        private DateTime date;

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string name) {
            if (PropertyChanged!=null)
                PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
        public DateTime Date
        {
            get { return date; }
            set { date = value;
                OnPropertyChanged(nameof(Date));
            }
        }


    }
}
