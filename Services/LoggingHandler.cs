using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace EHMR.Services
{
    public class LoggingHandler : DelegatingHandler
    {
        private readonly ILogger<LoggingHandler> _logger;

        public LoggingHandler(ILogger<LoggingHandler> logger)
        {
            _logger=logger;
        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var start = DateTime.UtcNow;

            _logger.LogInformation("HTTP {Method} {Url} started",
                request.Method, request.RequestUri);

            var response = await base.SendAsync(request, cancellationToken);

            var duration = DateTime.UtcNow-start;

            _logger.LogInformation("HTTP {Method} {Url} completed in {Duration}ms with {StatusCode}",
                request.Method,
                request.RequestUri,
                duration.TotalMilliseconds,
                (int)response.StatusCode);

            return response;
        }
    }
}