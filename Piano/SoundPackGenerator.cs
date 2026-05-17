using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Piano
{
    public static class SoundPackGenerator
    {
        public static void EnsureGenerated()
        {
            string baseDir = AppDomain.CurrentDomain.BaseDirectory;
            string soundsDir = Path.Combine(baseDir, "Sounds");

            var notes = new Dictionary<string, double>
            {
                ["C4"] = 261.625565,
                ["Cs4"] = 277.182631,
                ["D4"] = 293.664768,
                ["Ds4"] = 311.126984,
                ["E4"] = 329.627557,
                ["F4"] = 349.228231,
                ["Fs4"] = 369.994423,
                ["G4"] = 391.995436,
                ["Gs4"] = 415.304698,
                ["A4"] = 440.0,
                ["As4"] = 466.163762,
                ["B4"] = 493.883301
            };

            EnsureSet(soundsDir, "Piano", notes, WaveType.Piano);
            EnsureSet(soundsDir, "Organ", notes, WaveType.Organ);
            EnsureSet(soundsDir, "Synth", notes, WaveType.Synth);
        }

        private static void EnsureSet(string soundsDir, string setName, Dictionary<string, double> notes, WaveType type)
        {
            string dir = Path.Combine(soundsDir, setName);
            Directory.CreateDirectory(dir);

            foreach (var kv in notes)
            {
                string file = Path.Combine(dir, kv.Key + ".wav");
                if (File.Exists(file)) continue;

                var samples = MakeSamples(kv.Value, type);
                WriteWavMono16(file, samples, 44100);
            }
        }

        private static short[] MakeSamples(double freq, WaveType type)
        {
            int sr = 44100;
            double seconds = 0.6;
            int n = (int)(sr * seconds);

            short[] data = new short[n];

            double attack = 0.008;
            double release = 0.03;

            for (int i = 0; i < n; i++)
            {
                double t = (double)i / sr;
                double env;

                if (t < attack) env = t / attack;
                else if (t > seconds - release) env = Math.Max(0, (seconds - t) / release);
                else env = 1.0;

                double v = 0.0;

                if (type == WaveType.Piano)
                {
                    double d = Math.Exp(-4.5 * t);
                    v =
                        Math.Sin(2 * Math.PI * freq * t) * 0.85 +
                        Math.Sin(2 * Math.PI * freq * 2 * t) * 0.25 +
                        Math.Sin(2 * Math.PI * freq * 3 * t) * 0.12;
                    v *= d;
                }
                else if (type == WaveType.Organ)
                {
                    v =
                        Math.Sin(2 * Math.PI * freq * t) * 0.75 +
                        Math.Sin(2 * Math.PI * freq * 2 * t) * 0.20 +
                        Math.Sin(2 * Math.PI * freq * 3 * t) * 0.12 +
                        Math.Sin(2 * Math.PI * freq * 4 * t) * 0.06;
                    v *= 0.85;
                }
                else
                {
                    v = SquareApprox(freq, t, 11) * 0.75;
                    double d = Math.Exp(-1.8 * t);
                    v *= d;
                }

                v *= env;

                int s = (int)Math.Round(v * 32767 * 0.6);
                if (s > 32767) s = 32767;
                if (s < -32768) s = -32768;

                data[i] = (short)s;
            }

            return data;
        }

        private static double SquareApprox(double freq, double t, int harmonics)
        {
            double sum = 0.0;
            for (int k = 1; k <= harmonics; k += 2)
            {
                sum += Math.Sin(2 * Math.PI * freq * k * t) / k;
            }
            return (4.0 / Math.PI) * sum;
        }

        private static void WriteWavMono16(string path, short[] samples, int sampleRate)
        {
            int channels = 1;
            short bits = 16;
            int blockAlign = channels * (bits / 8);
            int byteRate = sampleRate * blockAlign;
            int dataSize = samples.Length * blockAlign;
            int riffSize = 36 + dataSize;

            using (var fs = new FileStream(path, FileMode.Create, FileAccess.Write))
            using (var bw = new BinaryWriter(fs, Encoding.ASCII))
            {
                bw.Write(Encoding.ASCII.GetBytes("RIFF"));
                bw.Write(riffSize);
                bw.Write(Encoding.ASCII.GetBytes("WAVE"));

                bw.Write(Encoding.ASCII.GetBytes("fmt "));
                bw.Write(16);
                bw.Write((short)1);
                bw.Write((short)channels);
                bw.Write(sampleRate);
                bw.Write(byteRate);
                bw.Write((short)blockAlign);
                bw.Write(bits);

                bw.Write(Encoding.ASCII.GetBytes("data"));
                bw.Write(dataSize);

                for (int i = 0; i < samples.Length; i++)
                    bw.Write(samples[i]);
            }
        }

        private enum WaveType
        {
            Piano,
            Organ,
            Synth
        }
    }
}
