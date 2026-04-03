using LedgerSystem.Domain.Entities;
using LedgerSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerSystem.Infrastructure.Persistence.Configurations;

public sealed class LedgerEntryConfiguration : IEntityTypeConfiguration<LedgerEntry>
{
    public void Configure(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.ToTable("ledger_entries");

        builder.HasKey(le => le.Id);

        builder.Property(le => le.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(le => le.WalletId)
            .HasColumnName("wallet_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(le => le.TransferId)
            .HasColumnName("transfer_id")
            .HasColumnType("uuid")
            .IsRequired(false);

        builder.Property(le => le.EntryType)
            .HasColumnName("entry_type")
            .HasColumnType("character varying(10)")
            .HasMaxLength(10)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(le => le.Amount)
            .HasColumnName("amount")
            .HasColumnType("numeric(19,4)")
            .IsRequired();

        // Point-in-time balance snapshot: enables historical balance queries
        // without replaying all prior entries.
        builder.Property(le => le.BalanceAfter)
            .HasColumnName("balance_after")
            .HasColumnType("numeric(19,4)")
            .IsRequired();

        builder.Property(le => le.Description)
            .HasColumnName("description")
            .HasColumnType("text")
            .IsRequired(false);

        // No updated_at — ledger entries are intentionally immutable.
        builder.Property(le => le.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // DB-level guard: amounts stored must always be positive
        builder.ToTable(t =>
            t.HasCheckConstraint("ck_ledger_entries_amount_positive", "amount > 0"));

        // FK → wallets
        builder.HasOne<Wallet>()
            .WithMany(w => w.LedgerEntries)
            .HasForeignKey(le => le.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        // FK → transfers (nullable — entries can exist without a transfer, e.g. manual adjustments)
        builder.HasOne<Transfer>()
            .WithMany()
            .HasForeignKey(le => le.TransferId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired(false);

        // History queries: wallet + descending time is the dominant access pattern
        builder.HasIndex(le => new { le.WalletId, le.CreatedAt })
            .HasDatabaseName("idx_ledger_entries_wallet_created_at");

        // Point-in-time queries: find balance_after for a given wallet at timestamp T
        builder.HasIndex(le => le.CreatedAt)
            .HasDatabaseName("idx_ledger_entries_created_at");
    }
}
