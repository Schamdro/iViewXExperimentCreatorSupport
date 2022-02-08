using iViewXExperimentCreator.Core.Enums;
using iViewXExperimentCreator.Core.Subroutines;
using iViewXExperimentCreator.Core.Util;
using iViewXExperimentCreator.Core.ViewModels;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace iViewXExperimentCreator.Core.Models
{
    /// <summary>
    /// Führt die Logik der Kalibrierung aus.
    /// </summary>
    public class CalibrationModel
    {
        private UdpClient _udpClient;

        private const int CALIBRATION_POINT_SIZE = 50;
        private const int CALIBRATION_POINT_THICKNESS = 10;
        private readonly Color _calibrationPointColor = Color.SkyBlue;
        private readonly Color _calibrationPaneBackground = Color.Black;
        private (int, int)[] _receivedCalPoints = new (int, int)[100];
        private UdpListener _listener;

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="client"></param>
        public CalibrationModel(UdpClient client)
        {
            _udpClient = client;
        }

        /// <summary>
        /// Erstellt ein leeres Bild mit einem Fixiationskreuz an der Stelle, welche 
        /// durch den Parameter übergeben wurde.
        /// </summary>
        /// <param name="coords"></param>
        /// <returns></returns>
        public Image CreateCalibrationImage((int x, int y) coords)
        {
            Bitmap bmp = new(ExperimentFileManagerModel.CurrentExperiment.ResolutionX, 
                ExperimentFileManagerModel.CurrentExperiment.ResolutionY);
            Graphics graphic = Graphics.FromImage(bmp);

            graphic.Clear(_calibrationPaneBackground);

            Image fixationPoint = CreateFixationPoint();

            Point location = new(coords.x - fixationPoint.Width / 2,
                 coords.y - fixationPoint.Height / 2);
            SizeF size = new(fixationPoint.Width, fixationPoint.Height);
            RectangleF slotParams = new(location, size);

            graphic.DrawImage(fixationPoint, slotParams);

            graphic.Flush();
            return bmp;
        }

        /// <summary>
        /// Erstellt eine Fixiationskreuzgrafik.
        /// </summary>
        /// <param name="size"></param>
        /// <param name="thickness"></param>
        /// <param name="color"></param>
        /// <returns></returns>
        public Image CreateFixationPoint(int size, int thickness, Color color)
        {
            Bitmap bmp = new(size, size);
            Graphics graphic = Graphics.FromImage(bmp);
            graphic.Clear(Color.Transparent);
            graphic.FillRectangle(new SolidBrush(color), new(new(size / 2 - thickness / 2, 0), new(thickness, size)));
            graphic.FillRectangle(new SolidBrush(color), new(new(0, size / 2 - thickness / 2), new(size, thickness)));
            graphic.Flush();

            return bmp;
        }

        /// <summary>
        /// Erstellt eine Fixiationskreuzgrafik mit Standardwerten.
        /// </summary>
        /// <returns></returns>
        public Image CreateFixationPoint()
        {
            return CreateFixationPoint(CALIBRATION_POINT_SIZE, CALIBRATION_POINT_THICKNESS, _calibrationPointColor);
        }

        /// <summary>
        /// Startet die Kalibrierung.
        /// </summary>
        public void Start()
        {
            MainViewModel.Instance.PreviewVideo = null;
            MainViewModel.Instance.PreviewImage = null;
            MainViewModel.Instance.RefreshPreview();

            if(_udpClient is null)
            {
                Logger.Error(new SocketException(), "Kalibrierung konnte wegen Verbindungsproblemen nicht gestartet werden.");
                return;
            }
            Logger.Message("Starte Kalibrierung...");

            _udpClient.SendRemoteControlCommand(RemoteCommand.ET_CSZ,
                ExperimentFileManagerModel.CurrentExperiment.ResolutionX.ToString(),
                ExperimentFileManagerModel.CurrentExperiment.ResolutionY.ToString());
            _udpClient.SendRemoteControlCommand(RemoteCommand.ET_DEF);
            _udpClient.SendRemoteControlCommand(RemoteCommand.ET_EST);
            _udpClient.SendRemoteControlCommand(RemoteCommand.ET_CAL,
                ExperimentFileManagerModel.CurrentExperiment.SelectedCalibrationPoints.ToString());

            _listener = new(_udpClient, CalibrationCallback);
            _listener.Start();
        }

        /// <summary>
        /// Callback für die Kalibrierung, welcher vom Listener verwendet wird, um im Falle einer 
        /// Antwort des SMI Exp. Centers eine Reaktion der Anwendung zu ermöglichen.
        /// </summary>
        /// <param name="received"></param>
        private void CalibrationCallback(byte[] received)
        {
            var parameters = Encoding.ASCII.GetString(received).Split();
            if (parameters.Length < 1) return;
            switch (parameters[0])
            {
                case "ET_PNT":
                    if (parameters.Length < 4) break;
                    int i = Int32.Parse(parameters[1]);
                    int x = Int32.Parse(parameters[2]);
                    int y = Int32.Parse(parameters[3]);
                    _receivedCalPoints[i] = (x, y);
                    Logger.Debug($"Kalibrierungspunkt auf ({x},{y}) erhalten.");
                    break;
                case "ET_CHG":
                    if (parameters.Length < 2) break;
                    int calPointIdx = Int32.Parse(parameters[1]);
                    MainViewModel.Instance.PreviewImage = CreateCalibrationImage(_receivedCalPoints[calPointIdx]);
                    MainViewModel.Instance.RaisePropertyChanged("PreviewImage");
                    break;
                //case "ET_CSZ":
                //    //TODO: Denke, dass das hier nicht relevant ist. Ist in Demo auch nicht beachtet.
                //    break;
                case "ET_FIN":
                    EndCalibration();
                    break;
                default:
                    Logger.Debug($"Unbehandelter RCC {parameters[0]}");
                    break;
            }
        }

        /// <summary>
        /// Beendet die Kalibrierung und setzt den Rest der Anwendung darüber in Kenntniss. 
        /// </summary>
        /// <param name="cancel"></param>
        public void EndCalibration(bool cancel = false)
        {
            if (cancel && _udpClient is not null)
            {
                _udpClient.SendRemoteControlCommand(RemoteCommand.ET_BRK);
            }
            else
            {
                //MainViewModel.Instance.Calibrated = true;
                MainViewModel.Instance.RaisePropertyChanged("Calibrated");
            }

            MainViewModel.Instance.PreviewImage = null;
            MainViewModel.Instance.RaisePropertyChanged("PreviewImage");

            _listener.Stop();

            Logger.Message("Kalibrierung beendet.");
        }

        /// <summary>
        /// Unterbricht die Kalibrierung.
        /// </summary>
        public void Interrupt()
        {
            EndCalibration(true);
        }

        /// <summary>
        /// Akzeptiert den Kalibrierungspunkt und schreitet mit der Kalibrierung fort. 
        /// </summary>
        public void Continue()
        {
            if (_udpClient is null)
            {
                Logger.Debug("Continue: UDP client ist null");
                return;
            }
                
            _udpClient.SendRemoteControlCommand(RemoteCommand.ET_ACC);
            //Logger.Debug("Kalibrierung fortgesetzt.");
        }
    }
}
