using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace CleanArchitecture.Application.Common.Mapping
{
    public interface IMapper<T>
    {
        void Mapping(TypeAdapterConfig config);
    }
}
