namespace Examples.Mobile
{
    using System.Net.Http;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// The network message handler that collect performance measures.
    /// </summary>
    public class PerformanceLoggerHandler : DelegatingHandler
    {
        /// <summary>
        /// Record timing for all requests.
        /// </summary>
        /// <param name="request">The request.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The web response.</returns>
        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            using (new PerfTracer(request.RequestUri.AbsoluteUri))
            {
                return await base.SendAsync(request, cancellationToken);
            }
        }
    }
}
