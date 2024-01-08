using Google.Protobuf.WellKnownTypes;
using Microsoft.EntityFrameworkCore;
using PodsProto.V1;

namespace SensorM.GsoCommon.ServerLibrary.Context
{
    public class PodsContext : DbContext
    {
        public DbSet<UserIdentity> Users { get; set; }

        public DbSet<ManualContactInfo> ContactForUser { get; set; }

        public DbSet<ContactInfo> SharedContact { get; set; }

        public DbSet<ChatInfo> Chats { get; set; }

        public DbSet<ConnectInfo> Connects { get; set; }

        public DbSet<ChatMessage> Messages { get; set; }

        public DbSet<NoReadMessages> NoReadMessages { get; set; }

        public PodsContext(DbContextOptions<PodsContext> options) : base(options)
        {
           
        }

        protected override void OnConfiguring(DbContextOptionsBuilder options)
        {
            base.OnConfiguring(options);
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserIdentity>(entity =>
            {
                entity.Property(b => b.Id).ValueGeneratedOnAdd();
                entity.HasMany<ManualContactInfo>().WithOne().HasForeignKey(e => e.UserIdentityId);
                entity.HasMany<ChatInfo>().WithOne().HasForeignKey(e => e.UserIdentityId);
                entity.HasMany<NoReadMessages>().WithOne().HasForeignKey(e => e.UserIdentityId);
            });

            modelBuilder.Entity<ManualContactInfo>(entity =>
            {
                entity.Property(b => b.Id).ValueGeneratedOnAdd();
                entity.Property(b => b.LastActive).HasConversion(b => b.ToDateTimeOffset(), b => b.ToTimestamp());
            });

            modelBuilder.Entity<ContactInfo>(entity =>
            {
                entity.Property(b => b.Id).ValueGeneratedOnAdd();
                entity.Property(b => b.LastActive).HasConversion(b => b.ToDateTimeOffset(), b => b.ToTimestamp());
            });
            modelBuilder.Entity<ChatInfo>(entity =>
            {
                entity.Property(b => b.Id).ValueGeneratedOnAdd();
                entity.HasMany<ConnectInfo>(e => e.Items).WithOne().HasForeignKey(e => e.ChatInfoId);
                entity.HasMany<ChatMessage>().WithOne().HasForeignKey(e => e.ChatInfoId);
                entity.HasMany<NoReadMessages>().WithOne().HasForeignKey(e => e.ChatInfoId);
            });
            modelBuilder.Entity<ConnectInfo>(entity =>
            {
                entity.Property(b => b.Id).ValueGeneratedOnAdd();
                entity.HasOne<ChatInfo>().WithMany(e => e.Items).HasForeignKey(e => e.ChatInfoId);
            });
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.Property(b => b.Id).ValueGeneratedOnAdd();
                entity.Property(b => b.Date).HasConversion(b => b.ToDateTimeOffset(), b => b.ToTimestamp());
            });

            modelBuilder.Entity<NoReadMessages>(entity =>
            {
                entity.Property(b => b.Id).ValueGeneratedOnAdd();
            });

            base.OnModelCreating(modelBuilder);
        }


    }

}
