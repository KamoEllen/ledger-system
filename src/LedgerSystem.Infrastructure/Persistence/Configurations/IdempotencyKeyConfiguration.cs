using LedgerSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerSystem.Infrastructure.Persistence.Configurations;

public sealed class IdempotencyKeyConfiguration : IEntityTypeConfiguration<IdempotencyKey>
{
    public void Configure(EntityTypeBuilder<IdempotencyKey> builder)
    {
        builder.ToTable("idempotency_keys");

        // Composite primary key: key + user_id
        // Prevents one user's key from colliding with another user's same key string.
        builder.HasKey(ik => new { ik.Key, ik.UserId });

        builder.Property(ik => ik.Key)
            .HasColumnName("key")
            .HasColumnType("character varying(255)")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(ik => ik.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(ik => ik.RequestPath)
            .HasColumnName("request_path")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(ik => ik.ResponseStatus)
            .HasColumnName("response_status")
            .HasColumnType("integer")
            .IsRequired();

        // Stored as JSONB for efficient partial reads and indexing if needed
        builder.Property(ik => ik.ResponseBody)
            .HasColumnName("response_body")
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(ik => ik.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(ik => ik.ExpiresAt)
            .HasColumnName("expires_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW() + INTERVAL '24 hours'")
            .IsRequired();

        // FK → users
        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(ik => ik.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index for the background cleanup job that deletes expired keys
        builder.HasIndex(ik => ik.ExpiresAt)
            .HasDatabaseName("idx_idempotency_keys_expires_at");
    }
}
