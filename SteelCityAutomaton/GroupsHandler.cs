using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using OpenMetaverse;

namespace SteelCityAutomaton
{
    public class GroupsHandler
    {
        private GroupManager Groups;
        private ManualResetEvent GroupsEvent = new ManualResetEvent(false);
        public Dictionary<UUID, Group> GroupsCache = null;
        private UUID GroupMembersRequestID;

        public Dictionary<UUID, GroupMember> GroupMembers;

        public GroupsHandler(GroupManager gm)
        {
            Groups = gm;
            gm.GroupMembersReply += GroupMembersHandler;
        }

        public void ReloadGroupsCache()
        {
            Groups.CurrentGroups += Groups_CurrentGroups;
            Groups.RequestCurrentGroups();
            GroupsEvent.WaitOne(10000, false);
            Groups.CurrentGroups -= Groups_CurrentGroups;
            GroupsEvent.Reset();
        }

        void Groups_CurrentGroups(object sender, CurrentGroupsEventArgs e)
        {
            if (null == GroupsCache)
                GroupsCache = e.Groups;
            else
                lock (GroupsCache) { GroupsCache = e.Groups; }
            GroupsEvent.Set();
        }

        private void GroupMembersHandler(object sender, GroupMembersReplyEventArgs e)
        {
            if (e.RequestID != GroupMembersRequestID) return;

            GroupMembers = e.Members;
        }
    }
}
