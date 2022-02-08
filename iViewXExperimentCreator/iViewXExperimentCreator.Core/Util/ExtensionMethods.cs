using iViewXExperimentCreator.Core.Enums;
using iViewXExperimentCreator.Core.Models;
using iViewXExperimentCreator.Core.ViewModels;
using MvvmCross.ViewModels;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace iViewXExperimentCreator.Core.Util
{
    /// <summary>
    /// Nützliche Erweiterungsmethoden.
    /// </summary>
    public static class ExtensionMethods
    {
        /// <summary>
        /// Erweiterungsmethode für Listen. Dies ist eine Implementierung des Fisher-Yates-Mischalgorithmus.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        public static void Shuffle<T>(this List<T> list)
        {
            Random rnd = new();

            for (int i = 0; i < list.Count-1; i++)
            {
                int k = rnd.Next(i, list.Count);
                (list[i], list[k]) = (list[k], list[i]);
            }
        }

        /// <summary>
        /// Tauscht die Positionen von Elementen in einer MvxObservableColletion.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="list"></param>
        /// <param name="obj1"></param>
        /// <param name="obj2"></param>
        public static void Swap<T>(this MvxObservableCollection<T> list, T obj1, T obj2)
        {
            if(list.Contains(obj1) && list.Contains(obj2))
            {
                int obj1idx = list.IndexOf(obj1);
                int obj2idx = list.IndexOf(obj2);
                
                (list[obj1idx], list[obj2idx]) = (list[obj2idx], list[obj1idx]);
            }
            else
            {
                Logger.Error(new ArgumentException(), $"{ obj1 } oder { obj2 } sind nicht in der Liste vorhanden.");
            }
        }


        /// <summary>
        /// Um eine tiefe Kopie zu erschaffen, wird das Objekt serialisiert und danach wieder deserialisiert.
        /// </summary>
        /// <returns></returns>
        public static T DeepCopy<T>(this T obj)
        {
            JsonSerializerSettings settings = ExperimentFileManagerModel.JsonSettings;
            T clone = default;
            try
            {
                clone = JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(obj, settings), settings);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Objekt konnte nicht geklont werden.");
            }
            return clone;
        }

        /// <summary>
        /// Mit dieser Methode wird die UdpClient-Klasse um eine Funktion erweitert, mit der Nachrichten im
        /// Remote Control Language Format des SMI Experiment Centers geschickt werden können.
        /// 
        /// Parameter können und werden nicht auf ihre Validität überprüft.
        /// </summary>
        /// <param name="udpClient"></param>
        /// <param name="command"></param>
        /// <param name="ps"></param>
        public static void SendRemoteControlCommand(this UdpClient udpClient, RemoteCommand command, params string[] ps)
        {
            if (udpClient == null)
            {
                Logger.Error(new NullReferenceException(), "Remote Control Command konnte nicht gesendet werden.");
                return;
            }

            //TODO send cmd
            string message = command.ToString();

            if (command == RemoteCommand.ET_SAV)
            {
                message += $" \"{ps[0]}\\{ps[1]}_{ps[2]}.idf\"";
            }
            else
                foreach (string parameter in ps)
                {
                    message += " " + parameter;
                }
            message += "\n";

            byte[] data = Encoding.ASCII.GetBytes(message);
            Logger.Debug($"Führe Command {message} aus.");
            try
            {
                udpClient.Send(data, data.Length);
            }
            catch (Exception e)
            {
                Logger.Error(e, "Fehler beim Senden des RCC " + message);
            }
        }

        /// <summary>
        /// Gibt das Maximum von 0 oder dem übergebenen Wert zurück. Bei Bedarf kann der maximale erlaubte Wert festgelegt werden.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="defaultValue"></param>
        /// <param name="maxValue"></param>
        /// <returns></returns>
        public static int ParseToPositiveInt(this string value, int defaultValue = 0, int maxValue = int.MaxValue)
        {
            if (uint.TryParse(value.ToString(), out uint intVal))
            {
                return Math.Min((int)intVal, maxValue);
            }
            return Math.Min(Math.Max(defaultValue, 0), maxValue);
        }

        /// <summary>
        /// Verändert den übergebenen String in solcher Art, dass er einen noch verfügbaren Dateipfad zurückgibt.
        /// 
        /// Appendiert mit (i) wobei (i) inkrementiert wird, wenn (i) schon existiert. Beginnt bei (2).
        /// </summary>
        /// <param name="path"></param>
        /// <returns></returns>
        public static string GetFreeFilePath(this string path)
        {
            string fileName, newPath = path;
            for (int i = 2; ; i++)
            {
                if (!File.Exists(newPath)) return newPath;
                fileName = $"{Path.GetFileNameWithoutExtension(path)} ({i}){Path.GetExtension(path)}";
                newPath = Path.Combine(Path.GetDirectoryName(path), fileName);
            }
        }

        /// <summary>
        /// Verändert den Namen eines IHasName-Elements derart, dass er in der übergebenen Collection einzigartig ist.
        /// 
        /// Appendiert mit (i) wobei (i) inkrementiert wird, wenn (i) schon existiert. Beginnt bei (2).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static string GetFreeNameInCollection<T>(this string name, IEnumerable<T> collection) where T : IHasName
        {
            if (collection is null) return name;
            string newName = name;
            for(int i = 2; ; i++)
            {
                if (!collection.Any(item => item.Name == newName)) return newName;
                newName = $"{name} ({i})";
            }
        }

        /// <summary>
        /// Erstellt einen neuen UDP-Client mit den im Hauptfenster eingetragenen Verbindungsinformationen.
        /// </summary>
        /// <returns></returns>
        public static bool TryInitializeUdpClient(this UdpClient client)
        {
            if (IPAddress.TryParse(MainViewModel.Instance.IP, out IPAddress ip))
            {
                IPEndPoint endPoint = new(IPAddress.Any, MainViewModel.Instance.Port); //Arbiträrer Port
                try
                {
                    //client.Client.SetSocketOption(SocketOptionLevel.Udp, SocketOptionName.ReuseAddress, true);
                    client.Client.Bind(endPoint);
                    client.Connect(ip, MainViewModel.Instance.Port);
                    return true;
                }
                catch (Exception e)
                {
                    Logger.Error(e, "Verbindung zum iViewX gescheitert.");
                }
            }
            else Logger.Error(new FormatException(), "Invalides Format der IP-Adresse.");
            return false;
        }
    }
}
