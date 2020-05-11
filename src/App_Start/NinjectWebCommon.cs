using WebActivator;

[assembly: WebActivator.PreApplicationStartMethod(typeof(SimpleMVC3App.App_Start.NinjectWebCommon), "Start")]
[assembly: ApplicationShutdownMethodAttribute(typeof(SimpleMVC3App.App_Start.NinjectWebCommon), "Stop")]

namespace SimpleMVC3App.App_Start
{
    using System;
    using System.Net.Http;
    using System.Web;
    using Microsoft.Extensions.Options;
    using Microsoft.Web.Infrastructure.DynamicModuleHelper;

    using Ninject;
    using Ninject.Web.Common;
    using Xero.Authorisation.Integration.Common;

    public static class NinjectWebCommon
    {
        public static StandardKernel kernel;
        private static readonly Bootstrapper bootstrapper = new Bootstrapper();

        /// <summary>
        /// Starts the application
        /// </summary>
        public static void Start() 
        {
            DynamicModuleUtility.RegisterModule(typeof(OnePerRequestHttpModule));
            DynamicModuleUtility.RegisterModule(typeof(NinjectHttpModule));
            bootstrapper.Initialize(CreateKernel);
        }
        
        /// <summary>
        /// Stops the application.
        /// </summary>
        public static void Stop()
        {
            bootstrapper.ShutDown();
        }
        
        /// <summary>
        /// Creates the kernel that will manage your application.
        /// </summary>
        /// <returns>The created kernel.</returns>
        private static IKernel CreateKernel()
        {
            kernel = new StandardKernel();
            try
            {
                kernel.Bind<Func<IKernel>>().ToMethod(ctx => () => new Bootstrapper().Kernel);
                kernel.Bind<IHttpModule>().To<HttpApplicationInitializationHttpModule>();

                kernel.Bind<IOptions<AuthorisationOptions>>().ToConstant(Options.Create(new AuthorisationOptions()));
                kernel.Bind<IAuthorisation>().To<Authorisation>().InRequestScope();
                kernel.Bind<IAuthorisationProvider>().To<LegacyAuthorisationProvider>().InRequestScope();
                kernel.Bind<IContextProvider>().To<ContextProvider>().InRequestScope();
                kernel.Bind<ILoggerAdapter>().To<LoggerAdapter>().InRequestScope();

                RegisterServices(kernel);
                return kernel;
            }
            catch
            {
                kernel.Dispose();
                throw;
            }
        }

        /// <summary>
        /// Load your modules or register your services here!
        /// </summary>
        /// <param name="kernel">The kernel.</param>
        private static void RegisterServices(IKernel kernel)
        {
        }        
    }
}
