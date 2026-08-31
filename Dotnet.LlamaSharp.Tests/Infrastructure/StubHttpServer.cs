using System.Net;
using System.Net.Sockets;
using System.Text;

namespace Dotnet.LlamaSharp.Tests.Infrastructure
{
    /// <summary>
    /// Servidor HTTP en proceso, sin dependencias externas (solo BCL: <see cref="HttpListener"/>),
    /// para interceptar las llamadas REST directas al servidor Chroma en los tests.
    /// Reemplaza a WireMock.Net, que arrastraba de forma transitiva paquetes vulnerables
    /// (Scriban.Signed y Microsoft.OpenApi) que estos tests no usaban.
    ///
    /// Las peticiones se enrutan por path + método HTTP + un predicado opcional sobre el cuerpo.
    /// Gana el primer stub registrado que cumple las tres condiciones (orden de registro).
    /// </summary>
    public sealed class StubHttpServer : IDisposable
    {
        private sealed record Stub(
            string Path,
            string Method,
            Func<string?, bool>? BodyMatch,
            int StatusCode,
            string ResponseBody,
            string ContentType);

        private readonly HttpListener _listener = new();
        private readonly List<Stub> _stubs = new();
        private readonly CancellationTokenSource _cts = new();

        /// <summary>URL base del servidor, sin barra final (p. ej. http://localhost:53421).</summary>
        public string Url { get; }

        public StubHttpServer()
        {
            var port = GetFreeTcpPort();
            Url = $"http://localhost:{port}";
            _listener.Prefixes.Add($"http://localhost:{port}/");
            _listener.Start();
            _ = Task.Run(() => ListenLoopAsync(_cts.Token));
        }

        /// <summary>
        /// Registra un stub para peticiones POST a <paramref name="path"/>.
        /// <paramref name="bodyMatch"/> null hace match con cualquier cuerpo.
        /// </summary>
        public StubHttpServer StubPost(
            string path,
            Func<string?, bool>? bodyMatch,
            string responseBody,
            string contentType = "application/json",
            int statusCode = 200)
        {
            _stubs.Add(new Stub(path, "POST", bodyMatch, statusCode, responseBody, contentType));
            return this;
        }

        private async Task ListenLoopAsync(CancellationToken ct)
        {
            while (!ct.IsCancellationRequested)
            {
                HttpListenerContext ctx;
                try
                {
                    ctx = await _listener.GetContextAsync();
                }
                catch
                {
                    break; // listener detenido o dispuesto
                }

                _ = Task.Run(() => HandleAsync(ctx));
            }
        }

        private async Task HandleAsync(HttpListenerContext ctx)
        {
            try
            {
                string body;
                using (var reader = new StreamReader(ctx.Request.InputStream, ctx.Request.ContentEncoding ?? Encoding.UTF8))
                {
                    body = await reader.ReadToEndAsync();
                }

                var path = ctx.Request.Url?.AbsolutePath ?? string.Empty;
                var method = ctx.Request.HttpMethod;

                var stub = _stubs.FirstOrDefault(s =>
                    s.Path == path &&
                    string.Equals(s.Method, method, StringComparison.OrdinalIgnoreCase) &&
                    (s.BodyMatch is null || s.BodyMatch(body)));

                if (stub is null)
                {
                    ctx.Response.StatusCode = 404;
                    ctx.Response.Close();
                    return;
                }

                var bytes = Encoding.UTF8.GetBytes(stub.ResponseBody);
                ctx.Response.StatusCode = stub.StatusCode;
                ctx.Response.ContentType = stub.ContentType;
                ctx.Response.ContentLength64 = bytes.Length;
                await ctx.Response.OutputStream.WriteAsync(bytes);
                ctx.Response.Close();
            }
            catch
            {
                try { ctx.Response.Abort(); } catch { /* la conexión ya no es utilizable */ }
            }
        }

        private static int GetFreeTcpPort()
        {
            var listener = new TcpListener(IPAddress.Loopback, 0);
            listener.Start();
            try
            {
                return ((IPEndPoint)listener.LocalEndpoint).Port;
            }
            finally
            {
                listener.Stop();
            }
        }

        public void Dispose()
        {
            try { _cts.Cancel(); } catch { /* ignore */ }
            try { if (_listener.IsListening) _listener.Stop(); } catch { /* ignore */ }
            try { _listener.Close(); } catch { /* ignore */ }
            _cts.Dispose();
        }
    }
}
