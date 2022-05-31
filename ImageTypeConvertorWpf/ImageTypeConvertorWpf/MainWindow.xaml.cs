﻿using System;
using System.Threading;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.WindowsAPICodePack.Dialogs;
using Emgu.CV;

namespace ImageTypeConvertorWpf
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string[] ImageFormats = new string[] {".jpg",".png",".bmp"};
        Task CurrentConversion;

        public MainWindow()
        {
            InitializeComponent();
            FileTypeToConvertComboBox.ItemsSource = ImageFormats;
            TargetFileTypeComboBox.ItemsSource = ImageFormats;
            //Load settings from last time
            DirectoryTextBox.Text = Properties.Settings.Default.FileDirectorySetting;
            FileTypeToConvertComboBox.SelectedIndex = Properties.Settings.Default.FileTypeIndex;
            TargetFileTypeComboBox.SelectedIndex = Properties.Settings.Default.TargetTypeIndex;
        }

        //Open Windows Explorer UI to select a file directory
        private void OpenExplorerButton_Click(object sender, RoutedEventArgs e)
        {
            //Open file explorer for selecting the folder
            using (CommonOpenFileDialog FolderDialog = new CommonOpenFileDialog())
            {
                FolderDialog.InitialDirectory = DirectoryTextBox.Text;
                FolderDialog.AllowNonFileSystemItems = true;
                FolderDialog.Multiselect = true;
                FolderDialog.ShowHiddenItems = true;
                FolderDialog.IsFolderPicker = true;
                FolderDialog.Title = "Select folder to scan";
                if (FolderDialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    //Set the UI to the selected folder
                    DirectoryTextBox.Text = FolderDialog.FileName;
                    //Scan the folder
                    ScanDirectoryButton_Click(sender, e);
                }
            }
        }

        //Scan directory for all files of the correct file extension
        private void ScanDirectoryButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                //scan the folder for all file names
                string[] Files = Directory.GetFiles(DirectoryTextBox.Text);

                //create regex to check the file extension
                Regex FileExtensionRegex = new Regex(@"\" + FileTypeToConvertComboBox.SelectedItem.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);

                //add just the file that have the correct file extension to the user UI
                FilesFoundListBox.Items.Clear();
                foreach (string file in Files)
                {
                    //check if the file is of the specified format
                    if (FileExtensionRegex.IsMatch(file))
                    {
                        //add file name to the user display
                        FilesFoundListBox.Items.Add(file.Replace(@DirectoryTextBox.Text + @"\", string.Empty));
                    }
                }
            }
            catch (Exception Except)
            {
                MessageBox.Show("Failed to scan folder: " + Except.Message);
            }
        }

        //Reset found files on directory change
        private void DirectoryTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            FilesFoundListBox.Items.Clear();
        }

        //Reset found files on file type to convert change
        private void FileTypeToConvertComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilesFoundListBox != null)
            {
                FilesFoundListBox.Items.Clear();
            }
        }

        //Convert the found files 
        private void ConvertFilesButton_Click(object sender, RoutedEventArgs e)
        {
            //Check if there are any files to convert
            if(FilesFoundListBox.Items.Count == 0)
            {
                MessageBox.Show("No image files found to convert.");
                return;
            }

            //Check if the file formats are different
            if(FileTypeToConvertComboBox.SelectedIndex == TargetFileTypeComboBox.SelectedIndex)
            {
                MessageBox.Show("File types are the same.");
                return;
            }

            //Check if a conversion is currently in progress
            if(CurrentConversion != null && CurrentConversion.IsCompleted == false && CurrentConversion.IsCompleted == false)
            {
                MessageBox.Show("Conversion is currently in progress.");
                return;
            }

            //Start async conversion task
            string FileTypeToConvert = FileTypeToConvertComboBox.SelectedItem.ToString();
            string FileTypeTarget = TargetFileTypeComboBox.SelectedItem.ToString();           
            string FileDirectory = DirectoryTextBox.Text;
            ItemCollection FileNames = FilesFoundListBox.Items;
            CurrentConversion = new Task(() => ConvertFiles(FileTypeToConvert,FileTypeTarget,FileDirectory,FileNames));
            CurrentConversion.Start();
        }

        //Save regex settings on close
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Properties.Settings.Default.FileDirectorySetting = DirectoryTextBox.Text;
            Properties.Settings.Default.FileTypeIndex = FileTypeToConvertComboBox.SelectedIndex;
            Properties.Settings.Default.TargetTypeIndex = TargetFileTypeComboBox.SelectedIndex;
            Properties.Settings.Default.Save();
        }

        //Convert the files
        private void ConvertFiles(string FileTypeToConvert,string FileTypeTarget,string FileDirectory, ItemCollection FileNames)
        {             
            this.Dispatcher.Invoke(() => { ConversionProgressBar.Value = 0; });
            Regex FileExtensionRegex = new Regex(@"\" + FileTypeToConvert, RegexOptions.IgnoreCase | RegexOptions.Compiled);
            //Convert each of the found files
            foreach (string File in FileNames)
            {
                try
                {
                    string SourceFileFullPath = FileDirectory + "\\" + File;
                    string TargetFileFullPath = FileDirectory + "\\" + FileExtensionRegex.Replace(File, FileTypeTarget);
                    Mat image = CvInvoke.Imread(SourceFileFullPath);
                    if (image != null)
                    {
                        CvInvoke.Imwrite(TargetFileFullPath, image);
                    }
                    else
                    {
                        MessageBox.Show("File could not be read: " + File);
                    }
                }
                catch (Exception Except)
                {
                    MessageBox.Show("Failed to convert file: " + Except.Message);
                }
                this.Dispatcher.Invoke(() => { ConversionProgressBar.Value = ConversionProgressBar.Value + ConversionProgressBar.Maximum / FilesFoundListBox.Items.Count; });
            }
            this.Dispatcher.Invoke(() => { ConversionProgressBar.Value = ConversionProgressBar.Maximum; });
            MessageBox.Show("Conversion complete");
            this.Dispatcher.Invoke(() => { FilesFoundListBox.Items.Clear(); });
            this.Dispatcher.Invoke(() => { ConversionProgressBar.Value = 0; });
        }
    }
}
