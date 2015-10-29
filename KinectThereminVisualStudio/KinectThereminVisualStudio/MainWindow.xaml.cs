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
using Microsoft.Kinect;
using NAudio.Wave;

// Kinect Theremin
// Created by Sara Adkins and Adam Zeloof
// 3-23-2015

namespace KinectThereminVisualStudio
{
    public partial class MainWindow : Window
    {
        KinectSensor sensor;
        ColorFrameReader colorFrameReader; //camera visual
        BodyFrameReader bodyFrameReader; //body sensor
        WriteableBitmap colorBitmap;
        DrawingGroup bodyHighlight;
        Body[] bodies;
        WaveOut waveOut;
        SineWaveOscillator osc;
        double freqReference;
        double freqRatio;
        double pitchReference;
        double currentPitch;
        double baseVol;
        float freqMult;

        public MainWindow()
        {
            // Connects to the kinect sensor and initializes the service
            InitializeComponent();
            sensor = KinectSensor.GetDefault();
            sensor.Open();

            // Sets up readers for the relevant data sources
            colorFrameReader = sensor.ColorFrameSource.OpenReader();
            bodyFrameReader = sensor.BodyFrameSource.OpenReader();

            // Creates event handlers
            colorFrameReader.FrameArrived += colorFrameReader_FrameArrived;
            bodyFrameReader.FrameArrived += bodyFrameReader_FrameArrived;

            // Creates a blank bitmap for storing color data
            colorBitmap = new WriteableBitmap(1920, 1080, 96.0, 96.0, PixelFormats.Bgr32, null);
            ColorImage.Source = colorBitmap;

            // Creates the graphics that highlight the user's image
            bodyHighlight = new DrawingGroup();

            //initializing sound components, default is A4
            osc = new SineWaveOscillator(44100);
            osc.Amplitude = 4000;
            osc.Frequency = 440;
            waveOut = new WaveOut();
            waveOut.Init(osc);
            freqReference = 440; //A5
            freqRatio = Math.Pow(Math.Pow(2.0, 1 / 2), 12.0);
            pitchReference = 49; //A5
            currentPitch = 49; 
            baseVol = 4000;
            freqMult = 220.0f;
        }

        // BodyFrameReader event handler
        void bodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame == null)
                {
                    return;
                }
                if (bodies == null)
                {
                    bodies = new Body[bodyFrame.BodyCount];
                }

                // Updates body data
                bodyFrame.GetAndRefreshBodyData(bodies);

                foreach (var body in bodies)
                {
                    if (body.IsTracked)
                    {
                        var joints = body.Joints;
                        // Sets up tracking for left and right hands
                        var handRight = joints[JointType.HandRight];
                        var handLeft = joints[JointType.HandLeft];

                        if (handRight.TrackingState == TrackingState.Tracked && handLeft.TrackingState == TrackingState.Tracked)
                        {
                            // Updates the onscreen labels with current data
                            handLx.Content = handLeft.Position.X;
                            handLy.Content = handLeft.Position.Y;
                            handLz.Content = handLeft.Position.Z;
                            handRx.Content = handRight.Position.X;
                            handRy.Content = handRight.Position.Y;
                            handRz.Content = handRight.Position.Z;

                            //pitch increases as right hand moves up, volume increases as left hand moves up
                            if (handRight.Position.Y <= 1.5)
                            {
                                currentPitch = handLeft.Position.Y / (1 / 6) * -1 + pitchReference;
                            }
                            else if (handRight.Position.Y > 1.5)
                            {
                                currentPitch = handLeft.Position.Y / (1 / 6) + pitchReference;
                            }

                            currentPitch = pitchReference + handRight.Position.Y;
                            osc.Frequency = freqReference * Math.Pow(freqRatio, currentPitch - pitchReference);
                            osc.Amplitude = handLeft.Position.Y/.05 * 2716;

                            //frequency output on screen in label
                            freqLabel.Content = osc.Frequency;

                            //plays note only if kinect has sensed a body
                            if (waveOut != null)
                            {
                                waveOut.Play();
                            }

                            using (var canvas = bodyHighlight.Open())
                            {

                                canvas.DrawRectangle(Brushes.Transparent, null, new Rect(0, 0, Width, Height));

                                // Place a circle around the user's left hand
                                canvas.DrawEllipse(Brushes.Red, null, new Point(handLeft.Position.X, handLeft.Position.Y), 20, 20);

                                // Place a circle around the user's right hand
                                canvas.DrawEllipse(Brushes.Blue, null, new Point(handRight.Position.X, handRight.Position.Y), 20, 20);

                                // Pushes bodyHighlight to the window
                                BodyOverlay.Source = new DrawingImage(bodyHighlight);
                            }
                        }
                    }
                }
            }
        }

        // ColorFrameReader event handler
        void colorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame == null)
                {
                    return;
                }

                 // Locks raw pixel data for the captured frame
                using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                {
                    colorBitmap.Lock();

                    colorFrame.CopyConvertedFrameDataToIntPtr(
                        colorBitmap.BackBuffer,
                        (uint)(1920 * 1080 * 4),
                        ColorImageFormat.Bgra);

                    // Defines the entire image as update space
                    colorBitmap.AddDirtyRect(new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight));
                    colorBitmap.Unlock();
                }
            }
        }
    }


    class SineWaveOscillator : WaveProvider16
    //https://msdn.microsoft.com/en-us/magazine/ee309883.aspx
    {
        double phaseAngle; //ranges between 0 and 2*PI

        public SineWaveOscillator(int sampleRate) :
            base(sampleRate, 1)
        {
        }

        public double Frequency { set; get; }
        public double Amplitude { set; get; }

        //called 10 times a second(default)
        //fills buffer with waveform data
        public override int Read(short[] buffer, int offset, int sampleCount)
        {

            //for each sample(taken 10 times a second)
            for (int index = 0; index < sampleCount; index++)
            {
                //pass phaseAngle to Math.Sin and add to buffer
                buffer[offset + index] = (short)(Amplitude * Math.Sin(phaseAngle));

                //increase by phase angle increment
                phaseAngle += 2 * Math.PI * Frequency / WaveFormat.SampleRate;

                //ensures angle doesn't exceed 2*PI
                if (phaseAngle > 2 * Math.PI)
                {
                    phaseAngle -= 2 * Math.PI;
                }
            }
            return sampleCount;
        }
    }
}