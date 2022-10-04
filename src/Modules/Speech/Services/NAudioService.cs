using System;
using System.Collections.Generic;
using System.IO;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Xpand.XAF.Modules.Speech.Services {
    static class NAudioService {
        internal static void CreateWaveFile16(this IEnumerable<ISampleProvider> sampleProviders,string fileName) 
            => WaveFileWriter.CreateWaveFile16(fileName, new ConcatenatingSampleProvider(sampleProviders));

        public static TimeSpan Duration(this FileInfo info) {
            using var audioFileReader = new AudioFileReader(info.FullName);
            return audioFileReader.TotalTime;
        }

        public static void TrimWavFile(this WaveFileReader reader, string outPath, TimeSpan cutFromStart, TimeSpan cutFromEnd) {
            if (cutFromStart <= TimeSpan.Zero && cutFromEnd <= TimeSpan.Zero) {
                return;
            }
            using (WaveFileWriter writer = new WaveFileWriter(outPath, reader.WaveFormat)) {
                int bytesPerMillisecond = reader.WaveFormat.AverageBytesPerSecond / 1000;

                int startPos = (int)cutFromStart.TotalMilliseconds * bytesPerMillisecond;
                startPos -= startPos % reader.WaveFormat.BlockAlign;

                int endBytes = (int)cutFromEnd.TotalMilliseconds * bytesPerMillisecond;
                endBytes -= endBytes % reader.WaveFormat.BlockAlign;
                int endPos = (int)reader.Length - endBytes; 

                reader.TrimWavFile( writer, startPos, endPos);
            }

        }
        

        private static void TrimWavFile(this WaveFileReader reader, WaveFileWriter writer, int startPos, int endPos) {
            reader.Position = startPos;
            byte[] buffer = new byte[1024];
            while (reader.Position < endPos) {
                int bytesRequired = (int)(endPos - reader.Position);
                if (bytesRequired > 0) {
                    int bytesToRead = Math.Min(bytesRequired, buffer.Length);
                    int bytesRead = reader.Read(buffer, 0, bytesToRead);
                    if (bytesRead > 0) {
                        writer.Write(buffer, 0, bytesRead);
                    }
                }
            }
        }
    }
}