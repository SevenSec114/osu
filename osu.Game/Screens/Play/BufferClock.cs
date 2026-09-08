// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Audio.Track;
using osu.Framework.Timing;

namespace osu.Game.Screens.Play
{
    public sealed class BufferClock : IAdjustableClock, IFrameBasedClock
    {
        private readonly double startTime;
        private readonly StopwatchClock stopWatch = new();
        private bool workingTrackStarted;
        private bool bufferComplete;
        private Track workingTrack { get; }
        public Track BufferTrack { get; }
        public Track Real => workingTrack;

        public double CurrentTime => bufferComplete
            ? workingTrack.CurrentTime
            : BufferTrack.CurrentTime + startTime;

        public bool IsRunning { get; private set; }

        public double ElapsedFrameTime { get; private set; }

        public double FramesPerSecond { get; private set; }

        public double Rate { get; set; } = 1;

        /// <summary>
        /// An encapsulated source to consume output device's buffer.
        /// </summary>
        public BufferClock(Track workingTrack, Track bufferTrack, double startTime)
        {
            this.workingTrack = workingTrack;
            workingTrack.Seek(0);
            BufferTrack = bufferTrack;
            this.startTime = startTime;
        }

        public void ProcessFrame()
        {
            double lastTime = CurrentTime;

            // Start the working track after elapsed start time.
            if (!workingTrackStarted && stopWatch.IsRunning && stopWatch.CurrentTime >= -startTime)
            {
                workingTrackStarted = true;
                workingTrack.Start();
            }

            // Buffer track is complete
            if (!bufferComplete && workingTrackStarted && BufferTrack.CurrentTime >= BufferTrack.Length)
            {
                bufferComplete = true;
            }

            ElapsedFrameTime = CurrentTime - lastTime;
        }

        public void Reset()
        {
            IsRunning = false;
            workingTrackStarted = false;
            bufferComplete = false;

            BufferTrack.Reset();
            workingTrack.Reset();
            stopWatch.Reset();
            ElapsedFrameTime = 0;
        }

        public void Start()
        {

            if (bufferComplete)
            {
                workingTrack.Start();
                IsRunning = true;
                return;
            }

            BufferTrack.Start();
            stopWatch.Start();

            if (workingTrackStarted)
                workingTrack.Start();

            IsRunning = true;
        }

        public void Stop()
        {
            IsRunning = false;

            BufferTrack.Stop();
            if (workingTrackStarted)
                workingTrack.Stop();

            stopWatch.Stop();
        }

        public bool Seek(double seek)
        {
            if (seek < 0)
            {
                workingTrackStarted = false;
                bufferComplete = false;

                stopWatch.Restart();

                bool success = BufferTrack.Seek(Math.Max(0, seek - startTime));

                return success;
            }

            bool realSuccess = workingTrack.Seek(seek);
            return realSuccess;
        }

        public void ResetSpeedAdjustments()
        {
            Rate = 1;
            BufferTrack.ResetSpeedAdjustments();
            workingTrack.ResetSpeedAdjustments();
        }
    }
}
