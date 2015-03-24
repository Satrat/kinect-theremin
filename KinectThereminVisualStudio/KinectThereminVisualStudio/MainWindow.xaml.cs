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
using NAudio.Wave;

namespace KinectThereminVisualStudio
{
    public partial class MainWindow : Window
    {
        WaveOut waveOut;

        public MainWindow()
        {
            InitializeComponent();
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
                osc.Frequency = 440; //A
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