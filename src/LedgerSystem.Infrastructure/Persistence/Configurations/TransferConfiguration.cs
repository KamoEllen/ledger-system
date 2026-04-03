using LedgerSystem.Domain.Entities;
using LedgerSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerSystem.Infrastructure.Persistence.Configurations;

public sealed class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> builder)
    {
        builder.ToTable("transfers");

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(t => t.SourceWalletId)
            .HasColumnName("source_wallet_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(t => t.DestinationWalletId)
            .HasColumnName("destination_wallet_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(t => t.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(19,4)")
            .IsRequired();

        builder.Property(t => t.Currency)
            .HasColumnName("currency")
            .HasColumnType("character(3)")
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(t => t.Status)
            .HasColumnName("status")
            .HasColumnType("character varying(20)")
            .HasMaxLength(20)
            .HasConversion<string>()
            .IsRequired();

        // Idempotency key stored and indexed for fast duplicate detection
        builder.Property(t => t.IdempotencyKey)
            .HasColumnName("idempotency_key")
            .HasColumnType("character varying(255)")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(t => t.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(t => t.CompletedAt)
            .HasColumnName("completed_at")
            .HasColumnType("timestamp with time zone")
            .IsRequired(false);

        // DB-level guards
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("ck_transfers_amount_positive", "amount > 0");
            t.HasCheckConstraint("ck_transfers_different_wallets",
                "source_wallet_id != destination_wallet_id");
        });

        // FK → wallets (source)
        builder.HasOne<Wallet>()
            .WithMany()
            .HasForeignKey(t => t.SourceWalletId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK → wallets (destination)
        builder.HasOne<Wallet>()
            .WithMany()
            .HasForeignKey(t => t.DestinationWalletId)
            .OnDelete(DeleteBehavior.Restrict);

        // Unique index on idempotency key — prevents duplicate inserts at DB level
        builder.HasIndex(t => t.IdempotencyKey)
            .IsUnique()
            .HasDatabaseName("idx_transfers_idempotency_key");

        builder.HasIndex(t => t.SourceWalletId)
            .HasDatabaseName("idx_transfers_source_wallet_id");

        builder.HasIndex(t => t.DestinationWalletId)
            .HasDatabaseName("idx_transfers_destination_wallet_id");

        builder.HasIndex(t => t.CreatedAt)
            .HasDatabaseName("idx_transfers_created_at");
    }
}
