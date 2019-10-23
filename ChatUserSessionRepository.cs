using Penguin.Cms.Communication;
using Penguin.Cms.Repositories;
using Penguin.Messaging.Core;
using Penguin.Persistence.Abstractions.Interfaces;
using Penguin.Security.Abstractions;
using Penguin.Security.Abstractions.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Penguin.Cms.Modules.Communication.Repositories
{
    [SuppressMessage("Naming", "CA1710:Identifiers should have correct suffix")]
    public class ChatUserSessionRepository : EntityRepository<ChatUserSession>
    {
        protected IUserSession UserSession { get; set; }
        private ISecurityProvider<ChatUserSession> SecurityProvider { get; set; }

        public ChatUserSessionRepository(ISecurityProvider<ChatUserSession> securityProvider, IPersistenceContext<ChatUserSession> dbContext, IUserSession userSession = null, MessageBus messageBus = null) : base(dbContext, messageBus)
        {
            SecurityProvider = securityProvider;
            UserSession = userSession;
        }

        public void AddUserToSession(Guid chatSession, Guid entity)
        {

            ChatUserSession existing = this.Where(c => c.ChatSession == chatSession && c.User == entity).FirstOrDefault();

            if (existing is null)
            {
                existing = new ChatUserSession()
                {
                    ChatSession = chatSession,
                    User = entity
                };

                SecurityProvider.AddPermissions(existing, PermissionTypes.Full, entity);

                this.Add(existing);
            }
        }

        [SuppressMessage("Globalization", "CA1303:Do not pass literals as localized parameters")]
        public List<ChatUserSession> GetSessionsForUser(IUser u)
        {
            u = u ?? this.UserSession.LoggedInUser;

            if (u is null)
            {
                throw new Exception("What the fuck?");
            }

            return this.GetSessionsForUser(u.Guid);
        }

        public List<ChatUserSession> GetSessionsForUser(Guid g) => this.Where(c => c.User == g).ToList();

        public List<Guid> GetUsersForSession(Guid Session) => this.Where(c => c.ChatSession == Session).Select(c => c.User).Distinct().ToList();
    }
}