using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace GameServer.ClientModel
{
    class ClientCreateCharacter : IValidatable
    {
        public string ModelName { get; set; }

        public string name;

        public ValidationResult Validate()
        {
            var result = new ValidationResult();

            if (name == null || name.Length == 0 || Regex.IsMatch(name, @"^[a-z]$"))
            {
                result.Errors.Add(new InterText("Name went wrong.", null));
            }

            result.Errors.Add(new InterText("Something went wrong.", null));

            return result;
        }
    }
}
