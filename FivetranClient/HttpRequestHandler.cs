using System.Net;
using FivetranClient.Infrastructure;

namespace FivetranClient;

public class HttpRequestHandler
{
    private readonly HttpClient _client;
    private readonly SemaphoreSlim? _semaphore;
    private readonly object _lock = new();
    private DateTime _retryAfterTime = DateTime.UtcNow;
    private static TtlDictionary<string, HttpResponseMessage> _responseCache = new();

    /// <summary>
    /// Handles HttpTooManyRequests responses by limiting the number of concurrent requests and managing retry logic.
    /// Also caches responses to avoid unnecessary network calls.
    /// </summary>
    /// <remarks>
    /// Set <paramref name="maxConcurrentRequests"/> to 0 to disable concurrency limit.
    /// </remarks>
    public HttpRequestHandler(HttpClient client, ushort maxConcurrentRequests = 0)
    {
        this._client = client;
        if (maxConcurrentRequests > 0)
        {
            this._semaphore = new SemaphoreSlim(maxConcurrentRequests, maxConcurrentRequests);
        }
    }

    public async Task<HttpResponseMessage> GetAsync(string url, CancellationToken cancellationToken)
    {
        return await _responseCache.GetOrAdd(
            url,
            () => this._GetAsyncAcquireSemaphore(url, cancellationToken).Result,
            TimeSpan.FromMinutes(60));
    }

    private async Task<HttpResponseMessage> _GetAsyncAcquireSemaphore(string url, CancellationToken cancellationToken)
    {
        if (this._semaphore is not null)
        {
            await this._semaphore.WaitAsync(cancellationToken);
        }
        try
        {
            return await _GetAsync(url, cancellationToken);

        }
        finally
        {
            this._semaphore?.Release();
        }
    }

    private async Task<HttpResponseMessage> _GetAsync(string url, CancellationToken cancellationToken)
    {
        

        
            TimeSpan timeToWait;
            lock (this._lock)
            {
                timeToWait = this._retryAfterTime - DateTime.UtcNow;
            }

            if (timeToWait > TimeSpan.Zero)
            {
                await Task.Delay(timeToWait, cancellationToken);
            }

            cancellationToken.ThrowIfCancellationRequested();

            var response = await this._client.GetAsync(new Uri(url, UriKind.Relative), cancellationToken);
            if (response.StatusCode is HttpStatusCode.TooManyRequests)
            {
                var retryAfter = response.Headers.RetryAfter?.Delta ?? TimeSpan.FromSeconds(60);

                lock (this._lock)
                {
                    this._retryAfterTime = DateTime.UtcNow.Add(retryAfter);
                }

                // new request will wait for the specified time before retrying
                return await this._GetAsync(url, cancellationToken);
            }
            else
            {
                response.EnsureSuccessStatusCode();
            }
            return response;
        
    }
}