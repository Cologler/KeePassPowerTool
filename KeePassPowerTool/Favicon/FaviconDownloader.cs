using KeePassLib;
using KeePassLib.Interfaces;
using KeePassLib.Serialization;
using KeePassPowerTool.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KeePassPowerTool.Favicon
{
    class FaviconDownloader : IDisposable
    {
        private const int MaxThreadCount = 8;
        private const string UserAgent =
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.110 Safari/537.36";
        private const int MaxFaviconSize = 1024 * 1024 * 2;

        private readonly FaviconComponent component;
        private readonly Queue<PwEntry> items = new Queue<PwEntry>();
        private readonly CancellationTokenSource cts = new CancellationTokenSource();
        private int total;
        private int threadCount;

        public event EventHandler<FaviconDownloader> Completed;

        public FaviconDownloader(FaviconComponent component)
        {
            this.component = component;
            var host = this.component.Root.Host;
            if (!host.Database.IsOpen) throw new InvalidOperationException();
            
            this.ConnectionInfo = host.Database.IOConnectionInfo;
            host.MainWindow.FileClosingPre += this.MainWindow_FileClosing;
        }

        public IOConnectionInfo ConnectionInfo { get; }

        private void MainWindow_FileClosing(object sender, KeePass.Forms.FileClosingEventArgs e)
        {
            this.cts.Cancel();
            this.items.Clear();
        }

        public void Enqueue(IEnumerable<PwEntry> items)
        {
            foreach (var item in items)
            {
                this.items.Enqueue(item);
            }
        }

        public void Start(IStatusLogger logger)
        {
            for (var i = 0; i < MaxThreadCount; i++)
            {
                this.StartCore(logger);
            }
        }

        public async void StartCore(IStatusLogger logger)
        {
            this.threadCount++;
            while (this.items.Count > 0)
            {
                var item = this.items.Dequeue();
                try
                {
                    await this.DownloadCoreAsync(item, this.cts.Token);
                }
                catch (OperationCanceledException)
                {
                    this.Dispose();
                    return;
                }
                this.total++;
                logger.SetProgress((uint)(this.total / (this.total + this.items.Count) * 100));
            }
            this.threadCount--;
            if (this.threadCount == 0)
            {
                this.Dispose();
                this.Completed?.Invoke(this, this);
            }
        }

        private async Task DownloadCoreAsync(PwEntry entry, CancellationToken token)
        {
            var url = entry.Strings.ReadSafe("URL");
            if (string.IsNullOrWhiteSpace(url))
            {
                return;
            }

            if (url.Contains("://")) // scheme
            {
                if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
                    !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    Debug.WriteLine($"<{entry.Uuid.ToHexString()}> invalid url: <{url}>.");
                    return;
                }
            }
            else
            {
                url = "http://" + url;
            }

            token.ThrowIfCancellationRequested();

            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
            {
                Debug.WriteLine($"<{entry.Uuid.ToHexString()}> invalid url: <{url}>.");
                return;
            }

            if (await this.ResolveFromHtmlAsync(entry, uri, token)) return;
            if (await this.ResolveFromFaviconAsync(entry, uri, token)) return;

            Debug.WriteLine($"cannot resolve for <{url}>.");
        }

        private async Task<bool> ResolveFromHtmlAsync(PwEntry entry, Uri uri, CancellationToken token)
        {
            var iconUrl = await Task.Run(async () =>
            {
                var request = CreateHttpRequest(uri);
                try
                {
                    string html = null;

                    using (token.Register(() => request.Abort(), useSynchronizationContext: false))
                    using (var response = await request.GetResponseAsync().ConfigureAwait(false))
                    {
                        token.ThrowIfCancellationRequested();
                        if (response.ContentLength > MaxFaviconSize) return null;
                        using (var inputStream = response.GetResponseStream())
                        {
                            using (var ms = new MemoryStream())
                            {
                                await inputStream.CopyToAsync(ms, 4096, token);
                                var buffer = ms.ToArray();
                                try
                                {
                                    html = Encoding.UTF8.GetString(buffer);
                                }
                                catch (DecoderFallbackException) { }
                                catch (ArgumentException) { }
                            }
                        }
                    }

                    token.ThrowIfCancellationRequested();

                    if (html != null)
                    {
                        var url = html.TryParseIconUrl();
                        if (url != null)
                        {
                            if (url.StartsWith("//")) return $"{uri.Scheme}:" + url;
                            else if (url.StartsWith("/")) return $"{uri.Scheme}://{uri.Host}{url}";
                            else return url;
                        }
                    }
                }
                catch (IOException) { }
                catch (WebException) { }

                return null;
            }, token);

            if (!Uri.TryCreate(iconUrl, UriKind.Absolute, out var iconUri)) return false;
            if (iconUri == null) return false;
            return await this.ResolveFromUriAsync(entry, uri, token);
        }

        private Task<bool> ResolveFromFaviconAsync(PwEntry entry, Uri uri, CancellationToken token)
        {
            var url = $"{uri.Scheme}://{uri.Host}/favicon.ico";
            uri = new Uri(url, UriKind.Absolute);
            return this.ResolveFromUriAsync(entry, uri, token);
        }

        private async Task<bool> ResolveFromUriAsync(PwEntry entry, Uri uri, CancellationToken token)
        {
            var buffer = await this.DownloadFileAsync(uri, token);
            if (buffer == null) return false;
            if (!this.CheckValid(buffer))
            {
                Debug.WriteLine($"<{uri.OriginalString}> invalid image.");
                return false;
            }
            token.ThrowIfCancellationRequested();
            var uuid = this.component.CustomIconAgent.TryAddIcon(buffer);
            if (uuid.Equals(entry.CustomIconUuid)) return true;
            entry.CustomIconUuid = uuid;
            entry.Touch(true);
            this.component.Root.Host.Database.UINeedsIconUpdate = true;
            this.component.Root.Host.MainWindow.UpdateUI(false, null, false, null, true, null, true);
            return true;
        }

        private static HttpWebRequest CreateHttpRequest(Uri uri)
        {
            var request = WebRequest.CreateHttp(uri);
            request.Method = WebRequestMethods.Http.Get;
            request.UserAgent = UserAgent;
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            request.Headers.Add(HttpRequestHeader.AcceptLanguage, "*");
            return request;
        }

        private async Task<byte[]> DownloadFileAsync(Uri uri, CancellationToken token)
        {
            var request = CreateHttpRequest(uri);

            try
            {
                using (token.Register(() => request.Abort(), useSynchronizationContext: false))
                using (var response = await request.GetResponseAsync().ConfigureAwait(false))
                {
                    token.ThrowIfCancellationRequested();
                    if (response.ContentLength > MaxFaviconSize) return null;
                    using (var inputStream = response.GetResponseStream())
                    {
                        using (var ms = new MemoryStream())
                        {
                            await inputStream.CopyToAsync(ms, 4096, token);
                            return ms.ToArray();
                        }
                    }
                }
            }
            catch (IOException) { }
            catch (WebException) { }

            token.ThrowIfCancellationRequested();
            return null;
        }

        private bool CheckValid(byte[] buffer)
        {
            if (buffer == null) return false;

            using (var ms = new MemoryStream(buffer))
            {
                try
                {
                    var icon = new Icon(ms);
                    return true;
                }
                catch (ArgumentException) { }
                catch (Win32Exception) { }

                try
                {
                    ms.Position = 0;
                    Image.FromStream(ms);
                    return true;
                }
                catch (ArgumentException) { }
            }

            return false;
        }

        public void Dispose()
        {
            this.cts.Dispose();
            this.items.Clear();
            if (!this.component.Root.IsTerminated)
            {
                this.component.Root.Host.MainWindow.FileClosingPre -= this.MainWindow_FileClosing;
            }
        }
    }
}
