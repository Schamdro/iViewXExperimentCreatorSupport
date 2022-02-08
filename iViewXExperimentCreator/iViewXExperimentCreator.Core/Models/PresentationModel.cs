using iViewXExperimentCreator.Core.Enums;
using iViewXExperimentCreator.Core.Util;
using iViewXExperimentCreator.Core.ViewModels;
using System;
using System.Drawing;
using System.IO;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace iViewXExperimentCreator.Core.Models
{
    /// <summary>
    /// Führt die Logik der Präsentation aus.
    /// </summary>
    public class PresentationModel
    {
        public const string IVIEWX_SAVE_PATH = "D:\\data\\openDoor\\";
        private bool _interrupted = false;
        private UdpClient _udpClient;
        private EventWaitHandle _interruptPresentationHandle;
        private readonly EventWaitHandle _eventWaitHandle;
        private PressedKey _pressedKey;
        private static bool _instanceExists = false;
        private bool _canStart = true;

        public delegate void UnpauseEvent();
        public event UnpauseEvent Unpaused;

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="client"></param>
        public PresentationModel(UdpClient client)
        {
            _udpClient = client;
            _interruptPresentationHandle = new(false, EventResetMode.AutoReset);
            _eventWaitHandle = new(false, EventResetMode.AutoReset);
            _pressedKey = PressedKey.None;
            if (_instanceExists)
            {
                Logger.Error(new InvalidOperationException(), "Es wird bereits eine Präsentation durchgeführt.");
                _canStart = false;
            }
            _instanceExists = true;
        }

        /// <summary>
        /// Startet die Präsentation der Snapshots und Videosnippets. Während dieser erfolgt eine einseitige Kommunikation
        /// mit dem SMI Exp. Center, in welcher dieses über die Änderung der Stimuli informiert wird. Hier wird die Versuchslog
        /// erstellt, es werden Versuchspersoneneingaben entgegenengenommen und in diese geschrieben und es wird die Logik der
        /// Präsentation ausgeführt.
        /// </summary>
        public void Start()
        {
            if (!_canStart) return;
            if(ExperimentFileManagerModel.CurrentExperiment.LatestPresentationVersion is null)
            {
                Logger.Message("Es konnte keine aktuelle Version der Präsentationselemente gefunden werden. Präsentation wird nicht gestartet.");
                return;
            }

            _udpClient.SendRemoteControlCommand(RemoteCommand.ET_FRM, "\"%PX %PY\"");
            _udpClient.SendRemoteControlCommand(RemoteCommand.ET_STR, "2");

            _interruptPresentationHandle.Reset();

            string presentableVersionsDirectory = Path.Combine(ExperimentFileManagerModel.CurrentExperiment.ExperimentPath, "Presentable Versions");
            string presentableSaveDirectory = Path.Combine(presentableVersionsDirectory, ExperimentFileManagerModel.CurrentExperiment.LatestPresentationVersion);
            string inputLogsDirectory = Path.Combine(presentableVersionsDirectory, "Input Logs");
            string inputLogPath = Path.Combine(inputLogsDirectory, $"{MainViewModel.Instance.SubjectName}.log").GetFreeFilePath();

            Directory.CreateDirectory(inputLogsDirectory);

            Logger.StartInputLogger(inputLogPath, ExperimentFileManagerModel.CurrentExperiment.LatestPresentationVersion);

            // Experiment dirty? Dann Ordner für neues Presentable-Set erstellen
            if (ExperimentFileManagerModel.CurrentExperiment.Dirty && MainViewModel.Instance.SaveSnapshotsToDrive)
            {
                Directory.CreateDirectory(presentableSaveDirectory);
            }

            foreach (IPresentable pres in ExperimentFileManagerModel.CurrentExperiment.PresentationElements)
            {
                Logger.LogPresentableChange(pres);

                if (pres is SnapshotModel ss)
                {
                    PresentSnapshot(ss, presentableSaveDirectory);
                }

                if (pres is VideoModel vid)
                {
                    if (vid.Stimulus is not null)
                    {
                        PresentVideo(vid, presentableSaveDirectory);
                    }
                    else // kein Stimulus? => keine Anzeige!
                    {
                        continue;
                    }
                }
                MainViewModel.Instance.SetVideoVisible(pres is VideoModel);

                _udpClient.SendRemoteControlCommand(RemoteCommand.ET_REC);

                _eventWaitHandle.Reset();

                if (pres.Pauses) WaitForInput(pres);
                Unpaused?.Invoke();

                WaitForPresentableDuration(pres);
                StopRecording(pres.Name);

                MainViewModel.Instance.PreviewVideo = null;
                MainViewModel.Instance.SetVideoVisible(false);

                if (_interrupted)
                {
                    Dispose();
                    return;
                }
            }

            if (MainViewModel.Instance.SaveSnapshotsToDrive)
            {
                ExperimentFileManagerModel.CurrentExperiment.Dirty = false;
                Logger.Message($"Die verwendeten Präsentationselemente sind unter '{presentableSaveDirectory}' zu finden.");
            }
            Logger.Message($"Die erstellte Versuchslog wurde in '{inputLogPath}' gespeichert.");
            Dispose();
        }

        /// <summary>
        /// Handhabung der Präsentation und des möglichen Speicherns eines Snapshots.
        /// </summary>
        /// <param name="ss"></param>
        /// <param name="presentableSaveDirectory"></param>
        private void PresentSnapshot(SnapshotModel ss, string presentableSaveDirectory)
        {
            MainViewModel.Instance.PreviewImage = ss.SnapshotImage;
            MainViewModel.Instance.RefreshPreview();

            if (ExperimentFileManagerModel.CurrentExperiment.Dirty && MainViewModel.Instance.SaveSnapshotsToDrive)
            {
                string savePath = Path.Combine(presentableSaveDirectory, $"{ss.Name}.png").GetFreeFilePath();
                ss.SnapshotImage.Save(savePath);
                Logger.Debug("Speichere Snapshot " + ss.Name);
            }
        }

        /// <summary>
        /// Handhabung der Präsentation und des möglichen Speicherns eines Videos.
        /// </summary>
        /// <param name="vid"></param>
        /// <param name="presentableSaveDirectory"></param>
        private void PresentVideo(VideoModel vid, string presentableSaveDirectory)
        {
            MainViewModel.Instance.PreviewVideo = null;
            MainViewModel.Instance.PreviewVideo = vid;

            if (ExperimentFileManagerModel.CurrentExperiment.Dirty && MainViewModel.Instance.SaveSnapshotsToDrive)
            {
                string savePath = Path.Combine(presentableSaveDirectory, $"{vid.Name}.mp4").GetFreeFilePath();
                // Es soll nicht darauf gewartet werden, dass das Video fertig geschnitten ist
                // Für die Präsentation wird die Hauptdatei in \Stimuli verwendet
                Task saveVid = new(() => vid.SaveVideoSnippet(savePath));
                saveVid.Start();
                Logger.Debug("Speichere Videosnippet " + vid.Name);
            }
        }

        /// <summary>
        /// Wartet die im IPresentable hinterlegte Dauer oder bricht bei Abbruch ab.
        /// 
        /// Abbruch erfolgt durch _interruptPresentationHandle.
        /// </summary>
        /// <param name="pres"></param>
        private void WaitForPresentableDuration(IPresentable pres)
        {
            CancellationTokenSource tokenSource = new();
            CancellationToken cancelToken = tokenSource.Token;

            Task waiter = new(() =>
            {
                Thread.Sleep((int)(pres.Duration * 1000));
                if (!cancelToken.IsCancellationRequested)
                {
                    _interruptPresentationHandle.Set();
                }
            });

            waiter.Start();
            _interruptPresentationHandle.WaitOne();
            tokenSource.Cancel();
            _interruptPresentationHandle.Reset();
        }

        /// <summary>
        /// Wartet auf Tasteninput der Versuchsperson (synchron).
        /// </summary>
        /// <param name="pres"></param>
        private void WaitForInput(IPresentable pres)
        {
            MainViewModel.Instance.PreviewBorderColor = Color.Green;
            if (_eventWaitHandle.WaitOne())
            {
                Logger.Message($"Registriere Eingabe '{_pressedKey}'.");
                switch (_pressedKey)
                {
                    case PressedKey.Hoch:
                        Logger.LogInput(PressedKey.Hoch, ExperimentFileManagerModel.CurrentExperiment.UpKeyMeaning, pres);
                        break;
                    case PressedKey.Runter:
                        Logger.LogInput(PressedKey.Runter, ExperimentFileManagerModel.CurrentExperiment.DownKeyMeaning, pres);
                        break;
                    case PressedKey.Links:
                        Logger.LogInput(PressedKey.Links, ExperimentFileManagerModel.CurrentExperiment.LeftKeyMeaning, pres);
                        break;
                    case PressedKey.Rechts:
                        Logger.LogInput(PressedKey.Rechts, ExperimentFileManagerModel.CurrentExperiment.RightKeyMeaning, pres);
                        break;
                    case PressedKey.Esc:
                        Logger.LogInput(PressedKey.Esc, "Abbruch der Präsentation.", pres);
                        break;
                    default: break;
                }
                _pressedKey = PressedKey.None;
                MainViewModel.Instance.PreviewBorderColor = Color.Red;
            }
        }

        /// <summary>
        /// Unterbricht die Präsentation.
        /// </summary>
        public void Interrupt()
        {
            _interrupted = true;
            _pressedKey = PressedKey.Esc;
            _interruptPresentationHandle.Set();
            _eventWaitHandle.Set();
        }

        /// <summary>
        /// Informiert die Präsentation über eine neue Eingabe.
        /// </summary>
        /// <param name="pressedKey"></param>
        public void EnterInput(PressedKey pressedKey)
        {
            _pressedKey = pressedKey;
            _eventWaitHandle.Set();
        }

        /// <summary>
        /// Schickt die notwendigen RCCs, um das SMI Exp. Center darüber zu informieren, dass die Aufnahme beendet wurde.
        /// </summary>
        /// <param name="presentableName"></param>
        private void StopRecording(string presentableName)
        {
            _udpClient.SendRemoteControlCommand(RemoteCommand.ET_STP);
            _udpClient.SendRemoteControlCommand(RemoteCommand.ET_SAV, IVIEWX_SAVE_PATH, MainViewModel.Instance.SubjectName, presentableName);
            _udpClient.SendRemoteControlCommand(RemoteCommand.ET_CLR);
        }

        /// <summary>
        /// Sollte nach dem Ende der Verwendung aufgerufen werden.
        /// </summary>
        public void Dispose()
        {
            if (!_canStart) return;
            Logger.DisposeInputLogger();
            _instanceExists = false;
        }
    }
}
