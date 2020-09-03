using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FMBot.Domain;
using FMBot.Persistence.Domain.Models;
using IF.Lastfm.Core.Api;
using IF.Lastfm.Core.Api.Enums;
using IF.Lastfm.Core.Objects;
using Microsoft.Extensions.Configuration;
using Npgsql;
using PostgreSQLCopyHelper;
using Serilog;

namespace FMBot.LastFM.Services
{
    public class GlobalIndexService
    {
        private readonly LastfmClient _lastFMClient;

        private readonly string _key;
        private readonly string _secret;

        private readonly string _connectionString;

        private readonly LastFMService lastFmService;

        public GlobalIndexService(
            IConfigurationRoot configuration,
            LastFMService lastFmService)
        {
            this.lastFmService = lastFmService;
            this._key = configuration.GetSection("LastFm:Key").Value;
            this._secret = configuration.GetSection("LastFm:Secret").Value;
            this._connectionString = configuration.GetSection("Database:ConnectionString").Value;
            this._lastFMClient = new LastfmClient(this._key, this._secret);
        }

        public async Task IndexUser(User user)
        {
            Thread.Sleep(10000);

            Log.Information($"Starting index for {user.UserNameLastFM}");
            var now = DateTime.UtcNow;

            var artists = await GetArtistsForUserFromLastFm(user);
            await InsertArtistsIntoDatabase(artists, user.UserId);

            var albums = await GetAlbumsForUserFromLastFm(user);
            await InsertAlbumsIntoDatabase(albums, user.UserId);

            var tracks = await GetTracksForUserFromLastFm(user);
            await InsertTracksIntoDatabase(tracks, user.UserId);

            var latestScrobbleDate = await GetLatestScrobbleDate(user);
            await SetUserIndexTime(user.UserId, now, latestScrobbleDate);
        }

        private async Task<IReadOnlyList<UserArtist>> GetArtistsForUserFromLastFm(User user)
        {
            Log.Information($"Getting artists for user {user.UserNameLastFM}");

            var topArtists = new List<LastArtist>();

            const int amountOfApiCalls = 4000 / 1000;
            for (var i = 1; i < amountOfApiCalls + 1; i++)
            {
                var artistResult = await this._lastFMClient.User.GetTopArtists(user.UserNameLastFM,
                    LastStatsTimeSpan.Overall, i, 1000);
                Statistics.LastfmApiCalls.Inc();

                topArtists.AddRange(artistResult);

                if (artistResult.Count() < 1000)
                {
                    break;
                }
            }

            if (topArtists.Count == 0)
            {
                return new List<UserArtist>();
            }

            return topArtists.Select(a => new UserArtist
            {
                Name = a.Name,
                Playcount = a.PlayCount.Value,
                UserId = user.UserId
            }).ToList();
        }

        private async Task<IReadOnlyList<UserAlbum>> GetAlbumsForUserFromLastFm(User user)
        {
            Log.Information($"Getting albums for user {user.UserNameLastFM}");

            var topAlbums = new List<LastAlbum>();

            const int amountOfApiCalls = 5000 / 1000;
            for (var i = 1; i < amountOfApiCalls + 1; i++)
            {
                var albumResult = await this._lastFMClient.User.GetTopAlbums(user.UserNameLastFM,
                    LastStatsTimeSpan.Overall, i, 1000);
                Statistics.LastfmApiCalls.Inc();

                topAlbums.AddRange(albumResult);

                if (albumResult.Count() < 1000)
                {
                    break;
                }
            }

            if (topAlbums.Count == 0)
            {
                return new List<UserAlbum>();
            }

            return topAlbums.Select(a => new UserAlbum
            {
                Name = a.Name,
                ArtistName = a.ArtistName,
                Playcount = a.PlayCount.Value,
                UserId = user.UserId
            }).ToList();
        }

