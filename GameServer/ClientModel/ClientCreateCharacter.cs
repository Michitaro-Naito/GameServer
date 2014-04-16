using MyResources;
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

            if (name == null || name.Length == 0 || !Regex.IsMatch(name, @"^\w{1,10}$"/*@"^[a-zA-Z0-9]{1,10}$"*/))
            {
                result.Errors.Add(new InterText("AMustBeBToCNonSpecialCharacters"/*"AMustBeBToCAlphanumericCharacters"*/, _Error.ResourceManager, new[]{
                    new InterText("Character_Name", _Model.ResourceManager),
                    new InterText("1", null),
                    new InterText("10", null)
                }));
            }

            var match = NGWordHelper.Regex.Match(name);
            if (match.Success)
            {
                result.Errors.Add(new InterText("AContainsNGWordB", _Error.ResourceManager, new[]{
                    new InterText("Character_Name", _Model.ResourceManager),
                    new InterText(match.Value, null)
                }));
            }

            //result.Errors.Add(new InterText("Something went wrong.", null));

            return result;
        }
    }
}
