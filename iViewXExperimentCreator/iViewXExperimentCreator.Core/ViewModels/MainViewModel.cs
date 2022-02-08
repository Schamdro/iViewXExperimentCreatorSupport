using MvvmCross.ViewModels;
using iViewXExperimentCreator.Core.Models;
using System.Collections.Generic;
using MvvmCross.Navigation;
using System.Threading.Tasks;
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using iViewXExperimentCreator.Core.Enums;
using System.Net.Sockets;
using System.Text;
using iViewXExperimentCreator.Core.Util;

namespace iViewXExperimentCreator.Core.ViewModels
{
    /// <summary>
    /// ViewModel für die GUI des Hauptfensters.
    /// </summary>
    public partial class MainViewModel : MvxViewModel
    {
        private readonly IMvxNavigationService _navigationService;

        private bool _experimentRunning = false;
        private bool _calibrating = false;
        //private bool _calibrated = false;
        private bool _saveSnapshotsToDrive = true;
        private bool _runMaximized = false;

        private string _subjectName = "default";
        private string _ip = "192.168.1.1";
        private int _port = 4444;
        private VideoModel _previewVideo;
        private Image _previewImage;

        private Color _previewBorderColor = Color.Black;
        private IPresentable _selectedPresentable;
        private IPresentable _lastSelectedPresentable;
        private AgnosticVisibility _fullscreen = AgnosticVisibility.Collapsed;
        private AgnosticVisibility _windowed = AgnosticVisibility.Visible;
        private AgnosticVisibility _imagePreviewVisibility = AgnosticVisibility.Visible;
        private AgnosticVisibility _videoPreviewVisibility = AgnosticVisibility.Collapsed;
        private IExperimentComponent _selectedExperimentComponent;

        private CalibrationModel _calibration;
        public CalibrationModel Calibration => _calibration;
        private PresentationModel _presentation;
        public PresentationModel Presentation => _presentation;


        /// <summary>
        /// Gibt zurück, ob ein Experiment geladen ist.
        /// </summary>
        public bool ExperimentLoaded => ExperimentFileManagerModel.CurrentExperiment is not null;

        //Asynchron editierbare Observable-Collections (normale ObservableCollection ist nicht ohne Dispatcher in Subroutine editierbar)
        public MvxObservableCollection<StimulusModel> ImageStimuli { get => ExperimentFileManagerModel.CurrentExperiment?.ImageStimuli; }
        public MvxObservableCollection<StimulusModel> VideoStimuli { get => ExperimentFileManagerModel.CurrentExperiment?.VideoStimuli; }
        /// <summary>
        /// Die Liste alle Snapshots und Videosnippets.
        /// </summary>
        public MvxObservableCollection<IPresentable> PresentationElements { get => ExperimentFileManagerModel.CurrentExperiment?.PresentationElements; }


        /// <summary>
        /// Die momentan vom Nutzer in der GUI selektierte Komponente.
        /// </summary>
        public IExperimentComponent SelectedExperimentComponent
        {
            get { return _selectedExperimentComponent; }
            set { SetProperty(ref _selectedExperimentComponent, value); }
        }


        /// <summary>
        /// Das derzeit ausgewählte Presentable. Muss über ein Binding über den View gesetzt werden.
        /// Kann bei Abwählen null werden (je nach Plattform) -> LastSelectedPresentable hält nach der
        /// ersten Zuweisung immer den letzten hier gehaltenen Wert.
        /// </summary>
        public IPresentable SelectedPresentable
        {
            get => _selectedPresentable;
            set
            {
                SetProperty(ref _selectedPresentable, value);

                LastSelectedPresentable = value ?? LastSelectedPresentable;
            }
        }

        /// <summary>
        /// Hält das zuletzt gewählte Presentable. Falls SelectedPresentable nicht null ist, ist dies
        /// immer gleich wie SelectedPresentable.
        /// </summary>
        public IPresentable LastSelectedPresentable
        {
            get => _lastSelectedPresentable;
            set
            {
                SetProperty(ref _lastSelectedPresentable, value);

                //PreviewImage = (value is SnapshotModel ss ? ss.SnapshotImage : null);
                PreviewVideo = (value is VideoModel vid ? vid : null);

                if (!ExperimentRunning && value is VideoModel vid2)
                {
                    LoopVideo(vid2);
                }

                SetVideoVisible(value is VideoModel);
                RefreshPreview();
            }
        }

