#region Using Statements

using System.Collections.Generic;
using Newtonsoft.Json;

#endregion

namespace Oxide.Plugins
{
    public partial class SteamChecks
    {
        private class WebHookAuthor
        {
            [JsonProperty(PropertyName = "icon_url")]
            public string AuthorIconUrl;

            [JsonProperty(PropertyName = "url")] 
            public string AuthorUrl;
            [JsonProperty(PropertyName = "name")] 
            public string Name;
        }

        private class WebHookContentBody
        {
            [JsonProperty(PropertyName = "content")]
            public string Content;
        }

        private class WebHookEmbed
        {
            [JsonProperty(PropertyName = "author")]
            public WebHookAuthor Author;

            [JsonProperty(PropertyName = "color")] 
            public int Color;

            [JsonProperty(PropertyName = "description")]
            public string Description;

            [JsonProperty(PropertyName = "fields")]
            public List<WebHookField> Fields;

            [JsonProperty(PropertyName = "footer")]
            public WebHookFooter Footer;

            [JsonProperty(PropertyName = "image")] 
            public WebHookImage Image;

            [JsonProperty(PropertyName = "thumbnail")]
            public WebHookThumbnail Thumbnail;

            [JsonProperty(PropertyName = "title")] 
            public string Title;

            [JsonProperty(PropertyName = "type")] 
            public string Type = "rich";
        }

        private class WebHookEmbedBody
        {
            [JsonProperty(PropertyName = "embeds")]
            public WebHookEmbed[] Embeds;
        }

        private class WebHookField
        {
            [JsonProperty(PropertyName = "inline")]
            public bool Inline;

            [JsonProperty(PropertyName = "name")] 
            public string Name;

            [JsonProperty(PropertyName = "value")] 
            public string Value;
        }

        private class WebHookFooter
        {
            [JsonProperty(PropertyName = "icon_url")]
            public string IconUrl;

            [JsonProperty(PropertyName = "proxy_icon_url")]
            public string ProxyIconUrl;

            [JsonProperty(PropertyName = "text")] 
            public string Text;
        }

        private class WebHookImage
        {
            [JsonProperty(PropertyName = "height")]
            public int? Height;

            [JsonProperty(PropertyName = "proxy_url")]
            public string ProxyUrl;

            [JsonProperty(PropertyName = "url")] 
            public string Url;

            [JsonProperty(PropertyName = "width")] 
            public int? Width;
        }

        private class WebHookThumbnail
        {
            [JsonProperty(PropertyName = "height")]
            public int? Height;

            [JsonProperty(PropertyName = "proxy_url")]
            public string ProxyUrl;

            [JsonProperty(PropertyName = "url")] 
            public string Url;

            [JsonProperty(PropertyName = "width")] 
            public int? Width;
        }
    }
}