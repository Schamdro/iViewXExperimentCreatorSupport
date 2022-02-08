using System;
using System.IO;
using iViewXExperimentCreator.Core.Enums;
using iViewXExperimentCreator.Core.Models;
using iViewXExperimentCreator.Core.Util;
using iViewXExperimentCreator.Core.ViewModels;
using Serilog;

namespace iViewXExperimentCreator.Core
{
    /// <summary>
    /// Dient als Preprocessor, vor Informationen an Serilog weitergegeben werden. So muss Serilog nicht überall referenziert werden und
    /// es erlaubt Reformatierung in einer zentralen Klasse. Damit sind keine Code-Schnipsel, die mit Logging-Reformatierungen zu tun haben, wild im
    /// Code verstreut. Außerdem werden hier das Log-Directory und Log-Files erstellt.
    /// </summary>
    public class Logger
    {
        //Keine Zeichen einfügen, die nicht in Filenames dürfen!
        private const string LOG_NAME_TIME_FORMAT = "dd-MM-yyyy--HH-mm-ss";
        private static Serilog.Core.Logger _inputLogger;
        public static string LogPath { get; private set;}

        /// <summary>
        /// Ein statischer Block, welcher den Logger initialisiert, ohne dass von diesem je eine Instanz erstellt werden muss.
        /// </summary>
        static Logger()
        {
            string logDirectory = System.AppContext.BaseDirectory + @"Logs";

            //TODO: Error handling für Directory-Erstellung
            if (!Directory.Exists(logDirectory))
                Directory.CreateDirectory(logDirectory);

            LogPath = $@"{logDirectory}\log-{DateTime.Now.ToString(LOG_NAME_TIME_FORMAT)}.log";

            Log.Logger = new LoggerConfiguration().
                WriteTo.File(LogPath)
#if DEBUG
                .MinimumLevel
                .Debug()
#endif
                .CreateLogger();
        }

        /// <summary>
        /// Fehlernachricht. Wird mit [ERR] in der Log gekennzeichnet.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="message"></param>
        public static void Error(Exception e, string message)
        {
            Log.Error(e, message);
            MainViewModel.Instance?.RaisePropertyChanged("LogText");
        }

        /// <summary>
        /// Informationsnachricht. Wird mit [INF] in der Log gekennzeichnet.
        /// </summary>
        /// <param name="message"></param>
        public static void Message(string message)
        {
            Log.Information(message);
            MainViewModel.Instance?.RaisePropertyChanged("LogText");
        }

        /// <summary>
        /// Debugnachricht. Wird mit [DBG] in der Log gekennzeichnet.
        /// 
        /// Diese Nachricht wird NICHT in der Log ausgegeben, wenn das Programm in einer Release-Build ist.
        /// </summary>
        /// <param name="message"></param>
        public static void Debug(string message)
        {
            Log.Debug(message);
            MainViewModel.Instance?.RaisePropertyChanged("LogText");
        }

        /// <summary>
        /// Separater Logger, welcher der Erstellung einer Versuchslog dient.
        /// </summary>
        /// <param name="logPath"></param>
        /// <param name="snapshotVersion"></param>
        public static void StartInputLogger(string logPath, string snapshotVersion)
        {
            File.WriteAllText(logPath, $"Experiment: {ExperimentFileManagerModel.CurrentExperiment.Name}\n" +
                $"Subjektname: {MainViewModel.Instance.SubjectName}\n" +
                $"Experimentversion: {snapshotVersion}\n" +
                $"Durchgeführt: { DateTime.Now.ToString("dd-MM-yyyy--HH-mm-ss") }" +
                $"\n\n" +
                $"***** EINGABEN DER VERSUCHSPERSON *****" +
                $"\n\n");

            _inputLogger = new LoggerConfiguration().
                WriteTo.File(logPath, outputTemplate: $"{{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz}} {{Message:lj}}")
                .MinimumLevel
                .Information()
                .CreateLogger();
        }

        /// <summary>
        /// Loggt eine Eingabe und ihre zugewiesene Bedeutung in der momentanen Versuchslog.
        /// </summary>
        /// <param name="input"></param>
        /// <param name="meaning"></param>
        /// <param name="pres"></param>
        public static void LogInput(PressedKey input, string meaning, IPresentable pres)
        {
            _inputLogger?.Information($"[{pres.Name}] {input}: {meaning}\n");
        }

        /// <summary>
        /// Loggt die Änderung eines Präsentationselements in der momentanen Versuchslog..
        /// </summary>
        /// <param name="pres"></param>
        public static void LogPresentableChange(IPresentable pres)
        {
            _inputLogger?.Information($"Aktives Präsentationselement zu {pres.Name} geändert.\n");
        }

        /// <summary>
        /// Gibt die Logger-Subroutine für die Versuchslog frei.
        /// </summary>
        public static void DisposeInputLogger()
        {
            _inputLogger?.Dispose();
            _inputLogger = null;
        }
    }
}
