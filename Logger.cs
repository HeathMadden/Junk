using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using PureIP.Portal.Domain.Data;
using PureIP.Portal.Domain.Models.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace PureIP.Portal.Services.Logging
{
    /// <summary>
    /// log to repository
    /// </summary>
    public class Logger : ILogger
    {
        private readonly string categoryName;
        private readonly Func<string, LogLevel, bool> filter;
        private readonly ILoggingRepository loggingRepository;
        private const int MessageMaxLength = 4000;
        private readonly IHttpContextAccessor context;

        public Logger(string categoryName, Func<string, LogLevel, bool> filter, ILoggingRepository loggingRepository, IHttpContextAccessor context)
        {
            this.categoryName = categoryName;
            this.filter = filter;
            this.loggingRepository = loggingRepository;
            this.context = context;
        }
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter)
        {
            if (!IsEnabled(logLevel))
            {
                return;
            }

            if (formatter == null)
            {
                throw new ArgumentNullException(nameof(formatter));
            }
            var message = formatter(state, exception);
            if (string.IsNullOrEmpty(message))
            {
                return;
            }
            if (exception != null)
            {
                message += "\n" + exception.ToString();
            }
            message = message.Length > MessageMaxLength ? message.Substring(0, MessageMaxLength) : message;
            Log log = new Log
            {
                Level = logLevel,
                Logger = categoryName,
                Exception = $"{exception?.Message} {exception?.InnerException?.Message} {exception?.InnerException?.InnerException?.Message}",
                Message = message,
                EventId = eventId.Id,
                CreatedTime = DateTime.UtcNow,
                UserId = context?.HttpContext?.User.Id() ?? Guid.Empty,
                User = context?.HttpContext?.User.Name()
            };
            loggingRepository.AddLog(log);
        }
        public bool IsEnabled(LogLevel logLevel)
        {
            return (filter == null || filter(categoryName, logLevel));
        }
        public IDisposable BeginScope<TState>(TState state)
        {
            return null;
        }
    }
}
