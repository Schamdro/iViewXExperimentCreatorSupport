using iViewXExperimentCreator.Core.Models;
using System;
using System.IO;
using System.Linq;
using iViewXExperimentCreator.Core.Enums;
using iViewXExperimentCreator.Core.Util;

namespace iViewXExperimentCreator.Core.Subroutines
{
    /// <summary>
    /// Diese Klasse startet eine Subroutine, die den Ordner \Stimuli auf während der Laufzeit hinzugefügte Reize 
    /// überprüft und fügt diese Reize über ein Action-Delegate der Stimuli-Liste aus dem MainViewModel hinzu.
    /// </summary>
    public class StimulusListUpdater 
    {
        /// <summary>
        /// Das Stimulus-Verzeichnis im Experimentordner.
        /// </summary>
        public static string StimulusDirectory { get; set; }

        /// <summary>
        /// Unterstützte Dateiformate für Bildreize.
        /// 
        /// PNG, JPG, JPEG, BMP, PDF, GIF, TIF, TIFF
        /// </summary>
        public static readonly string[] SUPPORTED_IMAGE_EXTENSIONS = { ".png", ".jpg", ".bmp", ".jpeg", ".pdf", ".gif", ".tif", ".tiff" };
        /// <summary>
        /// Unterstützte Dateiformate für Videoreize.
        /// 
        /// MP4, AVI, MOV, WMV
        /// </summary>
        public static readonly string[] SUPPORTED_VIDEO_EXTENSIONS = { ".mp4", ".avi", ".mov", ".wmv" };

        public static readonly string[] SUPPORTED_EXTENSIONS = SUPPORTED_IMAGE_EXTENSIONS
                                                                .Union(SUPPORTED_VIDEO_EXTENSIONS)
                                                                .ToArray();


        private FileSystemWatcher _watcher;
        /// <summary>
        /// Der FileSystemWatcher. Überprüft den Stimuli-Ordnet im Experimentverzeichnis auf neue Dateien.
        /// </summary>
        public FileSystemWatcher Watcher { get => _watcher; }

        /// <summary>
        /// Referenziert Add-Funktion der Stimuli-MvxObservableCollection im ExperimentModel.
        /// </summary>
        private static Action<StimulusModel, ExtensionType> _addToList;
        
        /// <summary>
        /// Konstruktor. Nimmt ein Delegate entegegen. Dieser wird zum Hinzufügen der Reize in eine Liste verwendet.
        /// Erklärung, wieso das so ist, ist bei der Übergabe im ExperimentModel selbst gegeben.
        /// </summary>
        /// <param name="addToList"></param>
        /// <param name="path"></param>
        public StimulusListUpdater(Action<StimulusModel, ExtensionType> addToList, string path)
        {
            StimulusDirectory = Path.Combine(path, "Stimuli");
            //Logger.Debug($"StimulusListUpdater: FileWatcherSystem-Subroutine auf {StimulusDirectory} gestartet.");
            _addToList = addToList;
            //ProcessInitialFolderContents();
            InitFileSystemWatcher();
            ConfigureFileSystemWatcher();
        }

        /// <summary>
        /// Hält den Watcher an und gibt alle gehaltenen Dateien frei.
        /// </summary>
        public void Stop()
        {
            Watcher.Dispose();
        }

        /// <summary>
        /// Startet den FileSystemWatcher auf dem Stimuli-Verzeichnis im Experimentordner.
        /// </summary>
        private void InitFileSystemWatcher()
        {
            //2 Mal probieren. Wenn Directory nicht erstellt werden kann, dann liegt ein anderes Problem vor.
            for (int i = 0; i < 2; i++)
            {
                try
                {
                    _watcher = new FileSystemWatcher(StimulusDirectory);
                    break;
                }
                catch (ArgumentException e)
                {
                    if (i == 0)
                    {
                        //Wenn das Directory noch nicht existiert, dann wird es jetzt erstellt.
                        Logger.Message("Stimulus-Ordner erstellen...");
                        Directory.CreateDirectory(StimulusDirectory);
                    }
                    else
                    {
                        Logger.Error(e, "Stimulus-Ordner konnte nicht erstellt werden.");
                    }
                }
            }
        }

