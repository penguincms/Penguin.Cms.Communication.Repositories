﻿using Penguin.Cms.Repositories;
using Penguin.Messaging.Core;
using Penguin.Persistence.Abstractions.Interfaces;
using Penguin.Security.Abstractions.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Penguin.Cms.Communication.Repositories
{
    public class ChatMessageRepository : EntityRepository<ChatMessage>
    {
        protected ChatUserSessionRepository ChatUserSessionRepository { get; set; }

        protected ISecurityProvider<ChatMessage> SecurityProvider { get; set; }

        public ChatMessageRepository(IPersistenceContext<ChatMessage> dbContext, ISecurityProvider<ChatMessage> securityProvider, ChatUserSessionRepository chatUserSessionRepository, MessageBus messageBus = null) : base(dbContext, messageBus)
        {
            ChatUserSessionRepository = chatUserSessionRepository;
            SecurityProvider = securityProvider;
        }

        public List<ChatMessage> GetMessages(Guid chatSession)
        {
            return this.Where(m => m.ChatSession == chatSession).ToList();
        }

        public List<ChatMessage> GetMessagesAfter(int Id)
        {
            ChatMessage chatMessage = Find(Id) ?? throw new NullReferenceException($"No ChatMessage found with Id {Id}");

            if (chatMessage == null)
            {
                return new List<ChatMessage>();
            }

            Guid ChatSession = chatMessage.ChatSession;

            return this.Where(c => c.ChatSession == ChatSession && c._Id > Id).ToList();
        }

        public List<ChatMessage> GetMessagesFor(Guid Id)
        {
            return this.Where(c => c.ChatSession == Id).ToList();
        }

        public ChatMessage SendMessageToChat(ChatSession chatSession, IUser user, string Message)
        {
            return chatSession is null
                ? throw new ArgumentNullException(nameof(chatSession))
                : SendMessageToChat(chatSession.Guid, user, Message);
        }

        public ChatMessage SendMessageToChat(Guid chatSession, IUser user, string Message)
        {
            if (user is null)
            {
                throw new ArgumentNullException(nameof(user));
            }

            ChatMessage toSend = new()
            {
                ChatSession = chatSession,
                User = user.Guid,
                DisplayName = user.ToString(),
                Contents = Message
            };

            SecurityProvider.SetDefaultPermissions(toSend);

            Add(toSend);

            return toSend;
        }
    }
}