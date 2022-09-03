using Dapper;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Dapper.SqlMapper;

namespace Dapper.DpContext
{
    public class SPParameterBase
    {
        //public SPParameter(string name, object value, DbType type, ParameterDirection direction) : this(name, value.ToString(), type, direction) { }

        public SPParameterBase(string name, object? value)
        {
            this.name = name;
            this.value = value;
        }

        public SPParameterBase(string name, ICustomQueryParameter? value)
        {
            this.name = name;
            this.dataTable = value;
        }
        /// <summary>
        /// Name of prop
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// value of pass/out prop 
        /// </summary>
        public object? value { get; set; }

        /// <summary>
        /// DataTable of type table pass
        /// </summary>
        public ICustomQueryParameter? dataTable { get; set; }
    }

    public class SPParameter : SPParameterBase
    {
        public SPParameter(string name, object? value) : base(name, value) { }
        public SPParameter(string name, ICustomQueryParameter value) : base(name, value) { }
        public SPParameter(string name, object? value, DbType? type, ParameterDirection? direction) : this(name, value)
        {
            this.type = type;
            this.direction = direction;
        }

        /// <summary>
        /// string to pass to sql datatype
        /// </summary>
        public DbType? type { get; set; }
        /// <summary>
        /// kid of input/output/mix..
        /// </summary>
        public ParameterDirection? direction { get; set; }

        /// <summary>
        /// return the current value if is table type  or normal value param
        /// </summary>
        /// <returns></returns>
        public object? CurrectValue()
        {
            return (value is null && dataTable is not null) ? dataTable! : value;
        }

        /// <summary>
        /// get output value casting
        /// </summary>
        /// <typeparam name=""></typeparam>
        /// <returns></returns>
        public T? Get<T>()
        {
            return (T?)Convert.ChangeType(value, typeof(T?));
        }

        public object? Get()
        {
            return value;
        }
    }

    public abstract class AppDpRepositoryBase
    {
        public readonly DpContext Context;

        public AppDpRepositoryBase(DpContext appDpContext)
        {
            this.Context = appDpContext;
        }

        #region IEnumerable{T}


        protected virtual async Task<IEnumerable<TDto>> ExecStoredProcedureList<TDto>(string procedure, params SPParameter[] parameter)
        {
            if (parameter is null || parameter.Length == 0)
                return await ExecStoredProcedureList<TDto>(procedure);

            var parameters = new DynamicParameters();

            foreach (var par in parameter)
                parameters.Add(par.name, par.value, par.type, par.direction);

            var query = await ExecStoredProcedureQuery<TDto>(procedure, parameters);


            FillOuterParam(parameter, parameters);

            return query;
        }

        protected virtual async Task<IEnumerable<TDto>> ExecStoredProcedureList<TDto>(string procedure, params SPParameterBase[] parameter)
        {
            if (parameter is null || parameter.Length == 0)
                return await ExecStoredProcedureList<TDto>(procedure);

            var parameters = new DynamicParameters();

            foreach (var par in parameter)
                parameters.Add(par.name, par.value);

            return await ExecStoredProcedureQuery<TDto>(procedure, parameters);
        }


        protected virtual async Task<IEnumerable<TDto>> ExecStoredProcedureList<TDto>(string procedure)
        {
            return await ExecStoredProcedureQuery<TDto>(procedure, null);
        }

        protected virtual async Task<IEnumerable<TDto>> ExecStoredProcedureList<TExec, TDto>(string procedure, TExec exec) where TExec : class
        {
            var parameters = new DynamicParameters();

            ObjectToParams(exec, parameters);

            var query = await ExecStoredProcedureQuery<TDto>(procedure, parameters);

            FillOuterParam(exec, parameters);

            return query;
        }



        private async Task<IEnumerable<TDto>> ExecStoredProcedureQuery<TDto>(string procedure, DynamicParameters parameters)
        {
            using (var connection = Context.CreateConnection())
            {
                if (parameters is null)
                    return await connection.QueryAsync<TDto>
                    (procedure, commandType: CommandType.StoredProcedure);
                else
                    return await connection.QueryAsync<TDto>
                (procedure, parameters, commandType: CommandType.StoredProcedure);
            }
        }

        #endregion



        #region IDictionary<string,{IEnumerable{T}}


