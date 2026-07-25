using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Abstraction.Reflections
{
    public interface IEntityStructures
    {
        Task<string> GetProperties<T>();
    }
}
