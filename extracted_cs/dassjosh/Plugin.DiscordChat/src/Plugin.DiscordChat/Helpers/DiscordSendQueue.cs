using System;
using System.Text;
using DiscordChatPlugin.Placeholders;
using DiscordChatPlugin.Plugins;
using Oxide.Ext.Discord.Entities;
using Oxide.Ext.Discord.Libraries;
using Oxide.Plugins;

namespace DiscordChatPlugin.Helpers;

public class DiscordSendQueue
{
    private readonly StringBuilder _message = new();
    private Timer _sendTimer;
    private readonly DiscordChannel _channel;
    private readonly TemplateKey _templateId;
    private readonly Action _callback;
    private readonly PluginTimers _timer;

    public DiscordSendQueue(DiscordChannel channel, TemplateKey templateId, PluginTimers timers)
    {
        _channel = channel;
        _templateId = templateId;
        _callback = Send;
        _timer = timers;
    }

    public void QueueMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }
                
        if (_message.Length + message.Length > 2000)
        {
            Send();
        }

        _sendTimer ??= _timer.In(1f, _callback);

        _message.AppendLine(message);
    }
        
    public void Send()
    {
        if (_message.Length > 2000)
        {
            _message.Length = 2000;
        }

        PlaceholderData placeholders = DiscordChat.Instance.GetDefault().Add(PlaceholderDataKeys.TemplateMessage, _message.ToString());
        _message.Length = 0;
        DiscordChat.Instance.SendGlobalTemplateMessage(_templateId, _channel, placeholders);
        _sendTimer?.Destroy();
        _sendTimer = null;
    }
}