        protected virtual async Task<IDictionary<string, IEnumerable<object>>> ExecStoredProcedureMultiList(string procedure, params SPParameter[] parameter)
        {
            if (parameter is null || parameter.Length == 0)
                return await ExecStoredProcedureMultiList(procedure);

            var parameters = new DynamicParameters();

            foreach (var par in parameter)
                parameters.Add(par.name, par.value, par.type, par.direction);

            var query = await ExecStoredProcedureGridQuery(procedure, parameters);


            FillOuterParam(parameter, parameters);

            return query;
        }

        protected virtual async Task<IDictionary<string, IEnumerable<object>>> ExecStoredProcedureMultiList(string procedure, params SPParameterBase[] parameter)
        {
            if (parameter is null || parameter.Length == 0)
                return await ExecStoredProcedureMultiList(procedure);

            var parameters = new DynamicParameters();

            foreach (var par in parameter)
                parameters.Add(par.name, par.value);

            return await ExecStoredProcedureGridQuery(procedure, parameters);
        }


        protected virtual async Task<IDictionary<string, IEnumerable<object>>> ExecStoredProcedureMultiList(string procedure)
        {
            return await ExecStoredProcedureGridQuery(procedure, null);
        }

        protected virtual async Task<IDictionary<string, IEnumerable<object>>> ExecStoredProcedureMultiList<TExec>(string procedure, TExec exec) where TExec : class
        {
            var parameters = new DynamicParameters();

            ObjectToParams(exec, parameters);

            var query = await ExecStoredProcedureGridQuery(procedure, parameters);

            FillOuterParam(exec, parameters);

            return query;
        }



        private async Task<IDictionary<string, IEnumerable<object>>> ExecStoredProcedureGridQuery(string procedure, DynamicParameters parameters)
        {
            using (var connection = Context.CreateConnection())
            {
                Dictionary<string, IEnumerable<object>> list = new();
                GridReader grid;
                if (parameters is null)

                    grid = await connection.QueryMultipleAsync
                        (procedure, commandType: CommandType.StoredProcedure);

                else
                    grid = await connection.QueryMultipleAsync
                (procedure, parameters, commandType: CommandType.StoredProcedure);

                int i = 0;
                while (!grid.IsConsumed)
                {
                    list.Add("Table" + (++i), await grid.ReadAsync<object>());
                }

                return list;
            }
        }

        #endregion


        #region DataTable



        protected virtual async Task<DataTable> ExecStoredProcedureDataTable(string procedure, params SPParameter[] parameter)
        {
            if (parameter is null || parameter.Length == 0)
                return await ExecStoredProcedureDataTable(procedure);

            var parameters = new DynamicParameters();

            foreach (var par in parameter)
                parameters.Add(par.name, par.value, par.type, par.direction);


            var query = await ExecStoredProcedureQuery(procedure, parameters);

            FillOuterParam(parameter, parameters);

            return query;
        }

        protected virtual async Task<DataTable> ExecStoredProcedureDataTable(string procedure, params SPParameterBase[] parameter)
        {
            if (parameter is null || parameter.Length == 0)
                return await ExecStoredProcedureDataTable(procedure);

            var parameters = new DynamicParameters();

            foreach (var par in parameter)
                parameters.Add(par.name, par.value);

            return await ExecStoredProcedureQuery(procedure, parameters);
        }


        protected virtual async Task<DataTable> ExecStoredProcedureDataTable(string procedure)
        {
            return await ExecStoredProcedureQuery(procedure, null);
        }

        protected virtual async Task<DataTable> ExecStoredProcedureDataTable<TExec>(string procedure, TExec exec) where TExec : class
        {
            var parameters = new DynamicParameters();

            ObjectToParams(exec, parameters);

            var query = await ExecStoredProcedureQuery(procedure, parameters);

            FillOuterParam(exec, parameters);

            return query;
        }



        private async Task<DataTable> ExecStoredProcedureQuery(string procedure, DynamicParameters parameters)
        {
            using (var connection = Context.CreateConnection())
            {
                DataTable dataTable = new();

                if (parameters is null)
                    dataTable.Load(await connection.ExecuteReaderAsync
                    (procedure, commandType: CommandType.StoredProcedure));
                else
                    dataTable.Load(await connection.ExecuteReaderAsync
                (procedure, parameters, commandType: CommandType.StoredProcedure));

                return dataTable;
            }
        }


        #endregion



        #region DataSet



