﻿/*
    DT Converter - Dani's Tools Video Converter    
    Copyright (C) 2021 Daniznf

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
    
    https://github.com/daniznf/DTConverter
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Path = System.IO.Path;
using System.Diagnostics;
using System.Threading;
using System.Windows.Data;
using System.Globalization;

namespace DTConverter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public readonly string AppData;
        public readonly string WorkDir;
        public readonly string DTVersion;

        public MainWindow()
        {
            InitializeComponent();
            System.Drawing.SystemIcons s;
            TvwVideos.Items.Clear();
            TvwVideos.IsEnabled = false;

            BtnConvert.IsEnabled = false;

            Task.Run(() =>
            {
                try
                {
                    FFmpegWrapper.FindFFPaths(new Action<string, bool>(WriteStatus), new Action<bool>(FindCompleted));
                }
                catch (Exception E)
                {
                    WriteStatus(E.Message, true);
                }
            });

            Version v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            LblName.Content += " " + v.ToString(2);

            // we do some bindings here, not in XAML, so we can use the XAML editor more confortably
            ChkEnableCrop.SetBinding(CheckBox.IsCheckedProperty, "IsCropEnabled");
            ChkEnablePadding.SetBinding(CheckBox.IsCheckedProperty, "IsPaddingEnabled");
            ChkEnableSlices.SetBinding(CheckBox.IsCheckedProperty, "IsSliceEnabled");

            // start time cannot be in frames, so add manually every timeunit except frames
            CbxStartTimeUnit.Items.Clear();
            foreach (object dt in Enum.GetValues(typeof(DurationTypes)))
            {
                if (!(dt is DurationTypes.Frames))
                {
                    CbxStartTimeUnit.Items.Add(dt);
                }
            }
            CbxStartTimeUnit.SelectedItem = DurationTypes.Seconds;

            // Binding needs to be done in code
            Binding bdg = new Binding("StartTime.DurationType");
            bdg.Mode = BindingMode.OneWay;
            CbxStartTimeUnit.SetBinding(ComboBox.TextProperty, bdg);

            UpdateImgPreviewIn();

            WorkDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "DTConverter");
            if (!Directory.Exists(WorkDir))
            {
                Directory.CreateDirectory(WorkDir);
            }

            App.Current.Exit += Current_Exit;

            ConversionList = new List<ConversionParameters>();
        }

        private void Current_Exit(object sender, ExitEventArgs e)
        {
            CleanWorkDir();
        }

        private void CleanWorkDir()
        {
            try
            {
                foreach (string eachFile in Directory.GetFiles(WorkDir))
                {
                    File.Delete(eachFile);
                }
            }
            catch (Exception E) { }
        }

        public void FindCompleted(bool success)
        {
            if (success)
            {
                Dispatcher.Invoke(() =>
                {
                    TvwVideos.IsEnabled = true;
                    WriteStatus("Ready!", false);
                });
            }
        }

        #region Write Status
        private void WriteStatus(string message, bool isError)
        {
            if (message != null)
            {
                if (message.StartsWith("[hap") && message.Contains("is not multiple"))
                {
                    isError = true;
                    message = message.Substring(message.IndexOf(']') + 1);
                }

                if (isError)
                {
                    Dispatcher.Invoke(() =>
                    {
                        SttMessages.Content = "⚠ " + message;
                        SttMessages.Foreground = Brushes.DarkRed;
                        SttMessages.FontWeight = FontWeights.Bold;
                    });
                }
                else
                {
                    Dispatcher.Invoke(() =>
                    {
                        SttMessages.Content = message;
                        SttMessages.Foreground = Brushes.Black;
                        SttMessages.FontWeight = FontWeights.Normal;
                    });
                }

                if (isError)
                {
                    Thread.Sleep(5000);
                }
            }
        }
        #endregion

        #region Conversion Start Stop
        /// <summary>
        /// Contains all ConversionParameters objects corresponding to files in TvwVideos, either checked or not.
        /// </summary>
        private List<ConversionParameters> ConversionList;

        /// <summary>
        /// ConversionParameters currently displayed, used for binding visual elements with it
        /// </summary>
        public ConversionParameters DisplayedConversionParameters { get; set; }

        public void ConversionStarted()
        {
            PnlVideoSettings.IsEnabled = false;
            BtnConvert.Content = "Stop Conversions";
            BtnConvert.Click -= BtnStartConvert_Click;
            BtnConvert.Click += BtnStopConvert_Click;
            WriteStatus("Conversions started", false);
        }

        public void ConversionFinished()
        {
            PnlVideoSettings.IsEnabled = true;
            BtnConvert.Content = "Start Conversions";
            BtnConvert.Click -= BtnStopConvert_Click;
            BtnConvert.Click += BtnStartConvert_Click;
            WriteStatus("All conversions finished", false);
        }

        /// <summary>
        /// The file being converted
        /// </summary>
        private ConversionParameters convertingCP;
        private async void BtnStartConvert_Click(object sender, RoutedEventArgs e)
        {
            stopConversion = false;

            ConversionStarted();
            await Task.Run(() =>
            {
                foreach (ConversionParameters cp in ConversionList)
                {
                    try
                    {
                        // FFmpeg writes interesting data to StandardError so I consider everything as StandardOutput
                        convertingCP = cp;
                        if (cp.IsValid && cp.IsConversionEnabled)
                        {
                            cp.ConvertVideo(
                                (object o, DataReceivedEventArgs d) => WriteStatus(d.Data, false),
                                (object o, DataReceivedEventArgs d) => WriteStatus(d.Data, false));
                        }
                    }
                    catch (Exception E)
                    {
                        WriteStatus(E.Message, true);
                    }
                    if (stopConversion)
                    {
                        break;
                    }
                }
            });
            ConversionFinished();
        }

        // Signal to quit the conversion cycle
        private bool stopConversion;
        private void BtnStopConvert_Click(object sender, RoutedEventArgs e)
        {
            stopConversion = true;
            if (convertingCP != null)
            {
                convertingCP.KillConversion();
            }
        }
        #endregion

        #region TvwVideos
        TaskFactory TF;
        List<Task> PreviewTasks;
        /// <summary>
        /// Adds all files and folder in a semi recursive way: files will be added directly.
        /// If directories are dropped, only files inside those directories will be added, child directories will not be added.
        /// </summary>
        private void TvwVideos_Drop(object sender, DragEventArgs e)
        {
            TF = new TaskFactory();
            PreviewTasks = new List<Task>();

            // BtnConvert gets disabled here and re-enabled after completion of all PreviewTasks
            BtnConvert.IsEnabled = false;
            if (e.Data.GetData(DataFormats.FileDrop) is string[] dropped)
            {
                foreach (string eachDrop in dropped)
                {
                    if (File.Exists(eachDrop))
                    {
                        AddTvwFile(eachDrop);
                    }
                    else if (Directory.Exists(eachDrop))
                    {
                        //foreach (string dir in Directory.GetDirectories(eachDrop))
                        //{
                        //    AddTvwDir(dir);
                        //}
                        foreach (string file in Directory.GetFiles(eachDrop))
                        {
                            AddTvwFile(file);
                        }
                    }
                }
            }
            if (PreviewTasks.Count > 0)
            {
                Task finalTask = TF.ContinueWhenAll(PreviewTasks.ToArray(), (Task[] t) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        BtnConvert.IsEnabled = true;
                    });
                });
            }
        }
        
        /// <summary>
        /// Adds given directory in TvwVideos, nesting it inside existing parent directory if parent exists
        /// </summary>
        /// <param name="dir">Full name of directory to add</param>
        private void AddTvwDir(string dir)
        {
            // TODO: Image sequence detection (maybe we need a full directory recursion?)
            if (SearchTvw(dir, TvwVideos) == null)
            {
                TreeViewItem tvItem = new TreeViewItem()
                {
                    Tag = dir
                };

                if (SearchTvw(Directory.GetParent(dir).FullName, TvwVideos) is TreeViewItem parentDir)
                {
                    DirectoryInfo dInfo = new DirectoryInfo(dir);
                    tvItem.Header = dInfo.Name;
                    parentDir.Items.Add(tvItem);
                }
                else
                {
                    tvItem.Header = dir;
                    TvwVideos.Items.Add(tvItem);
                }
            }
        }

        /// <summary>
        /// Adds given file in TvwVideos nesting it inside its parent directory.
        /// If parent directory does not exist, it will be added.
        /// Binding with CheckBox will be created here.
        /// Probing video info and creating preview image will be done in a new Task
        /// </summary>
        /// <param name="file">Full name of file to add</param>
        //private async Task AddTvwFile(string file)
        private void AddTvwFile(string file)
        {
            if (SearchTvw(file, TvwVideos) == null)
            {
                CheckBox cBox = new CheckBox();

                Label lbl = new Label()
                {
                    Content = Path.GetFileName(file)
                };
                StackPanel sPanel = new StackPanel()
                {
                    Tag = file
                };
                sPanel.Children.Add(cBox);
                sPanel.Children.Add(lbl);

                string parentPath = Path.GetDirectoryName(file);
                
                if (!(SearchTvw(parentPath, TvwVideos) is TreeViewItem parentItem))
                {
                    AddTvwDir(parentPath);
                    parentItem = SearchTvw(parentPath, TvwVideos) as TreeViewItem;
                }
                parentItem.Items.Add(sPanel);

                ConversionParameters cp = new ConversionParameters(file)
                {
                    ThumbnailPathIn = Path.Combine(WorkDir, Path.GetFileNameWithoutExtension(file)),
                };

                // Every checkbox has a different DataContext, corresponding to DisplayedConversionParameters
                cBox.DataContext = cp;
                sPanel.DataContext = cp;

                ConversionList.Add(cp);

                PreviewTasks.Add(TF.StartNew(() => ProbeSourceInfoAndPreviewImage(cp)));
            }
            return;
        }

        /// <summary>
        /// Probes VideoInfo and creates a preview image file at 50% of video duration for the ConversionParameters passed as argument
        /// </summary>
        /// <param name="cp"></param>
        private void ProbeSourceInfoAndPreviewImage(ConversionParameters cp)
        {
            try
            {
                cp.ProbeVideoInfo();
                if (cp.SourceInfo.Duration != null)
                {
                    VideoResolution vr = new VideoResolution()
                    {
                        Horizontal = 640,
                        Vertical = Convert.ToInt32(640 / cp.SourceInfo.AspectRatio)
                    };
                    cp.PreviewTime.Seconds = cp.SourceInfo.Duration.Seconds / 2;
                    cp.PreviewResolution = vr;
                    try
                    {
                        cp.CreateImagePreviewIn(
                            (object o, DataReceivedEventArgs d) => { WriteStatus(d.Data, false); }, 
                            (object o, DataReceivedEventArgs d) => { WriteStatus(d.Data, false); });
                        WriteStatus("", false);
                    }
                    catch (Exception E)
                    {
                        WriteStatus(E.Message, true);
                    }
                }
                UpdateImgPreviewIn();
            }
            catch (Exception E)
            {
                WriteStatus(E.Message, true);
            }
        }

        /// <summary>
        /// Searches, by tag, recursively.
        /// </summary>
        /// <param name="tag">Tag associated to element being searched</param>
        /// <param name="obElement">Element tested against the tag string. If it is a TreeViewItem or a TreeView, its children will be tested recursively</param>
        /// <returns>TreeViewItem if directory is found, StackPanel is file is found, null if nothing is found</returns>
        private object SearchTvw(string tag, object obElement)
        {
            if (obElement is StackPanel spElement)
            {
                if (spElement.Tag != null)
                {
                    if (spElement.Tag.ToString() == tag)
                    {
                        return spElement;
                    }
                }
            }
            else
            {
                if (obElement is TreeViewItem tviElement)
                {
                    if ((tviElement.Tag != null) && (tviElement.Tag.ToString() == tag))
                    {
                        return tviElement;
                    }
                    foreach (object eachItem in tviElement.Items)
                    {
                        object searched = SearchTvw(tag, eachItem);
                        if (searched != null)
                        {
                            return searched;
                        }
                    }
                }
                else
                {
                    if (obElement is TreeView tvwElement)
                    {
                        foreach (object eachItem in tvwElement.Items)
                        {
                            object searched = SearchTvw(tag, eachItem);
                            if (searched != null)
                            {
                                return searched;
                            }
                        }
                    }
                }
            }
            return null;
        }


        /// <summary>
        /// Checks or unchecks all children of a TreeviewItem (directory) in TvwVideos
        /// </summary>
        /// <param name="parent">TreeViewItem where to start from</param>
        /// <param name="check">True to check, False to uncheck </param>
        /// <param name="recursive">Cheks children recursively</param>
        private void CheckChildren(object parent, bool check, bool recursive)
        {
            if (parent is TreeViewItem tvParent)
            {
                foreach (object eachItem in tvParent.Items)
                {
                    /*if (eachItem is CheckBox cbItem)
                    {
                        cbItem.IsChecked = check;
                    }*/
                    if (eachItem is StackPanel spItem)
                    {
                        CheckBox cb = spItem.Children.OfType<CheckBox>().First();
                        if ((cb != null) && cb.IsEnabled)
                        {
                            cb.IsChecked = check;
                        }
                    }
                    else if ((recursive) && (eachItem is TreeViewItem))
                    {
                        CheckChildren(eachItem, check, true);
                    }
                }
            }
        }

        /// <summary>
        /// Removes an item from TvwVideos and from ConversionList
        /// </summary>
        /// <param name="obRemove">TreeviewItem (directory) or StackPanel (file) to remove</param>
        /// <param name="ParentItems">Collection in whom remove the item</param>
        /// <returns></returns>
        private bool RemoveTvwItem(object obRemove, ItemCollection ParentItems)
        {
            if (ParentItems == null)
            {
                return false;
            }
            
            if (ParentItems.Contains(obRemove))
            {
                string sourcePath = "";
                if (obRemove is StackPanel spRemove)
                {
                    sourcePath = spRemove.Tag.ToString();
                }
                else if (obRemove is TreeViewItem tvRemove)
                {
                    sourcePath = tvRemove.Tag.ToString();
                }

                // When removing Directories, this will just not find ConversionParameters to remove
                ConversionList.RemoveAll((ConversionParameters C) => C.SourcePath.Contains(sourcePath));

                ParentItems.Remove(obRemove);
                return true;
            }
            else
            {
                foreach (object eachItem in ParentItems)
                {
                    if (eachItem is TreeViewItem tvItem)
                    {
                        if (RemoveTvwItem(obRemove, tvItem.Items))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }
        #endregion

        #region TvwVideos events
        /// <summary>
        /// Inhibits regeneration of preview while changing DisplayedConversionParameters
        /// </summary>
        private bool ChangingDisplayedConversionParameters;
        /// <summary>
        /// Changes the DisplayedConversionParameters, updates the datacontext and calls UpdateImgPreviewOut()
        /// </summary>
        private void TvwVideos_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (TvwVideos.SelectedItem is StackPanel spSender)
            {
                if (spSender.Tag != null)
                {
                    ChangingDisplayedConversionParameters = true;
                    DisplayedConversionParameters = ConversionList.Find((ConversionParameters C) => C.SourcePath == spSender.Tag.ToString());
                    DTWindow.DataContext = DisplayedConversionParameters;

                    ChangingDisplayedConversionParameters = false;

                    UpdateImgPreviewIn();
                    if (ChkEnableCrop.IsChecked.Value || ChkEnablePadding.IsChecked.Value || ChkEnableSlices.IsChecked.Value)
                    {
                        UpdateImgPreviewOut();
                    }
                }
            }
        }

        /// <summary>
        /// Handles selection and multiple selection of items in TvwVideos
        /// </summary>
        private void TreeViewItem_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.A)
            {
                CheckChildren(sender as Control, !(Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift)),
                    (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl)));
                e.Handled = true;
            }
        }

        /// <summary>
        /// Handles deletion selection of item in TvwVideos
        /// </summary>
        private void TreeViewItem_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Delete)
            {
                RemoveTvwItem(sender as Control, TvwVideos.Items);
                e.Handled = true;
            }
        }
        #endregion

        #region Crop Padding Slices
        private void ChkEnableCrop_Checked(object sender, RoutedEventArgs e)
        {
            if (IsInitialized)
            {
                PnlCrop.Visibility = Visibility.Visible;
                OpenGrdPreviewOut();
            }
        }

        private void ChkEnableCrop_Unchecked(object sender, RoutedEventArgs e)
        {
            if (IsInitialized)
            {
                PnlCrop.Visibility = Visibility.Collapsed;
                TryCloseColCrop();
            }
        }

        private void ChkEnablePadding_Checked(object sender, RoutedEventArgs e)
        {
            if (IsInitialized)
            {
                PnlPadding.Visibility = Visibility.Visible;
                OpenGrdPreviewOut();
            }
        }

        private void ChkEnablePadding_Unchecked(object sender, RoutedEventArgs e)
        {
            if (IsInitialized)
            {
                PnlPadding.Visibility = Visibility.Collapsed;
                TryCloseColCrop();
            }
        }

        private void ChkEnableSlices_Checked(object sender, RoutedEventArgs e)
        {
            if (IsInitialized)
            {
                PnlSlices.Visibility = Visibility.Visible;
                OpenGrdPreviewOut();
                //ChkOriginal.IsChecked = false;
            }
        }

        private void ChkEnableSlices_Unchecked(object sender, RoutedEventArgs e)
        {
            if (IsInitialized)
            {
                PnlSlices.Visibility = Visibility.Collapsed;
                //CbxHorizontalSlices.SelectedIndex = 0;
                //CbxVerticalSlices.SelectedIndex = 0;
                ChkOriginal.IsChecked = true;
                TryCloseColCrop();
            }
        }

        /// <summary>
        /// Opens GrdPreviewOut, calls SliceGrdPreviewOut, and regenerates all previews
        /// </summary>
        private async void OpenGrdPreviewOut()
        {
            ColPreviewOut.Width = new GridLength(1, GridUnitType.Star);
            SliceGrdPreviewOut(Convert.ToInt32(CbxVerticalSlices.Text), Convert.ToInt32(CbxHorizontalSlices.Text));

            Task pvwIn = RegeneratePreviewInImages();
            Task pvwOut = RegeneratePreviewOutImages();
            await pvwIn.ConfigureAwait(true);
            await pvwOut.ConfigureAwait(true);
            UpdateImgPreviewIn();
            UpdateImgPreviewOut();
        }
        
        /// <summary>
        /// Closes ColPreviewOut only if ChkEnableCrop and ChkEnablePadding and ChkEnableSlices are unchecked at the same time
        /// </summary>
        private async void TryCloseColCrop()
        {
            if (!ChkEnableCrop.IsChecked.Value && !ChkEnablePadding.IsChecked.Value && !ChkEnableSlices.IsChecked.Value)
            {
                ColPreviewOut.Width = new GridLength(0, GridUnitType.Star);
            }
            //ChkOriginal.IsChecked = true;

            Task pvwIn = RegeneratePreviewInImages();
            await pvwIn.ConfigureAwait(true);
            UpdateImgPreviewIn();
        }

        /// <summary>
        /// Populates the grid GrdPreviewOut with specified number of rows and columns. 
        /// After calling this, it's necessary to call ReneratePreviewIn/Out or UpdateImgPreviewIn/Out
        /// </summary>
        private void SliceGrdPreviewOut(int rows, int columns)
        {
            if ((rows > 1) || (columns > 1))
            {
                GrdPreviewOut.Children.Clear();

                GrdPreviewOut.RowDefinitions.Clear();
                GrdPreviewOut.ColumnDefinitions.Clear();

                for (int i = 0; i < rows; i++)
                {
                    GrdPreviewOut.RowDefinitions.Add(new RowDefinition() { Height = new GridLength(1, GridUnitType.Star) });
                }

                for (int i = 0; i < columns; i++)
                {
                    GrdPreviewOut.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(1, GridUnitType.Star) });
                }

                string pathRC;
                for (int r = 0; r < rows; r++)
                {
                    for (int c = 0; c < columns; c++)
                    {
                        if (DisplayedConversionParameters != null)
                        {
                            Image im = new Image();
                            pathRC = DisplayedConversionParameters.getSliceName(DisplayedConversionParameters.ThumbnailPathOut, r + 1, c + 1);

                            BitmapImage bi = new BitmapImage();
                            im.Source = bi;

                            GrdPreviewOut.Children.Add(im);
                            Grid.SetColumn(im, c);
                            Grid.SetRow(im, r);
                        }
                    }
                }
            }
            else
            {
                if (DisplayedConversionParameters != null)
                {
                    GrdPreviewOut.Children.Clear();

                    GrdPreviewOut.RowDefinitions.Clear();
                    GrdPreviewOut.ColumnDefinitions.Clear();
                    Image im = new Image();
                    BitmapImage bi = new BitmapImage();
                    im.Source = bi;
                    
                    GrdPreviewOut.Children.Add(im);
                }
            }
        }

        private async void CbxHorizontalVerticalSlices_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (IsInitialized)
            {
                CbxHorizontalSlices.GetBindingExpression(ComboBox.TextProperty).UpdateSource();
                CbxVerticalSlices.GetBindingExpression(ComboBox.TextProperty).UpdateSource();

                if ((CbxVerticalSlices.SelectedItem is ComboBoxItem cbSelectedVertical) && 
                    (CbxHorizontalSlices.SelectedItem is ComboBoxItem cbSelectedHorizontal))
                {
                    Task pvwIn = RegeneratePreviewInImages();
                    Task pvwOut = RegeneratePreviewOutImages();

                    await pvwIn.ConfigureAwait(true);
                    await pvwOut.ConfigureAwait(true);

                    SliceGrdPreviewOut(Convert.ToInt32(cbSelectedVertical.Content), Convert.ToInt32(cbSelectedHorizontal.Content));

                    UpdateImgPreviewIn();
                    UpdateImgPreviewOut();
                }
            }
        }

        /// <summary>
        /// Reloads image of ImgPvwIn
        /// </summary>
        private void UpdateImgPreviewIn()
        {
            if (DisplayedConversionParameters != null && File.Exists(DisplayedConversionParameters.ThumbnailPathIn))
            {
                Dispatcher.Invoke(() =>
                {
                    if (GrdPreviewIn.Children[0] is Image img)
                    {
                        BitmapImage bi = new BitmapImage();
                        bi.BeginInit();
                        bi.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                        bi.CacheOption = BitmapCacheOption.OnLoad;
                        bi.UriSource = new Uri(DisplayedConversionParameters.ThumbnailPathIn);
                        bi.EndInit();
                        img.Source = bi;
                        img.UpdateLayout();
                    }
                });
            }
        }

        /// <summary>
        /// Reloads all images of ImgPvwOut, either if it's a single image, or if it is multiple images
        /// </summary>
        public void UpdateImgPreviewOut()
        {
            if (DisplayedConversionParameters != null)
            {
                Dispatcher.Invoke(() =>
                {
                    if (GrdPreviewOut.RowDefinitions.Count == 0 && GrdPreviewOut.ColumnDefinitions.Count == 0)
                    {
                        if (GrdPreviewOut.Children[0] is Image img)
                        {
                            string pathOut = DisplayedConversionParameters.ThumbnailPathOut;
                            if (File.Exists(pathOut))
                            {
                                BitmapImage bi = new BitmapImage();
                                bi.BeginInit();
                                bi.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                                bi.CacheOption = BitmapCacheOption.OnLoad;
                                bi.UriSource = new Uri(pathOut);
                                bi.EndInit();
                                img.Source = bi;
                                img.UpdateLayout();
                            }
                        }
                    }
                    else
                    {
                        int c, r;
                        string pathRC;
                        foreach (UIElement uiEl in GrdPreviewOut.Children)
                        {
                            c = Grid.GetColumn(uiEl);
                            r = Grid.GetRow(uiEl);
                            pathRC = DisplayedConversionParameters.getSliceName(DisplayedConversionParameters.ThumbnailPathOut, r + 1, c + 1);
                            if (uiEl is Image im)
                            {
                                if (File.Exists(pathRC))
                                {
                                    BitmapImage bi = new BitmapImage();
                                    bi.BeginInit();
                                    bi.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                                    bi.CacheOption = BitmapCacheOption.OnLoad;
                                    bi.UriSource = new Uri(pathRC);
                                    bi.EndInit();
                                    im.Source = bi;
                                    im.UpdateLayout();
                                }
                            }
                        }
                    }
                });
            }
        }

        /// <summary>
        /// Recreates images of ImgPreviewIn.
        /// This should be run in a separate Task or Thread
        /// </summary>
        private Task RegeneratePreviewInImages()
        {
            if (!ChangingDisplayedConversionParameters)
            {
                if (DisplayedConversionParameters != null)
                {
                    if (DisplayedConversionParameters.VideoConversionStatus == ConversionStatus.CreatingPreviewIn)
                    {
                        DisplayedConversionParameters.KillProcessPreviewIn();
                    }

                    if (GrdPreviewIn.Children[0] is Image img)
                    {
                        img.Source = null;
                    }

                    return Task.Run(() =>
                    {
                        try
                        {
                            DisplayedConversionParameters.CreateImagePreviewIn(
                                (object o, DataReceivedEventArgs d) => { WriteStatus(d.Data, false); },
                                (object o, DataReceivedEventArgs d) => { WriteStatus(d.Data, false); });
                            WriteStatus("", false);
                        }
                        catch (Exception E)
                        {
                            WriteStatus(E.Message, true);
                        }
                    });
                }
            }
            return  Task.Run(() => { return; });
        }

        /// <summary>
        /// Recreates images of ImgPreviewOut
        /// This should be run in a separate Task or Thread
        /// </summary>
        private Task RegeneratePreviewOutImages()
        {
            if (!ChangingDisplayedConversionParameters)
            {
                if (DisplayedConversionParameters != null)
                {
                    if (DisplayedConversionParameters.VideoConversionStatus == ConversionStatus.CreatingPreviewOut)
                    {
                        DisplayedConversionParameters.KillProcessPreviewOut();
                    }
                    if (DisplayedConversionParameters.IsCropEnabled ||
                        DisplayedConversionParameters.IsPaddingEnabled ||
                        DisplayedConversionParameters.IsSliceEnabled)
                    {
                        foreach (UIElement uiEl in GrdPreviewOut.Children)
                        {
                            if (uiEl is Image imgOut)
                            {
                                imgOut.Source = null;
                            }
                        }

                        return Task.Run(() =>
                        {
                            try
                            {
                                DisplayedConversionParameters.CreateImagePreviewOut(
                                    (object o, DataReceivedEventArgs d) => { WriteStatus(d.Data, false); },
                                    (object o, DataReceivedEventArgs d) => { WriteStatus(d.Data, false); });
                                WriteStatus("", false);
                            }
                            catch (Exception E)
                            {
                                WriteStatus(E.Message, true);
                            }
                        });
                    }
                }
            }
            return  Task.Run(() => { return; });
        }
        #endregion

        #region Preview Original Crop
        private void ChkOriginal_Checked(object sender, RoutedEventArgs e)
        {
            ColPreviewIn.Width = new GridLength(1, GridUnitType.Star);
        }

        private void ChkOriginal_Unchecked(object sender, RoutedEventArgs e)
        {
            ColPreviewIn.Width = new GridLength(0, GridUnitType.Star);
        }

        private async void AnyonePreviewRegeneration_PreviewKeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                if (sender is TextBox tbs)
                {
                    tbs.GetBindingExpression(TextBox.TextProperty).UpdateSource();
                }

                Task pvwIn = RegeneratePreviewInImages();
                Task pvwOut = RegeneratePreviewOutImages();
                await pvwIn.ConfigureAwait(true);
                await pvwOut.ConfigureAwait(true);
                UpdateImgPreviewIn();
                UpdateImgPreviewOut();
            }
        }

        private async void AnyonePreviewRegeneration_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            Task pvwIn = RegeneratePreviewInImages();
            Task pvwOut = RegeneratePreviewOutImages();
            await pvwIn.ConfigureAwait(true);
            await pvwOut.ConfigureAwait(true);
            UpdateImgPreviewIn();
            UpdateImgPreviewOut();
        }

        private async void CbxRotation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox cbs)
            {
                cbs.GetBindingExpression(ComboBox.TextProperty).UpdateSource();
                
                Task pvwIn = RegeneratePreviewInImages();
                Task pvwOut = RegeneratePreviewOutImages();
                await pvwIn.ConfigureAwait(true);
                await pvwOut.ConfigureAwait(true);
                UpdateImgPreviewIn();
                UpdateImgPreviewOut();
            }
        }
        #endregion

        #region Menu Events
        private void MnClearAll_Click(object sender, RoutedEventArgs e)
        {
            TvwVideos.Items.Clear();
            ConversionList = new List<ConversionParameters>();
        }

        private void MnRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            UIElement pTarget;
            pTarget = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget;

            if (pTarget != null)
            {
                RemoveTvwItem(pTarget, TvwVideos.Items);
            }
        }

        private void MnCheckAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (object eachItem in TvwVideos.Items)
            {
                CheckChildren(eachItem, true, true);
            }
        }

        private void MnCheckItems_Click(object sender, RoutedEventArgs e)
        {
            UIElement pTarget;
            pTarget = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget;

            if (pTarget is TreeViewItem tvTarget)
            {
                CheckChildren(tvTarget, true, false);
            }
        }

        private void MnUncheckItems_Click(object sender, RoutedEventArgs e)
        {
            UIElement pTarget;
            pTarget = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget;

            if (pTarget is TreeViewItem tvTarget)
            {
                CheckChildren(tvTarget, false, false);
            }
        }

        private void MnUncheckAll_Click(object sender, RoutedEventArgs e)
        {
            foreach (Control eachItem in TvwVideos.Items)
            {
                CheckChildren(eachItem, false, true);
            }
        }

        private void MnOpenContainingFolder_Click(object sender, RoutedEventArgs e)
        {
            string path = null;
            UIElement pTarget;
            pTarget = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget;
            
            if (pTarget is StackPanel spTarget)
            {
                path = spTarget.Tag.ToString();
            }
            else if (pTarget is TreeViewItem tvTarget)
            {
                path = tvTarget.Tag.ToString();
            }

            if (path != null)
            {
                System.Diagnostics.Process.Start("explorer.exe", Directory.GetParent(path).FullName);
            }
        }

        private void MnPasteSettingsAll_Click(object sender, RoutedEventArgs e)
        {
            UIElement pTarget;
            pTarget = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget;

            if (pTarget is TreeViewItem tviTarget)
            {
                foreach (object obEach in tviTarget.Items)
                {
                    if (obEach is StackPanel spEach)
                    {
                        ConversionParameters pasteTo;
                        pasteTo = ConversionList.Find((ConversionParameters C) => C.SourcePath == spEach.Tag.ToString());
                        if (pasteTo != null)
                        {
                            pasteTo.PasteParameters(copyingParameters);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// ConversionParameters that is being copied
        /// </summary>
        ConversionParameters copyingParameters;
        private void MnCopySettings_Click(object sender, RoutedEventArgs e)
        {
            UIElement pTarget;
            pTarget = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget;

            if (pTarget is StackPanel spSender)
            {
                copyingParameters = ConversionList.Find((ConversionParameters C) => C.SourcePath == spSender.Tag.ToString());
            }
        }

        private void MnPasteSettings_Click(object sender, RoutedEventArgs e)
        {
            UIElement pTarget;
            pTarget = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget;

            if (pTarget is StackPanel spSender)
            {
                if (copyingParameters != null)
                {
                    ConversionParameters cp = ConversionList.Find((ConversionParameters C) => C.SourcePath == spSender.Tag.ToString());
                    cp.PasteParameters(copyingParameters);
                }
            }
        }

        private void MnResetSettings_Click(object sender, RoutedEventArgs e)
        {
            UIElement pTarget;
            pTarget = ((sender as MenuItem).Parent as ContextMenu).PlacementTarget;

            if (pTarget is StackPanel spSender)
            {
                ConversionParameters cp = ConversionList.Find((ConversionParameters C) => C.SourcePath == spSender.Tag.ToString());
                cp.ResetDefaultValues();
                Task.Run(() =>
                {
                    try
                    {
                        cp.ProbeVideoInfo();
                    }
                    catch (Exception E)
                    {
                        WriteStatus(E.Message, true);
                    }
                });

            }
        }
        #endregion

        #region Time Events
        private void TxtStartTime_KeyUp(object sender, KeyEventArgs e)
        {
            TxtStartTime.Text = TxtStartTime.Text.Replace(',', '.');
            if (e.Key == Key.Enter)
            {
                if (DisplayedConversionParameters != null && TxtStartTime.Text != "")
                {
                    try
                    {
                        switch (CbxStartTimeUnit.SelectedItem)
                        {
                            case DurationTypes.Seconds:
                                DisplayedConversionParameters.StartTime.Seconds = Convert.ToDouble(TxtStartTime.Text, CultureInfo.InvariantCulture);
                                break;
                            case DurationTypes.MilliSeconds:
                                DisplayedConversionParameters.StartTime.MilliSeconds = Convert.ToDouble(TxtStartTime.Text);
                                break;
                            case DurationTypes.MicroSeconds:
                                DisplayedConversionParameters.StartTime.MicroSeconds = Convert.ToDouble(TxtStartTime.Text);
                                break;
                            case DurationTypes.Frames:
                                DisplayedConversionParameters.StartTime.Frames = Convert.ToInt32(TxtStartTime.Text);
                                break;
                            case DurationTypes.HMS:
                                DisplayedConversionParameters.StartTime.HMS = TxtStartTime.Text;
                                break;
                        }
                    }
                    catch { }
                }
            }
        }

        private void CbxStartTimeUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        { 
            switch (CbxStartTimeUnit.SelectedItem)
            { 
                case DurationTypes.Seconds:
                    TxtStartTime.SetBinding(TextBox.TextProperty, "StartTime.Seconds");
                    break;
                case DurationTypes.MilliSeconds:
                    TxtStartTime.SetBinding(TextBox.TextProperty, "StartTime.MilliSeconds");
                    break;
                case DurationTypes.MicroSeconds:
                    TxtStartTime.SetBinding(TextBox.TextProperty, "StartTime.MicroSeconds");
                    break;
                case DurationTypes.Frames:
                    TxtStartTime.SetBinding(TextBox.TextProperty, "StartTime.Frames");
                    break;
                case DurationTypes.HMS:
                    TxtStartTime.SetBinding(TextBox.TextProperty, "StartTime.HMS");
                    break;
            }
            TxtStartTime.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

        private void TxtDurationTime_KeyUp(object sender, KeyEventArgs e)
        {
            TxtDurationTime.Text = TxtDurationTime.Text.Replace(',', '.');
            if (e.Key == Key.Enter)
            {
                
                if (DisplayedConversionParameters != null && TxtDurationTime.Text != "")
                {
                    try
                    {
                        switch (CbxDurationTimeUnit.SelectedItem)
                        {
                            case DurationTypes.Seconds:
                                DisplayedConversionParameters.DurationTime.Seconds = Convert.ToDouble(TxtDurationTime.Text);
                                break;
                            case DurationTypes.MilliSeconds:
                                DisplayedConversionParameters.DurationTime.MilliSeconds = Convert.ToDouble(TxtDurationTime.Text);
                                break;
                            case DurationTypes.MicroSeconds:
                                DisplayedConversionParameters.DurationTime.MicroSeconds = Convert.ToDouble(TxtDurationTime.Text);
                                break;
                            case DurationTypes.Frames:
                                DisplayedConversionParameters.DurationTime.Frames = Convert.ToInt32(TxtDurationTime.Text);
                                break;
                            case DurationTypes.HMS:
                                DisplayedConversionParameters.DurationTime.HMS = TxtDurationTime.Text;
                                break;
                        }
                    }
                    catch { }
                }
            }
        }

        private void CbxDurationTimeUnit_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (CbxDurationTimeUnit.SelectedItem)
            {
                case DurationTypes.Seconds:
                    TxtDurationTime.SetBinding(TextBox.TextProperty, "DurationTime.Seconds");
                    break;
                case DurationTypes.MilliSeconds:
                    TxtDurationTime.SetBinding(TextBox.TextProperty, "DurationTime.MilliSeconds");
                    break;
                case DurationTypes.MicroSeconds:
                    TxtDurationTime.SetBinding(TextBox.TextProperty, "DurationTime.MicroSeconds");
                    break;
                case DurationTypes.Frames:
                    TxtDurationTime.SetBinding(TextBox.TextProperty, "DurationTime.Frames");
                    break;
                case DurationTypes.HMS:
                    TxtDurationTime.SetBinding(TextBox.TextProperty, "DurationTime.HMS");
                    break;
            }
            TxtDurationTime.GetBindingExpression(TextBox.TextProperty).UpdateSource();
        }

        private void BtnSetStart_Click(object sender, RoutedEventArgs e)
        {
            if (DisplayedConversionParameters != null)
            {
                DisplayedConversionParameters.StartTime.Seconds = DisplayedConversionParameters.PreviewTime.Seconds;
            }
        }

        private void BtnSetEnd_Click(object sender, RoutedEventArgs e)
        {
            if (DisplayedConversionParameters != null)
            {
                DisplayedConversionParameters.DurationTime.Seconds = DisplayedConversionParameters.PreviewTime.Seconds - DisplayedConversionParameters.StartTime.Seconds;
            }
        }
        #endregion

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            About a = new About();
            a.Owner = this;
            a.Show();
        }
    }
}