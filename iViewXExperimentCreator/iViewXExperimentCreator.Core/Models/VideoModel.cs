using iViewXExperimentCreator.Core.Util;
using iViewXExperimentCreator.Core.ViewModels;
using MediaToolkit;
using MediaToolkit.Model;
using MediaToolkit.Options;
using MvvmCross.ViewModels;
using System;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace iViewXExperimentCreator.Core.Models
{
    /// <summary>
    /// Eine Videokomponente. VideoModels sind sowohl Experimentkomponenten als auch direkt präsentierbare Elemente. Damit
    /// fallen sie simulatan unter die selben Kategorien wie Sequenzen und Snapshots. VideoModels verwenden die MediaToolkit-
    /// Bibliothek für das Schneiden der Videos. 
    /// </summary>
    public class VideoModel : MvxNotifyPropertyChanged, IExperimentComponent, IPresentable
    {
        private string _name = "Default Video";
        private float _timestamp = 0f;
        private float _duration = 0f;
        private bool _active = true;
        private bool _pauses = false;
        private float _volume = 1f;
        private StimulusModel _stimulus;
        private MediaFile videoFile;

        /// <summary>
        /// Der dem Video zu Grunde liegende Reiz. Muss ein Videoreiz sein.
        /// </summary>
        public StimulusModel Stimulus
        {
            get => _stimulus;
            set
            {
                _stimulus = value;
                videoFile = new MediaFile { Filename = _stimulus?.FilePath };
                UpdateInformation();
            }
        }
        /// <summary>
        /// Der Name des Videos. Ist einzigartig und wird automatisch mit (i) (wo i = noch nicht vorhandene Zahl ist) appendiert, 
        /// wenn der Name bereits von einer Sequenz oder einem Video belegt wird.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set
            {
                value = value.GetFreeNameInCollection(MainViewModel.Instance.ExperimentComponents);
                SetProperty(ref _name, value);
            }
        }

        /// <summary>
        /// Schneidet, konvertiert und speichert dann das Videosnippet auf der Festplatte. 
        /// </summary>
        /// <param name="path"></param>
        public void SaveVideoSnippet(string path)
        {
            using (Engine engine = new())
            {
                var options = new ConversionOptions();
                options.CutMedia(TimeSpan.FromSeconds(Timestamp), TimeSpan.FromSeconds(Duration));
                MediaFile cutFile = new(path);
                engine.Convert(videoFile, cutFile, options);
            }
        }

        /// <summary>
        /// Gibt an, ob diese Komponente aktiv ist. Inaktive Komponenten werden in Präsentationen nicht berücksichtigt.
        /// </summary>
        public bool Active { get => _active; set => SetProperty(ref _active, value); }
        /// <summary>
        /// Gibt an, wie lange das Video ab dem Startzeitstempel geht. 
        /// </summary>
        public float Duration { get => _duration; set => SetProperty(ref _duration, Math.Max(value, 0)); }
        /// <summary>
        /// Gibt an, ab welchem Punkt das Video starten soll.
        /// </summary>
        public float Timestamp { get => _timestamp; set => SetProperty(ref _timestamp, Math.Max(value, 0)); }
        /// <summary>
        /// Gibt an, ob die Präsentation pausiert, vor das Video startet. 
        /// </summary>
        public bool Pauses { get => _pauses; set => SetProperty(ref _pauses, value); }
        /// <summary>
        /// Gibt an, wie laut das Video in der Präsentation sein soll (in Prozent).
        /// </summary>
        public float Volume { get => _volume; set => SetProperty(ref _volume, Math.Max(value, 0)); }

        /// <summary>
        /// Der Konstruktor. 
        /// </summary>
        public VideoModel(string name)
        {
            Name = name;
            Logger.Message($"Neues Video erstellt.");
        }

        /// <summary>
        /// Wird im Falle einer Änderung des Stimulus aufgerufen und liest die relevaten Metadaten aus dem Video
        /// erneut aus und passt gegebenenfalles Eigenschaften des VideoModels an.
        /// </summary>
        private void UpdateInformation()
        {
            if (_stimulus is null) return;
            using (Engine engine = new())
            {
                try
                {
                    engine.GetMetadata(videoFile);
                    Duration = (float)videoFile.Metadata.Duration.TotalSeconds;
                }
                catch (Exception e)
                {
                    Logger.Error(e, $"Fehler beim Laden der Videodatei {Name}.");
                }
            }
        }


        [JsonConstructor]
        public VideoModel() { }

        [OnDeserialized]
        internal void OnDeserialized(StreamingContext context)
        {
            Logger.Message($"Video geladen: '{Name}'");
        }
    }
}
