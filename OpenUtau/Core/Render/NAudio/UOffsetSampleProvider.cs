﻿using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core.Render.NAudio
{
    public class UOffsetSampleProvider : ISampleProvider
    {
        private readonly ISampleProvider sourceProvider;
        private int phase; // 0 = not started yet, 1 = delay, 2 = skip, 3 = take, 4 = lead_out, 5 = end
        private int phasePos;
        private int delayBySamples;
        private int skipOverSamples;
        private int takeSamples;
        private int leadOutSamples;

        private int TimeSpanToSamples(TimeSpan time)
        {
            var samples = (int)(time.TotalSeconds * WaveFormat.SampleRate) * WaveFormat.Channels;
            return samples;
        }

        private TimeSpan SamplesToTimeSpan(int samples)
        {
            return TimeSpan.FromSeconds((samples / WaveFormat.Channels) / (double)WaveFormat.SampleRate);
        }

        /// <summary>
        /// Number of samples of silence to insert before playing source
        /// </summary>
        public int DelayBySamples
        {
            get => delayBySamples;
            set
            {
                if (phase != 0)
                {
                    throw new InvalidOperationException("Can't set DelayBySamples after calling Read");
                }
                if (value % WaveFormat.Channels != 0)
                {
                    throw new ArgumentException("DelayBySamples must be a multiple of WaveFormat.Channels");
                }
                delayBySamples = Math.Max(value, 0);
            }
        }

        /// <summary>
        /// Amount of silence to insert before playing
        /// </summary>
        public TimeSpan DelayBy
        {
            get => SamplesToTimeSpan(delayBySamples);
            set => delayBySamples = Math.Max(TimeSpanToSamples(value),0);
        }

        /// <summary>
        /// Number of samples in source to discard
        /// </summary>
        public int SkipOverSamples
        {
            get => skipOverSamples;
            set
            {
                if (phase != 0)
                {
                    throw new InvalidOperationException("Can't set SkipOverSamples after calling Read");
                }
                if (value % WaveFormat.Channels != 0)
                {
                    throw new ArgumentException("SkipOverSamples must be a multiple of WaveFormat.Channels");
                }
                skipOverSamples = value;
            }
        }

        /// <summary>
        /// Amount of audio to skip over from the source before beginning playback
        /// </summary>
        public TimeSpan SkipOver
        {
            get => SamplesToTimeSpan(skipOverSamples);
            set => skipOverSamples = TimeSpanToSamples(value);
        }


        /// <summary>
        /// Number of samples to read from source (if 0, then read it all)
        /// </summary>
        public int TakeSamples
        {
            get => takeSamples;
            set
            {
                if (phase != 0)
                {
                    throw new InvalidOperationException("Can't set TakeSamples after calling Read");
                }
                if (value % WaveFormat.Channels != 0)
                {
                    throw new ArgumentException("TakeSamples must be a multiple of WaveFormat.Channels");
                }
                takeSamples = value;
            }
        }

        /// <summary>
        /// Amount of audio to take from the source (TimeSpan.Zero means play to end)
        /// </summary>
        public TimeSpan Take
        {
            get => SamplesToTimeSpan(takeSamples);
            set => takeSamples = TimeSpanToSamples(value);
        }

        /// <summary>
        /// Number of samples of silence to insert after playing source
        /// </summary>
        public int LeadOutSamples
        {
            get => leadOutSamples;
            set
            {
                if (phase != 0)
                {
                    throw new InvalidOperationException("Can't set LeadOutSamples after calling Read");
                }
                if (value % WaveFormat.Channels != 0)
                {
                    throw new ArgumentException("LeadOutSamples must be a multiple of WaveFormat.Channels");
                }
                leadOutSamples = value;
            }
        }

        /// <summary>
        /// Amount of silence to insert after playing source
        /// </summary>
        public TimeSpan LeadOut
        {
            get => SamplesToTimeSpan(leadOutSamples);
            set => leadOutSamples = TimeSpanToSamples(value);
        }

        /// <summary>
        /// Creates a new instance of offsetSampleProvider
        /// </summary>
        /// <param name="sourceProvider">The Source Sample Provider to read from</param>
        public UOffsetSampleProvider(ISampleProvider sourceProvider)
        {
            this.sourceProvider = sourceProvider;
        }

        public UOffsetSampleProvider Clone() {
            ISampleProvider cs;
            if (sourceProvider is TrackSampleProvider track)
            {
                cs = track.Clone();
            }
            else if (sourceProvider is SequencingSampleProvider sequence)
            {
                cs = sequence.Clone();
            }
            else if (sourceProvider is EnvelopeSampleProvider envelope)
            {
                cs = envelope.Clone();
            }
            else if (sourceProvider is RenderItemSampleProvider renderitem)
            {
                cs = renderitem.Clone();
            }
            else if (sourceProvider is CachedSoundSampleProvider cache)
            {
                cs = cache.Clone();
            }
            else if (sourceProvider is MonoToStereoSampleProvider mts)
            {
                var field = typeof(MonoToStereoSampleProvider).GetField("source", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                var innersrc = field.GetValue(mts) as ISampleProvider;
                cs = new MonoToStereoSampleProvider(innersrc) { LeftVolume = mts.LeftVolume, RightVolume = mts.RightVolume};
            }
            else
            {
                cs = sourceProvider;
            }
            return new UOffsetSampleProvider(cs) { DelayBy = DelayBy, SkipOver = SkipOver, LeadOut = LeadOut, Take = Take};
        }

        /// <summary>
        /// The WaveFormat of this SampleProvider
        /// </summary>
        public WaveFormat WaveFormat => sourceProvider.WaveFormat;

        /// <summary>
        /// Reads from this sample provider
        /// </summary>
        /// <param name="buffer">Sample buffer</param>
        /// <param name="offset">Offset within sample buffer to read to</param>
        /// <param name="count">Number of samples required</param>
        /// <returns>Number of samples read</returns>
        public int Read(float[] buffer, int offset, int count)
        {
            int samplesRead = 0;

            if (phase == 0) // not started yet
            {
                phase++;
            }

            if (phase == 1) // delay
            {
                int delaySamples = Math.Min(count, DelayBySamples - phasePos);
                for (int n = 0; n < delaySamples; n++)
                {
                    buffer[offset + n] = 0;
                }
                phasePos += delaySamples;
                samplesRead += delaySamples;
                if (phasePos >= DelayBySamples)
                {
                    phase++;
                    phasePos = 0;
                }
            }

            if (phase == 2) // skip
            {
                if (SkipOverSamples > 0)
                {
                    /*var skipBuffer = new float[SkipOverSamples];
                    // skip everything
                    int samplesRequired = Math.Min(SkipOverSamples, skipBuffer.Length);
                    sourceProvider.Read(skipBuffer, 0, samplesRequired);     */
                    var skipBuffer = new float[WaveFormat.SampleRate * WaveFormat.Channels];
                    // skip everything
                    int samplesSkipped = 0;
                    while (samplesSkipped < SkipOverSamples)
                    {
                        int samplesRequired = Math.Min(SkipOverSamples - samplesSkipped, skipBuffer.Length);
                        var read = sourceProvider.Read(skipBuffer, 0, samplesRequired);
                        if (read == 0) // source has ended while still in skip
                        {
                            break;
                        }
                        samplesSkipped += read;
                    }
                }
                phase++;
                phasePos = 0;
            }

            if (phase == 3) // take
            {
                int samplesRequired = count - samplesRead;
                if (takeSamples > 0)
                    samplesRequired = Math.Min(samplesRequired, takeSamples - phasePos);
                int read = sourceProvider.Read(buffer, offset + samplesRead, samplesRequired);
                phasePos += read;
                samplesRead += read;
                if (read < samplesRequired || (takeSamples > 0 && phasePos >= takeSamples))
                {
                    phase++;
                    phasePos = 0;
                }
            }

            if (phase == 4) // lead out
            {
                int samplesRequired = Math.Min(count - samplesRead, LeadOutSamples - phasePos);
                for (int n = 0; n < samplesRequired; n++)
                {
                    buffer[offset + samplesRead + n] = 0;
                }
                phasePos += samplesRequired;
                samplesRead += samplesRequired;
                if (phasePos >= LeadOutSamples)
                {
                    phase++;
                    phasePos = 0;
                }
            }

            return samplesRead;
        }
    }

    public static class UWaveProviderExtension
    {
        public static ISampleProvider USkip(this ISampleProvider parent, TimeSpan skip) {
            return new UOffsetSampleProvider(parent) { SkipOver = skip };
        }
    }
}
