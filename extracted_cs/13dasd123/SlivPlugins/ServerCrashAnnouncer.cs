// Все права принадлежат дискорд сообществу https://discord.gg/VgNHPpNrz6
﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Newtonsoft.Json;
using Oxide.Core;
using Oxide.Core.Libraries;
using ProtoBuf;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Server Crash Announcer", "supreme", "1.0.0")]
    [Description("Sends a discord message when booting the server after a crash")]
    public class ServerCrashAnnouncer : RustPlugin
    {
        #region Class Fields

        private PluginConfig _pluginConfig;
        private PluginData _pluginData;

        #endregion

        #region Hooks

        private void Init()
        {
            LoadData();
            if (_pluginData.ServerCrashed)
            {
                SendCrashMessage();
            }

            _pluginData.ServerCrashed = true;
        }

        private void Unload()
        {
            _pluginData.ServerCrashed = false;
            SaveData();
        }

        private void OnServerShutdown()
        {
            _pluginData.ServerCrashed = false;
            SaveData();
        }
        
        private void OnServerSave()
        {
            _pluginData.LastSaveTime = DateTime.UtcNow.ToLocalTime();
            SaveData();
        }

        #endregion

        #region Core Methods

        private void SendCrashMessage()
        {
            Embed embed = new Embed();
            if (!string.IsNullOrEmpty(_pluginConfig.DiscordConfiguration.ThumbnailImage))
            {
                embed.AddThumbnail(_pluginConfig.DiscordConfiguration.ThumbnailImage);
            }

            if (!string.IsNullOrEmpty(_pluginConfig.DiscordConfiguration.TitleText))
            {
                embed.AddTitle(_pluginConfig.DiscordConfiguration.TitleText);
            }

            if (!string.IsNullOrEmpty(_pluginConfig.DiscordConfiguration.DescriptionText))
            {
                embed.AddDescription(_pluginConfig.DiscordConfiguration.DescriptionText);
            }

            if (!string.IsNullOrEmpty(_pluginConfig.DiscordConfiguration.FieldTitleText))
            {
                embed.AddField(_pluginConfig.DiscordConfiguration.FieldTitleText, _pluginConfig.DiscordConfiguration.FieldText.Replace("{0}", FormatTime(TimeSpan.FromSeconds((DateTime.UtcNow.ToLocalTime() - _pluginData.LastSaveTime).TotalSeconds))), true);
            }

            if (!string.IsNullOrEmpty(_pluginConfig.DiscordConfiguration.FooterText))
            {
                embed.AddFooter($"{_pluginConfig.DiscordConfiguration.FooterText} | {DateTime.UtcNow.ToLocalTime()}", _pluginConfig.DiscordConfiguration.FooterImage);
            }
            
            if (!string.IsNullOrEmpty(_pluginConfig.DiscordConfiguration.AuthorName))
            {
                embed.AddAuthor(_pluginConfig.DiscordConfiguration.AuthorName, _pluginConfig.DiscordConfiguration.AuthorImage);
            }
            
            if (!string.IsNullOrEmpty(_pluginConfig.DiscordConfiguration.EmbedColor))
            {
                embed.AddColor(_pluginConfig.DiscordConfiguration.EmbedColor);
            }
            
            SendDiscordMessage(_pluginConfig.DiscordConfiguration.WebHook, new DiscordMessage(new List<Embed>
            {
                embed
            }, string.IsNullOrEmpty(_pluginConfig.DiscordConfiguration.MessageText) ? null : _pluginConfig.DiscordConfiguration.MessageText, _pluginConfig.DiscordConfiguration.BotName, _pluginConfig.DiscordConfiguration.BotImage));
        }

        #endregion
        
        #region Configuration

        private class PluginConfig
        {
            [JsonProperty(PropertyName = "Discord Configuration")]
            public DiscordConfiguration DiscordConfiguration { get; set; }
        }

        private class DiscordConfiguration
        {
            [JsonProperty(PropertyName = "Webhook Url")]
            public string WebHook { get; set; }
            
            [JsonProperty(PropertyName = "Message Text")]
            public string MessageText { get; set; }
            
            [JsonProperty(PropertyName = "Bot Name")]
            public string BotName { get; set; }
            
            [JsonProperty(PropertyName = "Bot Image")]
            public string BotImage { get; set; }
            
            [JsonProperty(PropertyName = "Author Name")]
            public string AuthorName { get; set; }
            
            [JsonProperty(PropertyName = "Author Image")]
            public string AuthorImage { get; set; }
            
            [JsonProperty(PropertyName = "Title Text")]
            public string TitleText { get; set; }
            
            [JsonProperty(PropertyName = "Description Text")]
            public string DescriptionText { get; set; }
            
            [JsonProperty(PropertyName = "Field Title Text")]
            public string FieldTitleText { get; set; }
            
            [JsonProperty(PropertyName = "Field Text")]
            public string FieldText { get; set; }
            
            [JsonProperty(PropertyName = "Footer Text")]
            public string FooterText { get; set; }
            
            [JsonProperty(PropertyName = "Footer Image")]
            public string FooterImage { get; set; }
            
            [JsonProperty(PropertyName = "Thumbnail")]
            public string ThumbnailImage { get; set; }

            [JsonProperty(PropertyName = "Embed Color")]
            public string EmbedColor { get; set; }
        }
        
        protected override void LoadDefaultConfig()
        {
            PrintWarning("Loading Default Config");
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            Config.Settings.DefaultValueHandling = DefaultValueHandling.Populate;
            _pluginConfig = AdditionalConfig(Config.ReadObject<PluginConfig>());
            Config.WriteObject(_pluginConfig);
        }

        private PluginConfig AdditionalConfig(PluginConfig pluginConfig)
        {
            pluginConfig.DiscordConfiguration = pluginConfig.DiscordConfiguration ?? new DiscordConfiguration
            {
                WebHook = "https://support.discordapp.com/hc/en-us/articles/228383668-Intro-to-Webhooks",
                MessageText = "@everyone",
                AuthorName = "Author Name",
                AuthorImage = "https://yt3.ggpht.com/ytc/AKedOLQc1OCf9gztVmcVnmI_41uN9axrRP8wd4a-GflFRQ=s900-c-k-c0x00ffffff-no-rj",
                TitleText = "Server Name",
                DescriptionText = "Description Text",
                FieldTitleText = "Field Title Text",
                FieldText = "The server lost {0} of data",
                FooterText = "Footer Text",
                FooterImage = "https://yt3.ggpht.com/ytc/AKedOLQc1OCf9gztVmcVnmI_41uN9axrRP8wd4a-GflFRQ=s900-c-k-c0x00ffffff-no-rj",
                ThumbnailImage = "https://yt3.ggpht.com/ytc/AKedOLQc1OCf9gztVmcVnmI_41uN9axrRP8wd4a-GflFRQ=s900-c-k-c0x00ffffff-no-rj",
                BotName = "Bot Name",
                BotImage = "https://yt3.ggpht.com/ytc/AKedOLQc1OCf9gztVmcVnmI_41uN9axrRP8wd4a-GflFRQ=s900-c-k-c0x00ffffff-no-rj",
                EmbedColor = "#ce422b"
            };
            
            return pluginConfig;
        }

        #endregion
        
        #region Data

        private void SaveData()
        {
            if (_pluginData == null)
            {
                return;
            }
            
            ProtoStorage.Save(_pluginData, Name);
        }

        private void LoadData()
        {
            _pluginData = ProtoStorage.Load<PluginData>(Name) ?? new PluginData();
        }

        [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
        private class PluginData
        {
            public bool ServerCrashed { get; set; }
            
            public DateTime LastSaveTime { get; set; }
        }

        #endregion

        #region Discord Embed

        #region Send Embed Methods
        
        /// <summary>
        /// Headers when sending an embedded message
        /// </summary>
        private readonly Dictionary<string, string> _headers = new Dictionary<string, string>
        {
            ["Content-Type"] = "application/json"
        };

        /// <summary>
        /// Sends the DiscordMessage to the specified webhook url
        /// </summary>
        /// <param name="url">Webhook url</param>
        /// <param name="message">Message being sent</param>
        private void SendDiscordMessage(string url, DiscordMessage message)
        {
            webrequest.Enqueue(url, message.ToJson().ToString(), SendDiscordMessageCallback, this, RequestMethod.POST, _headers);
        }

        /// <summary>
        /// Callback when sending the embed if any errors occured
        /// </summary>
        /// <param name="code">HTTP response code</param>
        /// <param name="message">Response message</param>
        private void SendDiscordMessageCallback(int code, string message)
        {
            if (code != 204)
            {
                PrintError(message);
            }
        }
        
        #endregion

        #region Helper Methods
        
        private string GetPositionField(Vector3 pos)
        {
            return $"{pos.x:0.00} {pos.y:0.00} {pos.z:0.00}";
        }
        
        private string FormatTime(TimeSpan timeSpan)
        {
            string output = string.Empty;
            if (timeSpan.TotalDays >= 1)
            {
                output += string.Format("{0} {1}", timeSpan.Days, "days") + " ";
            }

            if (timeSpan.TotalHours >= 1)
            {
                output += string.Format("{0} {1}", timeSpan.Hours, "hours") + " ";
            }

            if (timeSpan.TotalMinutes >= 1)
            {
                output += string.Format("{0} {1}", timeSpan.Minutes, "minutes") + " ";
            }
            
            if (timeSpan.TotalSeconds > 0)
            {
                output += string.Format("{0} {1}", timeSpan.Seconds, "seconds");
            }
            
            return output;
        }
        
        #endregion

        #region Embed Classes
        
        private class DiscordMessage
        {
            /// <summary>
            /// The name of the user sending the message changing this will change the webhook bots name
            /// </summary>
            [JsonProperty("username")]
            private string Username { get; set; }

            /// <summary>
            /// The avatar url of the user sending the message changing this will change the webhook bots avatar
            /// </summary>
            [JsonProperty("avatar_url")]
            private string AvatarUrl { get; set; }

            /// <summary>
            /// String only content to be sent
            /// </summary>
            [JsonProperty("content")]
            private string Content { get; set; }

            /// <summary>
            /// Embeds to be sent
            /// </summary>
            [JsonProperty("embeds")]
            private List<Embed> Embeds { get; }

            public DiscordMessage(List<Embed> embeds, string content = null, string username = null, string avatarUrl = null)
            {
                Embeds = embeds;
                Content = content;
                Username = username;
                AvatarUrl = avatarUrl;
            }

            /// <summary>
            /// Adds a new embed to the list of embed to send
            /// </summary>
            /// <param name="embed">Embed to add</param>
            /// <returns>This</returns>
            /// <exception cref="IndexOutOfRangeException">Thrown if more than 10 embeds are added in a send as that is the discord limit</exception>
            public DiscordMessage AddEmbed(Embed embed)
            {
                if (Embeds.Count >= 10)
                {
                    throw new IndexOutOfRangeException("Only 10 embed are allowed per message");
                }

                Embeds.Add(embed);
                return this;
            }

            /// <summary>
            /// Adds string content to the message
            /// </summary>
            /// <param name="content"></param>
            /// <returns></returns>
            public DiscordMessage AddContent(string content)
            {
                Content = content;
                return this;
            }

            /// <summary>
            /// Changes the username and avatar image for the bot sending the message
            /// </summary>
            /// <param name="username">username to change</param>
            /// <param name="avatarUrl">avatar img url to change</param>
            /// <returns>This</returns>
            public DiscordMessage AddSender(string username, string avatarUrl)
            {
                Username = username;
                AvatarUrl = avatarUrl;
                return this;
            }

            /// <summary>
            /// Returns message as JSON to be sent in the web request
            /// </summary>
            /// <returns></returns>
            public StringBuilder ToJson()
            {
                return new StringBuilder(JsonConvert.SerializeObject(this, Formatting.None, new JsonSerializerSettings
                {
                    NullValueHandling = NullValueHandling.Ignore
                })); 
            }
        }

        private class Embed
        {
            /// <summary>
            /// Color of the left side bar of the embed message
            /// </summary>
            [JsonProperty("color")]
            private int Color { get; set; }

            /// <summary>
            /// Fields to be added to the embed message
            /// </summary>
            [JsonProperty("fields")]
            private List<Field> Fields { get; } = new List<Field>();

            /// <summary>
            /// Title of the embed message
            /// </summary>
            [JsonProperty("title")]
            private string Title { get; set; }

            /// <summary>
            /// Description of the embed message
            /// </summary>
            [JsonProperty("description")]
            private string Description { get; set; }

            /// <summary>
            /// Description of the embed message
            /// </summary>
            [JsonProperty("url")]
            private string Url { get; set; }

            /// <summary>
            /// Image to added to the embed message. Appears at the bottom of the message above the footer
            /// </summary>
            [JsonProperty("image")]
            private Image Image { get; set; }

            /// <summary>
            /// Thumbnail image added to the embed message. Appears in the top right corner
            /// </summary>
            [JsonProperty("thumbnail")]
            private Image Thumbnail { get; set; }

            /// <summary>
            /// Video to add to the embed message
            /// </summary>
            [JsonProperty("video")]
            private Video Video { get; set; }

            /// <summary>
            /// Author to add to the embed message. Appears above the title.
            /// </summary>
            [JsonProperty("author")]
            private AuthorInfo Author { get; set; }

            /// <summary>
            /// Footer to add to the embed message. Appears below all content.
            /// </summary>
            [JsonProperty("footer")]
            private Footer Footer { get; set; }

            /// <summary>
            /// Adds a title to the embed message
            /// </summary>
            /// <param name="title">Title to add</param>
            /// <returns>This</returns>
            public Embed AddTitle(string title)
            {
                Title = title;
                return this;
            }

            /// <summary>
            /// Adds a description to the embed message
            /// </summary>
            /// <param name="description">description to add</param>
            /// <returns>This</returns>
            public Embed AddDescription(string description)
            {
                Description = description;
                return this;
            }

            /// <summary>
            /// Adds a url to the embed message
            /// </summary>
            /// <param name="url"></param>
            /// <returns>This</returns>
            public Embed AddUrl(string url)
            {
                Url = url;
                return this;
            }

            /// <summary>
            /// Adds an author to the embed message. The author will appear above the title
            /// </summary>
            /// <param name="name">Name of the author</param>
            /// <param name="iconUrl">Icon Url to use for the author</param>
            /// <param name="url">Url to go to when the authors name is clicked on</param>
            /// <param name="proxyIconUrl">Backup icon url. Can be left null if you only have one icon url</param>
            /// <returns>This</returns>
            public Embed AddAuthor(string name, string iconUrl = null, string url = null, string proxyIconUrl = null)
            {
                Author = new AuthorInfo(name, iconUrl, url, proxyIconUrl);
                return this;
            }

            /// <summary>
            /// Adds a footer to the embed message
            /// </summary>
            /// <param name="text">Text to be added to the footer</param>
            /// <param name="iconUrl">Icon url to add in the footer. Appears to the left of the text</param>
            /// <param name="proxyIconUrl">Backup icon url. Can be left null if you only have one icon url</param>
            /// <returns>This</returns>
            public Embed AddFooter(string text, string iconUrl = null, string proxyIconUrl = null)
            {
                Footer = new Footer(text, iconUrl, proxyIconUrl);
                return this;
            }

            /// <summary>
            /// Adds an int based color to the embed. Color appears as a bar on the left side of the message
            /// </summary>
            /// <param name="color"></param>
            /// <returns></returns>
            public Embed AddColor(int color)
            {
                if (color < 0x0 || color > 0xFFFFFF)
                {
                    throw new Exception($"Color '{color}' is outside the valid color range");
                }

                Color = color;
                return this;
            }

            /// <summary>
            /// Adds a hex based color. Color appears as a bar on the left side of the message
            /// </summary>
            /// <param name="color">Color in string hex format</param>
            /// <returns>This</returns>
            /// <exception cref="Exception">Exception thrown if color is outside of range</exception>
            public Embed AddColor(string color)
            {
                int parsedColor = int.Parse(color.TrimStart('#'), NumberStyles.AllowHexSpecifier);
                if (parsedColor < 0x0 || parsedColor > 0xFFFFFF)
                {
                    throw new Exception($"Color '{color}' is outside the valid color range");
                }

                Color = parsedColor;
                return this;
            }

            /// <summary>
            /// Adds a RGB based color. Color appears as a bar on the left side of the message
            /// </summary>
            /// <param name="red">Red value between 0 - 255</param>
            /// <param name="green">Green value between 0 - 255</param>
            /// <param name="blue">Blue value between 0 - 255</param>
            /// <returns>This</returns>
            /// <exception cref="Exception">Thrown if red, green, or blue is outside of range</exception>
            public Embed AddColor(int red, int green, int blue)
            {
                if (red < 0 || red > 255 || green < 0 || green > 255 || green < 0 || green > 255)
                {
                    throw new Exception($"Color Red:{red} Green:{green} Blue:{blue} is outside the valid color range. Must be between 0 - 255");
                }

                Color = red * 65536 + green * 256 + blue;
                return this;
            }

            /// <summary>
            /// Adds a blank field.
            /// If inline it will add a blank column.
            /// If not inline will add a blank row
            /// </summary>
            /// <param name="inline">If the field is inline</param>
            /// <returns>This</returns>
            public Embed AddBlankField(bool inline)
            {
                Fields.Add(new Field("\u200b", "\u200b", inline));
                return this;
            }

            /// <summary>
            /// Adds a new field with the name as the title and value as the value.
            /// If inline will add a new column. If row will add in a new row.
            /// </summary>
            /// <param name="name"></param>
            /// <param name="value"></param>
            /// <param name="inline"></param>
            /// <returns></returns>
            public Embed AddField(string name, string value, bool inline)
            {
                Fields.Add(new Field(name, value, inline));
                return this;
            }

            /// <summary>
            /// Adds an image to the embed. The url should point to the url of the image.
            /// If using attachment image you can make the url: "attachment://{image name}.{image extension}
            /// </summary>
            /// <param name="url">Url for the image</param>
            /// <param name="width">width of the image</param>
            /// <param name="height">height of the image</param>
            /// <param name="proxyUrl">Backup url for the image</param>
            /// <returns></returns>
            public Embed AddImage(string url, int? width = null, int? height = null, string proxyUrl = null)
            {
                Image = new Image(url, width, height, proxyUrl);
                return this;
            }

            /// <summary>
            /// Adds a thumbnail in the top right corner of the embed
            /// If using attachment image you can make the url: "attachment://{image name}.{image extension}
            /// </summary>
            /// <param name="url">Url for the image</param>
            /// <param name="width">width of the image</param>
            /// <param name="height">height of the image</param>
            /// <param name="proxyUrl">Backup url for the image</param>
            /// <returns></returns>
            public Embed AddThumbnail(string url, int? width = null, int? height = null, string proxyUrl = null)
            {
                Thumbnail = new Image(url, width, height, proxyUrl);
                return this;
            }

            /// <summary>
            /// Adds a video to the embed
            /// </summary>
            /// <param name="url">Url for the video</param>
            /// <param name="width">Width of the video</param>
            /// <param name="height">Height of the video</param>
            /// <returns></returns>
            public Embed AddVideo(string url, int? width = null, int? height = null)
            {
                Video = new Video(url, width, height);
                return this;
            }
        }

        /// <summary>
        /// Field for and embed message
        /// </summary>
        private class Field
        {
            /// <summary>
            /// Name of the field
            /// </summary>
            [JsonProperty("name")]
            private string Name { get; }

            /// <summary>
            /// Value for the field
            /// </summary>
            [JsonProperty("value")]
            private string Value { get; }

            /// <summary>
            /// If the field should be in the same row or a new row
            /// </summary>
            [JsonProperty("inline")]
            private bool Inline { get; }

            public Field(string name, string value, bool inline)
            {
                Name = name;
                Value = value;
                Inline = inline;
            }
        }

        /// <summary>
        /// Image for an embed message
        /// </summary>
        private class Image
        {
            /// <summary>
            /// Url for the image
            /// </summary>
            [JsonProperty("url")]
            private string Url { get; }

            /// <summary>
            /// Width for the image
            /// </summary>
            [JsonProperty("width")]
            private int? Width { get; }

            /// <summary>
            /// Height for the image
            /// </summary>
            [JsonProperty("height")]
            private int? Height { get; }

            /// <summary>
            /// Proxy url for the image
            /// </summary>
            [JsonProperty("proxyURL")]
            private string ProxyUrl { get; }

            public Image(string url, int? width, int? height, string proxyUrl)
            {
                Url = url;
                Width = width;
                Height = height;
                ProxyUrl = proxyUrl;
            }
        }

        /// <summary>
        /// Video for an embed message
        /// </summary>
        private class Video
        {
            /// <summary>
            /// Url to the video
            /// </summary>
            [JsonProperty("url")]
            private string Url { get; }

            /// <summary>
            /// Width of the video
            /// </summary>
            [JsonProperty("width")]
            private int? Width { get; }

            /// <summary>
            /// Height of the video
            /// </summary>
            [JsonProperty("height")]
            private int? Height { get; }

            public Video(string url, int? width, int? height)
            {
                Url = url;
                Width = width;
                Height = height;
            }
        }

        /// <summary>
        /// Author of an embed message
        /// </summary>
        private class AuthorInfo
        {
            /// <summary>
            /// Name of the author
            /// </summary>
            [JsonProperty("name")]
            private string Name { get; }

            /// <summary>
            /// Url to go to when clicking on the authors name
            /// </summary>
            [JsonProperty("url")]
            private string Url { get; }

            /// <summary>
            /// Icon url for the author
            /// </summary>
            [JsonProperty("icon_url")]
            private string IconUrl { get; }

            /// <summary>
            /// Proxy icon url for the author
            /// </summary>
            [JsonProperty("proxy_icon_url")]
            private string ProxyIconUrl { get; }

            public AuthorInfo(string name, string iconUrl, string url, string proxyIconUrl)
            {
                Name = name;
                Url = url;
                IconUrl = iconUrl;
                ProxyIconUrl = proxyIconUrl;
            }
        }

        /// <summary>
        /// Footer for an embed message
        /// </summary>
        private class Footer
        {
            /// <summary>
            /// Text for the footer
            /// </summary>
            [JsonProperty("text")]
            private string Text { get; }

            /// <summary>
            /// Icon url for the footer
            /// </summary>
            [JsonProperty("icon_url")]
            private string IconUrl { get; }

            /// <summary>
            /// Proxy icon url for the footer
            /// </summary>
            [JsonProperty("proxy_icon_url")]
            private string ProxyIconUrl { get; }

            public Footer(string text, string iconUrl, string proxyIconUrl)
            {
                Text = text;
                IconUrl = iconUrl;
                ProxyIconUrl = proxyIconUrl;
            }
        }
        
        #endregion

        #region Attachment Classes
        
        /// <summary>
        /// Enum for attachment content type
        /// </summary>
        private enum AttachmentContentType
        {
            Png,
            Jpg
        }

        private class Attachment
        {
            /// <summary>
            /// Attachment data
            /// </summary>
            public byte[] Data { get; }

            /// <summary>
            /// File name for the attachment.
            /// Used in the url field of an image
            /// </summary>
            public string Filename { get; }

            /// <summary>
            /// Content type for the attachment
            /// https://developer.mozilla.org/en-US/docs/Web/HTTP/Basics_of_HTTP/MIME_types
            /// </summary>
            public string ContentType { get; }

            public Attachment(byte[] data, string filename, AttachmentContentType contentType)
            {
                Data = data;
                Filename = filename;

                switch (contentType)
                {
                    case AttachmentContentType.Jpg:
                        ContentType = "image/jpeg";
                        break;

                    case AttachmentContentType.Png:
                        ContentType = "image/png";
                        break;
                }
            }

            public Attachment(byte[] data, string filename, string contentType)
            {
                Data = data;
                Filename = filename;
                ContentType = contentType;
            }
        }
        
        #endregion
        
        #endregion
    }
}