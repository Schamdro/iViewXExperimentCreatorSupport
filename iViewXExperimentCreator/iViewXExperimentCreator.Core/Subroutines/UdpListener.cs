using iViewXExperimentCreator.Core.ViewModels;
using System;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace iViewXExperimentCreator.Core.Subroutines
{
    /// <summary>
    /// Diese Klasse wird dafür gebraucht, um eine Verbindung über das Netzwerk herzustellen.
    /// 
    /// Diese Klasse wurde aus dem ursprünglichen Programm EyeTrackingDemo von Michael Winter übernommen. 
    /// Es wurden einige Anpassungen vorgenommen, die Grundstruktur ist jedoch effektiv die gleiche.
    /// </summary>
    public class UdpListener
    {
        private const int TIMEOUT_VALUE = 500;

        private IPAddress _ip;
        private readonly UdpClient _client = null;
        private volatile bool _listening;
        private Action<byte[]> _callback;
        //Thread listeningThread;

        /// <summary>
        /// Konstruktor.
        /// </summary>
        /// <param name="client"></param>
        /// <param name="callback"></param>
        public UdpListener(UdpClient client, Action<byte[]> callback)
        {
            _listening = false;
            _client = client;
            _client.Client.ReceiveTimeout = TIMEOUT_VALUE;
            _callback = callback;
        }

        /// <summary>
        /// Fängt an auf Antworten der Verbindung zu warten.
        /// </summary>
        public void Start()
        {
            if(IPAddress.TryParse(MainViewModel.Instance.IP, out IPAddress ip))
            {
                _ip = ip;

                if (!_listening)
                {
                    //listeningThread = new(Listen);
                    //listeningThread.IsBackground = true;
                    _listening = true;
                    //listeningThread.Start();
                    Listen();
                }
            }
            else
            {
                /* Diese Exception sollte aufgrund vorheriger Checks eigentlich NIE geworfen werden.
                * Das ist trotzdem einmal passiert, dann aber nie wieder. Es ist nicht ganz klar, wie
                * das sein kann. Möglicherweise wegen eines langsamen/nicht mehr synchronen UI-Threads?
                */
                Logger.Error(new FormatException(), "IP-Adresse besitzt ein ungeeignetes Format.");
            }
        }

        /// <summary>
        /// Hört auf, auf Antworten der Verbindung zu warten.
        /// </summary>
        public void Stop()
        {
            _listening = false;
            //TOFO: Hier EndReceive verwenden, dafür muss aber async-Receive verwendet werden
        }

        /// <summary>
        /// Listener-Thread, in dem auf Pakete gewartet wird. 
        /// </summary>
        private void Listen()
        {

            if (_client is not null)
            {
                IPEndPoint groupEP = new(IPAddress.Any, MainViewModel.Instance.Port);
                Logger.Debug("UdpListener hat Endpoint: " + groupEP.ToString());

                try
                {
                    while (_listening)
                    {
                        try
                        {
                            //Hier async-Receive verwenden
                            byte[] bytes = _client.Receive(ref groupEP);
                            _callback(bytes);
                            Logger.Debug("Nachricht von iViewX erhalten: " + Encoding.ASCII.GetString(bytes));
                        }
                        catch(Exception e)
                        {
                            /* Das hier ist ok. Hier muss nichts getan werden, da Exception nur bei lange ausbleibender Antwort
                            * auftritt. Da die Verbindung zum SMI Experiment Center komplett über UDP-Sockets läuft und das 
                            * SMI Experiment Center auch nur per Ping Lebenszeichen zurückgibt, wird das alles hier einfach über 
                            * Timeouts geregelt. Man könnte alternativ auch das neuere ReceiveAsync verwenden, aber der Mehraufwand 
                            * in der Implementierung und die zusätzliche Komplexität durch weiteres Multithreading bringt hier nur
                            * Nachteile. Ein großer Teil dieser Klasse ist aus dem ursprünglichen Programm EyeTrackingDemo von
                            * Michael Winter genommen und funktioniert so auch gut.
                            */
                        }
                    }
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Fehler beim Erhalten von Nachrichten von iViewX.");
                }
            }
        }
    }
}
