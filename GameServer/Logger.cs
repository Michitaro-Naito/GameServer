using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Diagnostics;

namespace GameServer {
    public static class Logger {
        public static void WriteLine(string body) {
            Debug.WriteLine(body);
            using (var w = File.AppendText("log.txt")) {
                w.WriteLine(DateTime.UtcNow);
                w.WriteLine(body);
                w.WriteLine();
            }
        }
    }
}