        /// <summary>
        /// Gibt das momentane Vorschaubild zurück. Im Falle eines nicht gesetzten Vorschaubildes wird ein leeres Bild mit
        /// Hintergrundfarbe und Auflösung des aktuellen Experiments erstellt. Falls kein Experiment geladen ist, wird null
        /// zurückgegeben.
        /// </summary>
        public Image PreviewImage
        {
            get
            {
                if (!ExperimentRunning && (_lastSelectedPresentable is null || _previewImage is null || ExperimentFileManagerModel.CurrentExperiment is null))
                {
                    return CreateEmptyPreviewImage();
                }
                return _previewImage;
            }
            set
            {
                //SetProperty(ref _previewImage, value);
                _previewImage = value;
            }
        }
        /// <summary>
        /// Das anzuzeigende Video.
        /// </summary>
        public VideoModel PreviewVideo
        {
            get => _previewVideo;
            set => SetProperty(ref _previewVideo, value);
        }

        /// <summary>
        /// Die Sichtbarkeit der Video-Preview-Komponente der GUI.
        /// </summary>
        public AgnosticVisibility VideoPreviewVisibility
        {
            get => _videoPreviewVisibility;
            set => SetProperty(ref _videoPreviewVisibility, value);
        }
        /// <summary>
        /// Die Sichtbarkeit der Image-Preview-Komponente der GUI.
        /// </summary>
        public AgnosticVisibility ImagePreviewVisibility
        {
            get => _imagePreviewVisibility;
            set => SetProperty(ref _imagePreviewVisibility, value);
        }

