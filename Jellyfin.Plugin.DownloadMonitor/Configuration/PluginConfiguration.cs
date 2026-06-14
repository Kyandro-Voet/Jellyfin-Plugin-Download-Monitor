using MediaBrowser.Model.Plugins;

namespace Jellyfin.Plugin.DownloadMonitor.Configuration
{
    /// <summary>
    /// Plugin configuration.
    /// </summary>
    public class PluginConfiguration : BasePluginConfiguration
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PluginConfiguration"/> class.
        /// </summary>
        public PluginConfiguration()
        {
            // Standaardwaarden
            RadarrUrl = "http://localhost:7878";
            RadarrApiKey = string.Empty;
            SonarrUrl = "http://localhost:8989";
            SonarrApiKey = string.Empty;
            RefreshInterval = 2; // Seconden
        }

        /// <summary>
        /// Gets or sets the Radarr URL.
        /// </summary>
        public string RadarrUrl { get; set; }

        /// <summary>
        /// Gets or sets the Radarr API key.
        /// </summary>
        public string RadarrApiKey { get; set; }

        /// <summary>
        /// Gets or sets the Sonarr URL.
        /// </summary>
        public string SonarrUrl { get; set; }

        /// <summary>
        /// Gets or sets the Sonarr API key.
        /// </summary>
        public string SonarrApiKey { get; set; }

        /// <summary>
        /// Gets or sets the refresh interval in seconds.
        /// </summary>
        public int RefreshInterval { get; set; }
    }
}