        protected virtual async Task<DataSet> ExecStoredProcedureDataSet(string procedure, params SPParameter[] parameter)
        {
            if (parameter is null || parameter.Length == 0)
                return await ExecStoredProcedureDataSet(procedure);

            var parameters = new DynamicParameters();

            foreach (var par in parameter)
                parameters.Add(par.name, par.value, par.type, par.direction);


            var query = await ExecStoredProcedureMultiQuery(procedure, parameters);

            FillOuterParam(parameter, parameters);

            return query;
        }

        protected virtual async Task<DataSet> ExecStoredProcedureDataSet(string procedure, params SPParameterBase[] parameter)
        {
            if (parameter is null || parameter.Length == 0)
                return await ExecStoredProcedureDataSet(procedure);

            var parameters = new DynamicParameters();

            foreach (var par in parameter)
                parameters.Add(par.name, par.value);

            return await ExecStoredProcedureMultiQuery(procedure, parameters);
        }


        protected virtual async Task<DataSet> ExecStoredProcedureDataSet(string procedure)
        {
            return await ExecStoredProcedureMultiQuery(procedure, null);
        }

        protected virtual async Task<DataSet> ExecStoredProcedureDataSet<TExec>(string procedure, TExec exec) where TExec : class
        {
            var parameters = new DynamicParameters();

            ObjectToParams(exec, parameters);

            var query = await ExecStoredProcedureMultiQuery(procedure, parameters);

            FillOuterParam(exec, parameters);

            return query;
        }



        private async Task<DataSet> ExecStoredProcedureMultiQuery(string procedure, DynamicParameters parameters)
        {
            using (var connection = Context.CreateConnection())
            {
                DataSet dataSet = new();
                IDataReader reader;
                //var _ = await connection.QueryMultipleAsync
                // (procedure, commandType: CommandType.StoredProcedure);

                if (parameters is null)
                    reader = await connection.ExecuteReaderAsync
                      (procedure, commandType: CommandType.StoredProcedure);
                else
                    reader = await connection.ExecuteReaderAsync
                      (procedure, parameters, commandType: CommandType.StoredProcedure);

                int i = 0;
                while (!reader.IsClosed)
                {
                    dataSet.Tables.Add("Table" + (i + 1));
                    dataSet.EnforceConstraints = false;
                    dataSet.Tables[i].Load(reader);
                    i++;
                }

                return dataSet;
            }
        }


        #endregion


        #region  Exec-Return;


        protected virtual async Task<TReturn?> ExecStoredProcedure<TReturn>(string procedure, params SPParameter[] parameter)
        {

            var parameters = new DynamicParameters();

            foreach (var par in parameter)
                parameters.Add(par.name, par.CurrectValue(), par.type, par.direction);

            parameters.Add("@returnvalue", direction: ParameterDirection.ReturnValue);

            var @return = await ExecStoredProcedureReturnValue<TReturn?>(procedure, parameters);

            FillOuterParam(parameter, parameters);

            return @return;
        }



        protected virtual async Task<TReturn?> ExecStoredProcedure<TReturn>(string procedure, params SPParameterBase[] parameter)
        {

            var parameters = new DynamicParameters();

            foreach (var par in parameter)
                parameters.Add(par.name, par.value);

            parameters.Add("@returnvalue", direction: ParameterDirection.ReturnValue);

            var @return = await ExecStoredProcedureReturnValue<TReturn?>(procedure, parameters);

            return @return;
        }

        protected virtual async Task<TReturn?> ExecStoredProcedure<TExec, TReturn>(string procedure, TExec exec) where TExec : class
        {
            var parameters = new DynamicParameters();

            ObjectToParams(exec, parameters);

            parameters.Add("@returnvalue", direction: ParameterDirection.ReturnValue);

            var @return = await ExecStoredProcedureReturnValue<TReturn?>(procedure, parameters);

            FillOuterParam(exec, parameters);

            return @return;

        }



        protected virtual async Task<TReturn?> ExecStoredProcedure<TReturn>(string procedure)
        {
            return await ExecStoredProcedureReturnValue<TReturn?>(procedure, null);
        }


        private async Task<TReturn?> ExecStoredProcedureReturnValue<TReturn>(string procedure, DynamicParameters parameters)
        {
            using (var connection = Context.CreateConnection())
            {
                await connection.ExecuteAsync(procedure, parameters, commandType: CommandType.StoredProcedure);
                return parameters is not null ? parameters.Get<TReturn>("@returnvalue") : default(TReturn?);
            }
        }

        #endregion


        #region Exec

