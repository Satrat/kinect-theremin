﻿using System;
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

namespace KinectThereminVisualStudio
{
    public partial class MainWindow : Window
    {
        WaveOut waveOut;
        KinectSensor sensor;
        ColorFrameReader colorFrameReader;
        BodyFrameReader bodyFrameReader;
        WriteableBitmap colorBitmap;
        Body[] bodies;
        double multiplier;
        public double freq;

        public MainWindow()
        {
            InitializeComponent();
            sensor = KinectSensor.GetDefault();
            sensor.Open();

            colorFrameReader = sensor.ColorFrameSource.OpenReader();
            bodyFrameReader = sensor.BodyFrameSource.OpenReader();

            colorFrameReader.FrameArrived += colorFrameReader_FrameArrived;
            bodyFrameReader.FrameArrived += bodyFrameReader_FrameArrived;

            colorBitmap = new WriteableBitmap(1920, 1080, 96.0, 96.0, PixelFormats.Bgr32, null);
            ColorImage.Source = colorBitmap;

            multiplier = 220;
        }
        void bodyFrameReader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyFrame == null)
                {
                    return;
                }
                if (bodies == null);
                {
                    bodies = new Body[bodyFrame.BodyCount];
                }

                bodyFrame.GetAndRefreshBodyData(bodies);

                foreach (var body in bodies)
                {
                    if (body.IsTracked)
                    {
                        var joints = body.Joints;
                        var handRight = joints[JointType.HandRight];
                        var handLeft = joints[JointType.HandLeft];

                        if (handRight.TrackingState == TrackingState.Tracked && handLeft.TrackingState == TrackingState.Tracked)
                        {
                            handLx.Content = handLeft.Position.X;
                            handLy.Content = handLeft.Position.Y;
                            handLz.Content = handLeft.Position.Z;
                            handRx.Content = handRight.Position.X;
                            handRy.Content = handRight.Position.Y;
                            handRz.Content = handRight.Position.Z;
                            freq = handLeft.Position.X * multiplier;
                        }
                    }
                }
            }
        }

        void colorFrameReader_FrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame == null)
                {
                    return;
                }

                using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer());
                {
                    colorBitmap.Lock();

                    colorFrame.CopyConvertedFrameDataToIntPtr(
                        colorBitmap.BackBuffer,
                        (uint)(1920 * 1080 * 4),
                        ColorImageFormat.Bgra);

                    colorBitmap.AddDirtyRect(
                        new Int32Rect(0, 0, colorBitmap.PixelWidth, colorBitmap.PixelHeight));
                    colorBitmap.Unlock();
                }
            }
        }

        private void playWave_Click(object sender, EventArgs e)
        {
            StartStopSineWave();
        }

        private void StartStopSineWave()
        {
            //if no sound, start sine wave
            if (waveOut == null)
            {
                waveOut = new WaveOut();
                SineWaveOscillator osc = new SineWaveOscillator(44100); //standard sampling frequency
                osc.Frequency = freq; //A
                osc.Amplitude = 8192;
                waveOut.Init(osc);
                waveOut.Play();
            }

            //if playing, stop sine wave
            else
            {
                waveOut.Stop();
                waveOut.Dispose();
                waveOut = null;
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
        public short Amplitude { set; get; }

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