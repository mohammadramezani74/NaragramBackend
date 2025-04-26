using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Domain.Exceptions
{
    public class UnSupportedPostalCodeException(string postalCode)
     : Exception($"Postalcode \"{postalCode}\" is unsupported.")
    {
    }
}
