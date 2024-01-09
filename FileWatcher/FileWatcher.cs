using System.Text.RegularExpressions;

namespace MyNamespace
{
    struct FileOperation {
        public string path;
        public DateTime date;
        public WatcherChangeTypes type;

        public FileOperation(string path, DateTime date, WatcherChangeTypes type) {
            this.path = path;
            this.date = date;
            this.type = type;
        }
    }

    class MyFileWatcher
    {
        static List<FileOperation> fileOperationList = new List<FileOperation>();
        static TimeSpan MaxMoveDuration = new TimeSpan(0,0,3);

        static System.Timers.Timer refreshTimer = new System.Timers.Timer();
        static string outputFilePath =  @"output.txt";      

        static void Main(string[] arguments)
        {
            string pathToWatch = @".";
            if (arguments.Length >= 1)
            {
                pathToWatch = arguments[0];
            }
            if (arguments.Length >= 2)
            {
                outputFilePath = arguments[1];
            }
            
            Console.WriteLine($"Watching folder : {Path.GetFullPath(pathToWatch)}");
            Console.WriteLine($"Output log file : {Path.GetFullPath(outputFilePath)}");
        

            refreshTimer.Stop();
            refreshTimer.Elapsed += OnRefreshTimer;
            refreshTimer.Interval = MaxMoveDuration.TotalMilliseconds;
            refreshTimer.AutoReset = false;


            using var watcher = new FileSystemWatcher(pathToWatch);
            watcher.NotifyFilter = NotifyFilters.DirectoryName
                                 | NotifyFilters.FileName
                                 | NotifyFilters.LastWrite
                                 | NotifyFilters.Size;

                               

            watcher.Created += OnCreated;
            watcher.Deleted += OnDeleted;
            watcher.Changed += OnChanged;
            watcher.Renamed += OnRenamed;
            watcher.Error += OnError;

            watcher.Filter = "*.*";
            watcher.IncludeSubdirectories = true;
            watcher.EnableRaisingEvents = true;

            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
            Console.Write(fileOperationList.ToString());
        }
        private static void WriteOperationToLogFile(DateTime date, string operation, string path1, string path2)
        {
            using (StreamWriter sw = File.AppendText(outputFilePath))
            {
                sw.WriteLine($"{date} | {operation} | {path1} | {path2}");
            }	
        }

        private static void FileCreated(string path, DateTime date) {
            Console.ForegroundColor = ConsoleColor.DarkGreen;
            string value = $"{date} Created: {path}";
            WriteOperationToLogFile(date, "Created", path, "");
            Console.WriteLine(value);
            Console.ResetColor();

        }
        private static void FileDeleted(string path, DateTime date) {
            Console.ForegroundColor = ConsoleColor.DarkMagenta;
            string value = $"{date} Deleted: {path}";
            WriteOperationToLogFile(date, "Deleted", path, "");
            Console.WriteLine(value);
            Console.ResetColor();

        }
        private static void FileMoved(string oldPath, string newPath, DateTime date) {
            Console.WriteLine($"{date} Moved: {oldPath} => {newPath}");
            WriteOperationToLogFile(date, "Moved", oldPath, newPath);
        }
        private static void FileChanged(string path, DateTime date) {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"{date} CONFLICT : Changed : {path}");
            WriteOperationToLogFile(date, "Changed", path, "");
            Console.ResetColor();
        }
        private static void FileRenamed(string oldPath, string newPath, string newName, DateTime date) {
            Console.ForegroundColor = ConsoleColor.DarkBlue;
            Console.WriteLine($"{date} Renamed: {oldPath} => {newName}");
            WriteOperationToLogFile(date, "Renamed", oldPath, newPath);
            Console.ResetColor();
        }
        private static bool IsPathANewFolder(string path)
        {
            return Regex.IsMatch(path, @"\\Nouveau dossier( \(d+\))?$");
        }

        private static void OnCreated(object sender, FileSystemEventArgs e)
        {
            if (e.ChangeType != WatcherChangeTypes.Created)
            {
                return;
            }

            //Skip log file
            if (Path.GetFullPath(outputFilePath) == Path.GetFullPath(e.FullPath))
            {
                return;
            }


            FileOperation fileCreation = new FileOperation(e.FullPath, DateTime.Now, e.ChangeType);

            //If it's a new folder, we assume it will be renamed
            if (Directory.Exists(e.FullPath) && IsPathANewFolder(e.FullPath))
            { 
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{DateTime.Now} WARNING ! New folder creation skipped: {e.FullPath}");
                Console.ResetColor();
                return;
            }


            FileOperation? fileDeletion = LookForFileMove(fileCreation);
            if(fileDeletion.HasValue)
            {
                //move detected
                lock (fileOperationList) 
                {
                    fileOperationList.Remove(fileDeletion.Value);
                }
                FileMoved(fileDeletion.Value.path, fileCreation.path, fileCreation.date);
            }
            else
            {
                lock (fileOperationList) 
                {
                    fileOperationList.Add(fileCreation);
                }
                if(!refreshTimer.Enabled)
                    refreshTimer.Start();
            } 
        }

