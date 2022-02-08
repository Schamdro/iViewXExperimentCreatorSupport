using MvvmCross.ViewModels;
using Newtonsoft.Json;
using System;

namespace iViewXExperimentCreator.Core.Models
{
    /// <summary>
    /// Ein Slot enthält einen Reiz und Informationen darüber, wann und wie dieser angezeigt werden soll. Dabei
    /// können Startzeitpunkt, Dauer, Position und weitere Eigenschaften bestimmt werden.
    /// </summary>
    public class SlotModel : MvxNotifyPropertyChanged
    {
        public static string FallbackStimulusPath => AppContext.BaseDirectory + @"\Rsc\fallbackStim.png";
        
        private StimulusModel _stimulus;
        private float _startTime;
        private float _duration = 1f;
        private float _scale = 1f;
        private (int x, int y) _coordinates;
        private bool _pauses = false;
        private int _layer = 0;

        /// <summary>
        /// Zeitpunkt, an welchem der Slot innerhalb der Sequenz beginnt.
        /// </summary>
        public float StartTime
        {
            get { return _startTime; }
            set { SetProperty(ref _startTime, Math.Max(value, 0)); }
        }

        /// <summary>
        /// Dauer, wie lange der Slot innerhalb der Sequenz aktiv ist.
        /// </summary>
        public float Duration
        {
            get { return _duration; }
            set { SetProperty(ref _duration, Math.Max(value, 0)); }
        }

        /// <summary>
        /// X-Koordinate des Reizes auf dem generierten Snapshot.
        /// </summary>
        public int XCoordinate
        {
            get { return _coordinates.x; }
            set
            {
                Logger.Debug("XCoordinate setting");
                SetProperty(ref _coordinates.x, value);
                Logger.Debug("XCoordinate set");
            }
        }

        /// <summary>
        /// Y-Koordinate des Reizes auf dem generierten Snapshot.
        /// </summary>
        public int YCoordinate
        {
            get { return _coordinates.y; }
            set
            {
                Logger.Debug("YCoordinate setting");
                SetProperty(ref _coordinates.y, value);
                Logger.Debug("YCoordinate set");
            }
        }

        /// <summary>
        /// Größenskalierung des Reizes auf dem generierten Snapshot.
        /// </summary>
        public float Scale
        {
            get { return _scale; }
            set { SetProperty(ref _scale, Math.Max(value, 0)); }
        }

        /// <summary>
        /// Bestimmt, ob in einer Präsentation der erste Snapshot, welcher aus diesem Slot generiert wurde, die Präsentation pausiert.
        /// </summary>
        public bool Pauses
        {
            get { return _pauses; }
            set { SetProperty(ref _pauses, value); }
        }

        /// <summary>
        /// Auf welcher Ebene wird der in diesem Slot enthaltene Reiz im Snapshot gezeichnet? Höhere Ebenen werden
        /// über niedrigeren Ebenen gezeichnet.
        /// </summary>
        public int Layer
        {
            get { return _layer; }
            set { SetProperty(ref _layer, Math.Max(value, 0)); }
        }

        /// <summary>
        /// Der Zeitstempel, an welchem der Slot endet. Wird aus Startzeit + Dauer berechnet.
        /// </summary>
        public float EndTime => StartTime + Duration;

        /// <summary>
        /// Der Reiz, welcher dem Slot zugewiesen ist. Kann nur ein Bildreiz sein.
        /// </summary>
        public StimulusModel Stimulus
        {
            get
            {
                return _stimulus;
            }
            set
            {
                SetProperty(ref _stimulus, value);
                RaisePropertyChanged("StimulusPath");
            }
        }

        /// <summary>
        /// Der Dateipfad des zugewiesenen Reizes.
        /// </summary>
        [JsonIgnore]
        public string StimulusPath => Stimulus?.FilePath ?? FallbackStimulusPath;

        /// <summary>
        /// Passt die Position des Reizes so an, dass dieser zentriert im Snapshot ist.
        /// </summary>
        public void CenterStimulus()
        {
            XCoordinate = ExperimentFileManagerModel.CurrentExperiment.ResolutionX / 2;
            YCoordinate = ExperimentFileManagerModel.CurrentExperiment.ResolutionY / 2;
        }

        /// <summary>
        /// Passt die Skalierung des Reizes an die Auflösung des Experiments an.
        /// </summary>
        public void StretchStimulus()
        {
            //Logger.Debug($"Trying to stretch stimulus\n{Stimulus.ResX}/{Stimulus.ResY}");

            if (Stimulus.ResX == 0 || Stimulus.ResY == 0)
            {
                Logger.Message("Der Reiz wurde fehlerhaft importiert. Die Strecken-Funktion ist deshalb deaktiviert.");
                return;
            }

            //Logger.Debug("Stretch Stimulus");

            float xResScalingFactor = (float) ExperimentFileManagerModel.CurrentExperiment.ResolutionX / (float) Stimulus.ResX;
            float yResScalingFactor = (float) ExperimentFileManagerModel.CurrentExperiment.ResolutionY / (float) Stimulus.ResY;

            Scale = MathF.Min(xResScalingFactor, yResScalingFactor);

            //Logger.Debug("Scale ist jetzt " + Scale);
        }
    }
}
