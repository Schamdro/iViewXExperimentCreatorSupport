using MvvmCross.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using iViewXExperimentCreator.Core.ViewModels;
using System.Collections.Specialized;
using System.Drawing;
using iViewXExperimentCreator.Core.Util;
using System.ComponentModel;
using Newtonsoft.Json;
using System.IO;
using iViewXExperimentCreator.Core.Subroutines;
using iViewXExperimentCreator.Core.Enums;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using Microsoft.VisualBasic.FileIO;
using System.Runtime.Serialization;
using MvvmCross;
using MvvmCross.Base;

namespace iViewXExperimentCreator.Core.Models
{
    /// <summary>
    /// Ein Experiment. Experimente bestehen aus Einstellungen, Reizen und Komponenten. Aus den Komponenten werden die 
    /// Präsentationselemente generiert. Experimente können geladen und gespeichert werden.
    /// </summary>
    public class ExperimentModel : MvxNotifyPropertyChanged, IHasName
    {
        [JsonProperty]
        private Color _background = Color.Black;

        private bool _loading = false;
        private bool _dirty = true;

        [JsonProperty]
        private string _latestPresentationVersion = "NULL";

        public static int[] CalibrationPoints { get; } = { 5, 9, 13 };
        private string _name;

        private int _resX;
        private int _resY;

        [JsonIgnore]
        private MvxObservableCollection<IPresentable> _presentationElements;
        private int _selectedCalibrationPoints;
        private MvxObservableCollection<IExperimentComponent> _experimentComponents;
        [JsonIgnore]
        private StimulusListUpdater _stimListUpd;


        private MvxObservableCollection<StimulusModel> _imageStimuli = new();
        /// <summary>
        /// Alle in Bildformat vorliegenden Reize.
        /// </summary>
        public MvxObservableCollection<StimulusModel> ImageStimuli
        {
            get { return _imageStimuli; }
            set { SetProperty(ref _imageStimuli, value); }
        }

        private MvxObservableCollection<StimulusModel> _videoStimuli = new();
        /// <summary>
        /// Alle in Videoformat vorliegenden Reize.
        /// </summary>
        public MvxObservableCollection<StimulusModel> VideoStimuli
        {
            get { return _videoStimuli; }
            set { SetProperty(ref _videoStimuli, value); }
        }

        [JsonIgnore]
        public string LatestPresentationVersion 
        { 
            get => _latestPresentationVersion;
            set
            {
                if (_loading) return;
                //Logger.Debug("Latest Presentation Version ist jetzt " + value);
                _latestPresentationVersion = value;
            }
        }

        /// <summary>
        /// Der Name des Experiments. Veränderungen des Namens sollten mit Bedacht geschehen, da damit auch immer
        /// eine Umbenennung des Ordners einhergeht. Diese Umbenennung kann bei einem geladenen Experiment aber nicht
        /// ohne weiteren Aufwand geschehen.
        /// </summary>
        public string Name
        {
            get { return _name; }
            set
            {
                Logger.Message($"Experiment '{Name}' wurde in '{value}' umbenannt.");
                string newExpPath = ExperimentFileManagerModel.BasePath + value;
                string oldExpPath = ExperimentPath;
                string oldName = _name;

                if (Directory.Exists(newExpPath))
                {
                    Logger.Message(@$"Experiment mit dem Namen '{value}' existiert bereits.");
                    return;
                }

                /* Umbenennen eines Experiments ist kompliziert, da auch der Ordner umbenannt werden muss.
                 * 
                 * Dies kann nur durch Kopieren und Löschen geschehen, da in Directory keine plattformunabhängige
                 * Funktion existiert, um ein Directory umzubennenen. Der Befehl Directory.Move existiert zwar,
                 * funktioniert aber nicht, wenn noch Ressourcen im Source-Directory verwendet werden. Da Bilder
                 * direkt an die UI binden (Design-Fehler und an diesem Punkt sehr aufwendig zu beheben, da sonst zB
                 * die Drag-and-Drop-Funktionalität komplett anders und neu designt werden müsste), ist müssen erst
                 * alle Pfade aktualisiert werden, bevor das alte Verzeichnis gelöscht werden kann.
                 */


                // Fahre File System Watcher herunter, da dieser das alte Verzeichnis beobachtet
                HandleFileWatcherShutDown();
                // Setze neuen Experimentnamen und informiere GUI
                SetProperty(ref _name, value);
                // Pfad hat sich ebenfalls geändert, informiere GUI
                RaisePropertyChanged("ExperimentPath");
                // Speichere Experiment, damit beim Kopieren nachher auch alle Daten aktuell sind
                ExperimentFileManagerModel.SaveExperiment(oldExpPath);
                // Kopiere Verzeichnis und Stimuli-Unterverzeichnis inkl aller Daten
                //CopyExperimentDirectory(oldExpPath, newExpPath);
                FileSystem.CopyDirectory(oldExpPath, newExpPath);
                // Aktualisiere die Pfade und GUI-Bindungen der Bilder
                UpdateStimulusPaths(newExpPath);
                // Fahre File System Watcher mit neuem Verzeichnis als Ziel hoch
                HandleFileWatcherLoadUp();
                // Lösche altes Verzeichnis, da nun alles freigegeben ist
                ExperimentFileManagerModel.DeleteExperiment(oldName, true);
            }
        }

