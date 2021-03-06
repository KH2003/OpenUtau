﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using System.ComponentModel;

using OpenUtau.Core;
using OpenUtau.Core.USTx;
using OpenUtau.UI.Controls;

namespace OpenUtau.UI.Models
{
    internal class MidiViewModel : INotifyPropertyChanged, ICmdSubscriber
    {
        # region Properties

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged(string name)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public UProject Project => DocManager.Inst.Project;

        public bool LyricsPresetDedicate { get; set; }

        UVoicePart _part;
        public UVoicePart Part => _part;

        public Canvas TimelineCanvas;
        public Canvas MidiCanvas;
        public Canvas ExpCanvas;
        public Canvas PhonemeCanvas;

        protected bool _updated = false;
        public void MarkUpdate() { _updated = true; }

        string _title = "New Part";
        double _trackHeight = UIConstants.NoteDefaultHeight;
        double _quarterCount = UIConstants.MinQuarterCount;
        double _quarterWidth = UIConstants.MidiQuarterDefaultWidth;
        double _viewWidth;
        double _viewHeight;
        double _offsetX = 0;
        double _offsetY = UIConstants.NoteDefaultHeight * 5 * 12;
        double _quarterOffset = 0;
        double _minTickWidth = UIConstants.MidiTickMinWidth;
        int _beatPerBar = 4;
        int _beatUnit = 4;
        int _visualPosTick;
        int _visualDurTick;
        bool _showPhoneme = true;
        bool _showPitch = true;
        bool _snap = true;

        public string Title { set { _title = value; OnPropertyChanged("Title"); } get => "Midi Editor - " + _title;
        }
        public double TotalHeight => UIConstants.MaxNoteNum * _trackHeight - _viewHeight;
        public double TotalWidth => _quarterCount * _quarterWidth - _viewWidth;
        public double QuarterCount { set { _quarterCount = value; HorizontalPropertiesChanged(); } get => _quarterCount;
        }
        public double TrackHeight
        {
            set
            {
                _trackHeight = Math.Max(ViewHeight / UIConstants.MaxNoteNum, Math.Max(UIConstants.NoteMinHeight, Math.Min(UIConstants.NoteMaxHeight, value)));
                VerticalPropertiesChanged();
            }
            get => _trackHeight;
        }

        public double QuarterWidth
        {
            set
            {
                _quarterWidth = Math.Max(ViewWidth / QuarterCount, Math.Max(UIConstants.MidiQuarterMinWidth, Math.Min(UIConstants.MidiQuarterMaxWidth, value)));
                HorizontalPropertiesChanged();
            }
            get => _quarterWidth;
        }

        public double ViewWidth { set { _viewWidth = value; HorizontalPropertiesChanged(); QuarterWidth = QuarterWidth; OffsetX = OffsetX; } get => _viewWidth;
        }
        public double ViewHeight { set { _viewHeight = value; VerticalPropertiesChanged(); TrackHeight = TrackHeight; OffsetY = OffsetY; } get => _viewHeight;
        }
        public double OffsetX { set { _offsetX = Math.Min(TotalWidth, Math.Max(0, value)); HorizontalPropertiesChanged(); } get => _offsetX;
        }
        public double OffsetY { set { _offsetY = Math.Min(TotalHeight, Math.Max(0, value)); VerticalPropertiesChanged(); } get => _offsetY;
        }
        public double ViewportSizeX { get { if (TotalWidth < 1) return 10000; else return ViewWidth * (TotalWidth + ViewWidth) / TotalWidth; } }
        public double ViewportSizeY { get { if (TotalHeight < 1) return 10000; else return ViewHeight * (TotalHeight + ViewHeight) / TotalHeight; } }
        public double SmallChangeX => ViewportSizeX / 10;
        public double SmallChangeY => ViewportSizeY / 10;
        public double QuarterOffset { set { _quarterOffset = value; HorizontalPropertiesChanged(); } get => _quarterOffset;
        }
        public double MinTickWidth { set { _minTickWidth = value; HorizontalPropertiesChanged(); } get => _minTickWidth;
        }
        public int BeatPerBar { set { _beatPerBar = value; HorizontalPropertiesChanged(); } get => _beatPerBar;
        }
        public int BeatUnit { set { _beatUnit = value; HorizontalPropertiesChanged(); } get => _beatUnit;
        }
        public bool ShowPitch { set { _showPitch = value; if(notesElement != null)notesElement.ShowPitch = value; OnPropertyChanged("ShowPitch"); } get => _showPitch;
        }
        public bool ShowPhoneme { set { _showPhoneme = value; OnPropertyChanged("PhonemeVisibility"); OnPropertyChanged("ShowPhoneme"); } get => _showPhoneme;
        }
        public Visibility PhonemeVisibility => _showPhoneme ? Visibility.Visible : Visibility.Collapsed;
        public bool Snap { set { _snap = value; OnPropertyChanged("Snap"); } get => _snap;
        }
        
