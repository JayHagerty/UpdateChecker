using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries.Covalence;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using Version = Oxide.Core.VersionNumber;
namespace Oxide.Plugins
{
    [Info("UpdateChecker", "LaserHydra", "2.2.2", ResourceId = 681)]
    [Description("Checks for and notifies of any outdated plugins")]
    public sealed class UpdateChecker : CovalencePlugin
    {
        #region Fields
        private const string PluginInformationUrl = "http://oxide.laserhydra.com/plugins/{resourceId}/";
        private const int greenEmbed = 3329330;
        private const int redEmbed = 13447730;
        private const string bold = "{bold}";
        private const string italic = "{italic}";
        private const string underline = "{underline}";
        [PluginReference]
        private Plugin EmailAPI, PushAPI, DiscordMessages;
        #endregion
        #region Hooks
        private void Loaded()
        {
            LoadConfig();
            timer.Repeat(GetConfig(60f, "Settings", "Auto Check Interval (in Minutes)") * 60, 0, () => CheckForUpdates(null));
            CheckForUpdates(null);
        }
        #endregion

        #region Loading

        private new void LoadConfig()
        {
            SetConfig("Settings", "Auto Check Interval (in Minutes)", 60f);
            SetConfig("Settings", "Use PushAPI", false);
            SetConfig("Settings", "Use EmailAPI", false);
            SetConfig("Settings", "Use DiscordMessages", false);
            SetConfig("Settings", "Discord Webhook", "");
            SaveConfig();
        }

        private new void LoadDefaultMessages()
        {
            lang.RegisterMessages(new Dictionary<string, string>
            {
                {"Checking v2", "Checking for updates... This may take a few seconds. Please be patient."},
                {"Outdated Plugin List v2", "{bold}The following plugins are outdated:{bold}"},
                {"Outdated Plugin Info Title v2", "# {bold}{title}{bold}"},
                {"Outdated Plugin Info Body v2", "Installed: {bold}{installed}{bold} - Latest: {bold}{latest}{bold} | {url}"},
                {"All Checked Plugins Up To Date v2", "{bold}All checked plugins are up to date.{bold}"},
                {"Failure Plugin List v2", "{bold}The following plugins could not be checked for the following reasons:{bold}"},
                {"Missing ResourceId v2", "{bold}Missing resource id:{bold}"},
                {"Resource Unavailable v2", "{bold}Resource unavailable:{bold}"},
                {"Resource Details Unavailable v2", "{bold}Invalid version name:{bold}"},
                {"No Failures v2", "{bold}All Addons checked successfully{bold}"},
            }, this);
        }

        protected override void LoadDefaultConfig() => PrintWarning("Generating new config file...");

        #endregion

        #region Commands

        [Command("updates"), Permission("updatechecker.use")]
        private void CmdUpdates(IPlayer player, string cmd, string[] args)
        {
            SendMessage(player, new MessageWrapper(GetMsg("Checking v2", player.Id), greenEmbed));
            CheckForUpdates(player);
        }

        #endregion

        #region Notifications

        private void Notify(MessageWrapper messageWrapper)
        {
            if (GetConfig(false, "Settings", "Use PushAPI"))
               PushAPI?.Call("PushMessage", "Plugin Update Notification", messageWrapper.GetNoFormatting());

            if (GetConfig(false, "Settings", "Use EmailAPI"))
               EmailAPI?.Call("EmailMessage", "Plugin Update Notification", messageWrapper.GetNoFormatting());

            if (GetConfig(false, "Settings", "Use DiscordMessages") && !string.IsNullOrEmpty(GetConfig("", "Settings", "Discord Webhook"))) 
               DiscordMessages?.Call("API_SendFancyMessage", GetConfig("", "Settings", "Discord Webhook"), messageWrapper.GetDiscordTitle(), messageWrapper.GetDiscordMessages(), null, messageWrapper.GetColourEmbed());
        }

        private void SendMessage(IPlayer player, MessageWrapper messageWrapper)
        {
            if (player != null)
                player.Reply(messageWrapper.GetNoFormatting());
            else
                Notify(messageWrapper);
                PrintWarning(messageWrapper.GetNoFormatting());
        }

        #endregion

        #region Update Checks

