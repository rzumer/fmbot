using System;
using FMBot.Persistence.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace FMBot.Persistence.EntityFrameWork
{
    public class FMBotDbContext : DbContext
    {
        public virtual DbSet<Friend> Friends { get; set; }
        public virtual DbSet<Guild> Guilds { get; set; }
        public virtual DbSet<GuildUser> GuildUsers { get; set; }
        public virtual DbSet<User> Users { get; set; }
        public virtual DbSet<Supporter> Supporters { get; set; }

        public virtual DbSet<InactiveUsers> InactiveUsers { get; set; }

        public virtual DbSet<UserArtist> UserArtists { get; set; }
        public virtual DbSet<UserAlbum> UserAlbums { get; set; }
        public virtual DbSet<UserTrack> UserTracks { get; set; }
        public virtual DbSet<UserPlay> UserPlays { get; set; }

        public virtual DbSet<Artist> Artists { get; set; }
        public virtual DbSet<Album> Albums { get; set; }
        public virtual DbSet<Track> Tracks { get; set; }

        public virtual DbSet<CensoredMusic> CensoredMusic { get; set; }

        public virtual DbSet<ArtistGenre> ArtistGenres { get; set; }
        public virtual DbSet<ArtistAlias> ArtistAliases { get; set; }


        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                // When creating migrations, make sure to enter the connection string below.
                optionsBuilder.UseNpgsql("Host=localhost;Port=5432;Username=postgres;Password=password;Database=fmbot;Command Timeout=1024;Timeout=1024;Persist Security Info=True"
                   , builder =>
                {
                    builder.EnableRetryOnFailure(3, TimeSpan.FromSeconds(10), null);
                });

                optionsBuilder.UseSnakeCaseNamingConvention();
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.HasPostgresExtension("citext");
            modelBuilder.HasPostgresExtension("pg_trgm");

            modelBuilder.Entity<Friend>(entity =>
            {
                entity.HasKey(e => e.FriendId);

                entity.HasOne(d => d.FriendUser)
                    .WithMany(p => p.FriendedByUsers)
                    .HasForeignKey(d => d.FriendUserId)
                    .HasConstraintName("FK.Friends.Users_FriendUserID");

                entity.HasOne(d => d.User)
                    .WithMany(p => p.Friends)
                    .HasForeignKey(d => d.UserId)
                    .HasConstraintName("FK.Friends.Users_UserID");
            });

            modelBuilder.Entity<Guild>(entity =>
            {
                entity.HasKey(e => e.GuildId);

                entity.HasIndex(i => i.DiscordGuildId);

                entity.Property(e => e.DisabledCommands)
                    .HasConversion(
                        v => string.Join(',', v),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries));

                entity.Property(e => e.EmoteReactions)
                    .HasConversion(
                        v => string.Join(',', v),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries));
            });

            modelBuilder.Entity<GuildUser>(entity =>
            {
                entity.HasKey(e => new { e.GuildId, e.UserId });

                entity.HasOne(sc => sc.Guild)
                    .WithMany(s => s.GuildUsers)
                    .HasForeignKey(sc => sc.GuildId);

                entity.HasOne(sc => sc.User)
                    .WithMany(s => s.GuildUsers)
                    .HasForeignKey(sc => sc.UserId);
            });

            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);

                entity.HasIndex(i => i.UserId);

                entity.HasIndex(i => i.DiscordUserId);
            });

            modelBuilder.Entity<Supporter>(entity =>
            {
                entity.HasKey(e => e.SupporterId);
            });

            modelBuilder.Entity<InactiveUsers>(entity =>
            {
                entity.HasKey(e => e.InactiveUserId);

                entity
                    .HasOne<User>()
                    .WithMany()
                    .HasForeignKey(p => p.UserId);
            });

            modelBuilder.Entity<UserArtist>(entity =>
            {
                entity.HasKey(a => a.UserArtistId);

                entity.HasIndex(i => i.UserId);

                entity.Property(e => e.Name)
                    .HasColumnType("citext");

                entity.HasOne(u => u.User)
                    .WithMany(a => a.Artists)
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserAlbum>(entity =>
            {
                entity.HasKey(a => a.UserAlbumId);

                entity.HasIndex(i => i.UserId);

                entity.Property(e => e.Name)
                    .HasColumnType("citext");

                entity.Property(e => e.ArtistName)
                    .HasColumnType("citext");

                entity.HasOne(u => u.User)
                    .WithMany(a => a.Albums)
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserTrack>(entity =>
            {
                entity.HasKey(a => a.UserTrackId);

                entity.HasIndex(i => i.UserId);

                entity.Property(e => e.Name)
                    .HasColumnType("citext");

                entity.Property(e => e.ArtistName)
                    .HasColumnType("citext");

                entity.HasOne(u => u.User)
                    .WithMany(a => a.Tracks)
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<UserPlay>(entity =>
            {
                entity.HasKey(a => a.UserPlayId);

                entity.HasIndex(i => i.UserId);

                entity.Property(e => e.TrackName)
                    .HasColumnType("citext");

                entity.Property(e => e.AlbumName)
                    .HasColumnType("citext");

                entity.Property(e => e.ArtistName)
                    .HasColumnType("citext");

                entity.HasOne(u => u.User)
                    .WithMany(a => a.Plays)
                    .HasForeignKey(f => f.UserId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<Artist>(entity =>
            {
                entity.HasKey(e => e.Id);

                entity.Property(e => e.Aliases)
                    .HasConversion(
                        v => string.Join(',', v),
                        v => v.Split(',', StringSplitOptions.RemoveEmptyEntries));
            });

            modelBuilder.Entity<Album>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.HasOne(d => d.Artist)
                    .WithMany(p => p.Albums)
                    .HasForeignKey(d => d.ArtistId);
            });

            modelBuilder.Entity<Track>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.HasOne(d => d.Artist)
                    .WithMany(p => p.Tracks)
                    .HasForeignKey(d => d.ArtistId);
            });

            modelBuilder.Entity<CensoredMusic>(entity =>
            {
                entity.HasKey(a => a.CensoredMusicId);
            });

            modelBuilder.Entity<ArtistAlias>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.HasOne(d => d.Artist)
                    .WithMany(p => p.ArtistAliases)
                    .HasForeignKey(d => d.ArtistId)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<ArtistGenre>(entity =>
            {
                entity.HasKey(a => a.Id);

                entity.HasOne(d => d.Artist)
                    .WithMany(p => p.ArtistGenres)
                    .HasForeignKey(d => d.ArtistId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
