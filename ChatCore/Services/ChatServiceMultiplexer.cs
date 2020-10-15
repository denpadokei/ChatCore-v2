﻿using Microsoft.Extensions.Logging;
using ChatCore.Interfaces;
using ChatCore.Services.Twitch;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.Linq;
using ChatCore.Models.Twitch;
using ChatCore.Utilities;

namespace ChatCore.Services
{
    /// <summary>
    /// A multiplexer for all the supported streaming services.
    /// </summary>
    public class ChatServiceMultiplexer : ChatServiceBase, IChatService
    {
        public string DisplayName { get; private set; } = "Generic";

        public ChatServiceMultiplexer(ILogger<ChatServiceMultiplexer> logger, IList<IChatService> streamingServices)
        {
            _logger = logger;
            _streamingServices = streamingServices;
            _twitchService = (TwitchService)streamingServices.First(s => s is TwitchService);

            var sb = new StringBuilder();
            foreach (var service in _streamingServices)
            {
                service.OnTextMessageReceived += Service_OnTextMessageReceived;
                service.OnJoinChannel += Service_OnJoinChannel;
                service.OnRoomStateUpdated += Service_OnRoomStateUpdated;
                service.OnLeaveChannel += Service_OnLeaveChannel;
                service.OnLogin += Service_OnLogin;
                service.OnChatCleared += Service_OnChatCleared;
                service.OnMessageCleared += Service_OnMessageCleared;
                service.OnChannelResourceDataCached += Service_OnChannelResourceDataCached;

                if(sb.Length > 0)
                {
                    sb.Append(", ");
                }
                sb.Append(service.DisplayName);
            }
            DisplayName = sb.ToString();
        }

        private ILogger _logger;
        private IList<IChatService> _streamingServices;
        private TwitchService _twitchService;
        private object _invokeLock = new object();

        private void Service_OnChannelResourceDataCached(IChatService svc, IChatChannel channel, Dictionary<string, IChatResourceData> resources)
        {
            lock (_invokeLock)
            {
                ChannelResourceDataCached.InvokeAll(Assembly.GetCallingAssembly(), svc, channel, resources, _logger);
            }
        }

        private void Service_OnMessageCleared(IChatService svc, string messageId)
        {
            lock (_invokeLock)
            {
                MessageClearedCallbacks.InvokeAll(Assembly.GetCallingAssembly(), svc, messageId, _logger);
            }
        }

        private void Service_OnChatCleared(IChatService svc, string userId)
        {
            lock (_invokeLock)
            {
                ChatClearedCallbacks.InvokeAll(Assembly.GetCallingAssembly(), svc, userId, _logger);
            }
        }

        private void Service_OnLogin(IChatService svc)
        {
            lock (_invokeLock)
            {
                LoginCallbacks.InvokeAll(Assembly.GetCallingAssembly(), svc, _logger);
            }
        }

        private void Service_OnLeaveChannel(IChatService svc, IChatChannel channel)
        {
            lock (_invokeLock)
            {
                LeaveRoomCallbacks.InvokeAll(Assembly.GetCallingAssembly(), svc, channel, _logger);
            }
        }

        private void Service_OnRoomStateUpdated(IChatService svc, IChatChannel channel)
        {
            lock (_invokeLock)
            {
                RoomStateUpdatedCallbacks.InvokeAll(Assembly.GetCallingAssembly(), svc, channel, _logger);
            }
        }

        private void Service_OnTextMessageReceived(IChatService svc, IChatMessage message)
        {
            lock (_invokeLock)
            {
                TextMessageReceivedCallbacks.InvokeAll(Assembly.GetCallingAssembly(), svc, message, _logger);
            }
        }

        private void Service_OnJoinChannel(IChatService svc, IChatChannel channel)
        {
            lock (_invokeLock)
            {
                JoinRoomCallbacks.InvokeAll(Assembly.GetCallingAssembly(), svc, channel, _logger);
            }
        }

        public void SendTextMessage(string message, IChatChannel channel)
        {
            if(channel is TwitchChannel)
            {
                _twitchService.SendTextMessage(Assembly.GetCallingAssembly(), message, channel.Id);
            }
        }

        public TwitchService GetTwitchService()
        {
            return _twitchService;
        }
    }
}