        #region Versuchspersoneneingaben

        private bool _leftKeyInputPossible = true;
        private bool _rightKeyInputPossible = true;
        private bool _upKeyInputPossible = false;
        private bool _downKeyInputPossible = false;

        private string _leftKeyMeaning = string.Empty;
        private string _rightKeyMeaning = string.Empty;
        private string _upKeyMeaning = string.Empty;
        private string _downKeyMeaning = string.Empty;

        /// <summary>
        /// Die der linken Pfeiltaste zugewiesene Bedeutung. Wird in der Versuchslog hinterlegt.
        /// </summary>
        public string LeftKeyMeaning { get => _leftKeyMeaning; set => _leftKeyMeaning = value; }
        /// <summary>
        /// Die der rechten Pfeiltaste zugewiesene Bedeutung. Wird in der Versuchslog hinterlegt.
        /// </summary>
        public string RightKeyMeaning { get => _rightKeyMeaning; set => _rightKeyMeaning = value; }
        /// <summary>
        /// Die der oberen Pfeiltaste zugewiesene Bedeutung. Wird in der Versuchslog hinterlegt.
        /// </summary>
        public string UpKeyMeaning { get => _upKeyMeaning; set => _upKeyMeaning = value; }
        /// <summary>
        /// Die der unteren Pfeiltaste zugewiesene Bedeutung. Wird in der Versuchslog hinterlegt.
        /// </summary>
        public string DownKeyMeaning { get => _downKeyMeaning; set => _downKeyMeaning = value; }

        /// <summary>
        /// Wahr, wenn Eingaben der linken Pfeiltaste während der Präsentation erlaubt sind.
        /// </summary>
        public bool LeftKeyInputPossible { get => _leftKeyInputPossible; set => _leftKeyInputPossible = value; }
        /// <summary>
        /// Wahr, wenn Eingaben der rechten Pfeiltaste während der Präsentation erlaubt sind.
        /// </summary>
        public bool RightKeyInputPossible { get => _rightKeyInputPossible; set => _rightKeyInputPossible = value; }
        /// <summary>
        /// Wahr, wenn Eingaben der oberen Pfeiltaste während der Präsentation erlaubt sind.
        /// </summary>
        public bool UpKeyInputPossible { get => _upKeyInputPossible; set => _upKeyInputPossible = value; }
        /// <summary>
        /// Wahr, wenn Eingaben der unteren Pfeiltaste während der Präsentation erlaubt sind.
        /// </summary>
        public bool DownKeyInputPossible { get => _downKeyInputPossible; set => _downKeyInputPossible = value; }

        #endregion

        /// <summary>
        /// Die Komponenten, aus welchen das Experiment besteht. Dies können Sequenzen (SequenceModel) und Videos (VideoModel) sein.
        /// </summary>
        public MvxObservableCollection<IExperimentComponent> Components
        {
            get => _experimentComponents;
            set
            {
                SetProperty(ref _experimentComponents, value);
                Components.CollectionChanged += OnComponentsChanged;
            }
        }

        /// <summary>
        /// Die Breite der Auflösung, in welcher Snapshots und Videos im Preview angezeigt werden.
        /// </summary>
        public int ResolutionX 
        { 
            get => _resX;
            set
            {
                SetProperty(ref _resX, value);
                if(!_loading) Dirty = true;
                MainViewModel.Instance.RefreshPreview();
            }
        }


