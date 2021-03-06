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
using Microsoft.WindowsAPICodePack.Dialogs;
using OpenUtau.Core;

namespace OpenUtau.UI.Dialogs
{
    /// <summary>
    /// Interaction logic for Preferences.xaml
    /// </summary>
    public partial class PreferencesDialog : Window
    {
        private Grid _selectedGrid = null;
        private Grid SelectedGrid
        {
            set
            {
                if (_selectedGrid == value) return;
                if (_selectedGrid != null) _selectedGrid.Visibility = System.Windows.Visibility.Hidden;
                _selectedGrid = value;
                if (_selectedGrid != null) _selectedGrid.Visibility = System.Windows.Visibility.Visible;
            }
            get => _selectedGrid;
        }

        List<string> singerPaths;
        public PreferencesDialog()
        {
            InitializeComponent();

            pathsItem.IsSelected = true;
            UpdateSingerPaths();
            UpdateEngines();
            UpdateRenders();
            comboBoxLang.ItemsSource = Lang.LanguageManager.ListLanuage();
            comboBoxLang.SelectedItem = Lang.LanguageManager.GetLocalized("DisplayName");
            comboBoxLang.SelectionChanged += ComboBoxLang_SelectionChanged;
            chkboxEditOnEnter.IsChecked = Core.Util.Preferences.Default.EnterToEdit;
            ComboWavePlayer.SelectedItem = Core.Util.Preferences.Default.WavePlayer;
            chkboxAutoConvert.IsChecked = Core.Util.Preferences.Default.AutoConvertStyles;
            if (!NAudio.Wave.AsioOut.isSupported()) {
                ComboWavePlayer.Items.Remove("ASIO");
            }
            comboSamplingR.SelectedValue = Core.Util.Preferences.Default.BitDepth + ";" + Core.Util.Preferences.Default.SamplingRate;
            chkboxUseScript.IsChecked = Core.Util.Preferences.Default.UseScript;
            txtboxWavtool.Text = Core.Util.Preferences.Default.ScriptWavtool;
        }

        private void ComboBoxLang_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Lang.LanguageManager.LanguagesInDisplayName.TryGetValue(comboBoxLang.SelectedItem.ToString(), out string langTag);
            if (string.IsNullOrWhiteSpace(langTag)) return;
            Core.Util.Preferences.Default.Language = langTag;
            Lang.LanguageManager.UseLanguage(langTag);
        }

