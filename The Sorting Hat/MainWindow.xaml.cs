using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace The_Sorting_Hat
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        int totalPositions = 0;
        int folderPosition;
        int totalPhotos;
        BackgroundWorker sortingWork = new BackgroundWorker();
        string sourceDirectory;

        public MainWindow()
        {
            InitializeComponent();

            initializeBackgroundWorker();

            SortingProgressBar.Value = -1;
        }

        private void initializeBackgroundWorker()
        {
            sortingWork.DoWork += SortingWork_DoWork;
            sortingWork.RunWorkerCompleted += SortingWork_RunWorkerCompleted;
            sortingWork.ProgressChanged += SortingWork_ProgressChanged;
            sortingWork.WorkerReportsProgress = true;
            sortingWork.WorkerSupportsCancellation = true;
        }

        private void SortingWork_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            SortingProgressBar.Value = e.ProgressPercentage;
        }

        private void SortingWork_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if(e.Error != null)
            {
                MessageBox.Show(e.Error.Message);
            }
            else if(e.Cancelled)
            {
                MessageBox.Show("Sorting was cancelled");

                SortingProgressBar.Value = -1;
            }
            else
            {
                MessageBox.Show("Sorting was completed");

                SortingProgressBar.Value = -1;
            }
        }

        private void SortingWork_DoWork(object sender, DoWorkEventArgs e)
        {
            int numFilesCompleted = 0;

            //read in files from function args of folder to a list
            var pngFiles = Directory.EnumerateFiles(sourceDirectory, "*.png").OrderBy(p => new FileInfo(p).CreationTime);
            totalPhotos = pngFiles.Count();

            //creating folders for images
            for (int i = 0; i < totalPositions; i++)
            {
                //check for cancellation
                if (sortingWork.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                DirectoryInfo di = Directory.CreateDirectory(sourceDirectory + "\\Position" + (i +1));

                Console.WriteLine("Position directory was created successfully at {0}.", DateTime.Now);
            }

            //list of all folders in source directory
            var dirPaths = Directory.GetDirectories(sourceDirectory).OrderBy(d => new DirectoryInfo(d).CreationTime); //sorted by creation time
            string[] dirPath = dirPaths.ToArray();

            //Go through first set and make new directories for each pot
            foreach (string currentFile in pngFiles)
            { 
                //check for cancellation
                if(sortingWork.CancellationPending)
                {
                    e.Cancel = true;
                    return;
                }

                string filename = System.IO.Path.GetFileName(currentFile); //filename of file being copied

                string destFile = System.IO.Path.Combine(dirPath[folderPosition].ToString(), filename); //filename of destination file 

                File.Move(currentFile, destFile); //copy process
                folderPosition++; //move to next folder

                //start at beginning of folders again when cycle done
                if (folderPosition > (totalPositions - 1))
                {
                    //check for cancellation
                    if (sortingWork.CancellationPending)
                    {
                        e.Cancel = true;
                        return;
                    }

                    folderPosition = 0;
                }

                numFilesCompleted++;

                sortingWork.ReportProgress(CalcReportProgress(numFilesCompleted, totalPhotos));

              //  Console.WriteLine("File was copy to {0} from {1}", destFile, sourceDirectory);
            }


        }

        /// <summary>
        /// Finds folder path from dialog window search and sets source directory for sorting to it
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void SourceDirectoryFolderSelectionBtn_Click(object sender, RoutedEventArgs e)
        {
            string folderResult = GetFolderResult();

            if (folderResult != null) //if user chose something
            {
                sourceDirectory = folderResult;

                SourceDirectoryPathTextBox.Text = folderResult; //set text to folder path
            }
        }

        /// <summary>
        /// Opens dialog window to search for window to save photos t0
        /// </summary>
        /// <returns>String name of Folder path for saving</returns>
        private string GetFolderResult()
        {
            using (var dialog = new System.Windows.Forms.FolderBrowserDialog())
            {
                System.Windows.Forms.DialogResult result = dialog.ShowDialog();
                switch (result)
                {
                    case System.Windows.Forms.DialogResult.OK:
                        return dialog.SelectedPath;
                    case System.Windows.Forms.DialogResult.Cancel:
                    default:
                        return null;
                }
            }
        }

        private void StartSortingBtn_Click(object sender, RoutedEventArgs e)
        {
            totalPositions = (int)NumberOfPosition.Value;
            folderPosition = 0;

            sortingWork.RunWorkerAsync();

            StartSortingBtn.IsEnabled = false;
            CancelSortingBtn.IsEnabled = true;
        }

        private void CancelSortingBtn_Click(object sender, RoutedEventArgs e)
        {
            sortingWork.CancelAsync();

            CancelSortingBtn.IsEnabled = false;
            StartSortingBtn.IsEnabled = true;
        }

        private int CalcReportProgress(int workDone, int workTotal)
        {
            int workCompleted = (int)(((float)workDone) / ((float)workTotal) * 100);
            return workCompleted;
        }
    }
}