        public bool? AutoConvert { set { if (Part != null) Part.ConvertStyle = value; OnPropertyChanged(nameof(AutoConvert)); } get => Part?.ConvertStyle; }

        public void HorizontalPropertiesChanged()
        {
            OnPropertyChanged("QuarterWidth");
            OnPropertyChanged("TotalWidth");
            OnPropertyChanged("OffsetX");
            OnPropertyChanged("ViewportSizeX");
            OnPropertyChanged("SmallChangeX");
            OnPropertyChanged("QuarterOffset");
            OnPropertyChanged("QuarterCount");
            OnPropertyChanged("MinTickWidth");
            OnPropertyChanged("BeatPerBar");
            OnPropertyChanged("BeatUnit");
            MarkUpdate();
        }

        public void VerticalPropertiesChanged()
        {
            OnPropertyChanged("TrackHeight");
            OnPropertyChanged("TotalHeight");
            OnPropertyChanged("OffsetY");
            OnPropertyChanged("ViewportSizeY");
            OnPropertyChanged("SmallChangeY");
            MarkUpdate();
        }

        # endregion
        public bool AnyNotesEditing { get; set; }
        Dictionary<string, ExpElement> expElements = new Dictionary<string, ExpElement>();
        public NotesElement notesElement;
        public PhonemesElement phonemesElement;
        public ExpElement visibleExpElement, shadowExpElement;

        public MidiViewModel() { }

        public void RedrawIfUpdated()
        {
            if (LyricsPresetDedicate) ShowPitch = false;
            if (_updated)
            {
                if (visibleExpElement != null)
                {
                    visibleExpElement.X = -OffsetX;
                    visibleExpElement.ScaleX = QuarterWidth / Project.Resolution;
                    visibleExpElement.VisualHeight = ExpCanvas.ActualHeight;
                }
                if (shadowExpElement != null)
                {
                    shadowExpElement.X = -OffsetX;
                    shadowExpElement.ScaleX = QuarterWidth / Project.Resolution;
                    shadowExpElement.VisualHeight = ExpCanvas.ActualHeight;
                }
                if (notesElement != null)
                {
                    notesElement.X = -OffsetX;
                    notesElement.Y = -OffsetY;
                    notesElement.VisualHeight = MidiCanvas.ActualHeight;
                    notesElement.TrackHeight = TrackHeight;
                    notesElement.QuarterWidth = QuarterWidth;
                }
                if (phonemesElement != null)
                {
                    phonemesElement.X = -OffsetX;
                    phonemesElement.QuarterWidth = QuarterWidth;
                }
                updatePlayPosMarker();
            }
            _updated = false;
            foreach (var pair in expElements) pair.Value.RedrawIfUpdated();
            if (notesElement != null) notesElement.RedrawIfUpdated();
            if (phonemesElement != null && ShowPhoneme) phonemesElement.RedrawIfUpdated();
        }

