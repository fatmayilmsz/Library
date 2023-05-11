using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace Library.utils
{

    public static class LcUtils
    {
        public static IEnumerable<PropertyInfo> GetDbSetTypes(Library.Models.LibraryContext context)
        {
            var contextType = context.GetType();

            var dbSetProperties = contextType.GetProperties()
                .Where(p => p.PropertyType.IsGenericType && p.PropertyType.GetGenericTypeDefinition() == typeof(DbSet<>));

            return dbSetProperties;
        }
    }
}
