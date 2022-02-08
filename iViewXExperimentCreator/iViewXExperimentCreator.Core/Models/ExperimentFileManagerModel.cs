using iViewXExperimentCreator.Core.ViewModels;
using Newtonsoft.Json;
using System;
using System.IO;

namespace iViewXExperimentCreator.Core.Models
{
    /// <summary>
    /// Hält das aktive Experiment und führt Speicher- und Ladeoperationen daran aus.
    /// </summary>
    public static class ExperimentFileManagerModel
    {
        /// <summary>
        /// Pfad des Experimentordners.
        /// </summary>
        public static string BasePath => AppContext.BaseDirectory + @"Experiments\";


        private static ExperimentModel _currentExperiment;
        /// <summary>
        /// Gibt das derzeitige Experiment zurück oder, falls dieses neu gesetzt wird, wird das
        /// abgelöste Experiment zunächst gespeichert, dann ersetzt und danach werden alle Eigenschaften
        /// an den View als geändert gemeldet, da sich die meisten Eigenschaften geändert haben sollten.
        /// </summary>
        public static ExperimentModel CurrentExperiment
        {
            get { return _currentExperiment; }
            set
            {
                if (_currentExperiment != null) SaveExperiment();
                _currentExperiment = value;
                MainViewModel.Instance.SelectedPresentable = null;
                MainViewModel.Instance.LastSelectedPresentable = null;
                MainViewModel.Instance.PreviewImage = null;
                MainViewModel.Instance.RaiseAllPropertiesChanged();
            }
        }

        /// <summary>
        /// JSON-Einstellungen.
        /// </summary>
        public static JsonSerializerSettings JsonSettings { get; } = new()
        {
            TypeNameHandling = TypeNameHandling.Auto,
            Formatting = Formatting.Indented,
            PreserveReferencesHandling = PreserveReferencesHandling.All
        };

        /// <summary>
        /// Erstellt ein neues Experiment.
        /// </summary>
        /// <param name="expName"></param>
        /// <param name="res"></param>
        /// <param name="calPoints"></param>
        public static void CreateExperiment(string expName, (int, int) res, int calPoints)
        {
            ExperimentModel experiment = new(expName, res, calPoints);
            Logger.Message($"Neues Experiment erstellt:\nName: '{expName}'\n" +
                $"Auflösung: {res}\nKalibrierungspunkte: {calPoints}");
            experiment.SelectedCalibrationPoints = calPoints;
            CurrentExperiment = experiment;
            SaveExperiment();
            MainViewModel.Instance.CloseAllOtherViewModels();
        }

        /// <summary>
        /// Erstellt eine JSON-Datei im Experiment-Ordner mit den Experiment-Einstellungen. Falls
        /// die Datei bereits existiert, wird die Datei überspeichert. 
        /// </summary>
        public static void SaveExperiment() => SaveExperiment(CurrentExperiment?.ExperimentPath);

        /// <summary>
        /// Erstellt eine JSON-Datei im Experiment-Ordner mit den Experiment-Einstellungen. Falls
        /// die Datei bereits existiert, wird die Datei überspeichert. 
        /// </summary>
        /// <param name="path"></param>
        public static void SaveExperiment(string path)
        {
            if (CurrentExperiment == null) return;
            string expJson = JsonConvert.SerializeObject(CurrentExperiment, JsonSettings);
            File.WriteAllText(Path.Combine(path, "ExperimentSettings.json"), expJson);
        }
        /// <summary>
        /// Lädt das Experiment mit dem übergebenen Namen.
        /// </summary>
        /// <param name="expName"></param>
        public static void LoadExperiment(string expName)
        {
            if (expName == CurrentExperiment?.Name) return;

            string experimentDirectory = Path.Combine(AppContext.BaseDirectory, $@"Experiments\{expName}");
            if (Directory.Exists(experimentDirectory))
            {

                string settingsPath = Path.Combine(experimentDirectory, "ExperimentSettings.json");
                if (File.Exists(settingsPath))
                {
                    CurrentExperiment = JsonConvert.DeserializeObject<ExperimentModel>(File.ReadAllText(settingsPath), JsonSettings);
                    CurrentExperiment.HandleLoadUp();
                    //MainViewModel.Instance.Calibrated = false;
                    MainViewModel.Instance.CloseAllOtherViewModels();
                    //MainViewModel.Instance.RaiseAllPropertiesChanged();
                }
                else
                {
                    Logger.Error(new FileNotFoundException(), "ExperimentSettings.json konnte nicht geladen werden.");
                }
            }
            else
            {
                Logger.Error(new DirectoryNotFoundException(), $"Das gesuchte Verzeichnis {experimentDirectory} konnte nicht gefunden werden.");
            }
        }

        /// <summary>
        /// Löscht das Experiment mit dem übergebenen Namen. Dadurch werden ALLE Daten im Experimentverzeichnis gelöscht, inkl aller Versuchslogs. 
        /// </summary>
        /// <param name="expName"></param>
        public static void DeleteExperiment(string expName, bool dueToRename = false)
        {
            if (CurrentExperiment is not null)
            {
                if (CurrentExperiment.Name == expName)
                {
                    CurrentExperiment.HandleFileWatcherShutDown();
                    CurrentExperiment.ClearStimulusList();
                    CurrentExperiment.ClearExperimentComponentsList();
                    MainViewModel.Instance.SelectedPresentable = null;
                    MainViewModel.Instance.LastSelectedPresentable = null;
                    CurrentExperiment.PresentationElements = null;
                    MainViewModel.Instance.CloseAllOtherViewModels();
                    CurrentExperiment = null;
                }
            }

            string experimentDirectory = Path.Combine(AppContext.BaseDirectory, @$"Experiments\{expName}");
            if (Directory.Exists(experimentDirectory) && expName != null)
            {
                MainViewModel.Instance.PreviewImage = null;
                try
                {
                    Directory.Delete(experimentDirectory, true);
                    if(!dueToRename) Logger.Message($"Experiment '{ expName }' wurde erfolgreich gelöscht.");
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"Konnte Verzeichnis {experimentDirectory} nicht löschen.");
                }
            }
        }
    }
}
