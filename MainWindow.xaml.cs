using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.ComponentModel;

namespace WPFTest
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static Image image = new Image();
        public MainWindow()
        {
            //initialize Shiz
            InitializeComponent();
            //this.SizeToContent = SizeToContent.WidthAndHeight;
            MainGrid.Children.Add(image);
            RenderOptions.SetBitmapScalingMode(image, BitmapScalingMode.NearestNeighbor);

            //setup render thread
            BackgroundWorker renderEngineWorker = new BackgroundWorker();
            renderEngineWorker.DoWork += CeebInterface.CeebInterface.StartRenderer;
            renderEngineWorker.RunWorkerAsync();

            //setup exit handling
            App.Current.MainWindow.Closing += exitHandler;
        }

        private void exitHandler(object Sender, CancelEventArgs e)
        {
            System.Environment.Exit(0);
        }
    }
}