        protected virtual async Task<int> ExecStoredProcedure(string procedure, params SPParameter[] parameter)
        {
            var parameters = new DynamicParameters();

            foreach (var par in parameter)
                parameters.Add(par.name, par.CurrectValue(), par.type, par.direction);

            var affected = await ExecStoredProcedureExecute(procedure, parameters);

            FillOuterParam(parameter, parameters);

            return affected;
        }



        protected virtual async Task<int> ExecStoredProcedure(string procedure, params SPParameterBase[] parameter)
        {

            var parameters = new DynamicParameters();

            foreach (var par in parameter)
                parameters.Add(par.name, par.value);

            var affected = await ExecStoredProcedureExecute(procedure, parameters);

            return affected;
        }

        protected virtual async Task<int> ExecStoredProcedure<TExec>(string procedure, TExec exec) where TExec : class
        {
            var parameters = new DynamicParameters();

            ObjectToParams(exec, parameters);

            var affected = await ExecStoredProcedureExecute(procedure, parameters);

            FillOuterParam(exec, parameters);

            return affected;
        }



        protected virtual async Task<int> ExecStoredProcedure(string procedure)
        {
            return await ExecStoredProcedureExecute(procedure, null);
        }


        private async Task<int> ExecStoredProcedureExecute(string procedure, DynamicParameters parameters)
        {
            using (var connection = Context.CreateConnection())
            {
                return await connection.ExecuteAsync(procedure, parameters, commandType: CommandType.StoredProcedure);
            }
        }

        #endregion


        private void FillOuterParam(SPParameter[] parameter, DynamicParameters parameters)
        {
            parameter.Where(p => p.direction == ParameterDirection.Output).All(p =>
            {
                p.value = parameters.Get<object?>(p.name);
                return true;
            });
        }

        private void FillOuterParam<TExec>(TExec parameter, DynamicParameters parameters)
        {
            var virtualProperties = typeof(TExec).GetProperties()
                .Where(x => IsVirtual(x));
            virtualProperties.All(p =>
            {
                p.SetValue(parameter, parameters.Get<object>(p.Name) );
                return true;
            });
        }

        private void ObjectToParams<TExec>(TExec exec, DynamicParameters parameters) where TExec : class
        {
            if (exec.GetType().Namespace == "System.Text.Json")
            {
                foreach (var prop in JsonDocument.Parse(exec.ToString()).RootElement.EnumerateObject())
                {
                    string valu = prop.Value.ToString(); //u
                    Span<byte> buffer = new Span<byte>(new byte[valu.Length]);

                    if (Convert.TryFromBase64String(valu, buffer, out int c))
                    {
                        if (buffer.IsAsRealFileSequence())
                            parameters.Add(prop.Name, buffer.Slice(0, c).ToArray());
                        else
                            parameters.Add(prop.Name, valu);
                    }
                    else
                    {
                        parameters.Add(prop.Name, prop.Value.ToString());
                    }
                    buffer.Clear();
                }

            }
            else
            {
                foreach (var prop in exec.GetType().GetProperties(BindingFlags.Public |
                                        BindingFlags.Instance))
                {
                    if (Attribute.IsDefined(prop, typeof(FromBase64StringAttribute)))
                    {
                        string? valu = (string?)prop.GetValue(exec, null);
                        if (valu is null)
                        {
                            parameters.Add(prop.Name, valu, direction: IsVirtual(prop) ? ParameterDirection.Output : ParameterDirection.Input);
                            continue;
                        }
                        Span<byte> buffer = new Span<byte>(new byte[valu.Length]);
                        Convert.TryFromBase64String(valu, buffer, out int c);

                        parameters.Add(prop.Name, buffer.Slice(0, c).ToArray(), direction: IsVirtual(prop) ? ParameterDirection.Output : ParameterDirection.Input);

                        buffer.Clear();
                    }
                    else
                     parameters.Add(prop.Name, prop.GetValue(exec, null), direction: IsVirtual(prop)?ParameterDirection.Output:ParameterDirection.Input);
                }

            }
        }

        private bool IsVirtual(PropertyInfo property)
        {
            return (!property.GetAccessors()[0].IsFinal && property.GetAccessors()[0].IsVirtual);
        }

