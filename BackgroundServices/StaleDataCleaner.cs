﻿using BirthdayBot.Data;
using NpgsqlTypes;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BirthdayBot.BackgroundServices
{
    /// <summary>
    /// Automatically removes database information for guilds that have not been accessed in a long time.
    /// </summary>
    class StaleDataCleaner : BackgroundService
    {
        public StaleDataCleaner(BirthdayBot instance) : base(instance) { }

        public override async Task OnTick()
        {
            // Build a list of all values to update
            var updateList = new Dictionary<ulong, List<ulong>>();
            foreach (var gi in BotInstance.GuildCache)
            {
                var existingUsers = new List<ulong>();
                updateList[gi.Key] = existingUsers;

                var guild = BotInstance.DiscordClient.GetGuild(gi.Key);
                if (guild == null) continue; // Have cache without being in guild. Unlikely, but...

                // Get IDs of cached users which are currently in the guild
                var cachedUserIds = from cu in gi.Value.Users select cu.UserId;
                var guildUserIds = from gu in guild.Users select gu.Id;
                var existingCachedIds = cachedUserIds.Intersect(guildUserIds);
            }

            using (var db = await BotInstance.Config.DatabaseSettings.OpenConnectionAsync())
            {
                // Prepare to update a lot of last-seen values
                var cUpdateGuild = db.CreateCommand();
                cUpdateGuild.CommandText = $"update {GuildStateInformation.BackingTable} set last_seen = now() "
                    + "where guild_id = @Gid";
                var pUpdateG = cUpdateGuild.Parameters.Add("@Gid", NpgsqlDbType.Bigint);
                cUpdateGuild.Prepare();

                var cUpdateGuildUser = db.CreateCommand();
                cUpdateGuildUser.CommandText = $"update {GuildUserSettings.BackingTable} set last_seen = now() "
                    + "where guild_id = @Gid and user_id = @Uid";
                var pUpdateGU_g = cUpdateGuildUser.Parameters.Add("@Gid", NpgsqlDbType.Bigint);
                var pUpdateGU_u = cUpdateGuild.Parameters.Add("@Uid", NpgsqlDbType.Bigint);
                cUpdateGuildUser.Prepare();

                // Do actual updates
                foreach (var item in updateList)
                {
                    var guild = item.Key;
                    var userlist = item.Value;

                    pUpdateG.Value = (long)guild;
                    cUpdateGuild.ExecuteNonQuery();

                    pUpdateGU_g.Value = (long)guild;
                    foreach (var userid in userlist)
                    {
                        pUpdateGU_u.Value = (long)userid;
                        cUpdateGuildUser.ExecuteNonQuery();
                    }
                }

                // Delete all old values - expects referencing tables to have 'on delete cascade'
                using (var t = db.BeginTransaction())
                {
                    using (var c = db.CreateCommand())
                    {
                        // Delete data for guilds not seen in 4 weeks
                        c.CommandText = $"delete from {GuildStateInformation.BackingTable} where (now() - interval '28 days') > last_seen";
                        var r = c.ExecuteNonQuery();
                        if (r != 0) Log($"Removed {r} stale guild(s).");
                    }
                    using (var c = db.CreateCommand())
                    {
                        // Delete data for users not seen in 8 weeks
                        c.CommandText = $"delete from {GuildUserSettings.BackingTable} where (now() - interval '56 days') > last_seen";
                        var r = c.ExecuteNonQuery();
                        if (r != 0) Log($"Removed {r} stale user(s).");
                    }
                    t.Commit();
                }
            }
        }
    }
}
