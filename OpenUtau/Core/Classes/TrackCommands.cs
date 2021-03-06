﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using OpenUtau.Core.USTx;
using System.Windows.Media;

namespace OpenUtau.Core
{
    public abstract class TrackCommand : UCommand
    {
        public UProject project;
        public UTrack track;
        public override void Execute()
        {
            ++track.ModifyCount;
        }
        public override void Unexecute()
        {
            --track.ModifyCount;
        }
        public void UpdateTrackNo()
        {
            Dictionary<int, int> trackNoRemapTable = new Dictionary<int, int>();
            for (int i = 0; i < project.Tracks.Count; i++)
            {
                if (project.Tracks[i].TrackNo != i)
                {
                    trackNoRemapTable.Add(project.Tracks[i].TrackNo, i);
                    project.Tracks[i].TrackNo = i;
                }
            }
            int j = 0;
            foreach (var part in project.Parts)
            {
                if (trackNoRemapTable.Keys.Contains(part.TrackNo))
                {
                    part.TrackNo = trackNoRemapTable[part.TrackNo];
                }
                part.PartNo = j;
                if (part is UVoicePart voice)
                {
                    foreach (var note in voice.Notes)
                    {
                        note.PartNo = j;
                    }
                }
                j++;
            }
        }
    }

    public class AddTrackCommand : TrackCommand
    {
        internal static int colorRandCount = 0;
        private static Random rand = new Random();
        public AddTrackCommand(UProject project, UTrack track) { this.project = project; this.track = track; }
        public override string ToString() { return "Add track"; }
        public override void Execute()
        {
            if (track.TrackNo < project.Tracks.Count) project.Tracks.Insert(track.TrackNo, track);
            else project.Tracks.Add(track);
            if (track.Color.Equals(Colors.Transparent))
            {
                track.Color = GenerateColor();
            }
            UpdateTrackNo();
            base.Execute();
        }

        internal static Color GenerateColor()
        {
            Color clrR;
            if (UI.ThemeManager.NoteFillBrushes.Count <= colorRandCount)
            {
                Color clr1 = UI.ThemeManager.NoteFillBrushes[rand.Next(UI.ThemeManager.NoteFillBrushes.Count)].Color;
                Color clr2 = UI.ThemeManager.NoteFillBrushes[rand.Next(UI.ThemeManager.NoteFillBrushes.Count)].Color;
                clrR = clr1 * (float)rand.NextDouble() + clr2 * (float)rand.NextDouble();
            }
            else
            {
                clrR = UI.ThemeManager.NoteFillBrushes[colorRandCount].Color;
            }
            colorRandCount++;
            return clrR;
        }

        public override void Unexecute() { project.Tracks.Remove(track); UpdateTrackNo();
            base.Unexecute();
        }
    }

    public class RemoveTrackCommand : TrackCommand
    {
        public List<UPart> removedParts = new List<UPart>();
        public RemoveTrackCommand(UProject project, UTrack track)
        {
            this.project = project;
            this.track = track;
            foreach (var part in project.Parts)
                if (part.TrackNo == track.TrackNo)
                    removedParts.Add(part);
        }
        public override string ToString() { return "Remove track"; }
        public override void Execute() {
            project.Tracks.Remove(track);
            foreach (var part in removedParts)
            {
                project.Parts.Remove(part);
                part.TrackNo = -1;
            }
            UpdateTrackNo();
            base.Execute();
        }
        public override void Unexecute()
        {
            if (track.TrackNo < project.Tracks.Count) project.Tracks.Insert(track.TrackNo, track);
            else project.Tracks.Add(track);
            foreach (var part in removedParts) project.Parts.Add(part);
            track.TrackNo = -1;
            UpdateTrackNo();
            base.Unexecute();
        }
    }

    public class TrackChangeSingerCommand : TrackCommand
    {
        USinger newSinger, oldSinger;
        public TrackChangeSingerCommand(UProject project, UTrack track, USinger newSinger) { this.project = project; this.track = track; this.newSinger = newSinger; this.oldSinger = track.Singer; }
        public override string ToString() { return "Change singer"; }
        public override void Execute() {
            track.Singer = newSinger;
            if (!project.Singers.Contains(newSinger))
                project.Singers.Add(newSinger);
            foreach (var item in project.Parts.Where(pt=>pt.TrackNo == track.TrackNo).OfType<UVoicePart>())
            {
                PartManager.UpdatePart(item);
            }
            base.Execute();
        }
        public override void Unexecute() { track.Singer = oldSinger;
            base.Unexecute();
        }
    }
}
