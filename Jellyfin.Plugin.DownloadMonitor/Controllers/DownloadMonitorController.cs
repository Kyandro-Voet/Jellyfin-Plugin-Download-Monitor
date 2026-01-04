using System;
using System.IO;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Plugin.DownloadMonitor.Controllers
{
    /// <summary>
    /// Controller for serving plugin resources and API endpoints.
    /// </summary>
    [ApiController]
    [AllowAnonymous]
    public class DownloadMonitorController : ControllerBase
    {
        private static readonly HttpClient HttpClient = new HttpClient();

        /// <summary>
        /// Serves the client-side script.
        /// </summary>
        /// <returns>The JavaScript file.</returns>
        [HttpGet("Plugins/DownloadMonitor/ClientScript")]
        [Produces("application/javascript")]
        public async Task<ActionResult> GetClientScript()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Jellyfin.Plugin.DownloadMonitor.Web.inject.js";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                return NotFound();
            }

            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync().ConfigureAwait(false);

            return Content(content, "application/javascript");
        }

        /// <summary>
        /// Serves the client-side script via the plugin GUID path.
        /// This matches the URL injected into index.html.
        /// </summary>
        /// <returns>The JavaScript file.</returns>
        [HttpGet("Plugins/4344669f-555b-4525-b86f-d66dbb9ae81b/inject.js")]
        [Produces("application/javascript")]
        public async Task<ActionResult> GetInjectScript()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Jellyfin.Plugin.DownloadMonitor.Web.inject.js";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                return NotFound();
            }

            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync().ConfigureAwait(false);

            return Content(content, "application/javascript");
        }

        /// <summary>
        /// Serves the downloads page HTML.
        /// </summary>
        /// <returns>The downloads HTML page.</returns>
        [HttpGet("Plugins/DownloadMonitor/Page")]
        [Produces("text/html")]
        public async Task<ActionResult> GetDownloadsPage()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = "Jellyfin.Plugin.DownloadMonitor.Web.downloads.html";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                return NotFound();
            }

            using var reader = new StreamReader(stream);
            var content = await reader.ReadToEndAsync().ConfigureAwait(false);

            return Content(content, "text/html");
        }

        /// <summary>
        /// Gets current download status from Radarr.
        /// </summary>
        /// <returns>JSON array of download items.</returns>
        [HttpGet("Plugins/DownloadMonitor/Downloads")]
        [Produces("application/json")]
        public async Task<ActionResult> GetDownloads()
        {
            var config = Plugin.Instance?.Configuration;
            if (config == null || string.IsNullOrEmpty(config.RadarrUrl) || string.IsNullOrEmpty(config.RadarrApiKey))
            {
                return Ok(new { records = Array.Empty<object>() });
            }

            var commandUrl = $"{config.RadarrUrl.TrimEnd('/')}/api/v3/command?apikey={config.RadarrApiKey}";
            try
            {
                var commandJson = "{\"name\":\"RefreshMonitoredDownloads\"}";
                var content = new StringContent(commandJson, Encoding.UTF8, "application/json");
                await HttpClient.PostAsync(commandUrl, content).ConfigureAwait(false);
            }
            catch (Exception)
            {
            }

            var requestUrl = $"{config.RadarrUrl.TrimEnd('/')}/api/v3/queue?apikey={config.RadarrApiKey}";

            try
            {
                var response = await HttpClient.GetAsync(requestUrl).ConfigureAwait(false);
                if (response.IsSuccessStatusCode)
                {
                    var jsonString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    return Content(jsonString, "application/json");
                }

                return StatusCode((int)response.StatusCode);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
