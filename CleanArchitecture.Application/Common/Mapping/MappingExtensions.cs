using Mapster;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Common.Mapping
{
    public static class MappingExtensions
    {
        public static Task<List<TDestination>> ProjectToListAsync<TDestination>(this IQueryable queryable)
            => queryable.ProjectToType<TDestination>().ToListAsync();
    }
}
