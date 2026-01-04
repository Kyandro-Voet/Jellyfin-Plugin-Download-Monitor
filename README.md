# Jellyfin Download Monitor Plugin

A Jellyfin plugin that adds a **Downloads** menu item to the sidebar, allowing you to monitor active downloads from Radarr/Sonarr directly within the Jellyfin web interface.

![Downloads Menu](https://img.shields.io/badge/Jellyfin-10.11+-blue)
![License](https://img.shields.io/badge/License-GPLv3-green)

## Features

- 📥 **Downloads sidebar menu** - Quick access to download status
- 🎬 **Radarr/Sonarr integration** - Monitor movie and TV downloads in real-time
- 🔄 **Auto-refresh** - Configurable refresh interval
- 🎨 **Native Jellyfin styling** - Blends seamlessly with the UI
- 🔌 **WebSocket updates** - Real-time status updates without page refresh
- ⚡ **Smart caching** - Only sends updates when data changes to reduce network traffic

## Requirements

- Jellyfin 10.11.0 or newer
- Radarr or Sonarr with API access
- **[Jellyfin Plugin File Transformation](https://github.com/IAmParadox27/jellyfin-plugin-file-transformation)** (recommended for automatic script injection)

## Installation

### Step 1: Install File Transformation Plugin (Recommended) and Download Monitor Plugin

For automatic script injection, first install the File Transformation plugin:

1. Go to **Dashboard** → **Plugins** → **Manage Repositories** → **New Repository**
2. Add https://www.iamparadox.dev/jellyfin/plugins/manifest.json and https://kyandro-voet.github.io/Jellyfin-Plugin-Download-Monitor/manifest.json
3. Search and install "File Transformation" in the catalog
4. Search and install "Download Monitor" in the catalog
5. Restart Jellyfin

## Configuration

1. Navigate to the **Downloads** menu item in the sidebar
2. Or go to **Dashboard** → **Plugins** → **Download Monitor**
3. Enter your Radarr/Sonarr settings:
   - **URL**: e.g., `http://192.168.1.50:7878`
   - **API Key**: Found in Radarr/Sonarr under Settings → General
   - **Refresh Interval**: How often to check for updates (in seconds, default: 2)
4. Click **Save**
5. The downloads page will now display active downloads

## License

This project is licensed under the GNU General Public License v3.0 - see the [LICENSE](LICENSE) file for details.

## Acknowledgments

- [Jellyfin](https://jellyfin.org/) - The Free Software Media System
- [Radarr](https://radarr.video/) - Movie collection manager
