using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Microsoft.Kinect;

namespace Trackborn
{
  /// <summary>
  /// MainWindow.xaml の相互作用ロジック
  /// </summary>
  public partial class MainWindow : Window
  {

      string csvpath = "get_hand_depth.csv";
      Encoding sjisEnc = Encoding.GetEncoding("Shift-jis");
      StreamWriter writer;
      System.Diagnostics.Stopwatch sw = System.Diagnostics.Stopwatch.StartNew();

      SkeletonPoint skeletonPoing_HandRight;

      private DateTime _StartFrameTime;
      private int _TotalFrames;
      
    /// <summary>
    /// コンストラクタ
    /// </summary>
    public MainWindow()
    {
      try {
        InitializeComponent();

        // Kinectが接続されているかどうかを確認する
        if ( KinectSensor.KinectSensors.Count == 0 ) {
          throw new Exception( "Kinectを接続してください" );
        }

        // Kinectの動作を開始する
        StartKinect( KinectSensor.KinectSensors[0] );
      }
      catch ( Exception ex ) {
        MessageBox.Show( ex.Message );
        Close();
      }
    }

    /// <summary>
    /// Kinectの動作を開始する
    /// </summary>
    /// <param name="kinect"></param>
    private void StartKinect( KinectSensor kinect )
    {
      // RGBカメラを有効にして、フレーム更新イベントを登録する
      kinect.ColorStream.Enable();
      kinect.ColorFrameReady +=
          new EventHandler<ColorImageFrameReadyEventArgs>( kinect_ColorFrameReady );

      // 距離カメラを有効にして、フレーム更新イベントを登録する
      kinect.SkeletonStream.Enable();
      kinect.SkeletonFrameReady +=
          new EventHandler<SkeletonFrameReadyEventArgs>( kinect_SkeletonFrameReady );

      // Kinectの動作を開始する
      kinect.Start();
      this._StartFrameTime = DateTime.Now;

        //defaultモードとnearモードの切り替え
      comboBox1.Items.Clear();
      foreach (var range in Enum.GetValues(typeof(DepthRange)))
      {
          comboBox1.Items.Add(range.ToString());
      }
        //defaultモードとseatedモードの切り替え
      //comboBox2.Items.Clear();
      foreach (var range in Enum.GetValues(typeof(SkeletonTrackingMode)))
      {
          comboBox2.Items.Add(range.ToString());
      }
    }

    /// <summary>
    /// Kinectの動作を停止する
    /// </summary>
    /// <param name="kinect"></param>
    private void StopKinect( KinectSensor kinect )
    {
      if ( kinect != null ) {
        if ( kinect.IsRunning ) {
          // フレーム更新イベントを削除する
          kinect.ColorFrameReady -= kinect_ColorFrameReady;
          kinect.SkeletonFrameReady -= kinect_SkeletonFrameReady;

          // Kinectの停止と、ネイティブリソースを解放する
          kinect.Stop();
          kinect.Dispose();
          kinect = null;

          imageRgb.Source = null;
        }
      }
    }

    /// <summary>
    /// RGBカメラのフレーム更新イベント
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    void kinect_ColorFrameReady( object sender, ColorImageFrameReadyEventArgs e )
    {
      try {
        // RGBカメラのフレームデータを取得する
        using ( ColorImageFrame colorFrame = e.OpenColorImageFrame() ) {
          if ( colorFrame != null ) {
            // RGBカメラのピクセルデータを取得する
            byte[] colorPixel = new byte[colorFrame.PixelDataLength];
            colorFrame.CopyPixelDataTo( colorPixel );

            // ピクセルデータをビットマップに変換する
            imageRgb.Source = BitmapSource.Create( colorFrame.Width, colorFrame.Height, 96, 96,
                PixelFormats.Bgr32, null, colorPixel, colorFrame.Width * colorFrame.BytesPerPixel );
          }
          FramesPerSecondElement.Text = string.Format("{0:0} fps", (this._TotalFrames++ / DateTime.Now.Subtract(this._StartFrameTime).TotalSeconds));
        }
      }
      catch ( Exception ex ) {
        MessageBox.Show( ex.Message );
      }
    }

