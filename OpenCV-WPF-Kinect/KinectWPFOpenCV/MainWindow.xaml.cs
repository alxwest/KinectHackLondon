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
using System.Drawing;
using Microsoft.Kinect;
using Emgu.CV;
using Emgu.CV.Structure;
using System.IO;
using System.ComponentModel;

namespace KinectWPFOpenCV
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        KinectSensor sensor;
        // WriteableBitmap depthBitmap;
        WriteableBitmap colorBitmap;
        short[] depthPixels;
        byte[] colorPixels;

        int blobCount = 0;

        public MainWindow()
        {
            InitializeComponent();

            this.Loaded += MainWindow_Loaded;
            this.Closing += MainWindow_Closing;
            this.MouseDown += MainWindow_MouseDown;

        }


        void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {

            foreach (var potentialSensor in KinectSensor.KinectSensors)
            {
                if (potentialSensor.Status == KinectStatus.Connected)
                {
                    this.sensor = potentialSensor;
                    break;
                }
            }


            if (null != this.sensor)
            {

                this.sensor.DepthStream.Enable(DepthImageFormat.Resolution640x480Fps30);
                this.sensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                this.colorPixels = new byte[this.sensor.ColorStream.FramePixelDataLength];
                this.depthPixels = new short[this.sensor.DepthStream.FramePixelDataLength];
                this.colorBitmap = new WriteableBitmap(this.sensor.ColorStream.FrameWidth, this.sensor.ColorStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                //this.depthBitmap = new WriteableBitmap(this.sensor.DepthStream.FrameWidth, this.sensor.DepthStream.FrameHeight, 96.0, 96.0, PixelFormats.Bgr32, null);
                //this.colorImg.Source = this.colorBitmap;

                this.sensor.AllFramesReady += this.sensor_AllFramesReady;
                this.sensor.DepthStream.Range = DepthRange.Near;
                try
                {
                    this.sensor.Start();
                }
                catch (IOException)
                {
                    this.sensor = null;
                }
            }

            if (null == this.sensor)
            {
                this.outputViewbox.Visibility = System.Windows.Visibility.Collapsed;
                this.txtError.Visibility = System.Windows.Visibility.Visible;
                this.txtInfo.Text = "No Kinect Found";

            }

        }

        private void sensor_AllFramesReady(object sender, AllFramesReadyEventArgs e)
        {

            blobCount = 0;

            var bCw = new BackgroundWorker();

            var colorRange = (int)sliderColorRange.Value;
            var sliderMaxValue = (int)sliderMax.Value;
            var sliderMinSizeValue = sliderMinSize.Value;
            var sliderMaxSizeValue = sliderMaxSize.Value;

            
            bCw.DoWork += (s, a) =>
           {
               using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
               using (DepthImageFrame depthFrame = e.OpenDepthImageFrame())
               {
                   if (colorFrame != null && depthFrame != null)
                   {
                       BitmapSource colorBmp = null;
                       // BitmapSource depthBmp = null;
                       depthFrame.CopyPixelDataTo(this.depthPixels);
                       //var greyPixels = new byte[depthFrame.Height * depthFrame.Width * 4];
                       colorFrame.CopyPixelDataTo(this.colorPixels);
                       //depthBmp = BitmapSource.Create(depthFrame.Width, depthFrame.Height, 96, 96, PixelFormats.Bgr32, null, greyPixels, depthFrame.Width * 4);
                       colorBmp = BitmapSource.Create(colorFrame.Width, colorFrame.Height, 96, 96, PixelFormats.Bgr32, null, colorPixels, colorFrame.Width * 4);
                       Image<Bgr, Byte> returnImage = new Image<Bgr, byte>(colorBmp.ToBitmap());
               
                       const int BlueIndex = 0;
                       const int GreenIndex = 1;
                       const int RedIndex = 2;

                       double minDepth = int.MaxValue;
                       System.Drawing.Point minPoint = new System.Drawing.Point();

                       for (int depthIndex = 0, colorIndex = 0;
                           depthIndex < depthPixels.Length && colorIndex < colorPixels.Length;
                           depthIndex++, colorIndex += 4)
                       {

                           // Calculate the distance represented by the two depth bytes
                           int depth = depthPixels[depthIndex] >> DepthImageFrame.PlayerIndexBitmaskWidth;
                           int y = colorIndex / (colorFrame.Width * 4);
                           int x = (colorIndex - (y * colorFrame.Width * 4)) / 4;
                           if (minDepth > depth && depth > 0)
                           {
                               minDepth = depth;

                               minPoint = new System.Drawing.Point(x, y);
                           }

                           // Apply the intensity to the color channels
                           if (depth > sliderMaxValue)
                           {
                               colorPixels[colorIndex + BlueIndex] = 0; //blue
                               colorPixels[colorIndex + GreenIndex] = 0; //green
                               colorPixels[colorIndex + RedIndex] = 0; //red   
                           }

                       }

                       colorBmp = BitmapSource.Create(colorFrame.Width, colorFrame.Height, 96, 96, PixelFormats.Bgr32, null, colorPixels, colorFrame.Width * 4);

                       Image<Hsv, Byte> openCVImg = new Image<Hsv, byte>(colorBmp.ToBitmap());
                       var smoothed = openCVImg.SmoothMedian(7);
                       //Increase saturation
                       smoothed[1] += 30;

                       //Get 10x10 pixel color sample from the nearest point
                       Image<Gray, Byte> averageMask = new Image<Gray, byte>(colorFrame.Width, colorFrame.Height, new Gray(0));
                       averageMask.Draw(new System.Drawing.Rectangle(minPoint.X - 5, minPoint.Y - 5, 10, 10), new Gray(255), -1);


                       //Make a HSV theshold mask
                       Image<Gray, Byte> theshold;

                       // 2. Obtain the 3 channels (hue, saturation and value) that compose the HSV image
                       Image<Gray, byte>[] channels = smoothed.Split();

                       try
                       {
                           var avgColor = channels[0].GetAverage(averageMask);
                           // 3. Remove all pixels from the hue channel that are not in the range [40, 60]
                           CvInvoke.cvInRangeS(channels[0], new Gray(avgColor.Intensity - colorRange).MCvScalar, new Gray(avgColor.Intensity + colorRange).MCvScalar, channels[0]);

                           // 4. Display the result
                           theshold = channels[0];
                       }
                       finally
                       {
                           channels[1].Dispose();
                           channels[2].Dispose();
                       }


                       //Find blob
                       using (MemStorage stor = new MemStorage())
                       {
                           //Find contours with no holes try CV_RETR_EXTERNAL to find holes
                           Contour<System.Drawing.Point> contours = theshold.FindContours(
                            Emgu.CV.CvEnum.CHAIN_APPROX_METHOD.CV_CHAIN_APPROX_SIMPLE,
                            Emgu.CV.CvEnum.RETR_TYPE.CV_RETR_EXTERNAL ,
                            stor);

                           for (int i = 0; contours != null; contours = contours.HNext)
                           {
                               i++;

                               if ((contours.Area > Math.Pow(sliderMinSizeValue, 2)) && (contours.Area < Math.Pow(sliderMaxSizeValue, 2)))
                               {
                                   MCvBox2D box = contours.GetMinAreaRect();
                                   returnImage.Draw(box, new Bgr(System.Drawing.Color.Red), 2);
                                   blobCount++;
                               }
                           }
                       } 

                       //get depthpoint data (point cloud)

                       //work out if cuboid by tracking point clouds

                       //draw yellow cross on nearest point
                       returnImage.Draw(new Cross2DF(minPoint, 50, 50), new Bgr(System.Drawing.Color.Yellow), 4);
                       theshold.Draw(new Cross2DF(minPoint, 50, 50), new Gray(255), 4);

                       outImg.Dispatcher.BeginInvoke(new Action(() =>
                      {
                          this.outImg.Source = ImageHelpers.ToBitmapSource(theshold );
                          txtBlobCount.Text = string.Format("x:{0}, y:{1}", minPoint.X, minPoint.Y);
                      }));

                   }
               }
           };

            bCw.RunWorkerAsync();
        }


        #region Window Stuff
        void MainWindow_MouseDown(object sender, MouseButtonEventArgs e)
        {
            this.DragMove();
        }


        void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (null != this.sensor)
            {
                this.sensor.Stop();
            }
        }

        private void CloseBtnClick(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
        #endregion
    }
}
