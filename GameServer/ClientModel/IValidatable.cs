using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer
{
    public class ValidationResult
    {
        public bool Success { get { return Errors == null || Errors.Count == 0; } }
        public List<InterText> Errors { get; set; }

        public ValidationResult()
        {
            Errors = new List<InterText>();
        }
    }

    /// <summary>
    /// Exposes Validation.
    /// </summary>
    interface IValidatable
    {
        string ModelName { get; set; }
        ValidationResult Validate();
    }
}
