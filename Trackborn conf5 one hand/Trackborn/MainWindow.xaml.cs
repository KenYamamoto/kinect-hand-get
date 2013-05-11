using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.VisualBasic;
using Microsoft.Kinect;
//using Coding4Fun.Kinect.Wpf;

namespace Trackborn
{
  public partial class MainWindow : Window
  {
    KinectSensor kinect;
    string csvpath = "oikawa.csv";
    string stCurrentDir = System.IO.Directory.GetCurrentDirectory();
    Encoding sjisEnc = Encoding.GetEncoding("Shift-jis");
    StreamWriter writer;
    System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();
    SkeletonPoint skeletonPoing_HandRight;
    private DateTime _StartFrameTime;
    private int _TotalFrames;
    int x;

    public MainWindow()
    {
        InitializeComponent();

        //string[] stArrayData = csvpath.Split('.');
        //string[] files = System.IO.Directory.GetFiles(
        //stCurrentDir,
        //stArrayData[0] + "*");

        //if (File.Exists(csvpath))
        //{
        //    x = files.Length;
        //    csvpath = string.Concat(stArrayData[0], x, ".csv");
        //}

        try
        {
            if (KinectSensor.KinectSensors.Count == 0)
            {
                throw new Exception("Kinectを接続してください");
            }

            kinect = KinectSensor.KinectSensors[0];

            kinect.ColorFrameReady += kinect_ColorFrameReady;
            kinect.ColorStream.Enable();

            TransformSmoothParameters transformsmoothParameters = new TransformSmoothParameters()
            {
                Smoothing = 0.45f,
                Correction = 0.6f,
                Prediction = 0.4f,
                JitterRadius = 0.4f,
                MaxDeviationRadius = 0.4f,

                //Smoothing = 0.7f,
                //Correction = 0.3f,
                //Prediction = 0.4f,
                //JitterRadius = 0.10f,
                //MaxDeviationRadius = 0.5f,
            };

            kinect.SkeletonStream.Enable(transformsmoothParameters);

            //kinect.SkeletonStream.Enable();

            kinect.Start();
            this._StartFrameTime = DateTime.Now;

            comboBox1.Items.Clear();
            foreach (var range in Enum.GetValues(typeof(DepthRange)))
            {
                comboBox1.Items.Add(range.ToString());
            }
            //defaultモードとseatedモードの切り替え
            comboBox2.Items.Clear();
            foreach (var range in Enum.GetValues(typeof(SkeletonTrackingMode)))
            {
                comboBox2.Items.Add(range.ToString());
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
            Close();
        }
    }

    void kinect_ColorFrameReady(object sender, ColorImageFrameReadyEventArgs e)
    {
        try
        {
            using (ColorImageFrame colorFrame = e.OpenColorImageFrame())
            {
                if (colorFrame != null)
                {
                    byte[] colorPixel = new byte[colorFrame.PixelDataLength];
                    colorFrame.CopyPixelDataTo(colorPixel);
                    imageRgb.Source = BitmapSource.Create(colorFrame.Width, colorFrame.Height, 96, 96,
                        PixelFormats.Bgr32, null, colorPixel, colorFrame.Width * colorFrame.BytesPerPixel);
                }
                FramesPerSecondElement.Text = string.Format("{0:0} fps", (this._TotalFrames++ / DateTime.Now.Subtract(this._StartFrameTime).TotalSeconds));
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
        
        Vector4 accelerometer = kinect.AccelerometerGetCurrentReading();
        textAccelerometerX.Text = accelerometer.X.ToString();
        textAccelerometerY.Text = accelerometer.Y.ToString();
        textAccelerometerZ.Text = accelerometer.Z.ToString();

        textAngle.Text = GetAccelerometerAngle().ToString();

        textTiltAngle.Text = kinect.ElevationAngle.ToString();
    }

    /// <summary>
    /// 加速度センサーの値からZ軸の角度を求める
    /// </summary>
    /// <returns></returns>
    int GetAccelerometerAngle()
    {
        // http://thinkit.co.jp/story/2011/11/11/2329?page=0,2
        Vector4 accelerometer = kinect.AccelerometerGetCurrentReading();
        return -(int)Math.Round(Math.Asin(accelerometer.Z) * 180 / Math.PI);
    }

    /// <summary>
    /// チルト角度を設定する
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void buttonBaseUpdate_Click_1(object sender, RoutedEventArgs e)
    {
        try
        {
            kinect.ElevationAngle = int.Parse(textBaseAngle.Text);
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }
                      
    void kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
    {
        ShowSkeleton(e);
    }

    private void ShowSkeleton(SkeletonFrameReadyEventArgs e)
    {
        canvasSkeleton.Children.Clear();

        SkeletonFrame skeletonFrame = e.OpenSkeletonFrame();
        if (skeletonFrame != null)
        {
            Skeleton[] skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
            skeletonFrame.CopySkeletonDataTo(skeletons);

            foreach (Skeleton skeleton in skeletons)
            {
                if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
                {
                    this.skeletonPoing_HandRight = skeleton.Joints[JointType.HandRight].Position;

                    //xdepth.Text = string.Format("{0}mm", skeletonPoing_HandRight.X);
                    //ydepth.Text = string.Format("{0}mm", skeletonPoing_HandRight.Y);
                    if (skeletonPoing_HandRight.Z > 0.90 && skeletonPoing_HandRight.Z < 1.00)
                    {
                        zdepth.Foreground = new SolidColorBrush(Colors.Red);
                        zdepth.Text = string.Format("{0}m", skeletonPoing_HandRight.Z);
                    }
                    else
                    {
                        zdepth.Foreground = new SolidColorBrush(Colors.Black);
                        zdepth.Text = string.Format("{0}m", skeletonPoing_HandRight.Z);
                    }
                    
                    //this.writer.WriteLine(
                    //    (double)(sw.ElapsedMilliseconds) / 1000
                    //    + "," +
                    //    skeleton.Joints[JointType.HandRight].Position.X
                    //    + ","
                    //    + skeleton.Joints[JointType.HandRight].Position.Y
                    //    + ","
                    //    + skeleton.Joints[JointType.HandRight].Position.Z
                    //    );

                    ColorImagePoint handpoint = kinect.CoordinateMapper.MapSkeletonPointToColorPoint(skeletonPoing_HandRight, kinect.ColorStream.Format);
                    
                    this.writer.WriteLine((double)(sw.ElapsedMilliseconds) / 1000 + "," + handpoint.X + "," + handpoint.Y + "," +  skeletonPoing_HandRight.Z);
                    xdepth.Text = string.Format("{0}pixel", handpoint.X);
                    ydepth.Text = string.Format("{0}pixel", handpoint.Y);

                    foreach (Joint joint in skeleton.Joints)
                    {
                        const int R = 4;
                        //ColorImagePoint point = kinect.CoordinateMapper.MapSkeletonPointToColorPoint(joint.Position, kinect.ColorStream.Format);
                        ColorImagePoint point = kinect.CoordinateMapper.MapSkeletonPointToColorPoint(skeletonPoing_HandRight, kinect.ColorStream.Format);

                        point.X = (int)ScaleTo(point.X, kinect.ColorStream.FrameWidth, canvasSkeleton.Width);
                        point.Y = (int)ScaleTo(point.Y, kinect.ColorStream.FrameHeight, canvasSkeleton.Height);

                        canvasSkeleton.Children.Add(new Ellipse()
                        {
                            Fill = new SolidColorBrush(Colors.Red),
                            Margin = new Thickness(point.X - R, point.Y, 0, 0),
                            Width = R * 2,
                            Height = R * 2,
                        });
                    }
                }
            }
            skeletonFrame.Dispose();
        }
    }

    double ScaleTo(double value, double source, double dest)
    {
        return (value * dest) / source;
    }

    private void Window_Closing( object sender, System.ComponentModel.CancelEventArgs e )
    {
      kinect.Stop();
      kinect.Dispose();
      imageRgb.Source = null;
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        string[] stArrayData = csvpath.Split('.');
        string[] files = System.IO.Directory.GetFiles(
        stCurrentDir,
        stArrayData[0] + "*");
        if (File.Exists(csvpath))
        {
            x = files.Length;
            csvpath = string.Concat(stArrayData[0], x, ".csv");
        }
        writer = new StreamWriter(csvpath, false, sjisEnc);

        kinect.SkeletonFrameReady += kinect_SkeletonFrameReady;
        sw.Reset();
        sw.Start();
        this.writer.WriteLine("t" + "," + "X" + "," + "Y" + "," + "Z");
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        kinect.SkeletonFrameReady -= kinect_SkeletonFrameReady;
        sw.Stop();
        writer.Close();
        canvasSkeleton.Children.Clear();
    }

    private void comboBox_depthchange(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        try
        {
            KinectSensor.KinectSensors[0].DepthStream.Range = (DepthRange)comboBox1.SelectedIndex;
        }
        catch(Exception){
            comboBox1.SelectedIndex = 0;
        }
    }

    private void comboBox_modechange(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        try
        {
            KinectSensor.KinectSensors[0].SkeletonStream.TrackingMode = (SkeletonTrackingMode)comboBox2.SelectedIndex;
        }
        catch (Exception)
        {
            comboBox2.SelectedIndex = 0;
        }
    }
  }
}