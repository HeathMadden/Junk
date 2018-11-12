using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PureIP.Portal.Domain.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace PureIP.Portal.Data
{
    public class ServiceConfigurator : IServiceConfigurator
    {
        private readonly Security.SecurityContext securityContext;
        private readonly Logging.LoggingContext loggingContext;
        private readonly Territory.TerritoryContext territoryContext;
        private readonly Product.ProductContext productContext;
        private readonly Number.NumberContext numberContext;
        private readonly Quote.QuoteContext quoteContext;
        private readonly Customer.CustomerContext customerContext;

        public ServiceConfigurator(Security.SecurityContext securityContext,
            Logging.LoggingContext loggingContext,
            Territory.TerritoryContext territoryContext,
            Product.ProductContext productContext,
            Number.NumberContext numberContext,
            Quote.QuoteContext quoteContext,
            Customer.CustomerContext customerContext)
        {
            this.securityContext = securityContext;
            this.loggingContext = loggingContext;
            this.territoryContext = territoryContext;
            this.productContext = productContext;
            this.numberContext = numberContext;
            this.quoteContext = quoteContext;
            this.customerContext = customerContext;
        }

        public void Migrate()
        {
            securityContext.Database.Migrate();
            loggingContext.Database.Migrate();
            territoryContext.Database.Migrate();
            productContext.Database.Migrate();
            numberContext.Database.Migrate();
            quoteContext.Database.Migrate();
            customerContext.Database.Migrate();
        }

        public static void SetDatabaseConfiguration(IServiceCollection services, string portalConnection, string cdrConnection, string customerConnection, string numberConnection)
        {
            services.AddDbContext<Security.SecurityContext>(options => options.UseSqlServer(portalConnection));
            services.AddDbContext<Logging.LoggingContext>(options => options.UseSqlServer(portalConnection), ServiceLifetime.Singleton);
            services.AddDbContext<Territory.TerritoryContext>(options => options.UseSqlServer(portalConnection));
            services.AddDbContext<Product.ProductContext>(options => options.UseSqlServer(portalConnection));
            services.AddDbContext<Number.NumberContext>(options => options.UseSqlServer(portalConnection));
            services.AddDbContext<Quote.QuoteContext>(options => options.UseSqlServer(portalConnection));
            services.AddDbContext<Customer.CustomerContext>(options => options.UseSqlServer(portalConnection));

            services.AddDbContext<CallDetails.CallDetailsContext>(options => options.UseSqlServer(cdrConnection));
            services.AddDbContext<PortalCustomer.PortalCustomerContext>(options => options.UseSqlServer(customerConnection));
            services.AddDbContext<PortalNumber.PortalNumberContext>(options => options.UseSqlServer(numberConnection));
        }

        public static void SetDependency(IServiceCollection services)
        {
            services.AddTransient<IServiceConfigurator, ServiceConfigurator>();

            //todo: move out
            services.AddSingleton<IEmailAgentConfiguration>(new Email.EmailAgentConfiguration()
            {
                Server = "smtp.office365.com",
                UserName = "alerts@pure-ip.com",
                Password = "Will witty novel dankly waft3!",
                MailBox = "alerts@pure-ip.com",
                SendEmailFlag = false
            });
            services.AddTransient<IEmailAgent, Email.EmailAgent>();
            services.AddTransient<ISecurityRepository, Security.SecurityRepository>();
            services.AddSingleton<ILoggingRepository, Logging.LoggingRepository>();

            services.AddTransient<IProductRepository, Product.ProductRepository>();
            services.AddTransient<ITerritoryRepository, Territory.TerritoryRepository>();
            services.AddTransient<INumberRepository, Number.NumberRepository>();
            services.AddTransient<IQuoteRepository, Quote.QuoteRepository>();
            services.AddTransient<ICustomerRepository, Customer.CustomerRepository>();

            services.AddTransient<ICallDetailsRepository, CallDetails.CallDetailsRepository>();
            services.AddTransient<IPortalCustomerRepository, PortalCustomer.PortalCustomerRepository>();
            services.AddTransient<IPortalNumberRepository, PortalNumber.PortalNumberRepository>();
        }
    }
}
