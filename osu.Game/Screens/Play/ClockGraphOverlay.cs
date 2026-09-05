// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Graphics.Sprites;

namespace osu.Game.Screens.Play
{
    public partial class ClockGraphOverlay : Container
    {
        private const int sample_count = 240;
        private const float column_width = 3f;
        private const int bar_height = 2;
        private const int grid_line_count = 6;
        private const double pad_fraction = 0.1;

        private readonly Box[] selfBars = new Box[sample_count];
        private readonly Box[] sourceBars = new Box[sample_count];
        private readonly Box[] trackBars = new Box[sample_count];
        private readonly double[] selfSamples = new double[sample_count];
        private readonly double[] sourceSamples = new double[sample_count];
        private readonly double[] trackSamples = new double[sample_count];

        private readonly Box[] gridLines = new Box[grid_line_count];
        private readonly OsuSpriteText[] gridLabels = new OsuSpriteText[grid_line_count];

        private int cursor;
        private int samplesTaken;

        private double windowMin;
        private double windowMax;
        private double windowPad;

        /// <summary>
        /// The clock container to read the interpolator from. Set by the layer that adds this overlay.
        /// </summary>
        public GameplayClockContainer GameplayClock { get; set; } = null!;

        public ClockGraphOverlay()
        {
            Anchor = Anchor.BottomRight;
            Origin = Anchor.BottomRight;
            Width = sample_count * column_width;
            Height = 160;
            Margin = new MarginPadding { Bottom = 20, Right = 20 };
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new Box { Colour = new Colour4(0, 0, 0, 180), RelativeSizeAxes = Axes.Both });

            for (int i = 0; i < grid_line_count; i++)
            {
                gridLines[i] = new Box { Colour = new Colour4(255, 255, 255, 60), Height = 1 };
                Add(gridLines[i]);
                gridLabels[i] = new OsuSpriteText { Colour = Colour4.White, Font = FontUsage.Default.With(size: 10) };
                Add(gridLabels[i]);
            }

            for (int i = 0; i < sample_count; i++)
            {
                selfBars[i] = new Box { Colour = Colour4.Red, Width = column_width, Height = bar_height };
                sourceBars[i] = new Box { Colour = Colour4.Cyan, Width = column_width, Height = bar_height };
                trackBars[i] = new Box { Colour = Colour4.Yellow, Width = column_width, Height = bar_height };
                Add(selfBars[i]);
                Add(sourceBars[i]);
                Add(trackBars[i]);
            }
        }

        private float yOf(double valueMs)
        {
            double top = windowMax + windowPad;
            double bottom = windowMin - windowPad;
            if (top <= bottom) return 0;
            return DrawHeight * (float)((top - valueMs) / (top - bottom));
        }

        protected override void Update()
        {
            base.Update();

            var interp = GameplayClock?.BeatmapClock.InterpolatedTrack;
            if (interp == null)
                return;

            double self = interp.CurrentTime;
            double source = interp.Source.CurrentTime;
            double track = GameplayClock.BeatmapClock.Source.CurrentTime;

            selfSamples[cursor] = self;
            sourceSamples[cursor] = source;
            trackSamples[cursor] = track;
            cursor = (cursor + 1) % sample_count;
            samplesTaken++;

            // range = min/max over the visible window (leftmost..rightmost), plus padding.
            int valid = Math.Min(samplesTaken, sample_count);
            windowMin = double.MaxValue;
            windowMax = double.MinValue;

            // newest sample is at cursor-1, walk back over the valid filled entries.
            for (int offset = 0; offset < valid; offset++)
            {
                int idx = (cursor - 1 - offset + sample_count * 2) % sample_count;
                double v = Math.Min(selfSamples[idx], Math.Min(sourceSamples[idx], trackSamples[idx]));
                if (v < windowMin) windowMin = v;
                v = Math.Max(selfSamples[idx], Math.Max(sourceSamples[idx], trackSamples[idx]));
                if (v > windowMax) windowMax = v;
            }

            if (windowMin > windowMax) return;
            windowPad = (windowMax - windowMin) * pad_fraction;

            // gridlines evenly spanning the range
            double span = (windowMax + windowPad) - (windowMin - windowPad);
            string format = span < 20 ? "F1" : span < 2 ? "F2" : "N0";
            for (int i = 0; i < grid_line_count; i++)
            {
                double ms = windowMax + windowPad - (windowMax + windowPad - (windowMin - windowPad)) * i / (grid_line_count - 1);
                float gy = yOf(ms);
                gridLines[i].X = 0;
                gridLines[i].Y = gy;
                gridLines[i].Width = DrawWidth;
                gridLabels[i].Text = ms.ToString(format);
                gridLabels[i].Position = new osuTK.Vector2(4, gy - 10);
            }

            for (int col = 0; col < sample_count; col++)
            {
                int sampleIdx = (cursor + 1 + col) % sample_count;

                float selfY = yOf(selfSamples[sampleIdx]);
                float sourceY = yOf(sourceSamples[sampleIdx]);
                float trackY = yOf(trackSamples[sampleIdx]);

                selfBars[col].X = col * column_width;
                selfBars[col].Y = selfY - bar_height;

                sourceBars[col].X = col * column_width;
                sourceBars[col].Y = sourceY - bar_height;

                trackBars[col].X = col * column_width;
                trackBars[col].Y = trackY - bar_height;
            }
        }
    }
}