        public void CheckForUpdates(IPlayer requestor)
        {
            var outdatedPlugins = new Dictionary<Plugin, ApiResponse.Data>();
            var failures = new Dictionary<string, List<Plugin>>
            {
                ["Missing ResourceId"] = new List<Plugin>(),
                ["Resource Unavailable"] = new List<Plugin>(),
                ["Resource Details Unavailable"] = new List<Plugin>()
            };

            var totalPlugins = plugins.GetAll().Length;
            var currentPlugin = 1;

            foreach (var plugin in plugins.GetAll())
            {
                if (plugin.IsCorePlugin)
                {
                    currentPlugin++;
                    continue;
                }

                if (plugin.ResourceId == 0)
                {
                    failures["Missing ResourceId"].Add(plugin);
                    currentPlugin++;
                    continue;
                }

                webrequest.Enqueue(PluginInformationUrl.Replace("{resourceId}", plugin.ResourceId.ToString()), null,
                    (code, response) =>
                    {
                        if (code != 200)
                        {
                            PrintWarning($"Failed to access plugin information api at {PluginInformationUrl.Replace("{resourceId}", plugin.ResourceId.ToString())}\nIf this keeps happening, please content the developer.");
                        }
                        else
                        {
                            var apiResponse = JsonConvert.DeserializeObject<ApiResponse>(response);
                            
                            if (!apiResponse.HasSucceeded && apiResponse.Error == "RESOURCE_NOT_AVAILABLE")
                            {
                                failures["Resource Unavailable"].Add(plugin);
                            }
                            // Version is null or empty; Unable to read version
                            else if (string.IsNullOrEmpty(apiResponse.PluginData.Version))
                            {
                                failures["Resource Details Unavailable"].Add(plugin);
                            }
                            else if (IsOutdated(plugin.Version, apiResponse.PluginData.Version))
                            {
                                outdatedPlugins.Add(plugin, apiResponse.PluginData);
                            }
                        }

                        // Reached last plugin
                        if (currentPlugin++ >= totalPlugins)
                        {
                            List<MessageBody> outdatedList = new List<MessageBody>();
                            foreach (var outdated in outdatedPlugins) 
                            {
                                outdatedList.Add(new MessageBody(GetMsg("Outdated Plugin Info Title v2").Replace("{title}", outdated.Key.Title),
                                    GetMsg("Outdated Plugin Info Body v2")
                                    .Replace("{installed}", outdated.Key.Version.ToString())
                                    .Replace("{latest}", outdated.Value.Version)
                                    .Replace("{url}", outdated.Value.Url)));
                            }

                            var outdatedPluginMesageWrapper = outdatedList.Count() > 0 ? new MessageWrapper(GetMsg("Outdated Plugin List v2"), redEmbed) : new MessageWrapper(GetMsg("All Checked Plugins Up To Date v2"), greenEmbed);
                            outdatedPluginMesageWrapper.AddMessageBodies(outdatedList);
                                                                           
                            SendMessage(requestor, outdatedPluginMesageWrapper); 
                             
                            List<MessageBody> failureList = new List<MessageBody>();
                            foreach (var failure in failures) 
                            {
                                if (failure.Value.Count > 0)
                                    failureList.Add(new MessageBody(GetMsg(failure.Key, requestor?.Id), failure.Value.Select(p => p.Name).ToSentence()));
                            }

                            var pluginUpdateCheckFailureReasonMessageWrapper = failureList.Count() > 0 ? new MessageWrapper(GetMsg("Failure Plugin List v2"), redEmbed) : new MessageWrapper(GetMsg("No Failures v2"), greenEmbed); 
                            pluginUpdateCheckFailureReasonMessageWrapper.AddMessageBodies(failureList);

                            SendMessage(requestor, pluginUpdateCheckFailureReasonMessageWrapper);
                        }

                    }, this);
            }
        }

        #endregion

        #region Version Related

        private bool IsOutdated(Version installed, string latest)
        {
            if (!IsNumeric(latest.Replace(".", string.Empty)))
            {
                return false;
            }
            
            var latestPartials = latest.Split('.').Select(int.Parse).ToArray();

            return installed < GetVersion(latestPartials);
        }

        private static Version GetVersion(int[] partials)
        {
            if (partials.Length >= 3)
                return new Version(partials[0], partials[1], partials[2]);

            return new Version();
        }

        #endregion

        #region Helper
        
        private void SetConfig(params object[] args)
        {
            List<string> stringArgs = (from arg in args select arg.ToString()).ToList();
            stringArgs.RemoveAt(args.Length - 1);

            if (Config.Get(stringArgs.ToArray()) == null)
                Config.Set(args);
        }

