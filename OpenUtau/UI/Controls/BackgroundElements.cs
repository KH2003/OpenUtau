﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

using OpenUtau.UI.Models;
using OpenUtau.Core;
using OpenUtau.Core.USTx;

namespace OpenUtau.UI.Controls
{
    class BackgroundElement : FrameworkElement
    {
        protected Size _size;
        protected bool _updated = false;

        public BackgroundElement()
        {
            this.SizeChanged += (o, e) => { _size = e.NewSize; MarkUpdate(); };
        }

        public void MarkUpdate() { _updated = true; }

        public void RenderIfUpdated() { if (_updated) this.InvalidateVisual(); _updated = false; }

        public static void MarkUpdateCallback(DependencyObject source, DependencyPropertyChangedEventArgs e) { ((BackgroundElement)source).MarkUpdate(); }
    }

    class TrackBackground : BackgroundElement
    {
        public double TrackHeight
        {
            set => SetValue(TrackHeightProperty, value);
            get => (double)GetValue(TrackHeightProperty);
        }

        public double OffsetY
        {
            set => SetValue(OffsetYProperty, value);
            get => (double)GetValue(OffsetYProperty);
        }

        public static readonly DependencyProperty TrackHeightProperty = DependencyProperty.Register("TrackHeight", typeof(double), typeof(TrackBackground), new PropertyMetadata(0.0, MarkUpdateCallback));
        public static readonly DependencyProperty OffsetYProperty = DependencyProperty.Register("OffsetY", typeof(double), typeof(TrackBackground), new PropertyMetadata(0.0, MarkUpdateCallback));

        public TrackBackground()
        {
            this.VerticalAlignment = VerticalAlignment.Stretch;
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.VisualEdgeMode = EdgeMode.Aliased;
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            int firstTrack = (int)(OffsetY / TrackHeight);
            bool alt = firstTrack % 2 == 1;
            double top = TrackHeight * firstTrack - OffsetY;

            while (top < _size.Height)
            {
                drawingContext.DrawRectangle(
                    alt ? ThemeManager.TrackBackgroundBrushAlt : ThemeManager.TrackBackgroundBrush,
                    null,
                    new Rect(0, (int)top, _size.Width, TrackHeight));
                top += TrackHeight;
                alt = !alt;
            }
        }
    }

    class KeyTrackBackground : TrackBackground
    {
        protected override void OnRender(DrawingContext drawingContext)
        {
            int firstTrack = (int)(OffsetY / TrackHeight);
            int alt = firstTrack % 12;
            double top = TrackHeight * firstTrack - OffsetY;

            while (top < _size.Height)
            {
                drawingContext.DrawRectangle(
                    MusicMath.IsBlackKey(11 - alt) ? ThemeManager.TrackBackgroundBrushAlt : ThemeManager.TrackBackgroundBrush,
                    null,
                    new Rect(0, (int)top, _size.Width, TrackHeight));
                top += TrackHeight;
                alt = (alt + 1) % 12;
            }
        }
    }

    class KeyboardBackground : KeyTrackBackground
    {
        protected override void OnRender(DrawingContext drawingContext)
        {
            int firstTrack = (int)(OffsetY / TrackHeight);
            int alt = firstTrack;
            int aalt = UIConstants.MaxNoteNum - alt - 1;
            double top = TrackHeight * firstTrack - OffsetY;
            var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            while (top < _size.Height)
            {
                drawingContext.DrawRectangle(
                    MusicMath.IsBlackKey(aalt) ? ThemeManager.BlackKeyBrushNormal : 
                    MusicMath.IsCenterKey(aalt) ? ThemeManager.CenterKeyBrushNormal : ThemeManager.WhiteKeyBrushNormal,
                    null,
                    new Rect(0, (int)top, _size.Width, TrackHeight));

                if (TrackHeight >= 12)
                {
                    FormattedText text = new FormattedText(
                        MusicMath.GetNoteString(aalt),
                        System.Threading.Thread.CurrentThread.CurrentUICulture,
                        FlowDirection.LeftToRight,
                        SystemFonts.CaptionFontFamily.GetTypefaces().First(),
                        12,
                        MusicMath.IsBlackKey(aalt) ? ThemeManager.BlackKeyNameBrushNormal :
                        MusicMath.IsCenterKey(aalt) ? ThemeManager.CenterKeyNameBrushNormal : ThemeManager.WhiteKeyNameBrushNormal,
                        pixelsPerDip
                    );
                    drawingContext.DrawText(text, new Point(42 - text.Width, (int)(top + (TrackHeight - text.Height) / 2)));
                }
                top += TrackHeight;
                alt ++;
                aalt--;
            }
        }
    }

