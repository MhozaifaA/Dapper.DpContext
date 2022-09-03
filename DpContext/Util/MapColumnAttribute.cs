using Dapper;
using Dapper.DpContext;
using Microsoft.AspNetCore.Builder;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Dapper.DpContext
{
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false)]
    public class MapColumnAttribute : ColumnAttribute
    {
        public MapColumnAttribute(params string[] names) : base(String.Join('|',names)) { }
    }

}

namespace Microsoft.AspNetCore.Builder
{
    public static class MapDpSql
    {
        /// <summary>
        /// Mapping colmun name with prop name in AppAPI.DataTransformObject 
        /// </summary>
        /// <param name="app"></param>
        /// <returns></returns>
        public static IApplicationBuilder UseDpMappingColumns(this IApplicationBuilder app)
        {
            MappingColumns();
            return app;
        }

        private static string[] DllDependencies => Directory.GetFiles(AppDomain.CurrentDomain.BaseDirectory).
          Where(x => Path.GetExtension(x) == ".dll").Select(x => Path.GetFileNameWithoutExtension(x)).ToArray();

        private static List<Type> AbleNameSpaceToType<T>(this T[] any)
       => any.Aggregate(new List<Type>(), (all, next) => { all.AddRange(getOverWriteNext(next)); return all; });

        /// <summary>
        /// Choice cast wise to implement <see langword="namespace"/> s
        /// </summary>
        private static Func<object, Type[]> getOverWriteNext = (next) => next switch
        {
            string s => Assembly.Load(s).GetTypes(),
            Type t => t.Assembly.GetTypes(),
            Assembly a => a.GetTypes(),
            _ => default
        };

        public static void MappingColumns()
        {
            DllDependencies.AbleNameSpaceToType().ToArray().AbleNameSpaceToType()
                       .Where(t => t.IsClass).ToList()
                       .ForEach(typeDto =>
                       {
                           var mapper = new CustomPropertyTypeMap(
                              typeDto, (type, columnName) => type.GetProperties().FirstOrDefault(prop =>
                              (prop.Name == columnName) || //same name with prop
                              prop.GetCustomAttributes(false).OfType<MapColumnAttribute>().
                              Any(attr =>attr?.Name?.Split("|").Select(x => x.Trim()).Contains(columnName) ?? false)  
                              ));

                           SqlMapper.SetTypeMap(typeDto, mapper);
                       });

            SqlMapper.AddTypeHandler(new DapperDateTimeTypeHandler());

        }

        public class DapperDateTimeTypeHandler : SqlMapper.TypeHandler<DateTime>
        {
            public override void SetValue(IDbDataParameter parameter, DateTime value)
            {
                parameter.Value = value;
            }
            
            public override DateTime Parse(object value)
            {
                DateTime? date = value as DateTime?;
                if(date is null)
                {
                    string dateStr = (string)value;

                    if (DateTime.TryParse(dateStr, out DateTime _date))
                    {
                        date = _date;
                    }
                    else if (DateTime.TryParseExact(dateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture,
                           DateTimeStyles.None, out DateTime _edate))
                    {
                        date = _edate;
                    }
                    else if (DateOnly.TryParse(dateStr, out DateOnly _dateOnly))
                    {
                        date = _dateOnly.ToDateTime(TimeOnly.MinValue);
                    }
                    else if (DateOnly.TryParseExact(dateStr, "dd/MM/yyyy", CultureInfo.InvariantCulture,
                           DateTimeStyles.None, out DateOnly _edateOnly))
                    {
                        date = _edateOnly.ToDateTime(TimeOnly.MinValue);
                    }

                }

                return date.Value;
            }
        }
    }
}

