using System;
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

        public MainWindow()
        {
            InitializeComponent();
            FileTypeToConvertComboBox.ItemsSource = ImageFormats;
            TargetFileTypeComboBox.ItemsSource = ImageFormats;
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
            //Check if the there are any files to convert
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

            //Convert each of the found files
            Regex FileExtensionRegex = new Regex(@"\" + FileTypeToConvertComboBox.SelectedItem.ToString(), RegexOptions.IgnoreCase | RegexOptions.Compiled);
            foreach (string File in FilesFoundListBox.Items)
            {
                try
                {
                    string SourceFileFullPath = DirectoryTextBox.Text + "\\" + File;
                    string TargetFileFullPath = DirectoryTextBox.Text + "\\" + FileExtensionRegex.Replace(File, TargetFileTypeComboBox.SelectedItem.ToString());
                    Mat image = CvInvoke.Imread(SourceFileFullPath);
                    if(image != null)
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
            }
            MessageBox.Show("Conversion complete");
            FilesFoundListBox.Items.Clear();
        }
    }
}
