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
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
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
            if (waveOut == null)
            {
                waveOut = new WaveOut();
                SineWaveOscillator osc = new SineWaveOscillator(44100);
                osc.Frequency = 440;
                osc.Amplitude = 8192;
                waveOut.Init(osc);
                waveOut.Play();
            }

            else
            {
                waveOut.Stop();
                waveOut.Dispose();
                waveOut = null;
            }
        }
    }

    class SineWaveOscillator : WaveProvider16
    {
        double phaseAngle;

        public SineWaveOscillator(int sampleRate) :
            base(sampleRate, 1)
        {
        }

        public double Frequency { set; get; }
        public short Amplitude { set; get; }

        public override int Read(short[] buffer, int offset,
          int sampleCount)
        {

            for (int index = 0; index < sampleCount; index++)
            {
                buffer[offset + index] =
                  (short)(Amplitude * Math.Sin(phaseAngle));
                phaseAngle +=
                  2 * Math.PI * Frequency / WaveFormat.SampleRate;

                if (phaseAngle > 2 * Math.PI)
                    phaseAngle -= 2 * Math.PI;
            }
            return sampleCount;
        }
    }
}