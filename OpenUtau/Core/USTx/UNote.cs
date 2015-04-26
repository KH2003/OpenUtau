﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpenUtau.Core.USTx
{
    /// <summary>
    /// Music note.
    /// </summary>
    public class UNote : IComparable
    {
        public int PosTick;
        public int DurTick;
        public int NoteNum;
        public string Lyric;
        public List<UPhoneme> Phonemes = new List<UPhoneme>();
        public Dictionary<string, UExpression> Expressions = new Dictionary<string, UExpression>();
        public PitchBendExpression PitchBend;
        public VibratoExpression Vibratio;
        public int Channel = 0;

        public int EndTick { get { return PosTick + DurTick; } }

        private UNote() {
            PitchBend = new PitchBendExpression(this);
            Vibratio = new VibratoExpression(this);
        }

        public static UNote Create() { return new UNote(); }

        public UNote Clone() {
            UNote _note = new UNote()
            {
                PosTick = PosTick,
                DurTick = DurTick,
                NoteNum = NoteNum,
                Lyric = Lyric,
                Channel = Channel
            };
            foreach (var phoneme in this.Phonemes) _note.Phonemes.Add(phoneme.Clone(_note));
            foreach (var pair in this.Expressions) _note.Expressions.Add(pair.Key, pair.Value.Clone(_note));
            _note.PitchBend = (PitchBendExpression)this.PitchBend.Clone(_note);
            return _note;
        }

        public string GetResamplerFlags() { return "\"\""; }

        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            UNote other = obj as UNote;
            if (other == null)
                throw new ArgumentException("CompareTo object is not a Note");

            if (other.Channel < this.Channel)
                return 1;
            else if (other.Channel > this.Channel)
                return -1;
            else if (other.PosTick < this.PosTick)
                return 1;
            else if (other.PosTick > this.PosTick)
                return -1;
            else if (other.GetHashCode() < this.GetHashCode())
                return 1;
            else if (other.GetHashCode() > this.GetHashCode())
                return -1;
            else
                return 0;
        }
    }
}