        /// <summary>
        /// Der Logtext, welcher von der GUI aus ausgelesen werden kann. Gibt einen String mit dem kompletten aktuellen Inhalt
        /// der Log-Datei zurück. Verändert sich in Echtzeit.
        /// </summary>
        public string LogText
        {
            get
            {
                string logText = "NO_TEXT";
                try
                {
                    using (var stream = File.Open(Logger.LogPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    {
                        var streamData = new byte[stream.Length];
                        stream.Read(streamData);
                        logText = Encoding.UTF8.GetString(streamData);
                    }
                }
                catch(Exception e)
                {
                    logText = "Konnte Log nicht öffnen.\n" + e.ToString();
                }
                return logText;     
            }
        }
        /// <summary>
        /// Wahr, wenn eine Kalibrierung läuft.
        /// </summary>
        public bool Calibrating
        {
            get => _calibrating; set
            {
                SetProperty(ref _calibrating, value);
                RaisePropertyChanged("CanStartExperiment");
                RaisePropertyChanged("CanStartCalibration");
            }
        }
        ///// <summary>
        ///// Wahr, wenn die Versuchsperson kalibriert wurde.
        ///// </summary>
        //public bool Calibrated
        //{
        //    get => _calibrated;
        //    set
        //    {
        //        SetProperty(ref _calibrated, value);
        //        RaisePropertyChanged("CanStartExperiment");
        //    }
        //}
        /// <summary>
        /// Fullscreen ist dann visible, wenn eine Kalibrierung oder Präsentation gestartet wurde und
        /// RunMaximized wahr ist. Ansonsten collapsed.
        /// </summary>
        public AgnosticVisibility Fullscreen
        {
            get => _fullscreen;
            set => SetProperty(ref _fullscreen, value);
        }
        /// <summary>
        /// Windowed ist immer visible, wenn RunMaximized falsch ist. Ansonsten collapsed.
        /// </summary>
        public AgnosticVisibility Windowed
        {
            get => _windowed;
            set => SetProperty(ref _windowed, value);
        }
        /// <summary>
        /// Wird über die GUI angesprochen und beeinflusst, ob Fullscreen/Window collapsed/visible (wahr) oder visible/collapsed (falsch) sind.
        /// </summary>
        public bool RunMaximized
        {
            get => _runMaximized;
            set => SetProperty(ref _runMaximized, value);
        }
        /// <summary>
        /// Ist wahr, wenn die Applikation bereit für den Start eine Kalibrierung ist.
        /// 
        /// Dies ist nicht der Fall, wenn IP oder CurrentExperiment null sind oder eine Präsentation läuft oder kalibriert wird.
        /// </summary>
        public bool CanStartCalibration
        {
            get
            {
                if (IP is not null && IP != string.Empty && ExperimentFileManagerModel.CurrentExperiment is not null && 
                    ExperimentRunning == false && Calibrating == false)
                {
                    return true;
                }
                return false;
            }
        }
        /// <summary>
        /// Ist wahr, wenn die Applikation bereit für den Start einer Präsentation ist.
        /// 
        /// Dies ist nicht der Fall, wenn SubjectName oder IP null oder leer sind, CurrentExperiment null ist oder kalibriert/präsentiert wird.
        /// Außerdem muss bereits eine Kalibrierung abgeschlossen worden sein.
        /// </summary>
        public bool CanStartExperiment
        {
            get
            {
                if (SubjectName is not null && SubjectName != string.Empty && 
                    IP is not null && IP != string.Empty &&
                    ExperimentFileManagerModel.CurrentExperiment is not null && ExperimentRunning == false &&
                    Calibrating == false)
                {
                    return true;
                }
                return false; // AUF true ÄNDERN FÜR DEBUG
            }
        }
        /// <summary>
        /// Der Name der Versuchsperson. Wird in den erstellten Versuchslogs verwendet und an das SMI Exp. Center gegeben.
        /// </summary>
        public string SubjectName
        {
            get => _subjectName;
            set
            {
                SetProperty(ref _subjectName, value);
                RaisePropertyChanged("CanStartExperiment");
            }
        }
        /// <summary>
        /// Die IP-Addresse, welche zur Verbindung mit dem SMI Exp. Center verwendet wird.
        /// </summary>
        public string IP
        {
            get => _ip;
            set
            {
                SetProperty(ref _ip, value);
                RaisePropertyChanged("CanStartCalibration");
                RaisePropertyChanged("CanStartExperiment");
            }
        }

        /// <summary>
        /// Der Port, welcher zur Verbindung mit dem SMI Exp. Center verwendet wird.
        /// </summary>
        public int Port
        {
            get => _port;
            set
            {
                SetProperty(ref _port, value);
                RaisePropertyChanged("CanStartCalibration");
                RaisePropertyChanged("CanStartExperiment");
            }
        }
        /// <summary>
        /// Die Farbe des Rahmenelements der Preview-Elemente, welches im Fenstermodus existiert.
        /// 
        /// Rot -> Präsentation/Kalibrierung läuft
        /// Grün -> Warte auf Eingabe
        /// Schwarz -> sonst
        /// </summary>
        public Color PreviewBorderColor
        {
            get => _previewBorderColor;
            set => SetProperty(ref _previewBorderColor, value);
        }
        /// <summary>
        /// Wahr, wenn eine Präsentation im Gange ist.
        /// </summary>
        public bool ExperimentRunning
        {
            get
            {
                return _experimentRunning;
            }
            set
            {
                SetProperty(ref _experimentRunning, value);
                RaisePropertyChanged("CanStartExperiment");
                RaisePropertyChanged("StartedAndMaximized");
            }
        }
        /// <summary>
        /// Von der GUI aus ansprechbar. Beeinflusst, ob Snapshots und Videosnippets gespeichert werden sollen.
        /// </summary>
        public bool SaveSnapshotsToDrive
        {
            get => _saveSnapshotsToDrive;
            set => SetProperty(ref _saveSnapshotsToDrive, value);
        }

        /// <summary>
        /// Instanz dieser Klasse. Da im MvvmCross kein statisches ViewModel möglich ist, ist
        /// eine statisch zugängliche Instanz für einfachen Zugriff notwendig. 
        /// </summary>
        public static MainViewModel Instance { get; private set; }

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="navigationService"></param>
        public MainViewModel(IMvxNavigationService navigationService)
        {
            _navigationService = navigationService;
            Instance = this; 
        }

        /// <summary>
        /// Fordert den Snapshot des zuletzt gewählten Presentables neu an.
        /// </summary>
        public void RefreshPreview()
        {
            Logger.Debug("Refreshing Preview");
            if (ExperimentRunning == false && Calibrating == false)
            {
                if (_lastSelectedPresentable is SnapshotModel ss)
                {
                    Logger.Debug("ss.SnapshotImage in RefreshPreview");
                    PreviewImage = ss.SnapshotImage;
                }
                else if(_lastSelectedPresentable is VideoModel vid)
                {
                    PreviewVideo = vid;
                }
            }
            RaisePropertyChanged("PreviewImage");
            RaisePropertyChanged("PreviewVideo");
        }

        /// <summary>
        /// Initialisiert das ViewModel und alle Commands, die hiermit verbunden sind.
        /// </summary>
        /// <returns></returns>
        public override async Task Initialize()
        {
            await base.Initialize();

            //Initialisiere Commands
            InitCommands();

            //Event, sobald App schließt
            AppDomain.CurrentDomain.ProcessExit += new EventHandler(OnProcessExit);
        }

        /// <summary>
        /// Wird ausgeführt, wenn das Programm regulär beendet wird. 
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void OnProcessExit(object sender, EventArgs e)
        {
            ExperimentFileManagerModel.SaveExperiment(); // Bei Beendigung des Programmes wird das derzeitige Experiment gespeichert
            Logger.Debug("Programm wurde regulär beendet."); // Loggen, dass das Programm regulärt beendet wird
        }
        /// <summary>
        /// Erstellt ein leeres Preview-Image mit korrekten Dimensionen. Wird als Platzhalter für
        /// Snapshots verwendet, wenn noch keine Sequenz hinzugefügt wurde.
        /// </summary>
        /// <returns>Image</returns>
        public static Image CreateEmptyPreviewImage()
        {
            if (ExperimentFileManagerModel.CurrentExperiment is null) return null;
            Bitmap bmp = new(ExperimentFileManagerModel.CurrentExperiment.ResolutionX, ExperimentFileManagerModel.CurrentExperiment.ResolutionY);
            Graphics graphic = Graphics.FromImage(bmp); //Zur Bearbeitung als Graphics-Objekt parsen
            Color color = Color.White;
            if (ExperimentFileManagerModel.CurrentExperiment is not null)
            {
                color = ExperimentFileManagerModel.CurrentExperiment.Background;
            }
            graphic.Clear(color); 
            graphic.Flush();
            return bmp;
        }

        /// <summary>
        /// Setzt das Vorschaubild auf den ersten Snapshot, in welchem der übergebene Slot vorhanden ist.
        /// </summary>
        /// <param name="slot"></param>
        public void SetPreviewToFirstSlotOccurence(SlotModel slot)
        {
            for (int i = 0; i < PresentationElements.Count; i++)
            {
                if (PresentationElements[i] is SnapshotModel ss)
                {
                    if (ss.SlotModels.Contains(slot))
                    {
                        Logger.Debug("ss.SnapshotImage in SetPreviewToFirstSlotOccurence");
                        //PreviewImage = ss.SnapshotImage;
                        SelectedPresentable = ss;
                        break;
                    }
                }
            }
        }


        public MvxObservableCollection<IExperimentComponent> ExperimentComponents { get => ExperimentFileManagerModel.CurrentExperiment?.Components; }

        /// <summary>
        /// Beginnt die Kalibrierung für das Experiment.
        /// </summary>
        private void Calibrate()
        {
            if (ExperimentFileManagerModel.CurrentExperiment is null || Calibrating || ExperimentRunning) return;
            using(UdpClient client = new())
            {
                if (client.TryInitializeUdpClient())
                {
                    Calibrating = true;
                    _calibration = new(client);
                    SetFullscreen(RunMaximized);
                    PreviewBorderColor = Color.Red;
                    CloseAllOtherViewModels();
                    _calibration.Start();
                    PreviewBorderColor = Color.Black;
                    SetFullscreen(false);
                    _calibration = null;
                    Calibrating = false;
                }
            }
        }

        /// <summary>
        /// Führt das Experiment durch. 
        /// </summary>
        private void ConductExperiment()
        {
            if (ExperimentFileManagerModel.CurrentExperiment is null || Calibrating || ExperimentRunning) return;
            using (UdpClient client = new()) //Nutze IDisposable
            {
                if (client.TryInitializeUdpClient())
                {
                    //Bereite die GUI auf Durchführung vor
                    ExperimentRunning = true;
                    Logger.Message($"Präsentation des Experiments '{ExperimentFileManagerModel.CurrentExperiment.Name}' wurde mit Subjekt '{SubjectName}' gestartet.");
                    PreviewBorderColor = Color.Red;
                    SetFullscreen(RunMaximized);
                   
                    //Führe Präsentation durch
                    _presentation = new(client);
                    CloseAllOtherViewModels();
                    _presentation.Start();
                    _presentation = null;
                    
                    //Stelle Ursprungszustand der GUI wieder her
                    ExperimentRunning = false;
                    SetFullscreen(false);
                    Logger.Message($"Präsentation des Experiments '{ExperimentFileManagerModel.CurrentExperiment.Name}' mit Subjekt '{SubjectName}' wurde abgeschlossen.");
                    PreviewBorderColor = Color.Black;
                }
            }
        }

        /// <summary>
        /// Wenn ein Video gezeigt werden soll, wird das Anzeigeelement für Snapshots im View ausgeblendet und
        /// das Anzeigeelement für Videos eingeblendet. Vice versa. Dies ist eventuell nicht für alle Plattformen notwendig.
        /// </summary>
        /// <param name="isVisible"></param>
        public void SetVideoVisible(bool isVisible)
        {
            if (isVisible)
            {
                ImagePreviewVisibility = AgnosticVisibility.Collapsed;
                VideoPreviewVisibility = AgnosticVisibility.Visible;
            }
            else
            {
                ImagePreviewVisibility = AgnosticVisibility.Visible;
                VideoPreviewVisibility = AgnosticVisibility.Collapsed;
            }
        }

        private bool _looping = false;
        /// <summary>
        /// Loopt das Video für den Preview-Modus.
        /// </summary>
        /// <param name="vid"></param>
        public void LoopVideo(VideoModel vid)
        {
            Task loopTask = new(() =>
            {
                if (!_looping)
                {
                    _looping = true;
                    while (LastSelectedPresentable == vid && vid.Duration > 0.001f && !ExperimentRunning && !Calibrating)
                    {
                        PreviewVideo = null;
                        PreviewVideo = vid;
                        RefreshPreview();
                        Thread.Sleep((int)vid.Duration * 1000);
                    }
                    _looping = false;
                }
            });
            loopTask.Start();
        }

        /// <summary>
        /// Blendet die Vollbildeinstellungen des Experimentes ein oder aus.
        /// </summary>
        /// <param name="fullscreen"></param>
        public void SetFullscreen(bool fullscreen)
        {
            if (fullscreen)
            {
                Fullscreen = AgnosticVisibility.Visible;
                Windowed = AgnosticVisibility.Collapsed;
            }
            else
            {
                Windowed = AgnosticVisibility.Visible;
                Fullscreen = AgnosticVisibility.Collapsed;
            }
        }

        /// <summary>
        /// Schließt alle ViewModels außer des MainViewModels.
        /// </summary>
        public void CloseAllOtherViewModels()
        {
            ExperimentSettingsViewModel.Instance?.Close();
            foreach (LoadExperimentViewModel vm in new List<LoadExperimentViewModel>(LoadExperimentViewModel.Instances)) vm.Close();
            foreach (NewExperimentViewModel vm in new List<NewExperimentViewModel>(NewExperimentViewModel.Instances)) vm.Close();
            foreach (SequenceEditorViewModel vm in new List<SequenceEditorViewModel>(SequenceEditorViewModel.Instances)) vm.Close();
            foreach (VideoEditorViewModel vm in new List<VideoEditorViewModel>(VideoEditorViewModel.Instances)) vm.Close();
        }

    }
}
