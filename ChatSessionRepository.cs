using Penguin.Cms.Communication;
using Penguin.Cms.Repositories;
using Penguin.Messaging.Core;
using Penguin.Persistence.Abstractions.Interfaces;
using Penguin.Security.Abstractions;
using Penguin.Security.Abstractions.Extensions;
using Penguin.Security.Abstractions.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Penguin.Cms.Modules.Communication.Repositories
{
    [SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix")]
    public class ChatSessionRepository : EntityRepository<ChatSession>
    {
        protected ChatUserSessionRepository ChatUserSessionRepository { get; set; }
        protected ISecurityProvider<ChatSession> SecurityProvider { get; set; }

        protected IUserSession UserSession { get; set; }

        public ChatSessionRepository(ChatUserSessionRepository chatUserSessionRepository, IPersistenceContext<ChatSession> dbContext, ISecurityProvider<ChatSession> securityProvider = null, IUserSession userSession = null, MessageBus messageBus = null) : base(dbContext, messageBus)
        {
            ChatUserSessionRepository = chatUserSessionRepository;
            SecurityProvider = securityProvider;
            UserSession = userSession;
        }

        public ChatSession GetForUsers(params Guid[] targets)
        {
            int DistinctUsers = targets.Distinct().Count();

            List<ChatUserSession> openChats = new List<ChatUserSession>();

            //All Users are in this chat
            foreach (Guid u in targets)
            {
                openChats.AddRange(this.ChatUserSessionRepository.GetSessionsForUser(u).ToList());
            };

            List<Guid> openChatIds = openChats.GroupBy(c => c.ChatSession).Where(g => g.Count() == DistinctUsers).Select(g => g.First().ChatSession).ToList();

            foreach (Guid thisChatSession in openChatIds)
            {
                int totalUsersInChat = this.ChatUserSessionRepository.Where(ci => thisChatSession == ci.ChatSession).Count();

                if (totalUsersInChat == DistinctUsers)
                {
                    return this.Find(thisChatSession);
                }
            }

            return null;
        }

        public List<ChatSession> GetOpenChatsForUser()
        {
            List<Guid> chatsesstionIds = this.ChatUserSessionRepository.GetSessionsForUser(UserSession.LoggedInUser).Select(c => c.ChatSession).ToList();

            return this.Where(c => chatsesstionIds.Contains(c.Guid)).ToList().Where(c => SecurityProvider.TryCheckAccess(c)).ToList();
        }

        public ChatSession OpenSession(params Guid[] targets)
        {
            ChatSession newSession = new ChatSession();

            foreach (Guid entity in targets)
            {

                SecurityProvider.AddPermissions(newSession, PermissionTypes.Full, entity);

                this.ChatUserSessionRepository.AddUserToSession(newSession.Guid, entity);
            }

            this.Add(newSession);

            return newSession;
        }
    }
}