        /// <summary>
        /// Die Höhe der Auflösung, in welcher Snapshots und Videos im Preview angezeigt werden.
        /// </summary>
        public int ResolutionY
        {
            get => _resY;
            set
            {
                SetProperty(ref _resY, value);
                if(!_loading) Dirty = true;
                MainViewModel.Instance.RefreshPreview();
            }
        }

        /// <summary>
        /// Wurde das Experiment in irgendwelcher Form verändert, dann ist es "dirty". Dies soll bezwecken, dass
        /// Snapshots und Videosnippets nur dann gespeichert werden, wenn sich diese auch tatsächlich verändert haben können.
        /// </summary>
        public bool Dirty
        {
            get { return _dirty; }
            set 
            {
                SetProperty(ref _dirty, value);
                if (value)
                {
                    LatestPresentationVersion = DateTime.Now.ToString("dd-MM-yyyy--HH-mm-ss");
                    //Logger.Debug("Aktuelle Version der Präsentationselemente ist jetzt " + LatestPresentationVersion);
                }
            }
        }

        /// <summary>
        /// Alle Videosnippets und Snapshots.
        /// </summary>
        public MvxObservableCollection<IPresentable> PresentationElements
        {
            get { return _presentationElements; }
            set { SetProperty(ref _presentationElements, value); }
        }

        /// <summary>
        /// Die Anzahl an Kalibrierungspunkte, welche vom Nutzer gewählt wurden.
        /// </summary>
        public int SelectedCalibrationPoints
        {
            get { return _selectedCalibrationPoints; }
            set
            {
                if (CalibrationPoints.Contains(value))
                {
                    if (value != _selectedCalibrationPoints)
                    {
                        _selectedCalibrationPoints = value;
                        //MainViewModel.Instance.Calibrated = false;
                        //MainViewModel.Instance.RaisePropertyChanged("Calibrated");
                    }
                }
                else
                    Logger.Message("Illegaler Übergabewert für Calibration Points in Experiment.");
            }
        }

        /// <summary>
        /// Die Hintergrundfarbe der generierten Snapshots.
        /// </summary>
        [JsonIgnore]
        public Color Background
        {
            get => _background;
            set
            {
                SetProperty(ref _background, value);
                Dirty = true;
                if (MainViewModel.Instance.LastSelectedPresentable is not null)
                    MainViewModel.Instance.RefreshPreview();
                else
                    MainViewModel.Instance.RaisePropertyChanged("PreviewImage");
            }
        }

        /// <summary>
        /// Der Dateipfad des Experiments.
        /// </summary>
        public string ExperimentPath
        {
            get => Path.Combine(ExperimentFileManagerModel.BasePath, Name);
        }



        /// <summary>
        /// Der Konstruktur.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="resolution"></param>
        /// <param name="calibrationPoints"></param>
        public ExperimentModel(string name, (int x, int y) resolution, int calibrationPoints)
        {
            _name = name;
            _resX = resolution.x;
            _resY = resolution.y;
            Directory.CreateDirectory(ExperimentPath);

            PresentationElements = new();
            Components = new();

            HandleFileWatcherLoadUp();
        }

        /// <summary>
        /// Resettet Eventhandler.
        /// </summary>
        public void HandleLoadUp()
        {
            //Logger.Debug($"Are events suppressed?: {Components.EventsAreSuppressed}\n" +
            //    $"Is Components null?: {Components == null}");

            Components.CollectionChanged += OnComponentsChanged;

            foreach (IExperimentComponent comp in Components)
            {
                comp.PropertyChanged += OnComponentChanged;
            }
        }

        /// <summary>
        /// Wird bei Deserialisierung ausgeführt. Wird gebraucht, damit Experiment während des Ladens nicht dirty wird.
        /// </summary>
        /// <param name="context"></param>
        [OnDeserializing]
        public void OnLoad(StreamingContext context)
        {
            _loading = true;
        }

        /// <summary>
        /// Wird nach der Deserialisierung ausgeführt. Wird gebraucht, damit Experiment während des Ladens nicht dirty wird.
        /// </summary>
        /// <param name="context"></param>
        [OnDeserialized]
        public void OnLoaded(StreamingContext context)
        {
            _loading = false;
            Logger.Message($"Experiment geladen: '{Name}'");
        }