        # region PlayPosMarker

        public int playPosTick = 0;
        Path playPosMarker;
        Rectangle playPosMarkerHighlight;

        private void initPlayPosMarker()
        {
            playPosTick = DocManager.Inst.playPosTick;
            if (playPosMarker == null)
            {
                playPosMarker = new Path()
                {
                    Fill = ThemeManager.TickLineBrushDark,
                    Data = Geometry.Parse("M 0 0 L 13 0 L 13 3 L 6.5 9 L 0 3 Z")
                };
                TimelineCanvas.Children.Add(playPosMarker);

                playPosMarkerHighlight = new Rectangle()
                {
                    Fill = ThemeManager.TickLineBrushDark,
                    Opacity = 0.25,
                    Width = 32
                };
                MidiCanvas.Children.Add(playPosMarkerHighlight);
                Canvas.SetZIndex(playPosMarkerHighlight, UIConstants.PosMarkerHightlighZIndex);
            }
        }

        public void updatePlayPosMarker()
        {
            if (Part == null) return;
            double quarter = (double)(playPosTick - (ViewMode ? 0 : Part.PosTick)) / DocManager.Inst.Project.Resolution;
            int playPosMarkerOffset = (int)Math.Round(QuarterToCanvas(quarter) + 0.5);
            Canvas.SetLeft(playPosMarker, playPosMarkerOffset - 6);
            playPosMarkerHighlight.Height = MidiCanvas.ActualHeight;
            double zoomRatio = MusicMath.getZoomRatio(QuarterWidth, BeatPerBar, BeatUnit, MinTickWidth);
            double interval = zoomRatio * QuarterWidth;
            int left = (int)Math.Round(QuarterToCanvas((int)(quarter / zoomRatio) * zoomRatio) + 0.5);
            playPosMarkerHighlight.Width = interval;
            Canvas.SetLeft(playPosMarkerHighlight, left);
        }

        #endregion

        #region Selection

        public List<UNote> SelectedNotes = new List<UNote>();
        public List<UNote> TempSelectedNotes = new List<UNote>();
        public List<UNote> ClippedNotes = new List<UNote>();
        public bool Copiable => SelectedNotes.Any();
        public bool Pastable => ClippedNotes.Any();

        public void SelectAll() { SelectedNotes.Clear(); foreach (UNote note in Part.Notes) { SelectedNotes.Add(note); note.Selected = true; } DocManager.Inst.ExecuteCmd(new RedrawNotesNotification()); OnPropertyChanged("Copiable"); }
        public void DeselectAll() { SelectedNotes.Clear(); foreach (UNote note in Part.Notes) note.Selected = false; DocManager.Inst.ExecuteCmd(new RedrawNotesNotification()); OnPropertyChanged("Copiable"); }

        public void SelectNote(UNote note) { if (!SelectedNotes.Contains(note)) { SelectedNotes.Add(note); note.Selected = true; DocManager.Inst.ExecuteCmd(new RedrawNotesNotification()); OnPropertyChanged("Copiable"); } }
        public void DeselectNote(UNote note) { SelectedNotes.Remove(note); note.Selected = false; DocManager.Inst.ExecuteCmd(new RedrawNotesNotification()); OnPropertyChanged("Copiable"); }

        public void CopyNotes()
        {
            ClippedNotes.Clear();
            foreach (var selected in SelectedNotes)
            {
                ClippedNotes.Add(selected.Clone());
            }
            OnPropertyChanged("Pastable");
        }

