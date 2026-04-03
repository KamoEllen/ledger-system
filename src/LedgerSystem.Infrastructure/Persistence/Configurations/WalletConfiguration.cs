using LedgerSystem.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerSystem.Infrastructure.Persistence.Configurations;

public sealed class WalletConfiguration : IEntityTypeConfiguration<Wallet>
{
    public void Configure(EntityTypeBuilder<Wallet> builder)
    {
        builder.ToTable("wallets");

        builder.HasKey(w => w.Id);

        builder.Property(w => w.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(w => w.UserId)
            .HasColumnName("user_id")
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(w => w.Currency)
            .HasColumnName("currency")
            .HasColumnType("character(3)")
            .HasMaxLength(3)
            .IsRequired();

        // NUMERIC(19,4) — exact decimal storage, never float for money
        builder.Property(w => w.Balance)
            .HasColumnName("balance")
            .HasColumnType("numeric(19,4)")
            .HasDefaultValue(0m)
            .IsRequired();

        builder.Property(w => w.IsActive)
            .HasColumnName("is_active")
            .HasColumnType("boolean")
            .HasDefaultValue(true)
            .IsRequired();

        builder.Property(w => w.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // DB-level guard: balance can never be stored negative
        builder.ToTable(t =>
            t.HasCheckConstraint("ck_wallets_balance_non_negative", "balance >= 0"));

        // FK → users
        builder.HasOne<User>()
            .WithMany(u => u.Wallets)
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Navigation: one wallet → many ledger entries
        builder.HasMany(w => w.LedgerEntries)
            .WithOne()
            .HasForeignKey(le => le.WalletId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Navigation(w => w.LedgerEntries)
            .HasField("_ledgerEntries")
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        // Indexes
        builder.HasIndex(w => w.UserId)
            .HasDatabaseName("idx_wallets_user_id");

        builder.HasIndex(w => new { w.UserId, w.Currency })
            .HasDatabaseName("idx_wallets_user_currency");
    }
}