    class TickBackground : BackgroundElement
    {
        public double QuarterWidth
        {
            set => SetValue(QuarterWidthProperty, value);
            get => (double)GetValue(QuarterWidthProperty);
        }

        public double MinTickWidth
        {
            set => SetValue(MinTickWidthProperty, value);
            get => (double)GetValue(MinTickWidthProperty);
        }

        public double OffsetX
        {
            set => SetValue(OffsetXProperty, value);
            get => (double)GetValue(OffsetXProperty);
        }

        public double QuarterOffset
        {
            set => SetValue(QuarterOffsetProperty, value);
            get => (double)GetValue(QuarterOffsetProperty);
        }

        public int BeatPerBar 
        {
            set => SetValue(BeatPerBarProperty, value);
            get => (int)GetValue(BeatPerBarProperty);
        }

        public int BeatUnit
        {
            set => SetValue(BeatUnitProperty, value);
            get => (int)GetValue(BeatUnitProperty);
        }

        public int TickMode
        {
            set => SetValue(TickModeProperty, value);
            get => (int)GetValue(TickModeProperty);
        }

        public int Resolution {
            set => SetValue(ResolutionProperty, value);
            get => (int)GetValue(ResolutionProperty);
        }

        public static readonly DependencyProperty QuarterWidthProperty = DependencyProperty.Register("QuarterWidth", typeof(double), typeof(TickBackground), new PropertyMetadata(0.0, MarkUpdateCallback));
        public static readonly DependencyProperty MinTickWidthProperty = DependencyProperty.Register("MinTickWidth", typeof(double), typeof(TickBackground), new PropertyMetadata(0.0, MarkUpdateCallback));
        public static readonly DependencyProperty OffsetXProperty = DependencyProperty.Register("OffsetX", typeof(double), typeof(TickBackground), new PropertyMetadata(0.0, MarkUpdateCallback));
        public static readonly DependencyProperty QuarterOffsetProperty = DependencyProperty.Register("QuarterOffset", typeof(double), typeof(TickBackground), new PropertyMetadata(0.0, MarkUpdateCallback));
        public static readonly DependencyProperty BeatPerBarProperty = DependencyProperty.Register("BeatPerBar", typeof(int), typeof(TickBackground), new PropertyMetadata(0, MarkUpdateCallback));
        public static readonly DependencyProperty BeatUnitProperty = DependencyProperty.Register("BeatUnit", typeof(int), typeof(TickBackground), new PropertyMetadata(0, MarkUpdateCallback));
        public static readonly DependencyProperty TickModeProperty = DependencyProperty.Register("TickMode", typeof(int), typeof(TickBackground), new PropertyMetadata(0, MarkUpdateCallback));
        public static readonly DependencyProperty ResolutionProperty = DependencyProperty.Register("Resolution", typeof(int), typeof(TickBackground), new PropertyMetadata(0, MarkUpdateCallback));

        protected Pen darkPen, lightPen, dashedPen;

        public TickBackground()
        {
            this.VerticalAlignment = VerticalAlignment.Stretch;
            this.HorizontalAlignment = HorizontalAlignment.Stretch;
            this.VisualEdgeMode = EdgeMode.Aliased;
            darkPen = new Pen(ThemeManager.TickLineBrushDark, 1);
            lightPen = new Pen(ThemeManager.TickLineBrushLight, 1);
            dashedPen = new Pen(ThemeManager.TickLineBrushLight, 1) { DashStyle = new DashStyle(UIConstants.DashLineArray, 0) };
        }

        protected override void OnRender(DrawingContext drawingContext)
        {
            double zoomRatio = MusicMath.getZoomRatio(QuarterWidth, BeatPerBar, BeatUnit, MinTickWidth);
            double interval = zoomRatio * QuarterWidth;
            int tick = (int)((OffsetX + QuarterOffset * QuarterWidth) / interval) + 1;
            double left = tick * interval - OffsetX - QuarterOffset * QuarterWidth;

            while (left < _size.Width)
            {
                double snappedLeft = Math.Round(left) + 0.5;
                if ((tick * zoomRatio * BeatUnit) % (BeatPerBar * 4) == 0)
                {
                    drawingContext.DrawLine(darkPen, new Point(snappedLeft, -0.5), new Point(snappedLeft, ActualHeight + 0.5));
                }
                else if ((tick * zoomRatio * BeatUnit) % 4 == 0)
                {
                    if (TickMode == 1)
                        drawingContext.DrawLine(darkPen, new Point(snappedLeft, -0.5), new Point(snappedLeft, ActualHeight + 0.5));
                    else
                        drawingContext.DrawLine(lightPen, new Point(snappedLeft, -0.5), new Point(snappedLeft, ActualHeight + 0.5));
                }
                else if ((tick * zoomRatio * BeatUnit) % 1 == 0)
                {
                    if (TickMode == 1)
                        drawingContext.DrawLine(lightPen, new Point(snappedLeft, -0.5), new Point(snappedLeft, ActualHeight + 0.5));
                    else
                        drawingContext.DrawLine(dashedPen, new Point(snappedLeft, -0.5), new Point(snappedLeft, ActualHeight + 0.5));
                }
                else
                {
                    drawingContext.DrawLine(dashedPen, new Point(snappedLeft, -0.5), new Point(snappedLeft, ActualHeight + 0.5));
                }
                left += interval;
                tick++;
            }
        }
    }

