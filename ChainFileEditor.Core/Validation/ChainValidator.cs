using System.Collections.Generic;
using System.Linq;

namespace ChainFileEditor.Core.Validation
{
    public sealed class ChainValidator
    {
        private readonly IEnumerable<IValidationRule> _rules;

        public ChainValidator(IEnumerable<IValidationRule> rules) => _rules = rules;

        public ValidationReport Validate(Models.ChainModel chain)
            => new ValidationReport(_rules.SelectMany(r => r.Validate(chain).Issues).ToList());
    }
}