        private async Task<IReadOnlyList<UserTrack>> GetTracksForUserFromLastFm(User user)
        {
            Log.Information($"Getting tracks for user {user.UserNameLastFM}");

            var trackResult = await this.lastFmService.GetTopTracksAsync(user.UserNameLastFM, "overall", 1000, 8);

            if (!trackResult.Success || trackResult.Content.TopTracks.Track.Count == 0)
            {
                return new List<UserTrack>();
            }

            return trackResult.Content.TopTracks.Track.Select(a => new UserTrack
            {
                Name = a.Name,
                ArtistName = a.Artist.Name,
                Playcount = Convert.ToInt32(a.Playcount),
                UserId = user.UserId
            }).ToList();
        }

        private async Task InsertArtistsIntoDatabase(IReadOnlyList<UserArtist> artists, int userId)
        {
            Log.Information($"Inserting artists for user {userId}");

            var copyHelper = new PostgreSQLCopyHelper<UserArtist>("public", "user_artists")
                .MapText("name", x => x.Name)
                .MapInteger("user_id", x => x.UserId)
                .MapInteger("playcount", x => x.Playcount);

            await using var connection = new NpgsqlConnection(this._connectionString);
            connection.Open();

            await using var deleteCurrentArtists = new NpgsqlCommand($"DELETE FROM public.user_artists WHERE user_id = {userId};", connection);
            await deleteCurrentArtists.ExecuteNonQueryAsync();

            await copyHelper.SaveAllAsync(connection, artists);
        }

        private async Task InsertAlbumsIntoDatabase(IReadOnlyList<UserAlbum> albums, int userId)
        {
            Log.Information($"Inserting albums for user {userId}");

            var copyHelper = new PostgreSQLCopyHelper<UserAlbum>("public", "user_albums")
                .MapText("name", x => x.Name)
                .MapText("artist_name", x => x.ArtistName)
                .MapInteger("user_id", x => x.UserId)
                .MapInteger("playcount", x => x.Playcount);

            await using var connection = new NpgsqlConnection(this._connectionString);
            connection.Open();

            await using var deleteCurrentAlbums = new NpgsqlCommand($"DELETE FROM public.user_albums WHERE user_id = {userId};", connection);
            await deleteCurrentAlbums.ExecuteNonQueryAsync();

            await copyHelper.SaveAllAsync(connection, albums);
        }

        private async Task InsertTracksIntoDatabase(IReadOnlyList<UserTrack> artists, int userId)
        {
            Log.Information($"Inserting tracks for user {userId}");

            var copyHelper = new PostgreSQLCopyHelper<UserTrack>("public", "user_tracks")
                .MapText("name", x => x.Name)
                .MapText("artist_name", x => x.ArtistName)
                .MapInteger("user_id", x => x.UserId)
                .MapInteger("playcount", x => x.Playcount);

            await using var connection = new NpgsqlConnection(this._connectionString);
            connection.Open();

            await using var deleteCurrentTracks = new NpgsqlCommand($"DELETE FROM public.user_tracks WHERE user_id = {userId};", connection);
            await deleteCurrentTracks.ExecuteNonQueryAsync();

            await copyHelper.SaveAllAsync(connection, artists);

        }

        private async Task SetUserIndexTime(int userId, DateTime now, DateTime lastScrobble)
        {
            Log.Information($"Setting user index time for user {userId}");

            await using var connection = new NpgsqlConnection(this._connectionString);
            connection.Open();

            await using var setIndexTime = new NpgsqlCommand($"UPDATE public.users SET last_indexed='{now:u}', last_updated='{now:u}', last_scrobble_update = '{lastScrobble:u}' WHERE user_id = {userId};", connection);
            await setIndexTime.ExecuteNonQueryAsync().ConfigureAwait(false);
        }

        private async Task<DateTime> GetLatestScrobbleDate(User user)
        {
            var recentTracks = await this._lastFMClient.User.GetRecentScrobbles(user.UserNameLastFM, count: 1);
            Statistics.LastfmApiCalls.Inc();
            if (!recentTracks.Success || !recentTracks.Content.Any() || !recentTracks.Content.Any(a => a.TimePlayed.HasValue))
            {
                Log.Information("Recent track call to get latest scrobble date failed!");
                return DateTime.UtcNow;
            }

            return recentTracks.Content.First(f => f.TimePlayed.HasValue).TimePlayed.Value.DateTime;
        }
    }
}
