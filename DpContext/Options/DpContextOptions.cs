using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dapper.DpContext
{
    public class DpContextOptions
    {
        internal string ConnectionString { get; set; }
        internal bool HasProtected { get; set; }
    }

    public static class DpContextOptionsExtensions
    {
        public static void UseSqlServer(this DpContextOptions options, string connectionString)
        {
            options.ConnectionString = connectionString;
        }
    }
}