    void kinect_SkeletonFrameReady(object sender, SkeletonFrameReadyEventArgs e)
    {
        try
        {
            KinectSensor kinect = sender as KinectSensor;
            if (kinect == null)
            {
                return;
            }
            using (SkeletonFrame skeletonFrame = e.OpenSkeletonFrame())
            {
                if (skeletonFrame != null)
                {
                    DrawSkeleton(kinect, skeletonFrame);
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(ex.Message);
        }
    }

    private void DrawSkeleton(KinectSensor kinect, SkeletonFrame skeletonFrame)
    {
        Skeleton[] skeletons = new Skeleton[skeletonFrame.SkeletonArrayLength];
        skeletonFrame.CopySkeletonDataTo(skeletons);

        //foreach (Skeleton skeleton in skeletons)
        //{
        //    if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
        //    {
        //        this.skeletonPoing_HandRight = skeleton.Joints[JointType.HandRight].Position;
        //        xdepth.Text = string.Format("{0}mm", skeletonPoing_HandRight.X);
        //        ydepth.Text = string.Format("{0}mm", skeletonPoing_HandRight.Y);
        //        zdepth.Text = string.Format("{0}mm", skeletonPoing_HandRight.Z);
        //    }
        //}

        canvasSkeleton.Children.Clear();

        foreach (Skeleton skeleton in skeletons)
        {
            if (skeleton.TrackingState == SkeletonTrackingState.Tracked)
            {
                this.skeletonPoing_HandRight = skeleton.Joints[JointType.HandRight].Position;
                xdepth.Text = string.Format("{0}mm", skeletonPoing_HandRight.X);
                ydepth.Text = string.Format("{0}mm", skeletonPoing_HandRight.Y);
                zdepth.Text = string.Format("{0}mm", skeletonPoing_HandRight.Z);

                //this.writer.WriteLine((double)(sw.ElapsedMilliseconds) / 1000);
                //this.writer.WriteLine(skeletonPoing_HandRight.X);

                //this.writer.WriteLine(
                //    (double)(sw.ElapsedMilliseconds) / 1000
                //    + ','
                //    + skeleton.Joints[JointType.HandRight].Position.X + ','
                //    + skeleton.Joints[JointType.HandRight].Position.Y + ','
                //    + skeleton.Joints[JointType.HandRight].Position.Z
                //    );

                foreach (Joint joint in skeleton.Joints)
                {
                    if (joint.TrackingState == JointTrackingState.NotTracked)
                    {
                        continue;
                    }
                    DrawEllipse(kinect,joint.Position);
                }
            }
            skeletonFrame.Dispose();
        }
    }

    private void DrawEllipse(KinectSensor kinect,SkeletonPoint position)
    {
        const int R = 5;

        ColorImagePoint point = kinect.CoordinateMapper.MapSkeletonPointToColorPoint(position,kinect.ColorStream.Format);

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

    double ScaleTo(double value, double source, double dest)
    {
        return (value * dest) / source;
    }

    /// <summary>
    /// Windowsが閉じられるときのイベント
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void Window_Closing( object sender, System.ComponentModel.CancelEventArgs e )
    {
      StopKinect( KinectSensor.KinectSensors[0] );
    }

    private void Start_Click(object sender, RoutedEventArgs e)
    {
        writer = new StreamWriter(csvpath, false, sjisEnc);
        sw.Reset();
        sw.Start();

        this.writer.WriteLine("t" + "," + "X" + "," + "Y" + "," + "Z");
    }

    private void Stop_Click(object sender, RoutedEventArgs e)
    {
        sw.Stop();
        writer.Close();
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