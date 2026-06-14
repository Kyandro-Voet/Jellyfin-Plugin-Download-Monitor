using System;
using System.Linq;
using System.Net.Http;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.DownloadMonitor.Helpers;
using MediaBrowser.Controller.Session;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.DownloadMonitor.Service
{
    /// <summary>
    /// Download status service that monitors Radarr downloads and broadcasts updates via WebSocket.
    /// </summary>
    public sealed class DownloadStatusService : IHostedService, IDisposable
    {
        private readonly ISessionManager _sessionManager;
        private readonly ILogger<DownloadStatusService> _logger;
        private readonly HttpClient _httpClient;
        private Timer? _timer;
        private string? _lastDataSent;

        /// <summary>
        /// Initializes a new instance of the <see cref="DownloadStatusService"/> class.
        /// </summary>
        /// <param name="sessionManager">The session manager for WebSocket communication.</param>
        /// <param name="logger">The logger.</param>
        public DownloadStatusService(
            ISessionManager sessionManager,
            ILogger<DownloadStatusService> logger)
        {
            _sessionManager = sessionManager;
            _logger = logger;
            _httpClient = new HttpClient();
        }

        /// <summary>
        /// Start the service.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("🚀 Download Monitor Service started!");

            // Register with File Transformation plugin
            RegisterWithFileTransformation();

            _timer = new Timer(OnTimerCallback, null, TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(10));
            return Task.CompletedTask;
        }

        private void RegisterWithFileTransformation()
        {
            try
            {
                _logger.LogInformation("[DownloadMonitor] Looking for File Transformation plugin...");

                // Find the File Transformation plugin assembly
                var ftAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Jellyfin.Plugin.FileTransformation");

                if (ftAssembly == null)
                {
                    _logger.LogWarning("[DownloadMonitor] File Transformation plugin not found. Please install it from: https://github.com/IAmParadox27/jellyfin-plugin-file-transformation");
                    return;
                }

                _logger.LogInformation("[DownloadMonitor] Found File Transformation plugin");

                // Find PluginInterface class with RegisterTransformation method
                var pluginInterfaceType = ftAssembly.GetType("Jellyfin.Plugin.FileTransformation.PluginInterface");
                if (pluginInterfaceType == null)
                {
                    _logger.LogError("[DownloadMonitor] Could not find PluginInterface type");
                    return;
                }

                var registerMethod = pluginInterfaceType.GetMethod("RegisterTransformation");
                if (registerMethod == null)
                {
                    _logger.LogError("[DownloadMonitor] Could not find RegisterTransformation method");
                    return;
                }

                // Find Newtonsoft.Json for creating JObject
                var newtonsoftAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "Newtonsoft.Json");

                if (newtonsoftAssembly == null)
                {
                    _logger.LogError("[DownloadMonitor] Newtonsoft.Json not found");
                    return;
                }

                var jObjectType = newtonsoftAssembly.GetType("Newtonsoft.Json.Linq.JObject");
                var parseMethod = jObjectType?.GetMethod("Parse", new[] { typeof(string) });

                if (parseMethod == null)
                {
                    _logger.LogError("[DownloadMonitor] Could not find JObject.Parse method");
                    return;
                }

                // Build registration JSON
                var callbackType = typeof(IndexHtmlTransformation);
                var transformationId = new Guid("d0a9de2a-3b1c-4e5f-8a7b-1c2d3e4f5a6b");

                // File Transformation uses Assembly.FullName for lookup
                var json = $@"{{
                    ""id"": ""{transformationId}"",
                    ""fileNamePattern"": ""index.html"",
                    ""callbackAssembly"": ""{callbackType.Assembly.FullName}"",
                    ""callbackClass"": ""{callbackType.FullName}"",
                    ""callbackMethod"": ""Transform""
                }}";

                _logger.LogInformation(
                    "[DownloadMonitor] Registering with assembly: {Assembly}, class: {Class}",
                    callbackType.Assembly.FullName,
                    callbackType.FullName);

                var jObject = parseMethod.Invoke(null, new object[] { json });

                // Call RegisterTransformation
                registerMethod.Invoke(null, new[] { jObject });

                _logger.LogInformation("[DownloadMonitor] ✅ Successfully registered index.html transformation with File Transformation plugin");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[DownloadMonitor] Failed to register with File Transformation plugin: {Message}", ex.Message);
            }
        }

        /// <summary>
        /// Stop the service.
        /// </summary>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("⏹️ Download Monitor Service stopping...");

            // Stop the timer completely during shutdown
            if (_timer != null)
            {
                _timer.Change(Timeout.Infinite, 0);
                await _timer.DisposeAsync().ConfigureAwait(false);
                _timer = null;
            }
        }

        private async void OnTimerCallback(object? state)
        {
            try
            {
                await CheckDownloads().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in download check loop");
            }
        }

        private async Task CheckDownloads()
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null)
            {
                return;
            }

            var hasRadarr = !string.IsNullOrEmpty(config.RadarrUrl) && !string.IsNullOrEmpty(config.RadarrApiKey);
            var hasSonarr = !string.IsNullOrEmpty(config.SonarrUrl) && !string.IsNullOrEmpty(config.SonarrApiKey);

            if (!hasRadarr && !hasSonarr)
            {
                return;
            }

            // Update timer interval based on user setting
            if (_timer != null)
            {
                _timer.Change(TimeSpan.FromSeconds(config.RefreshInterval), TimeSpan.FromSeconds(config.RefreshInterval));
            }

            var combinedRecords = new JsonArray();

            // Fetch Radarr downloads
            if (hasRadarr)
            {
                var requestUrl = $"{config.RadarrUrl.TrimEnd('/')}/api/v3/queue?apikey={config.RadarrApiKey}";
                try
                {
                    var response = await _httpClient.GetAsync(requestUrl).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var node = JsonNode.Parse(jsonString);
                        var records = node?["records"]?.AsArray();
                        if (records != null)
                        {
                            foreach (var record in records)
                            {
                                if (record != null)
                                {
                                    var recordCopy = JsonNode.Parse(record.ToJsonString());
                                    if (recordCopy != null)
                                    {
                                        recordCopy["mediaType"] = "movie";
                                        combinedRecords.Add(recordCopy);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Could not reach Radarr in background service: {Message}", ex.Message);
                }
            }

            // Fetch Sonarr downloads
            if (hasSonarr)
            {
                var requestUrl = $"{config.SonarrUrl.TrimEnd('/')}/api/v3/queue?apikey={config.SonarrApiKey}";
                try
                {
                    var response = await _httpClient.GetAsync(requestUrl).ConfigureAwait(false);
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        var node = JsonNode.Parse(jsonString);
                        var records = node?["records"]?.AsArray();
                        if (records != null)
                        {
                            foreach (var record in records)
                            {
                                if (record != null)
                                {
                                    var recordCopy = JsonNode.Parse(record.ToJsonString());
                                    if (recordCopy != null)
                                    {
                                        recordCopy["mediaType"] = "series";
                                        combinedRecords.Add(recordCopy);
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Could not reach Sonarr in background service: {Message}", ex.Message);
                }
            }

            var resultObject = new JsonObject
            {
                ["records"] = combinedRecords
            };

            var jsonStringToSend = resultObject.ToJsonString();

            // Only send WebSocket message if data has actually changed
            // This prevents spam and reduces KeepAlive message frequency
            if (!string.IsNullOrWhiteSpace(jsonStringToSend) && jsonStringToSend != _lastDataSent)
            {
                _lastDataSent = jsonStringToSend;

                // Send combined JSON to the frontend via WebSocket
                await _sessionManager.SendMessageToAdminSessions(
                    MediaBrowser.Model.Session.SessionMessageType.UserDataChanged,
                    new { MessageType = "DownloadStatusUpdate", Provider = "Combined", Data = jsonStringToSend },
                    cancellationToken: default).ConfigureAwait(false);
            }
        }

        /// <summary>
        /// Disposes the service and cleans up resources.
        /// </summary>
        public void Dispose()
        {
            _logger.LogDebug("🧹 Disposing Download Monitor Service...");

            // Dispose timer first to stop callbacks
            if (_timer != null)
            {
                _timer.Dispose();
                _timer = null;
            }

            // Then dispose HttpClient
            _httpClient?.Dispose();

            _logger.LogInformation("✅ Download Monitor Service disposed cleanly");
        }
    }
}
