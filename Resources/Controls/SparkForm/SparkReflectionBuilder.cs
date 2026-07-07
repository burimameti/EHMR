using System.Reflection;

namespace EHMR.Domain.SparkForm
{
    public sealed class SparkReflectionBuilder
    {
        private readonly SparkFieldFactory _factory = new();



        public IList<SparkFormField> BuildFields(
            Type entityType)
        {
            var fields = new List<SparkFormField>();


            var properties =
                entityType.GetProperties(
                    BindingFlags.Public|
                    BindingFlags.Instance|
                    BindingFlags.FlattenHierarchy);



            foreach(var property in properties)
            {
                if(!ShouldInclude(property))
                    continue;


                fields.Add(
                    _factory.Create(property));
            }



            return fields
                .OrderBy(x => x.Order)
                .ThenBy(x => x.Label)
                .ToList();
        }



        private static bool ShouldInclude(PropertyInfo property)
        {
            if(!property.CanRead)
                return false;


            if(property.GetIndexParameters().Length>0)
                return false;


            if(property.GetCustomAttribute<SparkIgnoreAttribute>()!=null)
                return false;


            if(IsCollection(property.PropertyType))
                return false;


            return true;
        }



        private static bool IsCollection(
            Type type)
        {
            return type!=typeof(string)
                &&typeof(System.Collections.IEnumerable)
                    .IsAssignableFrom(type);
        }
    }
}
