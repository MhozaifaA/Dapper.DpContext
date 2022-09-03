using Dapper.DpContext;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dapper.DpContext
{
    public class DpContext : IDpContext
    {
     
        private readonly DpContextOptions options;
        public DpContext(IOptions<DpContextOptions> options)
        {
            if(options is null || options.Value is null || string.IsNullOrEmpty(options.Value.ConnectionString))
                throw new ArgumentNullException(nameof(DpContextOptions) +"Can't be null or connection string empty");
            
            this.options = options.Value;
        }

        public IDbConnection CreateConnection()
            => new SqlConnection(options.ConnectionString);
    }
}