        /// <summary>
        /// Einstellungen des FileSystemWatchers werden hier konfiguriert. 
        /// </summary>
        private void ConfigureFileSystemWatcher()
        {
            _watcher.Created += OnCreated; //Event soll feuern, wenn neue Datei im Ordner entdeckt wird

            //Beobachte Dateien mit den unterstützten Endungen, die in SUPPORTED_EXTENSIONS definiert sind
            foreach (string ext in SUPPORTED_EXTENSIONS)
                _watcher.Filters.Add("*"+ext.ToLower());

            _watcher.NotifyFilter = NotifyFilters.Attributes |
                                    NotifyFilters.CreationTime |
                                    NotifyFilters.FileName |
                                    NotifyFilters.LastAccess |
                                    NotifyFilters.LastWrite |
                                    NotifyFilters.Size |
                                    NotifyFilters.Security;
            _watcher.InternalBufferSize = 64000; //Gab bei sehr vielen neuen Files gleichzeitig oder großen PDFs Probleme mit Buffer-Overflow

            _watcher.IncludeSubdirectories = true; //Falls Nutzer Subdirectories für Ordnung verwenden wollen
            _watcher.EnableRaisingEvents = true;
        }

        /// <summary>
        /// Wird aufgerufen, wenn FileSystemWatcher das Event feuert, dass im überwachten Ordner \Stimuli
        /// eine neue Datei gefunden wurde.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnCreated(object sender, FileSystemEventArgs e) 
        {
            if(Path.GetExtension(e.FullPath).ToLower() == ".pdf")
            {
                ConvertPDF(e.FullPath);
            }
            else
            {
                AddToListWithExt(e.FullPath);
            }
        } 

        /// <summary>
        /// Fügt abhängig von der Dateiendung den Stimulus in die korrekte Liste hinzu.
        /// </summary>
        /// <param name="fp"></param>
        /// <param name="loaded"></param>
        private static void AddToListWithExt(string path)
        {
            ExtensionType ext = ValidateFileExtension(path);

            _addToList.Invoke(CreateNewStimulus(path, ext), ext);
        }

        /// <summary>
        /// Überprüft, ob die Dateiendung valide ist.
        /// </summary>
        /// <param name="fp"></param>
        /// <returns></returns>
        private static ExtensionType ValidateFileExtension(string fp)
        {
            string ext = Path.GetExtension(fp);

            if (SUPPORTED_VIDEO_EXTENSIONS.Contains(ext.ToLower())) return ExtensionType.Video;
            if (SUPPORTED_IMAGE_EXTENSIONS.Contains(ext.ToLower())) return ExtensionType.Image;

            return ExtensionType.Invalid;
        }

        /// <summary>
        /// Erstellt ein neues StimulusModel mit dem übergebenen Dateipfad.
        /// </summary>
        /// <param name="path"></param>
        /// <param name="loaded"></param>
        /// <returns></returns>
        private static StimulusModel CreateNewStimulus(string path, ExtensionType ext)
        {
            StimulusModel sm = new(path, ext);
            return sm;
        }

        /// <summary>
        /// Kopiert eine Stimulus-Datei in das Stimuli-Verzeichnis im Experimentordner. 
        /// </summary>
        /// <param name="sourcePath"></param>
        public static void ImportNewStimulus(string sourcePath)
        {
            string name = Path.GetFileName(sourcePath);

            if (File.Exists(sourcePath))
            {
                if (Path.GetExtension(sourcePath).ToLower() == ".pdf")
                {
                    ConvertPDF(sourcePath);
                }
                else
                {
                    string destPath = Path.Combine(StimulusDirectory, name);
                    File.Copy(sourcePath, destPath.GetFreeFilePath(), true);
                    //Thread.Sleep(1000);
                }
            }
            else
            {
                Logger.Message("Verzeichnis " + sourcePath + " existiert nicht.");
            }
        }

        /// <summary>
        /// Konvertiert eine PDF-Datei mit der PDFConverter-Klasse unter Verwendung von Ghostscript.
        /// </summary>
        /// <param name="path"></param>
        public static void ConvertPDF(string path)
        {
            if (Path.GetExtension(path).ToLower() == ".pdf")
            {
                PDFConverter.ConvertToPng(path, StimulusDirectory);
            }
        }
    }
}
