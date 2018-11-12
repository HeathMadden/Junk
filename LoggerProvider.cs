using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PureIP.Portal.Domain.Data;
using System;
using System.Collections.Generic;
using System.Text;

namespace PureIP.Portal.Services.Logging
{
    /// <summary>
    /// custom logger creator
    /// </summary>
    public class LoggerProvider : Microsoft.Extensions.Logging.ILoggerProvider, Domain.Services.ILoggerProvider
    {
        private readonly Func<string, LogLevel, bool> filter;
        private readonly ILoggingRepository loggingRepository;
        private readonly IHttpContextAccessor context;

        public LoggerProvider(Func<string, LogLevel, bool> filter, ILoggingRepository loggingRepository, IHttpContextAccessor context)
        {
            this.filter = filter;
            this.loggingRepository = loggingRepository;
            this.context = context;
        }
        public ILogger CreateLogger(string categoryName)
        {
            return new Logger(categoryName, filter, loggingRepository, context);
        }
        public void Dispose()
        {
        }
    }
}