    class TimelineBackground : TickBackground
    {
        Dictionary<int, FormattedText> fTextPool = new Dictionary<int, FormattedText>();
        Dictionary<int, FormattedText> fTextPoolSubBpm = new Dictionary<int, FormattedText>();

        protected override void OnRender(DrawingContext drawingContext)
        {
            double zoomRatio = MusicMath.getZoomRatio(QuarterWidth, BeatPerBar, BeatUnit, MinTickWidth);
            double interval = zoomRatio * QuarterWidth;
            int tick = (int)((OffsetX + QuarterOffset * QuarterWidth) / interval);
            double left = tick * interval - OffsetX - QuarterOffset * QuarterWidth;
            bool first_tick = true;
            var pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            while (left < _size.Width)
            {
                double snappedLeft = Math.Round(left) + 0.5;
                if ((tick * zoomRatio * BeatUnit) % (BeatPerBar * 4) == 0)
                {
                    if (!first_tick) drawingContext.DrawLine(darkPen, new Point(snappedLeft, -0.5), new Point(snappedLeft, ActualHeight + 0.5));
                    int barNumber = (int)((tick * zoomRatio * BeatUnit) / BeatPerBar / 4 + 1);

                    FormattedText fText;
                    if (!fTextPool.ContainsKey(barNumber))
                    {
                        fText = new FormattedText(
                            barNumber.ToString(),
                            System.Threading.Thread.CurrentThread.CurrentUICulture,
                            FlowDirection.LeftToRight, SystemFonts.CaptionFontFamily.GetTypefaces().First(),
                            12,
                            darkPen.Brush,
                            pixelsPerDip);
                        fTextPool.Add(barNumber, fText);
                    }
                    else fText = fTextPool[barNumber];
                    drawingContext.DrawText(fText, new Point(snappedLeft + 3, 8));
                }
                if (DocManager.Inst.Project.SubBPM.ContainsKey((int)(tick * zoomRatio * Resolution)))
                {
                    FormattedText fText;
                    if (!fTextPoolSubBpm.ContainsKey((int)(tick * zoomRatio * Resolution)))
                    {
                        fText = new FormattedText(
                            DocManager.Inst.Project.SubBPM[(int)(tick * zoomRatio * Resolution)].ToString(),
                            System.Threading.Thread.CurrentThread.CurrentUICulture,
                            FlowDirection.LeftToRight, SystemFonts.CaptionFontFamily.GetTypefaces().First(),
                            12,
                            darkPen.Brush,
                            pixelsPerDip);
                        fTextPoolSubBpm.Add((int)(tick * zoomRatio * Resolution), fText);
                    }
                    else if (!fTextPoolSubBpm[(int)(tick * zoomRatio * Resolution)].Text.Equals(DocManager.Inst.Project.SubBPM[(int)(tick * zoomRatio * Resolution)].ToString()))
                    {
                        fText = new FormattedText(
                            DocManager.Inst.Project.SubBPM[(int)(tick * zoomRatio * Resolution)].ToString(),
                            System.Threading.Thread.CurrentThread.CurrentUICulture,
                            FlowDirection.LeftToRight, SystemFonts.CaptionFontFamily.GetTypefaces().First(),
                            12,
                            darkPen.Brush,
                            pixelsPerDip);
                        fTextPoolSubBpm[(int)(tick * zoomRatio * Resolution)]= fText;
                    }
                    else fText = fTextPoolSubBpm[(int)(tick * zoomRatio * Resolution)];
                    drawingContext.DrawText(fText, new Point(snappedLeft + 3, 0));
                }
                left += interval;
                tick++;
                first_tick = false;
            }
        }
    }
}
