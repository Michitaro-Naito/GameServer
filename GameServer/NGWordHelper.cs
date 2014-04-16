using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GameServer
{
    public static class NGWordHelper
    {
        public static Regex _regex = null;
        public static Regex Regex
        {
            get
            {
                if (_regex != null)
                    return _regex;
                _regex = new Utilities.RegularExpressions.JapaneseDirtyWord();
                return _regex;
            }
        }
    }
}
