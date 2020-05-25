using WebActivator;
using WebApplication14.Controllers;

[assembly: WebActivator.PreApplicationStartMethod(typeof(SimpleMVC3App.App_Start.NinjectWebCommon), "Start")]
[assembly: ApplicationShutdownMethodAttribute(typeof(SimpleMVC3App.App_Start.NinjectWebCommon), "Stop")]

namespace SimpleMVC3App.App_Start
{
    using System;
    using System.Collections.Generic;
    using System.Web;
    using GitHub;
    using LaunchDarkly.Client;
    using Microsoft.Extensions.Caching.Memory;
    using Microsoft.Extensions.Logging;
    using Microsoft.Extensions.Options;
    using Microsoft.Web.Infrastructure.DynamicModuleHelper;

    using Ninject;
    using Ninject.Web.Common;
    using NLog.Extensions.Logging;
    using Service.V1;
    using Xero.Authorisation.Integration.Common;
    using Xero.Authorisation.Integration.Common.Metrics;
    using Xero.Authorisation.Integration.NetFramework.Sdk;
    using Xero.Identity.Integration.Client;
    using Xero.Identity.Integration.Client.Options;
    using Xero.Identity.Integration.Core.Metrics;

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
            RegisterAuthZ(kernel);
        }
        


        static void RegisterAuthZ(IKernel kernel)
        {
            kernel.Bind<LoggerFactory>().To<LoggerFactory>();
#pragma warning disable IDISP001
            var loggerFactory = kernel.Get<LoggerFactory>();
            loggerFactory.AddNLog();
#pragma warning restore IDISP001
            kernel.Bind<ILogger<Authorisation>>().ToConstant(loggerFactory.CreateLogger<Authorisation>());
            kernel.Bind<ILogger<ClientCredentialsClientTokenManager>>().ToMethod(ct => loggerFactory.CreateLogger<ClientCredentialsClientTokenManager>());
            kernel.Bind<ILogger<AuthorisationComparisonResultPublisher>>().ToMethod(ctx => loggerFactory.CreateLogger<AuthorisationComparisonResultPublisher>());
            kernel.Bind<ILogger<ResilientPolicyFactory>>().ToConstant(loggerFactory.CreateLogger<ResilientPolicyFactory>());

            var logger = kernel.Get<ILogger<Authorisation>>();
            logger.LogInformation("Logger configured");

            //TODO: replace Client secret from web.config
            var identityOptions = ConfigurationLoader.GetOptions<XeroIdentityClientOptions>("Identity", opt =>
            {
                opt.ClientId = "xero_authorisation_sample-app";
                opt.ClientSecret = "secret";
                opt.Authority = "https://identity-stage.xero-test.com";
                opt.TokenEndpoint = "https://identity-stage.xero-test.com/connect/token";
                opt.Scopes = new[] { "xero_authorisation.pbac" };
            });

            ConfigureIdentityClient(identityOptions, loggerFactory, kernel);

            //TODO: work how what is the client information
            var clientDetails = new ClientDetails(identityOptions.ClientId, "1.0.0", "56cb280becf8d1c29c3978dcb9c624459bcf7a7c");

            ConfigureAuthorisation(clientDetails, kernel);

            ConfigureAuthorisationMetrics(loggerFactory, kernel);
        }

        static void ConfigureIdentityClient(XeroIdentityClientOptions identityOptions, ILoggerFactory loggerFactory, IKernel kernel)
        {
            kernel.Bind<IOptions<XeroIdentityClientOptions>>().ToConstant(Options.Create<XeroIdentityClientOptions>(identityOptions));
            kernel.Bind<IClientTokenManager>().To<ClientCredentialsClientTokenManager>().InSingletonScope();

            // registry dependencies for CacheTokenManager
            //BindOptionsFromConfigurations<MemoryCacheOptions>("MemoryCache");
            kernel.Bind<IOptions<CachingTokenManagerOptions>>().ToConstant(Options.Create<CachingTokenManagerOptions>(new CachingTokenManagerOptions()));
            kernel.Bind<IOptions<MemoryCacheOptions>>().ToConstant(new MemoryCacheOptions());
            kernel.Bind<IMemoryCache>().To<MemoryCache>().InSingletonScope();
            kernel.Bind<IClientTokenCache>().To<MemoryTokenCache>().InSingletonScope();
            kernel.Bind<Xero.Metrics.IMetrics>().To<NullMetrics>().InSingletonScope();
            // BindOptionsFromConfigurations<CachingTokenManagerOptions>("CachingTokenManager");

            // HACK: Create CachingTokenManager using reflection - this is only necessary until CachingTokenManager is made public in the Identity SDK
            kernel.Bind<ICachingTokenManager>().ToMethod(ctx =>
            {
                var type = typeof(ICachingTokenManager).Assembly.GetType("Xero.Identity.Integration.Client.CachingTokenManager");
                var clientTokenManger = ctx.Kernel.Get<IClientTokenManager>();
                var clientTokenCache = ctx.Kernel.Get<IClientTokenCache>();
                var metrics = ctx.Kernel.Get<Xero.Metrics.IMetrics>();
                var logger = typeof(Logger<>).MakeGenericType(type).GetConstructor(new Type[] { typeof(ILoggerFactory) }).Invoke(new object[] { loggerFactory });
                var options = ctx.Kernel.Get<IOptions<CachingTokenManagerOptions>>();
                var constructor = type.GetConstructor(new Type[] { typeof(IClientTokenManager), typeof(IClientTokenCache), typeof(Xero.Metrics.IMetrics), typeof(ILogger<>).MakeGenericType(type), typeof(IOptions<CachingTokenManagerOptions>) });
                return (ICachingTokenManager)constructor.Invoke(new object[] { clientTokenManger, clientTokenCache, metrics, logger, options });
            }).InSingletonScope();
        }

        static void ConfigureAuthorisation(ClientDetails clientDetails, IKernel kernel)
        {
            //TODO: read the settings host from web.config
            var authorisationOptions = ConfigurationLoader.GetOptions<AuthorisationOptions>("Authorisation", opt =>
            {
                opt.AuthorisationServiceHost = "http://127.0.0.1:50051/";
                opt.LaunchDarkly = new LaunchDarklyOptions
                {
                    Flags = new Dictionary<string, string>
                    {
                        {"bills", "enable-new-authorisation-service-for-bills"},
                        {"invoices", "enable-new-authorisation-service-for-invoices"},
                        {"quotes", "enable-new-authorisation-service-for-quotes"},
                        {"purchase_orders", "enable-new-authorisation-service-for-purchase-orders"},
                        {"multicurrency", "enable-new-authorisation-service-for-multicurrency"}
                    }
                };
            });

            kernel.Bind<ClientDetails>().ToConstant(clientDetails);

            var uri = authorisationOptions.GetAuthorisationHost();
            var authClient = AuthorisationServiceClientFactory.CreateFromUri(uri);
            kernel.Bind<AuthorisationService.AuthorisationServiceClient>().ToConstant(authClient);

            kernel.Bind<IContextProvider>().To<ContextProvider>().InRequestScope();

            kernel.Bind<ILoggerAdapter>().To<LoggerAdapter>().InRequestScope();
            kernel.Bind<IAuthorisation>().To<Authorisation>().WhenInjectedInto<SideBySideService>().WithConstructorArgument("authorisationProvider", c =>
            {
                return c.Kernel.Get<AuthorisationProviderWithComparison>();
            });

            kernel.Bind<IAuthorisation>().To<Authorisation>().WhenInjectedInto<AuthOnly>().WithConstructorArgument("authorisationProvider", c =>
                {
                    return c.Kernel.Get<ServiceAuthorisationProvider>();
                });

            kernel.Bind<IOptions<AuthorisationOptions>>().ToConstant(Options.Create(authorisationOptions));
            kernel.Bind<IResilientPolicyFactory>().To<ResilientPolicyFactory>().InSingletonScope();
            kernel.Bind<IResultPublisher>().To<AuthorisationComparisonResultPublisher>();
            kernel.Bind<ICachingContextProvider>().To<CachingContextProvider>().InRequestScope();

            kernel.Bind<IAuthorisationProvider>().To<AuthorisationProviderWithComparison>().WhenInjectedInto<SideBySideService>();
            kernel.Bind<IServiceAuthorisationProvider>().To<ServiceAuthorisationProvider>().InRequestScope();
            kernel.Bind<ILegacyAuthorisationProvider>().To<LegacyAuthorisationProvider>().InRequestScope();

            kernel.Bind<IAuthorisationProvider>().To<ServiceAuthorisationProvider>().WhenInjectedInto<AuthOnly>();

            kernel.Bind<IScientist>().To<Scientist>().InSingletonScope();
            ConfigureLaunchDarklyClient(kernel);
        }

        static void ConfigureLaunchDarklyClient(IKernel kernel)
        {
            kernel.Bind<ILdClient>().ToMethod((context) =>
            {
                var logger = context.Kernel.Get<ILogger<Authorisation>>();
                //logger.LogInformation("Configurate LaunchDarkly client");
                //try
                //{
                //    var parameterStoreClient = Kernel.Get<IParameterStoreClient>();
                //    var ssmKeyName = "/launchdarkly/platform/sdk.key";
                //    var parameters = parameterStoreClient.GetParameters(new List<string>() { ssmKeyName });

                //    var key = parameters.ContainsKey(ssmKeyName) ? parameters[ssmKeyName] : null;

                //    return new LdClient(key);
                //}
                //catch (Exception e)
                {
                    //logger.LogError(e, "Failed to setup LauchDarkly client");
                    var ldConfig = Configuration.Default("sdk-06279458-16da-47dc-b715-9a3c44b62fa3");
                    return new LdClient(ldConfig);
                }
            }).InSingletonScope();
        }

        static void ConfigureAuthorisationMetrics(ILoggerFactory loggerFactory, IKernel kernel)
        {
            kernel.Bind<ILogger<AuthorisationMetrics>>().ToConstant(loggerFactory.CreateLogger<AuthorisationMetrics>());
            kernel.Bind<IAuthorisationMetrics>().To<AuthorisationMetrics>().InSingletonScope();
        }

    }
}