        protected void ObjectToSPParameter<TExec>(TExec exec, ref List<SPParameter> parameters) where TExec : class
        {
            if (exec.GetType().Namespace == "System.Text.Json")
            {
                foreach (var prop in JsonDocument.Parse(exec.ToString()).RootElement.EnumerateObject())
                    parameters.Add(new SPParameter(prop.Name, prop.Value));
            }
            else
            {
                foreach (var prop in exec.GetType().GetProperties(BindingFlags.Public |
                                        BindingFlags.Instance))
                    parameters.Add(new SPParameter(prop.Name,prop.GetValue(exec, null)));

            }
        }
        //private static readonly Regex regBase64 = new Regex(@"^[a-zA-Z0-9\+/]*={0,3}$", RegexOptions.Compiled | RegexOptions.None);
        //private bool IsBase64(string base64)
        //{
        //    return  (base64.Length % 4 == 0) && regBase64.IsMatch(base64);
        //}




    }

    public static class AppDpRepositoryBaseExtesnions
    {

        internal static bool IsAsRealFileSequence(this Span<byte> bytes)
        {
            var bmp = Encoding.ASCII.GetBytes("BM");     // BMP
            var gif = Encoding.ASCII.GetBytes("GIF");    // GIF
            var png = new byte[] { 137, 80, 78, 71 };    // PNG
            var png1 = new byte[] { 137, 80, 78, 71, 13, 10, 26, 10, 0, 0, 0, 13, 73, 72, 68, 82 };
            var tiff = new byte[] { 73, 73, 42 };         // TIFF
            var tiff2 = new byte[] { 77, 77, 42 };         // TIFF
            var jpeg = new byte[] { 255, 216, 255, 224 };  // jpeg
            var jpeg2 = new byte[] { 255, 216, 255, 225 }; // jpeg canon
            var JPEG = new byte[] { 255, 216, 255, 237 };  // JPEG 
            var pdf = new byte[] { 37, 80, 68, 70, 45, 49, 46 };   // PDF 
            var doc = new byte[] { 208, 207, 17, 224, 161, 177, 26, 225 };
            var mp3 = new byte[] { 255, 251, 48 };
            var rar = new byte[] { 82, 97, 114, 33, 26, 7, 0 };
            var swf = new byte[] { 70, 87, 83 };
            var office = new byte[] { 80, 75, 3, 4 };
            var office1 = new byte[] { 208, 207, 17, 224, 161, 177, 26, 225 };


            if (bytes.Length > bmp.Length && bmp.AsSpan().SequenceEqual(bytes.Slice(0, bmp.Length)))
                return true;

            if (bytes.Length > gif.Length && gif.AsSpan().SequenceEqual(bytes.Slice(0, gif.Length)))
                return true;


            if (bytes.Length > png1.Length && png1.AsSpan().SequenceEqual(bytes.Slice(0, png1.Length)))
                return true;


            if (bytes.Length > png.Length && png.AsSpan().SequenceEqual(bytes.Slice(0, png.Length)))
                return true;


            if (bytes.Length > tiff.Length && tiff.AsSpan().SequenceEqual(bytes.Slice(0, tiff.Length)))
                return true;


            if (bytes.Length > tiff2.Length && tiff2.AsSpan().SequenceEqual(bytes.Slice(0, tiff2.Length)))
                return true;


            if (bytes.Length > jpeg.Length && jpeg.AsSpan().SequenceEqual(bytes.Slice(0, jpeg.Length)))
                return true;


            if (bytes.Length > jpeg2.Length && jpeg2.AsSpan().SequenceEqual(bytes.Slice(0, jpeg2.Length)))
                return true;


            if (bytes.Length > JPEG.Length && JPEG.AsSpan().SequenceEqual(bytes.Slice(0, JPEG.Length)))
                return true;

            if (bytes.Length > pdf.Length && pdf.AsSpan().SequenceEqual(bytes.Slice(0, pdf.Length)))
                return true;

            if (bytes.Length > doc.Length && doc.AsSpan().SequenceEqual(bytes.Slice(0, doc.Length)))
                return true;

            if (bytes.Length > mp3.Length && mp3.AsSpan().SequenceEqual(bytes.Slice(0, mp3.Length)))
                return true;

            if (bytes.Length > rar.Length && rar.AsSpan().SequenceEqual(bytes.Slice(0, rar.Length)))
                return true;

            if (bytes.Length > swf.Length && swf.AsSpan().SequenceEqual(bytes.Slice(0, swf.Length)))
                return true;

            if (bytes.Length > office.Length && office.AsSpan().SequenceEqual(bytes.Slice(0, office.Length)))
                return true;

            if (bytes.Length > office1.Length && office1.AsSpan().SequenceEqual(bytes.Slice(0, office1.Length)))
                return true;

            return false;
        }

    }
}
