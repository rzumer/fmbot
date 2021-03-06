using System;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using Serilog;

namespace FMBot.Bot.Handlers
{
    public class ClientLogHandler
    {
        private readonly DiscordShardedClient _client;

        public ClientLogHandler(DiscordShardedClient client)
        {
            this._client = client;
            this._client.Log += LogEvent;
            this._client.ShardLatencyUpdated += ShardLatencyEvent;
            this._client.ShardDisconnected += ShardDisconnectedEvent;
            this._client.ShardConnected += ShardConnectedEvent;
            this._client.JoinedGuild += ClientJoinedGuildEvent;
            this._client.LeftGuild += ClientLeftGuild;
        }

        private Task ClientJoinedGuildEvent(SocketGuild guild)
        {
            Task.Run(async () => ClientJoinedGuild(guild));
            return Task.CompletedTask;
        }

        private Task LogEvent(LogMessage logMessage)
        {
            Task.Run(() =>
            {
                switch (logMessage.Severity)
                {
                    case LogSeverity.Critical:
                        Log.Fatal(logMessage.Exception, "{logMessageSource} | {logMessage}", logMessage.Source, logMessage.Message);
                        break;
                    case LogSeverity.Error:
                        Log.Error(logMessage.Exception, "{logMessageSource} | {logMessage}", logMessage.Source,
                            logMessage.Message);
                        break;
                    case LogSeverity.Warning:
                        Log.Warning(logMessage.Exception, "{logMessageSource} | {logMessage}", logMessage.Source,
                            logMessage.Message);
                        break;
                    case LogSeverity.Info:
                        Log.Information(logMessage.Exception, "{logMessageSource} | {logMessage}", logMessage.Source,
                            logMessage.Message);
                        break;
                    case LogSeverity.Verbose:
                        Log.Verbose(logMessage.Exception, "{logMessageSource} | {logMessage}", logMessage.Source,
                            logMessage.Message);
                        break;
                    case LogSeverity.Debug:
                        Log.Debug(logMessage.Exception, "{logMessageSource} | {logMessage}", logMessage.Source,
                            logMessage.Message);
                        break;
                }
                    
            });
            return Task.CompletedTask;
        }

        private Task ShardDisconnectedEvent(Exception exception, DiscordSocketClient shard)
        {
            Task.Run(async () => ShardDisconnected(exception, shard));
            return Task.CompletedTask;
        }

        private Task ShardLatencyEvent(int oldPing, int updatePing, DiscordSocketClient shard)
        {
            Task.Run(async () => ShardLatencyUpdated(oldPing, updatePing, shard));
            return Task.CompletedTask;
        }

        private Task ShardConnectedEvent(DiscordSocketClient shard)
        {
            Task.Run(async () => ShardConnected(shard));
            return Task.CompletedTask;
        }

        private void ShardDisconnected(Exception exception, DiscordSocketClient shard)
        {
            Log.Warning("ShardDisconnected: shard {shardId} Disconnected",
                shard.ShardId, exception);
        }

        private void ShardConnected(DiscordSocketClient shard)
        {
            Log.Information("ShardConnected: shard {shardId} with {shardLatency} ms",
                shard.ShardId, shard.Latency);
        }

        private void ShardLatencyUpdated(int oldPing, int updatePing, DiscordSocketClient shard)
        {
            // If new or old latency if lager then 500ms.
            if (updatePing < 500 && oldPing < 500) return;
            Log.Information("Shard: {shardId} Latency update from {oldPing} ms to {updatePing} ms",
                shard.ShardId, oldPing, updatePing);
        }

        private void ClientJoinedGuild(SocketGuild guild)
        {
            Log.Information(
                "JoinedGuild: {guildName} / {guildId} | {memberCount} members", guild.Name, guild.Id, guild.MemberCount);
        }

        private async Task ClientLeftGuild(SocketGuild guild)
        {
            Log.Information(
                "LeftGuild: {guildName} / {guildId} | {memberCount} members", guild.Name, guild.Id, guild.MemberCount);
        }
    }
}
