using System.Collections.Concurrent;
using Microsoft.AspNetCore.SignalR;

namespace NET_Tutos.Hubs;

public class DiscussionHub : Hub
{
    // Maps topicGroup (e.g. "Tutorial_1", "Challenge_2") to connection IDs
    private static readonly ConcurrentDictionary<string, HashSet<string>> TopicSubscribers = new();
    private static readonly object LockObj = new();

    public async Task JoinTopic(string topicGroup)
    {
        if (string.IsNullOrWhiteSpace(topicGroup)) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, topicGroup);

        int count;
        lock (LockObj)
        {
            if (!TopicSubscribers.TryGetValue(topicGroup, out var list))
            {
                list = new HashSet<string>();
                TopicSubscribers[topicGroup] = list;
            }
            list.Add(Context.ConnectionId);
            count = list.Count;
        }

        await Clients.Group(topicGroup).SendAsync("UpdateLearnerCount", count);
    }

    public async Task LeaveTopic(string topicGroup)
    {
        if (string.IsNullOrWhiteSpace(topicGroup)) return;

        await Groups.RemoveFromGroupAsync(Context.ConnectionId, topicGroup);

        int count = 0;
        lock (LockObj)
        {
            if (TopicSubscribers.TryGetValue(topicGroup, out var list))
            {
                list.Remove(Context.ConnectionId);
                if (list.Count == 0)
                {
                    TopicSubscribers.TryRemove(topicGroup, out _);
                }
                else
                {
                    count = list.Count;
                }
            }
        }

        await Clients.Group(topicGroup).SendAsync("UpdateLearnerCount", count);
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var connId = Context.ConnectionId;
        var affectedGroups = new List<(string Group, int Count)>();

        lock (LockObj)
        {
            foreach (var kvp in TopicSubscribers)
            {
                if (kvp.Value.Remove(connId))
                {
                    affectedGroups.Add((kvp.Key, kvp.Value.Count));
                }
            }
        }

        foreach (var (group, count) in affectedGroups)
        {
            await Clients.Group(group).SendAsync("UpdateLearnerCount", count);
        }

        await base.OnDisconnectedAsync(exception);
    }
}
