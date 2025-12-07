using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using ManagedShell.Common.Helpers;
using ManagedShell.Common.Logging;
using ManagedShell.ShellFolders.Enums;
using ManagedShell.ShellFolders.Structs;
using Microsoft.VisualBasic.FileIO;

namespace ManagedShell.ShellFolders
{
    public class FileOperationWorker
    {
        private readonly BackgroundWorker _worker = new BackgroundWorker();

        public FileOperationWorker()
        {
            _worker.DoWork += WorkerDoWork;
        }

        public void PasteFromClipboard(string targetDirectory)
        {
            IDataObject clipFiles = Clipboard.GetDataObject();

            if (clipFiles == null)
            {
                return;
            }
            
            if (clipFiles.GetDataPresent(DataFormats.FileDrop))
            {
                if (clipFiles.GetData(DataFormats.FileDrop) is string[] files)
                {
                    PerformOperation(FileOperation.Copy, files, targetDirectory);
                }
            }
        }

        public void PerformOperation(FileOperation operation, string[] files, string targetDirectory)
        {
            _worker.RunWorkerAsync(new BackgroundFileOperation
            {
                Paths = files,
                Operation = operation,
                TargetPath = targetDirectory
            });
        }

        private void DoOperation(BackgroundFileOperation operation, string path)
        {
            try
            {
                if (!ShellHelper.Exists(path))
                {
                    return;
                }
                
                FileAttributes attr = File.GetAttributes(path);
                if ((attr & FileAttributes.Directory) == FileAttributes.Directory)
                {
                    DoDirectoryOperation(operation, path);
                }
                else
                {
                    DoFileOperation(operation, path);
                }
            }
            catch (Exception e)
            {
                ShellLogger.Error($"FileOperationWorker: Unable to perform {operation.Operation} on {path} into {operation.TargetPath}: {e.Message}");
            }
        }

        private void DoDirectoryOperation(BackgroundFileOperation operation, string path)
        {
            if (path == operation.TargetPath)
            {
                return;
            }

            string futureName = Path.Combine(operation.TargetPath, new DirectoryInfo(path).Name);
            if (futureName == path)
            {
                return;
            }

            switch (operation.Operation)
            {
                case FileOperation.Copy:
                    FileSystem.CopyDirectory(path, futureName, UIOption.AllDialogs);
                    break;
                case FileOperation.Move:
                    FileSystem.MoveDirectory(path, futureName, UIOption.AllDialogs);
                    break;
            }
        }

        private void DoFileOperation(BackgroundFileOperation operation, string path)
        {
            string futureName = Path.Combine(operation.TargetPath, Path.GetFileName(path));
            if (futureName == path)
            {
                return;
            }

            switch (operation.Operation)
            {
                case FileOperation.Copy:
                    FileSystem.CopyFile(path, futureName, UIOption.AllDialogs);
                    break;
                case FileOperation.Move:
                    FileSystem.MoveFile(path, futureName, UIOption.AllDialogs);
                    break;
            }
        }

        private void WorkerDoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundFileOperation operation = (BackgroundFileOperation)e.Argument;

            foreach (var path in operation.Paths)
            {
                DoOperation(operation, path);
            }
        }
    }
}
