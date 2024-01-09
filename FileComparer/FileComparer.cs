// See https://aka.ms/new-console-template for more information

using System.IO;

class FileComparer {

    static string outputLogPath = @"file_comparer_output.log";
    static string sourceFolder = @"e:";
    static string dstFolder = @"f:";
    static int fileCount = 1;
    static int i = 0, next_refresh = 0, diff = 0;

    static void WriteLog(string msg)
    {
        Console.WriteLine(msg);
        using (StreamWriter sw = File.AppendText(outputLogPath))
        {
            sw.WriteLine(msg);
        }
    }

    class DirectoryBrowser {
        Queue<string> directoryQueue = new Queue<string>();
        public DirectoryBrowser(string directory) {
            directoryQueue.Enqueue(directory);
        }
        public string GetCurrent() {
            return directoryQueue.Peek();
        }
        public bool IsEndReached() {
            return directoryQueue.Count() <= 0;
        }
        public void Next() {
            string currentDirPath = directoryQueue.Dequeue();
            DirectoryInfo currentDir = new DirectoryInfo(currentDirPath);
            foreach(DirectoryInfo subdir in currentDir.EnumerateDirectories())
            {
                if(!subdir.Attributes.HasFlag(FileAttributes.Hidden))
                {
                    directoryQueue.Enqueue(subdir.FullName);
                }
            }
        }
        public void Reset(string path){
            directoryQueue.Clear();
            directoryQueue.Enqueue(path);
        }
    }

    static int CountFiles(string path){
        DirectoryBrowser directoryBrowser = new DirectoryBrowser(path);
        int count = 0;
        while (!directoryBrowser.IsEndReached())
        {
            count += Directory.EnumerateFiles(directoryBrowser.GetCurrent()).Count();
            directoryBrowser.Next();
        }
        return count;
    }

    static void Main(string[] arguments){

        if (arguments.Length >= 1)
        {
            sourceFolder = arguments[0];
        }
        if (arguments.Length >= 2)
        {
            dstFolder = arguments[1];
        }
        if (arguments.Length >= 3)
        {
            outputLogPath = arguments[2];
        }
        WriteLog($"[{DateTime.Now}] Debut de la comparaison de {sourceFolder} avec {dstFolder}.");
        Console.WriteLine($"Fichier log (sortie) : {outputLogPath}");
        if(!Directory.Exists(sourceFolder) || !Directory.Exists(dstFolder))
        {
            WriteLog($"Erreur: dossiers a comparer introuvables");
            return;
        }

        WriteLog($"Enumeration des fichiers de {sourceFolder} ...");
        fileCount = CountFiles(sourceFolder);

        DirectoryBrowser directoryBrowser = new DirectoryBrowser(sourceFolder);
        
        while (!directoryBrowser.IsEndReached())
        {
            foreach (string filePath in Directory.EnumerateFiles(directoryBrowser.GetCurrent()))
            {
                i++;
                CompareFile(filePath);
            }
            directoryBrowser.Next();
        }


        WriteLog($"Fin de la comparaison, {diff} differences detectees.");
    }

    

    static void CompareFile(string path)
    {
        //Show user progression :   
        if(i >= next_refresh)
        {
            next_refresh = next_refresh + Math.Max(fileCount / 100, 1);
            int percent = 100 * i / fileCount;
            Console.WriteLine($"[{i}/{fileCount}] {path}");
        }

        string dstFile = Path.GetFullPath(Path.Join(dstFolder, Path.GetRelativePath(sourceFolder, path)));
    
        if (!File.Exists(dstFile))
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            WriteLog($"Fichier introuvable : {dstFile}");
            Console.ResetColor();
            diff++;
            return;
        }

        FileInfo dstFileInfo = new FileInfo(dstFile);
        FileInfo srcFileInfo = new FileInfo(path);

        if (dstFileInfo.LastWriteTimeUtc != srcFileInfo.LastWriteTimeUtc)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            WriteLog($"Date de modif. differente : {dstFile}");
            Console.ResetColor();
            diff++;
            return;
        }
        
        if (srcFileInfo.Length != dstFileInfo.Length)
        {
            Console.ForegroundColor = ConsoleColor.Blue;
            WriteLog($"Taille differente : {dstFile}");
            Console.ResetColor();
            diff++;
        }
    }
}

