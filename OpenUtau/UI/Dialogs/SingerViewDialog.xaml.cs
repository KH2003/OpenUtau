﻿using System;
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
using System.Windows.Shapes;

using OpenUtau.UI.Controls;
using OpenUtau.Core;
using OpenUtau.Core.USTx;
using System.Windows.Forms;
using System.Collections.ObjectModel;
using static OpenUtau.Core.Formats.UtauSoundbank;

namespace OpenUtau.UI.Dialogs
{
    /// <summary>
    /// Interaction logic for SingerViewDialog.xaml
    /// </summary>
    public partial class SingerViewDialog : Window
    {
        List<string> singerNames;
        public SingerViewDialog()
        {
            InitializeComponent();
            UpdateSingers();
        }

        private void UpdateSingers()
        {
            singerNames = new List<string>();
            foreach (var pair in DocManager.Inst.Singers)
            {
                singerNames.Add(pair.Value.Name);
            }
            if (singerNames.Count > 0)
            {
                this.name.SelectedIndex = 0;
                SetSinger(singerNames[0]);
            }
            this.name.ItemsSource = singerNames;
        }
        USinger SelectedSinger;
        public void SetSinger(string singerName)
        {
            USinger singer = SelectedSinger = null;
            foreach(var pair in DocManager.Inst.Singers)
                if (pair.Value.Name == singerName)
                {
                    singer = pair.Value;
                }
            if (singer == null) return;
            SelectedSinger = singer;
            this.name.Text = singer.Name;
            this.avatar.Source = singer.Avatar;
            this.info.Text = "Author: " + singer.Author + "\nWebsite: " + singer.Website + "\nPath: " + singer.Path;
            var observable = new ObservableCollection<UOto>(singer.AliasMap.Values);
            otoview.ItemsSource = observable;
        }

        private void name_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SaveSinger(SelectedSinger);
            SetSinger(singerNames[this.name.SelectedIndex]);
        }

        private void butRefresh_Click(object sender, RoutedEventArgs e)
        {
            DocManager.Inst.SearchAllSingers();
            UpdateSingers();
        }

        private void otoview_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var item = (UOto)((FrameworkElement)e.OriginalSource).DataContext;
            var dialog = new OtoEditDialog(SelectedSinger, item);
            dialog.Closing += (sender1, e1) =>
            {
                if (dialog.DialogResult == true)
                {
                    var result = dialog.EditingOto;
                    if (SelectedSinger.AliasMap.ContainsKey(result.Alias))
                    {
                        FixConflictedOto(e1, result, result.Alias);
                    }
                    else if (SelectedSinger.AliasMap.ContainsKey(dialog.aliasBak))
                    {
                        FixConflictedOto(e1, result, dialog.aliasBak);
                    }
                    else
                    {
                        SelectedSinger.AliasMap.Add(result.Alias, result);
                    }
                    otoview.Items.Refresh();
                }
            };
            dialog.ShowDialog();
        }

        private void FixConflictedOto(System.ComponentModel.CancelEventArgs e1, UOto result, string alias)
        {
            UOto conflictedOto = SelectedSinger.AliasMap[alias];
            if (!otoview.SelectedItem.Equals(conflictedOto) && !conflictedOto.Equals(result))
            {
                MessageBoxManager.Yes = Lang.LanguageManager.GetLocalized("Replace");
                MessageBoxManager.No = Lang.LanguageManager.GetLocalized("Duplicate");
                MessageBoxManager.Register();
                var warningResult = System.Windows.Forms.MessageBox.Show(string.Format("Singer {0} already has alia {1} (old: {2}({3}) , new: {4}({5})), replace or duplicate?", SelectedSinger.Name, result.Alias, conflictedOto.Alias, conflictedOto.File, result.Alias, result.File), "Conflict", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information);
                MessageBoxManager.Unregister();
                switch (warningResult)
                {
                    case System.Windows.Forms.DialogResult.Yes:
                        SelectedSinger.AliasMap[alias] = result;
                        break;
                    case System.Windows.Forms.DialogResult.No:
                        int i = 1;
                        for (; SelectedSinger.AliasMap.ContainsKey(result.Alias + " (" + i + ")"); ++i) { }
                        result.Alias += " (" + i + ")";
                        SelectedSinger.AliasMap.Add(result.Alias, result);
                        break;
                    case System.Windows.Forms.DialogResult.Cancel:
                        e1.Cancel = true;
                        break;
                    default:
                        break;
                }
            }
            else
            {
                SelectedSinger.AliasMap[alias] = result;
            }
        }

        private void window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            SaveSinger(SelectedSinger);
        }

        private void butDuplicate_Click(object sender, RoutedEventArgs e)
        {
            if (otoview.SelectedItem != null && otoview.SelectedItem is UOto oto)
            {
                UOto newOto = new UOto() { Alias = oto.Alias, Consonant = oto.Consonant, Cutoff = oto.Cutoff, Duration = oto.Duration, File = oto.File, Offset = oto.Offset, Overlap = oto.Overlap, Preutter = oto.Preutter};
                int i = 1;
                for (; SelectedSinger.AliasMap.ContainsKey(newOto.Alias + " (" + i + ")"); ++i) { }
                newOto.Alias += " (" + i + ")";
                SelectedSinger.AliasMap.Add(newOto.Alias, newOto);
                otoview.Items.Refresh();
            }
        }

        private void butRemove_Click(object sender, RoutedEventArgs e)
        {
            if (otoview.SelectedItem != null && otoview.SelectedItem is UOto oto)
            {
                SelectedSinger.AliasMap.Remove(oto.Alias);
                otoview.Items.Refresh();
            }
        }
    }
}
