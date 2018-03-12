**Update Checker** automatically checks all supported Oxide plugins for updates on server start or on command. This is useful to see if you are using an outdated, older versions of plugins. It will also automatically check for updates every hour, or at the time configured. Update Checker also supports the Email API, DiscordMessages API, and Push API plugins for instant notifications.

## Permissions

- **updatechecker.use** -- Allows player to run the update check

## Commands

- **updates** -- Triggers the plugin update checking sequence.

## Configuration

```json
{
  "Settings": {
    "Auto Check Interval (in Minutes)": 60.0,
    "Use EmailAPI": false,
    "Use PushAPI": false,
    "Discord Webhook": "",
    "Use DiscordMessages": true,
  }
}
```

The configuration file will update automatically if new options are added or removed. I'll do my best to preserve any existing settings and messages with each new version.

## Localization

The default messages are in the `UpdateChecker.json` file under the `oxide/lang/en directory`. To add support for another language, create a new language folder (ex. de for German) if not already created, copy the default language file to the new folder, and then customize the messages.
{bold}, {italic} and {underline} tags may be used for discord formatting; for other message targets, they are unsupported and will stripped out from the response.

```json
{
  "Checking v2": "Checking for updates... This may take a few seconds. Please be patient.",
  "Outdated Plugin List v2": "{bold}The following plugins are outdated:{bold}",
  "Outdated Plugin Info Title v2": "# {bold}{title}{bold}",
  "Outdated Plugin Info Body v2": "Installed: {bold}{installed}{bold} - Latest: {bold}{latest}{bold} | {url}",
  "All Checked Plugins Up To Date v2": "{bold}All checked plugins are up to date.{bold}",
  "Failure Plugin List v2": "{bold}The following plugins could not be checked for the following reasons:{bold}",
  "Missing ResourceId v2": "{bold}Missing resource id:{bold}",
  "Resource Unavailable v2": "{bold}Resource unavailable:{bold}",
  "Resource Details Unavailable v2": "{bold}Invalid version name:{bold}",
  "No Failures v2": "{bold}All Addons checked successfully{bold}"
}
```

## Plugin Developers

To add Update Checker support in your plugin if your plugin is still hosted on **oxidemod.org**, add the ResourceId variable and your plugin's ID from its URL, otherwise support will be handled automatically if your plugin is on **umod.org**.

**Example:** http://oxidemod.org/plugins/updatechecker.681/

### C# Example
```csharp
namespace Oxide.Plugins
{
    [Info("Title of Plugin", "Your Name", 0.1, ResourceId = 681)]
    [Description("This is what the plugin does")]
    public class PluginName : RustPlugin
    {
        // This is where your plugin will do its magic
    }
}
```

### JavaScript Example
```javascript
var PluginName = {
    Title : "Title of Plugin",
    Description : "This is what the plugin does",
    Author : "Your Name",
    Version : V(0, 1, 0),
    ResourceId : 681

    // This is where your plugin will do its magic
}
```

### Lua Example
```lua
PLUGIN.Title = "Title of Plugin"
PLUGIN.Description = "This is what the plugin does"
PLUGIN.Author = "Your Name"
PLUGIN.Version = V(0, 1, 0)
PLUGIN.ResourceId = 681

-- This is where your plugin will do its magic
```

### Python Example
```python
class PluginName:
    def __init__(self):
        self.Title = "Title of Plugin"
        self.Description = "This is what the plugin does"
        self.Author = "Your Name"
        self.Version = V(0, 1, 0)
        self.ResourceId = 681

    # This is where your plugin will do its magic
```