        /// <summary>
        /// Setzt Dateipfade der StimulusModels neu.
        /// </summary>
        /// <param name="newExpPath"></param>
        public void UpdateStimulusPaths(string newExpPath)
        {
            string newStimDir = Path.Combine(newExpPath, "Stimuli");

            foreach (StimulusModel sm in ImageStimuli.Union(VideoStimuli))
            {
                //Logger.Debug(sm.FilePath);
                sm.UpdateFilePath(newStimDir);
            }
        }

        /// <summary>
        /// Updated alle Snapshots, damit diese mit aktuellen Parametern neu erstellt werden.
        /// </summary>
        public void UpdateAllSnapshots()
        {
            if (Components is null) return;
            foreach (SequenceModel seq in Components.Where(comp => comp is SequenceModel).ToList())
            {
                //Logger.Debug("Sequenzname: " + seq.Name);
                seq.UpdateSnapshots();
            }
        }

        /// <summary>
        /// Gibt alle Referenzen an Slots und Komponenten im Experiment frei.
        /// 
        /// Kann als Vorsichtsmaßnahme verwendet werden, falls man zu oft vom GC betrogen wurde. :)
        /// </summary>
        public void ClearExperimentComponentsList()
        {
            for (int i = Components.Count - 1; i >= 0; i--)
            {
                if (Components[i] is SequenceModel seq)
                {
                    for (int j = seq.Slots.Count - 1; j >= 0; j--)
                    {
                        seq.Slots.RemoveAt(j);
                    }
                }
                if (Components[i] is VideoModel vid)
                {
                    vid.Stimulus = null;
                }

                Components.RemoveAt(i);
            }
        }

        /// <summary>
        /// Gibt alle Referenzen an Stimuli im Experiment frei.
        /// 
        /// Kann als Vorsichtsmaßnahme verwendet werden, falls man zu oft vom GC betrogen wurde. :)
        /// </summary>
        public void ClearStimulusList()
        {
            for (int i = ImageStimuli.Count - 1; i >= 0; i--)
            {
                ImageStimuli.RemoveAt(i);
            }
            for (int i = VideoStimuli.Count - 1; i >= 0; i--)
            {
                VideoStimuli.RemoveAt(i);
            }
        }

        /// <summary>
        /// Hält den StimulusListUpdater an und gibt ihn zur GC frei.
        /// </summary>
        public void HandleFileWatcherShutDown()
        {
            _stimListUpd?.Stop();
            _stimListUpd = null;
        }

        /// <summary>
        /// Startet dem StimulusListUpdater und übergibt ihm Delegates zur direkten Manipulation der
        /// im Experiment vorhandenen Reizlisten. Falls der StimulusListUpdater dann nicht korrekt beendet
        /// wird und im Hintergrund weiter läuft, wird er einem neu geladenen Experiment keine Stimuli 
        /// hinzufügen, da er Delegates des alten Experiments hält. Reine Vorsichtsmaßnahme und sollte eigentlich
        /// nicht notwendig sein.
        /// 
        /// Außerdem wird das Hinzufügen zu den respektiven Collections über den Main-Thread geregelt. Dafür wird
        /// hier der Main-Thread-Dispatcher des MvvmCross-Frameworks verwendet. Das muss gemacht werden, weil sonst
        /// manchmal die Collection Out-Of-Sync ist. Ich bin mir SEHR sicher, dass es sich hierbei um einen Bug des
        /// Frameworks handelt, da MvxObservableCollections eigentlich threadsicher sein sollten. Das sind sie in
        /// allen anderen Fällen auch, nur hier ist es wohl ein Problem.
        /// </summary>
        public void HandleFileWatcherLoadUp()
        {
            _stimListUpd = new((StimulusModel sm, ExtensionType extt) =>
            {
                var dispatcher = Mvx.IoCProvider.Resolve<IMvxMainThreadAsyncDispatcher>();
                dispatcher.ExecuteOnMainThreadAsync(() =>
                {
                    if (extt == ExtensionType.Image)
                    {
                        ImageStimuli.Add(sm);
                    }

                    if (extt == ExtensionType.Video)
                    {
                        VideoStimuli.Add(sm);
                    }
                });

            }, ExperimentPath);
        }

