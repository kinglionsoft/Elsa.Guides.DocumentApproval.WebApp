using Elsa.Activities.Console.Extensions;
using Elsa.Activities.Http.Extensions;
using Elsa.Activities.Timers.Extensions;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Elsa.Guides.DocumentApproval.WebApp
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            services
                .AddElsa()
                .AddConsoleActivities()
                .AddHttpActivities(options => options.Bind(Configuration.GetSection("Http")))
                .AddTimerActivities(options => options.Bind(Configuration.GetSection("BackgroundRunner")))
                .AddWorkflow<DocumentApprovalWorkflow>()
                //.AddElsaDashboard()
                ;
        }

        public void Configure(IApplicationBuilder app)
        {
            app.UseHttpActivities();

            app
                .UseStaticFiles()
                .UseRouting()
                .UseEndpoints(endpoints => endpoints.MapControllers())
                ;
        }
    }
}