        public void SelectTempNote(UNote note) { TempSelectedNotes.Add(note); note.Selected = true; }
        public void TempSelectInBox(double quarter1, double quarter2, int noteNum1, int noteNum2)
        {
            if (quarter2 < quarter1) { double temp = quarter1; quarter1 = quarter2; quarter2 = temp; }
            if (noteNum2 < noteNum1) { int temp = noteNum1; noteNum1 = noteNum2; noteNum2 = temp; }
            int tick1 = (int)(quarter1 * Project.Resolution);
            int tick2 = (int)(quarter2 * Project.Resolution);
            foreach (UNote note in TempSelectedNotes) note.Selected = false;
            TempSelectedNotes.Clear();
            foreach (UNote note in Part.Notes)
            {
                if (note.PosTick <= tick2 && note.PosTick + note.DurTick >= tick1 &&
                    note.NoteNum >= noteNum1 && note.NoteNum <= noteNum2) SelectTempNote(note);
            }
            DocManager.Inst.ExecuteCmd(new RedrawNotesNotification());
        }

        public void DoneTempSelect()
        {
            foreach (UNote note in TempSelectedNotes) SelectNote(note);
            TempSelectedNotes.Clear();
        }

        # endregion

        # region Calculation

        public double GetSnapUnit() { return Snap ? OpenUtau.Core.MusicMath.getZoomRatio(QuarterWidth, BeatPerBar, BeatUnit, MinTickWidth) : 1.0 / 96; }
        public double CanvasToQuarter(double X) { return (X + OffsetX) / QuarterWidth; }
        public double QuarterToCanvas(double X) { return X * QuarterWidth - OffsetX; }
        public double CanvasToSnappedQuarter(double X)
        {
            double quater = CanvasToQuarter(X);
            double snapUnit = GetSnapUnit();
            return (int)(quater / snapUnit) * snapUnit;
        }
        public double CanvasToNextSnappedQuarter(double X)
        {
            double quater = CanvasToQuarter(X);
            double snapUnit = GetSnapUnit();
            return (int)(quater / snapUnit) * snapUnit + snapUnit;
        }
        public double CanvasRoundToSnappedQuarter(double X)
        {
            double quater = CanvasToQuarter(X);
            double snapUnit = GetSnapUnit();
            return Math.Round(quater / snapUnit) * snapUnit;
        }
        public int CanvasToSnappedTick(double X) { return (int)(CanvasToSnappedQuarter(X) * Project.Resolution); }
        public double TickToCanvas(int tick) { return (QuarterToCanvas((double)tick / Project.Resolution)); }

        public int CanvasToNoteNum(double Y) { return UIConstants.MaxNoteNum - 1 - (int)((Y + OffsetY) / TrackHeight); }
        public double CanvasToPitch(double Y) { return UIConstants.MaxNoteNum - 1 - (Y + OffsetY) / TrackHeight + 0.5; }
        public double NoteNumToCanvas(int noteNum) { return TrackHeight * (UIConstants.MaxNoteNum - 1 - noteNum) - OffsetY; }
        public double NoteNumToCanvas(double noteNum) { return TrackHeight * (UIConstants.MaxNoteNum - 1 - noteNum) - OffsetY; }

        public virtual bool NoteIsInView(UNote note) // FIXME : improve performance
        {
            double leftTick = OffsetX / QuarterWidth * Project.Resolution - 512;
            double rightTick = leftTick + ViewWidth / QuarterWidth * Project.Resolution + 512;
            int adjust = (ViewMode ? Project.Parts.Find(part => part.PartNo == note.PartNo)?.PosTick ?? 0 : 0);
            return (note.PosTick + adjust < rightTick && note.EndTick + adjust > leftTick);
        }

        # endregion

        # region Cmd Handling

        private void UnloadPart()
        {
            SelectedNotes.Clear();
            Title = "";
            _part = null;

            if (notesElement != null)
            {
                notesElement.Part = null;
            }
            if (phonemesElement != null)
            {
                phonemesElement.Part = null;
            }

            foreach (var pair in expElements) { pair.Value.Part = null; pair.Value.MarkUpdate(); pair.Value.RedrawIfUpdated(); }
        }

