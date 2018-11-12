using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Text;
using System.Threading;

namespace PureIP.Portal.Services
{
    public abstract class BaseService<TRepository>
    {
        private readonly IServiceProvider _container;
        private readonly Lazy<ILogger> _logger;
        private readonly Lazy<TRepository> _repository;
        private readonly Lazy<HttpContext> _httpContext;
        private readonly Lazy<IMemoryCache> _memoryCache;
        private IMemoryCache memoryCache => _memoryCache.Value;
        private static CancellationTokenSource _resetCacheToken = new CancellationTokenSource();
        
        protected ILogger logger => _logger.Value;
        protected TRepository repository => _repository.Value;
        protected ClaimsPrincipal user => _httpContext.Value.User;

        public BaseService(IServiceProvider container)
        {
            _container = container;
            _repository = new Lazy<TRepository>(() => _container.GetService<TRepository>());
            var logType = typeof(ILogger<>).MakeGenericType(this.GetType());
            _logger = new Lazy<ILogger>(() => _container.GetService(logType) as ILogger);
            _httpContext = new Lazy<HttpContext>(() => _container.GetService<IHttpContextAccessor>().HttpContext);
            _memoryCache = new Lazy<IMemoryCache>(() => _container.GetService<IMemoryCache>());
        }

        protected T GetCached<T>(Func<T> create, string key = null)
        {
            var options = new MemoryCacheEntryOptions();
            options.SetSlidingExpiration(TimeSpan.FromMinutes(60));
            options.AddExpirationToken(new CancellationChangeToken(_resetCacheToken.Token));
            return memoryCache.GetOrCreate(key ?? typeof(T).ToString(), entry =>
            {
                try { entry.SetOptions(options); } catch { }//moq catch
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
