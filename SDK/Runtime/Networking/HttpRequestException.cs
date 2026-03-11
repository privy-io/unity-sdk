using System;

namespace Privy.Internal.Networking
{
    /// <summary>
    /// Exception thrown when an HTTP request made by <see cref="IHttpRequestHandler"/> fails.
    /// Contains the HTTP status code (when available) and the raw response body.
    /// </summary>
    internal class HttpRequestException : Exception
    {
        /// <summary>
        /// HTTP response status code (e.g. 422, 404). -1 if unavailable.
        /// </summary>
        public int StatusCode { get; }

        /// <summary>
        /// Raw response body returned by the server, if any.
        /// </summary>
        public string ResponseBody { get; }

        public HttpRequestException(string message, int statusCode = -1, string responseBody = null)
            : base(message)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }

        public HttpRequestException(string message, Exception inner, int statusCode = -1, string responseBody = null)
            : base(message, inner)
        {
            StatusCode = statusCode;
            ResponseBody = responseBody;
        }
    }
}
