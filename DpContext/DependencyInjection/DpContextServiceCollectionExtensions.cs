
using Dapper.DpContext;
using Dapper.DpContext;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class DpContextServiceCollectionExtensions
    {
        /// <summary>
        /// Add dapper Context
        /// </summary>
        /// <param name="services"></param>
        /// <param name="setupAction"></param>
        public static void AddDpContext<TDpContext>(this IServiceCollection services, Action<DpContextOptions> setupAction)
            where TDpContext : IDpContext
        {
            DpContextOptions dpContextOptions = new DpContextOptions();

            services.Configure(setupAction);

            services.AddScoped(typeof(TDpContext));
        }
    }
}