        private void LoadPart(UPart part, UProject project)
        {
            if (part == Part) return;
            if (!(part is UVoicePart)) return;
            UnloadPart();
            _part = (UVoicePart)part;

            OnPartModified();

            if (notesElement == null)
            {
                notesElement = new NotesElement() { Key = "pitchbend", Part = this.Part, midiVM = this };
                MidiCanvas.Children.Add(notesElement);
            }
            else
            {
                notesElement.Part = this.Part;
            }

            if (phonemesElement == null)
            {
                phonemesElement = new PhonemesElement() { Part = this.Part, midiVM = this };
                PhonemeCanvas.Children.Add(phonemesElement);
            }else
            {
                phonemesElement.Part = this.Part;
            }

            foreach (var pair in expElements) { pair.Value.Part = this.Part; pair.Value.MarkUpdate(); }
            BeatPerBar = project.BeatPerBar;
            BeatUnit = project.BeatUnit;
            initPlayPosMarker();
        }

        private bool _viewOnly;
        public bool ViewMode { get => _viewOnly;
            set {
                _viewOnly = value;
                MarkUpdate();
            }
        }

        UVoicePart _viewPart;
        public UVoicePart ViewingPart => ViewMode ? (_viewPart ?? (_viewPart = _part)) : _part;

        public void ToggleViewMode(bool toggle) {
            ViewMode = toggle;
            var oriOff = OffsetX;
            var newOff = oriOff;
            MidiCanvas.Children.Remove(notesElement);
            PhonemeCanvas.Children.Remove(phonemesElement);
            if (toggle) {
                notesElement = new ViewOnlyNotesElement() { Key = "pitchbend", Part = ViewingPart, midiVM = this};
                phonemesElement = new ViewOnlyPhonemesElement() { Part = ViewingPart, midiVM = this };
                foreach (var exp in new Dictionary<string, ExpElement>(expElements))
                {
                    ExpCanvas.Children.Remove(exp.Value);
                    if (expElements[exp.Key] is FloatExpElement) expElements[exp.Key] = new ViewOnlyFloatExpElement() { Key = exp.Value.Key, Part = ViewingPart, midiVM = this, DisplayMode = exp.Value.DisplayMode };
                    else if (expElements[exp.Key] is BoolExpElement) expElements[exp.Key] = new ViewOnlyBoolExpElement() { Key = exp.Value.Key, Part = ViewingPart, midiVM = this, DisplayMode = exp.Value.DisplayMode };
                    expElements[exp.Key].DisplayMode = exp.Value.DisplayMode;
                    ExpCanvas.Children.Add(expElements[exp.Key]);
                    if (exp.Value.DisplayMode == ExpDisMode.Shadow) shadowExpElement = expElements[exp.Key];
                    if (exp.Value.DisplayMode == ExpDisMode.Visible) visibleExpElement = expElements[exp.Key];
                    expElements[exp.Key].MarkUpdate();
                }
                newOff = TickToCanvas(Part.PosTick + CanvasToSnappedTick(0)) + OffsetX;
            }
            else
            {
                if (ViewingPart != Part) _part = ViewingPart;
                notesElement = new NotesElement() { Key = "pitchbend", Part = Part, midiVM = this };
                phonemesElement = new PhonemesElement() { Part = Part, midiVM = this };
                foreach (var exp in new Dictionary<string, ExpElement>(expElements))
                {
                    ExpCanvas.Children.Remove(exp.Value);
                    if (expElements[exp.Key] is FloatExpElement) expElements[exp.Key] = new FloatExpElement() { Key = exp.Value.Key, Part = Part, midiVM = this, DisplayMode = exp.Value.DisplayMode };
                    if (expElements[exp.Key] is BoolExpElement) expElements[exp.Key] = new BoolExpElement() { Key = exp.Value.Key, Part = Part, midiVM = this, DisplayMode = exp.Value.DisplayMode };
                    expElements[exp.Key].DisplayMode = exp.Value.DisplayMode;
                    ExpCanvas.Children.Add(expElements[exp.Key]);
                    if (exp.Value.DisplayMode == ExpDisMode.Shadow) shadowExpElement = expElements[exp.Key];
                    if (exp.Value.DisplayMode == ExpDisMode.Visible) visibleExpElement = expElements[exp.Key];
                    expElements[exp.Key].MarkUpdate();
                }
                newOff = TickToCanvas(CanvasToSnappedTick(0) - Part.PosTick) + OffsetX;
            }
            /*foreach (var pair in expElements) pair.Value.DisplayMode = ExpDisMode.Hidden;
            if (shadowExpElement != null) shadowExpElement.DisplayMode = ExpDisMode.Shadow;
            visibleExpElement.DisplayMode = ExpDisMode.Visible;*/
            MidiCanvas.Children.Add(notesElement);
            PhonemeCanvas.Children.Add(phonemesElement);
            OnPartModified();
            OffsetX = newOff;
            MarkUpdate();
        }

