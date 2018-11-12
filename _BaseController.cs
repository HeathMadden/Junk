using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;


namespace PureIP.Portal.Customer.Controllers
{
    public abstract class BaseController : Controller
    {
        private readonly IServiceProvider _container;
        private readonly Lazy<ILogger> _logger;
        private readonly Lazy<IMemoryCache> _memoryCache;

        protected ILogger logger => _logger.Value;
        private IMemoryCache memoryCache => _memoryCache.Value;
        private static CancellationTokenSource _resetCacheToken = new CancellationTokenSource();

        public BaseController(IServiceProvider container)
        {
            _container = container;
            var logType = typeof(ILogger<>).MakeGenericType(this.GetType());
            _logger = new Lazy<ILogger>(() => _container.GetService(logType) as ILogger);
            _memoryCache = new Lazy<IMemoryCache>(() => _container.GetService<IMemoryCache>());
        }

        protected T GetCached<T>(Func<T> create, string key = null)
        {
            var options = new MemoryCacheEntryOptions();
            options.SetSlidingExpiration(TimeSpan.FromMinutes(60));
            options.AddExpirationToken(new CancellationChangeToken(_resetCacheToken.Token));
            return memoryCache.GetOrCreate(key ?? typeof(T).ToString(), entry =>
            {
                entry.SetOptions(options);
                return create.Invoke();
            });
        }

        protected void ClearCached<T>(string key = null)
        {
            memoryCache.Remove(key ?? typeof(T).ToString());
        }

        protected void ResetCache()
        {
            if (_resetCacheToken != null && !_resetCacheToken.IsCancellationRequested && _resetCacheToken.Token.CanBeCanceled)
            {
                _resetCacheToken.Cancel();
                _resetCacheToken.Dispose();
            }

            _resetCacheToken = new CancellationTokenSource();
        }
    }
}