        /// <summary>
        /// Wird aufgerufen, wenn sich etwas an der Liste der Experimentkomponenten ändert.
        /// 
        /// Zunächst wird allen neuen Komponenten der Liste ein Callback im Falle dessen, dass sie
        /// sich verändern (Anknüpfung an INotifyPropertyChanged), hinzugefügt. Dieser Callback is OnComponentChanged (Singular)
        /// und wird von alten Elementen der Liste, welche dem Callback übergeben werden, entfernt. Danach werden die Presentables
        /// der verschiedenen Sequenzen und Videos neu in einer Liste zusammengeführt.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnComponentsChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null)
            {
                foreach (IExperimentComponent comp in e.NewItems)
                {
                    comp.PropertyChanged += OnComponentChanged;
                }
            }

            if (e.OldItems != null)
            {
                foreach (IExperimentComponent comp in e.OldItems)
                {
                    comp.PropertyChanged -= OnComponentChanged;
                }
            }

            ConsolidatePresentables();
        }

        /// <summary>
        /// Jedes Mal wenn sich eine Komponente ändert, wird die Liste der Presentables neu zusammengeführt.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnComponentChanged(object sender, PropertyChangedEventArgs e)
        {
            ConsolidatePresentables();
        }

        /// <summary>
        /// Führt die Listen den Presentables der einzelnen Sequenzen und Videos zusammen in PresentationElements.
        /// </summary>
        private void ConsolidatePresentables()
        {
            PresentationElements.Clear();

            List<IExperimentComponent> activeComps = Components.Where(comp => comp.Active).ToList();
            foreach (var comp in activeComps)
            {
                if (comp is VideoModel vid)
                {
                    PresentationElements.Add(vid);
                }
                if (comp is SequenceModel seq)
                {
                    //AddRange ist nicht supported für (Mvx)ObservableCollection
                    foreach (var ss in seq.Snapshots)
                    {
                        PresentationElements.Add(ss);
                    }
                }
            }
            Dirty = true;
        }


        /// <summary>
        /// Löscht den ausgewählten Slot aus der Liste der Experiment-Komponenten.
        /// </summary>
        /// <param name="slot"></param>
        public void DeleteSlot(SlotModel slot)
        {
            // Alle Komponenten werden nach dem Slot durchsucht
            foreach (IExperimentComponent comp in Components)
                if (comp is SequenceModel seq)
                    for (int i = 0; i < seq.Slots.Count; i++)
                        if (seq.Slots.Contains(slot))
                            // Wenn der Slot gefunden wurde, wird er gelöscht und die Suche beendet
                            if (seq.Slots[i] == slot)
                            {
                                seq.Slots.RemoveAt(i);
                                return;
                            }
        }

        /// <summary>
        /// Dupliziert die ausgewählte Komponente.
        /// </summary>
        /// <param name="comp"></param>
        public void DuplicateComponent(IExperimentComponent comp)
        {
            /* Sequenzen und Videos benötigen unterschiedliche Behandlung,
            /* da sie sonst nicht korrekt updaten.*/

            if (comp is SequenceModel seq)
            {
                SequenceModel seqClone = seq.DeepCopy();
                Components.Add(seqClone);
                seqClone.UpdateSnapshots();
            }

            if (comp is VideoModel vid)
            {
                VideoModel vidClone = vid.DeepCopy();
                Components.Add(vidClone);
                vidClone.RaiseAllPropertiesChanged();
            }
        }



        /// <summary>
        /// Erstellt einen neuen UDP-Client.
        /// </summary>
        /// <returns></returns>
        private UdpClient CreateUdpClient()
        {
            if (IPAddress.TryParse(MainViewModel.Instance.IP, out IPAddress ip))
            {
                UdpClient client = new();

                IPEndPoint endPoint = new(IPAddress.Any, MainViewModel.Instance.Port); //Arbiträrer Port
                try
                {
                    client.Client.Bind(endPoint);
                    client.Connect(ip, MainViewModel.Instance.Port);
                    return client;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Verbindung zum iViewX gescheitert.");
                }
            }
            else Logger.Error(new FormatException(), "Invalides Format der IP-Adresse.");

            return null;
        }

    }
}