        private void treeView_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (treeView.SelectedItem == pathsItem) SelectedGrid = pathsGrid;
            else if (treeView.SelectedItem == themesItem) SelectedGrid = themesGrid;
            else if (treeView.SelectedItem == playbackItem) SelectedGrid = playbackGrid;
            else if (treeView.SelectedItem == renderingItem) SelectedGrid = renderingGrid;
            else if (treeView.SelectedItem == generalItem) SelectedGrid = generalGrid;
            else SelectedGrid = null;
        }
        #region Paths

        private void UpdateSingerPaths()
        {
            singerPaths = PathManager.Inst.GetSingerSearchPaths().ToList();
            singerPathsList.ItemsSource = singerPaths;
        }

        private void singerPathAddButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();
            if (result == System.Windows.Forms.DialogResult.OK)
            {
                PathManager.Inst.AddSingerSearchPath(dialog.SelectedPath);
                UpdateSingerPaths();
                DocManager.Inst.SearchAllSingers();
            }
        }

        private void singerPathRemoveButton_Click(object sender, RoutedEventArgs e)
        {
            PathManager.Inst.RemoveSingerSearchPath((string)singerPathsList.SelectedItem);
            UpdateSingerPaths();
            singerPathRemoveButton.IsEnabled = false;
            DocManager.Inst.SearchAllSingers();
        }


        private void singerPathsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            singerPathRemoveButton.IsEnabled = (string)singerPathsList.SelectedItem != PathManager.DefaultSingerPath;
        }

        # endregion

        # region Engine select

        List<string> engines;

        private void UpdateEngines()
        {
            if (Core.Util.Preferences.Default.InternalEnginePreview) this.previewRatioInternal.IsChecked = true;
            else this.previewRatioExternal.IsChecked = true;
            if (Core.Util.Preferences.Default.InternalEngineExport) this.exportRatioInternal.IsChecked = true;
            else this.exportRatioExternal.IsChecked = true;

            var enginesInfo = Core.ResamplerDriver.ResamplerDriver.SearchEngines(PathManager.Inst.GetEngineSearchPath());
            engines = enginesInfo.Select(x => x.Name).ToList();
            if (engines.Count == 0)
            {
                this.previewRatioInternal.IsChecked = true;
                this.exportRatioInternal.IsChecked = true;
                this.previewRatioExternal.IsEnabled = false;
                this.exportRatioExternal.IsEnabled = false;
                this.previewEngineCombo.IsEnabled = false;
                this.exportEngineCombo.IsEnabled = false;
            }
            else
            {
                this.previewEngineCombo.ItemsSource = engines;
                this.exportEngineCombo.ItemsSource = engines;
                previewEngineCombo.SelectedIndex = Math.Max(0, engines.IndexOf(Core.Util.Preferences.Default.ExternalPreviewEngine));
                exportEngineCombo.SelectedIndex = Math.Max(0, engines.IndexOf(Core.Util.Preferences.Default.ExternalExportEngine));
            }
        }

        private void previewEngine_Checked(object sender, RoutedEventArgs e)
        {
            Core.Util.Preferences.Default.InternalEnginePreview = sender == this.previewRatioInternal;
            Core.Util.Preferences.Save();
        }

        private void exportEngine_Checked(object sender, RoutedEventArgs e)
        {
            Core.Util.Preferences.Default.InternalEngineExport = sender == this.exportRatioInternal;
            Core.Util.Preferences.Save();
        }

        private void previewEngineCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Core.Util.Preferences.Default.ExternalPreviewEngine = engines[this.previewEngineCombo.SelectedIndex];
            Core.Util.Preferences.Save();
        }

        private void exportEngineCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Core.Util.Preferences.Default.ExternalExportEngine = engines[this.exportEngineCombo.SelectedIndex];
            Core.Util.Preferences.Save();
        }

        #endregion
        
        private void chkboxInstantRender_Click(object sender, RoutedEventArgs e)
        {
            Core.Util.Preferences.Default.RenderNoteAtInstant = chkboxInstantRender.IsChecked.Value;
            Core.Util.Preferences.Save();
        }

        private string[] RenderManager = new string[] { "Instant", "PreRender", "PPS" };

        private void comboRenderMana_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Core.Util.Preferences.Default.RenderManager = RenderManager[comboRenderMana.SelectedIndex];
            Core.Util.Preferences.Save();
        }

        private void UpdateRenders() {
            comboRenderMana.SelectedItem = Core.Util.Preferences.Default.RenderManager;
            if(comboRenderMana.SelectedItem == null)
            {
                int index = RenderManager.ToList().IndexOf(Core.Util.Preferences.Default.RenderManager);
                if (index != -1) comboRenderMana.SelectedIndex = index;
            }
            chkboxInstantRender.IsChecked = Core.Util.Preferences.Default.RenderNoteAtInstant;
        }

        private void chkboxEditOnEnter_Click(object sender, RoutedEventArgs e)
        {
            Core.Util.Preferences.Default.EnterToEdit = chkboxEditOnEnter.IsChecked.Value;
            Core.Util.Preferences.Save();
        }

        private void ComboWavePlayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Core.Util.Preferences.Default.WavePlayer = ComboWavePlayer.SelectedItem;
            Core.Util.Preferences.Save();
        }

        private void chkboxAutoConvert_Click(object sender, RoutedEventArgs e)
        {
            Core.Util.Preferences.Default.AutoConvertStyles = chkboxAutoConvert.IsChecked.Value;
            Core.Util.Preferences.Save();
        }

        private void comboSamplingR_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            Core.Util.Preferences.Default.SamplingRate = Convert.ToInt32(((comboSamplingR.SelectedItem as ComboBoxItem)?.Tag as string)?.Split(';')[1] ?? "44100");
            Core.Util.Preferences.Default.BitDepth = Convert.ToInt32(((comboSamplingR.SelectedItem as ComboBoxItem)?.Tag as string)?.Split(';')[0] ?? "16");
            Core.Util.Preferences.Save();
            PPSPlaybackManager.Inst.Master.RegenFormat();
        }

        private void butBrowseWavtool_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new CommonOpenFileDialog();
            dialog.Filters.Add(new CommonFileDialogFilter("Wavtool", "*.exe"));
            if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                txtboxWavtool.Text = dialog.FileName;
                Core.Util.Preferences.Default.ScriptWavtool = txtboxWavtool.Text;
                Core.Util.Preferences.Save();
            }
        }

        private void chkboxUseScript_Click(object sender, RoutedEventArgs e)
        {
            Core.Util.Preferences.Default.UseScript = chkboxUseScript.IsChecked.Value;
            Core.Util.Preferences.Save();
        }
    }
}