        private T GetConfig<T>(T defaultVal, params string[] args)
        {
            if (Config.Get(args) == null)
            {
                PrintError($"The plugin failed to read something from the config: {string.Join("/", args)}{Environment.NewLine}Please reload the plugin and see if this message is still showing. If so, please post this into the support thread of this plugin.");
                return defaultVal;
            }

            return (T) Convert.ChangeType(Config.Get(args), typeof(T));
        }

        private string GetMsg(string key, object userID = null) => lang.GetMessage(key, this, userID?.ToString());
        
        private static bool IsNumeric(string text) => !text.Any(c => c < 48 || c > 57);

        #endregion

        #region Classes

        private struct ApiResponse
        {
            [JsonProperty("success")] private bool _hasSucceeded;
            [JsonProperty("data")] private Data _pluginData;
            [JsonProperty("error")] private string _error;

            public bool HasSucceeded => _hasSucceeded;
            public Data PluginData => _pluginData;
            public string Error => _error;

            public struct Data
            {
                [JsonProperty("resourceId")] private int _resourceId;
                [JsonProperty("title")] private string _title;
                [JsonProperty("version")] private string _version;
                [JsonProperty("developer")] private string _developer;
                [JsonProperty("url")] private string _url;

                public int ResourceId => _resourceId;
                public string Title => _title;
                public string Version => _version;
                public string Developer => _developer;
                public string Url => _url;
            }
        }

        private class MessageWrapper
        {
            public string Title { get; private set; }
            public int Colour { get; private set; }
            public List<MessageBody> Messages { get; private set; }

            public MessageWrapper(string title, int colour) 
            {
                this.Title = title;
                this.Colour = colour;
                this.Messages = new List<MessageBody>();
            }

            public string GetDiscordMessages() 
            {
                List<DiscordMessage> discordMessage = new List<DiscordMessage>();
                
                foreach (var message in this.Messages) 
                {
                    discordMessage.Add(new DiscordMessage(format(message.Title, MessageTarget.DISCORD), format(message.Message, MessageTarget.DISCORD), false));
                }
            
                return JsonConvert.SerializeObject(discordMessage);
            }

            public string GetDiscordTitle() {
                return format(Title, MessageTarget.DISCORD);
            }

            public int GetColourEmbed() {
                return Colour;
            }

            public string GetNoFormatting() 
            {

                string formattedMessage = $"{format(Title)}\n";
                foreach (var messageBody in Messages) {
                  formattedMessage += $"{format(messageBody.Title)} {format(messageBody.Message)}\n";
                }

                return formattedMessage;
            }

            public void AddMessageBody(MessageBody messageBody)
            {
                Messages.Add(messageBody);
            }

            public void AddMessageBodies(List<MessageBody> messageBodies)
            {
                Messages.AddRange(messageBodies);
            }

            private string format(string text) 
            {
                return format(text, MessageTarget.DEFAULT);
            }

            private string format(string text, MessageTarget messageTarget) 
            {
                return text.Replace(bold, messageTarget.Bold)
                           .Replace(italic, messageTarget.Italic)
                           .Replace(underline, messageTarget.Underline);
            }
        }

        private class MessageBody {

            public string Title { get; private set; }
            public string Message { get; private set; }

            public MessageBody(string title, string message) 
            {
                this.Title = title;
                this.Message = message; 
            }
        }

        private class DiscordMessage {

            [JsonProperty("name")]
            public string Name { get; private set; }
            [JsonProperty("value")]
            public string Dvalue { get; private set; }
            [JsonProperty("inline")]
            public bool Inline { get; private set; }

            public DiscordMessage(string name, string dValue, bool inline) 
            {
                this.Name = name;
                this.Dvalue = dValue; 
                this.Inline = inline;
            }
        }

        private class MessageTarget {
            public static readonly MessageTarget DEFAULT = new MessageTarget("", "", "");
            public static readonly MessageTarget DISCORD = new MessageTarget("**", "_", "__");
            public static readonly MessageTarget EMAIL = new MessageTarget("", "", "");
            public static readonly MessageTarget PUSH = new MessageTarget("", "", "");

            public string Bold { get; private set; }
            public string Italic { get; private set; }
            public string Underline { get; private set; }

            public MessageTarget(string bold, string italic, string underline) {
                this.Bold = bold;
                this.Italic = italic;
                this.Underline = underline;
            }
        }

        #endregion
    }
}