        private static void OnDeleted(object sender, FileSystemEventArgs e) {
            if (e.ChangeType != WatcherChangeTypes.Deleted)
                return;

            //Skip log file
            if (Path.GetFullPath(outputFilePath) == Path.GetFullPath(e.FullPath))
            {
                return;
            }

            //If it's a new folder, we assume it was empty
            if (Directory.Exists(e.FullPath) && IsPathANewFolder(e.FullPath))
            { 
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"{DateTime.Now} WARNING ! New folder deletion skipped: {e.FullPath}");
                Console.ResetColor();
                return;
            }

            FileOperation fileDeletion = new FileOperation(e.FullPath, DateTime.Now, WatcherChangeTypes.Deleted);
            FileOperation? fileCreation = LookForFileMove(fileDeletion);
           
                if(fileCreation.HasValue)
                {
                    lock (fileOperationList) 
                    {
                        fileOperationList.Remove(fileCreation.Value);
                    }
                    FileMoved(fileDeletion.path, fileCreation.Value.path, fileDeletion.date);
                }
                else
                {
                    lock (fileOperationList) 
                    {
                        fileOperationList.Add(fileDeletion);
                    }
                    if(!refreshTimer.Enabled)
                        refreshTimer.Start();
                }
        }

        private static FileOperation? LookForFileMove(FileOperation lastOperation)
        {
            WatcherChangeTypes changeTypeResearched = WatcherChangeTypes.Deleted;
            switch(lastOperation.type)
            {
                case WatcherChangeTypes.Created:
                    changeTypeResearched = WatcherChangeTypes.Deleted;
                    break;
                case WatcherChangeTypes.Deleted:
                    changeTypeResearched = WatcherChangeTypes.Created;
                    break;
                default:
                    return null;//error
            }
            
            lock(fileOperationList)
            {
                foreach(FileOperation operation in fileOperationList)
                {                    
                    if (operation.path != lastOperation.path && 
                        Path.GetFileName(operation.path) == Path.GetFileName(lastOperation.path) &&
                        changeTypeResearched == operation.type &&
                        lastOperation.date - operation.date <= MaxMoveDuration)
                    {
                        return operation;
                    }
                }
            }
        
            return null;
        }

        private static void OnRefreshTimer(Object? source, System.Timers.ElapsedEventArgs e)
        {
            lock(fileOperationList) {
                foreach (FileOperation operation in fileOperationList) {

                    if (operation.type != WatcherChangeTypes.Created && operation.type != WatcherChangeTypes.Deleted)
                        continue;

                    FileOperation? otherOperation = LookForFileMove(operation);
                    if(otherOperation.HasValue)
                    {
                        if (operation.type == WatcherChangeTypes.Deleted)
                            FileMoved(operation.path, otherOperation.Value.path, operation.date);
                        else
                            FileMoved(otherOperation.Value.path, operation.path, operation.date);
                    }
                    else
                    {
                        if (operation.type == WatcherChangeTypes.Created)
                            FileCreated(operation.path, operation.date);
                        else
                            FileDeleted(operation.path, operation.date);
                    }
                }
                fileOperationList.Clear();
            }
        }


        private static void OnChanged(object sender, FileSystemEventArgs e)
        {
            //Skip log file
            if (Path.GetFullPath(outputFilePath) == Path.GetFullPath(e.FullPath))
            {
                return;
            }

            if (e.ChangeType != WatcherChangeTypes.Changed || !File.Exists(e.FullPath))
            {
                return;
            }
            FileChanged(e.FullPath, DateTime.Now);
        }

        private static void OnRenamed(object sender, RenamedEventArgs e)
        {
            if (e.ChangeType == WatcherChangeTypes.Renamed)
            {
                //Skip log file
                if (Path.GetFullPath(outputFilePath) == Path.GetFullPath(e.FullPath))
                {
                    return;
                }

                if (File.Exists(e.FullPath))
                {
                    FileRenamed(e.OldFullPath, e.FullPath, Path.GetFileName(e.FullPath), DateTime.Now);
                } 
                else if (Directory.Exists(e.FullPath))
                {
                    if (IsPathANewFolder(e.OldFullPath))
                    { 
                        FileCreated(e.FullPath, DateTime.Now);
                    }
                    else 
                    {
                        DirectoryInfo newDir = new DirectoryInfo(e.FullPath);
                        FileRenamed(e.OldFullPath, e.FullPath, newDir.Name, DateTime.Now);
                    }
                }
            }
        }

        private static void OnError(object sender, ErrorEventArgs e) =>
            PrintException(e.GetException());

        private static void PrintException(Exception? ex)
        {
            if (ex != null)
            {
                Console.WriteLine($"Message: {ex.Message}");
                Console.WriteLine("Stacktrace:");
                Console.WriteLine(ex.StackTrace);
                Console.WriteLine();
                PrintException(ex.InnerException);
            }
        }
    }
}