        private void OnPartModified()
        {
            Title = ViewingPart.Name;
            QuarterOffset = (double)(ViewMode ? 0 : Part.PosTick) / Project.Resolution;
            int projectDurTick = Project.Parts.OrderBy(part => part.EndTick).LastOrDefault()?.EndTick ?? 0;
            QuarterCount = (double)(ViewMode ? projectDurTick : Part.DurTick) / Project.Resolution;
            QuarterWidth = QuarterWidth;
            OffsetX = OffsetX;
            MarkUpdate();
            _visualPosTick = ViewMode ? 0 : Part.PosTick;
            _visualDurTick = ViewMode ? projectDurTick : Part.DurTick;
        }

        private void OnSelectExpression(UNotification cmd)
        {
            var _cmd = cmd as SelectExpressionNotification;
            if (!expElements.ContainsKey(_cmd.ExpKey))
            {
                ExpElement expEl = new ExpElement();
                if(ViewMode)
                {
                    if (DocManager.Inst.Project.ExpressionTable[_cmd.ExpKey] is IntExpression || DocManager.Inst.Project.ExpressionTable[_cmd.ExpKey] is FloatExpression)
                    expEl = new ViewOnlyFloatExpElement() { Key = _cmd.ExpKey, Part = this.Part, midiVM = this };
                else if (DocManager.Inst.Project.ExpressionTable[_cmd.ExpKey] is BoolExpression)
                    expEl = new ViewOnlyBoolExpElement() { Key = _cmd.ExpKey, Part = this.Part, midiVM = this };
                }
                else
                {
                    if (DocManager.Inst.Project.ExpressionTable[_cmd.ExpKey] is IntExpression || DocManager.Inst.Project.ExpressionTable[_cmd.ExpKey] is FloatExpression)
                        expEl = new FloatExpElement() { Key = _cmd.ExpKey, Part = this.Part, midiVM = this };
                    else if (DocManager.Inst.Project.ExpressionTable[_cmd.ExpKey] is BoolExpression)
                        expEl = new BoolExpElement() { Key = _cmd.ExpKey, Part = this.Part, midiVM = this };
                }

                expElements.Add(_cmd.ExpKey, expEl);
                ExpCanvas.Children.Add(expEl);
            }

            if (_cmd.UpdateShadow) shadowExpElement = visibleExpElement;
            visibleExpElement = expElements[_cmd.ExpKey];
            visibleExpElement.MarkUpdate();
            this.MarkUpdate();

            foreach (var pair in expElements) pair.Value.DisplayMode = ExpDisMode.Hidden;
            if (shadowExpElement != null) shadowExpElement.DisplayMode = ExpDisMode.Shadow;
            visibleExpElement.DisplayMode = ExpDisMode.Visible;
        }

        private void OnPlayPosSet(int playPosTick)
        {
            this.playPosTick = playPosTick;
            UpdateViewRegion(playPosTick);
            MarkUpdate();
        }

