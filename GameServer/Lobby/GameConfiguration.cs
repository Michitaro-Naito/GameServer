using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer {
    public static class GameConfiguration {

        public static string Host {
            get { return ConfigurationManager.AppSettings["Host"]; }
        }

        public static int Port {
            get { return int.Parse(ConfigurationManager.AppSettings["Port"]); }
        }

        public static string Name {
            get { return ConfigurationManager.AppSettings["Name"]; }
        }

        public static int MaxPlayers {
            get { return int.Parse(ConfigurationManager.AppSettings["MaxPlayers"]); }
        }

        public static bool ReportStatus {
            get { return bool.Parse(ConfigurationManager.AppSettings["ReportStatus"]); }
        }

        public static string ListenUrl {
            get { return string.Format("http://{0}:{1}", Host, Port); }
        }

        public static bool HasError {
            get {
                return Host == null
                    || Port == 0
                    || Name == null
                    || MaxPlayers == 0;
            }
        }

        public static string ToString() {
            var error = "";
            if (HasError)
                error = "***ERROR*** ";
            return string.Format("[{0}LobbyConfiguration Host:{1} Port:{2} Name:{3} MaxPlayers:{4} ReportStatus:{5}]", error, Host, Port, Name, MaxPlayers, ReportStatus);
        }
    }
}
