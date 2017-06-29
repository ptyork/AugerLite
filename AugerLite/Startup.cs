using Microsoft.Owin;
using Owin;

[assembly: OwinStartupAttribute(typeof(Auger.Startup))]
namespace Auger
{
    public partial class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureAuth(app);
        }
    }
}