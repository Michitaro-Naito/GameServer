using MyResources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer {
    public class ClientException : Exception {
        public List<InterText> Errors { get; set; }

        public ClientException(List<InterText> errors) {
            Errors = errors;
        }
        public ClientException(InterText error) {
            Errors = new List<InterText>() { error };
        }
        public ClientException(string key) {
            Errors = new List<InterText>() { new InterText(key, _Error.ResourceManager) };
        }
    }
}