        public void UpdateViewRegion(int tick)
        {
            double tickPix = TickToCanvas(tick - (ViewMode ? 0 : (Part?.PosTick).GetValueOrDefault()));
            if (tickPix > MidiCanvas.ActualWidth * UIConstants.PlayPosMarkerMargin)
                OffsetX += tickPix - MidiCanvas.ActualWidth * UIConstants.PlayPosMarkerMargin;
            if (ViewMode && (ViewingPart.EndTick <= tick || tick <= ViewingPart.PosTick)) {
                UVoicePart uPart = Project.Parts.OfType<UVoicePart>().OrderBy(part=>part.TrackNo).ThenBy(part=>part.PosTick).SkipWhile(part=>Project.Tracks[part.TrackNo].ActuallyMuted).ToList().Find(part => part.PosTick <= tick && tick < part.EndTick);
                if (uPart != null)
                {
                    _viewPart = uPart;
                    if(notesElement != null)
                    {
                        notesElement.Part = ViewingPart;
                        notesElement.MarkUpdate();
                    }

                    if (phonemesElement != null)
                    {
                        phonemesElement.Part = ViewingPart;
                        phonemesElement.MarkUpdate();
                    }

                    foreach (var item in expElements)
                    {
                        item.Value.Part = ViewingPart;
                        item.Value.MarkUpdate();
                    }
                }
            }
        }

        private void OnPitchModified()
        {
            MarkUpdate();
            notesElement.MarkUpdate();
        }

        # endregion

        # region ICmdSubscriber

        public void Subscribe(ICmdPublisher publisher) { if (publisher != null) publisher.Subscribe(this); }

        public void OnNext(UCommand cmd, bool isUndo)
        {
            if (cmd is NoteCommand)
            {
                notesElement?.MarkUpdate();
                phonemesElement?.MarkUpdate();
            }
            else if (cmd is PartCommand)
            {
                var _cmd = cmd as PartCommand;
                if (_cmd.part != this.Part) return;
                else if (_cmd is RemovePartCommand) UnloadPart();
                else if (_cmd is ResizePartCommand) OnPartModified();
                else if (_cmd is MovePartCommand) OnPartModified();
            }
            else if (cmd is ExpCommand)
            {
                var _cmd = cmd as ExpCommand;
                if (_cmd is SetFloatExpCommand || _cmd is GlobelSetFloatExpCommand || _cmd is SetIntExpCommand || _cmd is GlobelSetIntExpCommand || _cmd is SetBoolExpCommand || _cmd is GlobelSetBoolExpCommand) expElements[_cmd.Key].MarkUpdate();
                else if (_cmd is PitchExpCommand) OnPitchModified();
            }
            else if (cmd is UNotification)
            {
                var _cmd = cmd as UNotification;
                if (_cmd is LoadPartNotification)
                {
                    if (!((_cmd as LoadPartNotification).Lite ^ LyricsPresetDedicate))
                    {
                        LoadPart(_cmd.part, _cmd.project);
                    }
                }
                else if (_cmd is LoadProjectNotification) UnloadPart();
                else if (_cmd is SelectExpressionNotification) OnSelectExpression(_cmd);
                else if (_cmd is ShowPitchExpNotification) { }
                else if (_cmd is HidePitchExpNotification) { }
                else if (_cmd is RedrawNotesNotification)
                {
                    if (notesElement != null) notesElement.MarkUpdate();
                    if (phonemesElement != null) phonemesElement.MarkUpdate();
                }
                else if (_cmd is SetPlayPosTickNotification) { OnPlayPosSet(((SetPlayPosTickNotification)_cmd).playPosTick); }
                else if (cmd is UpdateProjectPropertiesNotification)
                {
                    OnPropertyChanged("BeatPerBar");
                    OnPropertyChanged("BeatUnit");
                }
            }
        }

        public void PostOnNext(UCommandGroup cmds, bool isUndo) { }

        # endregion

    }
}
