using Mapster;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace CleanArchitecture.Application.Common.Mapping
{
    public class MappingProfile
    {
    
        public static void ApplyMappingsFromAssembly(Assembly assembly, TypeAdapterConfig config)
        {
            var types = assembly.GetExportedTypes()
                .Where(t => t.GetInterfaces().Any(i =>
                    i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IMapFrom<>)))
                .ToList();

            foreach (var type in types)
            {
                var instance = Activator.CreateInstance(type);


                //var methodInfo = type.GetMethod("Mapping")
                //                 ?? type.GetInterface("IMapFrom`1").GetMethod("Mapping");


                //methodInfo?.Invoke(instance, new object[] { config });
            }
        }
    }
}
