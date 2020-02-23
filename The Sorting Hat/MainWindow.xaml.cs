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
        int totalPositions = 0; //number of positions to be sorted
        int folderPosition; 
        int totalPhotos; //number of photos in source directory
        BackgroundWorker sortingWork = new BackgroundWorker(); //background thread for sorting 
        string sourceDirectory; //the path to the directory being sorted

        public MainWindow()
        {
            InitializeComponent();

            initializeBackgroundWorker();

            SortingProgressBar.Value = -1; //resetting progess bar to empty
        }

        /// <summary>
        /// Initializing background worker thread for sorting work
        /// </summary>
        private void initializeBackgroundWorker()
        {
            sortingWork.DoWork += SortingWork_DoWork;
            sortingWork.RunWorkerCompleted += SortingWork_RunWorkerCompleted;
            sortingWork.ProgressChanged += SortingWork_ProgressChanged;
            sortingWork.WorkerReportsProgress = true;
            sortingWork.WorkerSupportsCancellation = true;
        }

        /// <summary>
        /// Updates progress bar about progress of sorting 
        /// </summary>
        private void SortingWork_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            SortingProgressBar.Value = e.ProgressPercentage;
        }

        /// <summary>
        /// What is displayed after background thread completes, ends in error, or is cancelled. If completed, dialog box saying completed is displayed 
        /// and progress bar is reset. Cancel and start buttons are enabled/disabled respectively
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
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

                CancelSortingBtn.IsEnabled = false;
                StartSortingBtn.IsEnabled = true;
            }
        }

        /// <summary>
        /// Work thread of sorting. First finds total number of photos. Then makes folders in target directory based on number of positions chosen by user.
        /// Those folders are put into an array that is iterated through for each photo (because recorded in sequential order).
        /// </summary>
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
            }

            //list of all folders in source directory
            var dirPaths = Directory.GetDirectories(sourceDirectory).OrderBy(d => new DirectoryInfo(d).CreationTime); //sorted by creation time
            string[] dirPath = dirPaths.ToArray();

            //sorts photos by moving
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
            }


        }

        /// <summary>
        /// Finds folder path from dialog window search and sets source directory for sorting to it
        /// </summary>
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

        /// <summary>
        /// When Start Sorting Button is clicked. Checks for if folder for sorting is collected yet. Then user confirms it.
        /// Background worker sorter is started.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void StartSortingBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                totalPositions = (int)NumberOfPosition.Value;
                folderPosition = 0;

                //if no folder to sort chosen yet then do nothing
                if(sourceDirectory == null)
                {
                    //MessageBox.Show("No folder path has been specified to sort, please choose one before sorting...");
                    throw new NullReferenceException(); //source directory is null
                }
                
                //string message for confirmation window
                string confirmationMessage;
                string template = "Are you sure that you want to sort {0} positions?";
                string data = totalPositions.ToString();
                confirmationMessage = string.Format(template, data);

                //confirmation window to make sure user wants to sort so no missorts
                MessageBoxResult messageBoxResult = MessageBox.Show(confirmationMessage, "Confirmation", MessageBoxButton.YesNo);
                if (messageBoxResult == MessageBoxResult.Yes)
                {
                    sortingWork.RunWorkerAsync();
                } 

                StartSortingBtn.IsEnabled = false;
                CancelSortingBtn.IsEnabled = true;
            }
            catch(NullReferenceException ex)
            {
                MessageBox.Show("No folder path has been specified to sort, please choose one before sorting...");
            }
        }

        /// <summary>
        /// Cancels sorting operation during sorting
        /// </summary>
        private void CancelSortingBtn_Click(object sender, RoutedEventArgs e)
        {
            sortingWork.CancelAsync();

            CancelSortingBtn.IsEnabled = false;
            StartSortingBtn.IsEnabled = true;
        }

        /// <summary>
        /// Calculates the percentage of progress made by sorting
        /// </summary>
        /// <param name="workDone">The number of photos sorted already</param>
        /// <param name="workTotal">The total number of photos to sort still</param>
        /// <returns></returns>
        private int CalcReportProgress(int workDone, int workTotal)
        {
            int workCompleted = (int)(((float)workDone) / ((float)workTotal) * 100);
            return workCompleted;
        }
    }
}
