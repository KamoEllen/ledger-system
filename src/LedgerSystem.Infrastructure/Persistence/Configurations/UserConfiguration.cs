using LedgerSystem.Domain.Entities;
using LedgerSystem.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace LedgerSystem.Infrastructure.Persistence.Configurations;

public sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("users");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .HasColumnType("uuid")
            .HasDefaultValueSql("gen_random_uuid()")
            .ValueGeneratedOnAdd();

        builder.Property(u => u.Email)
            .HasColumnName("email")
            .HasColumnType("character varying(255)")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(u => u.PasswordHash)
            .HasColumnName("password_hash")
            .HasColumnType("text")
            .IsRequired();

        builder.Property(u => u.Role)
            .HasColumnName("role")
            .HasColumnType("character varying(50)")
            .HasMaxLength(50)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(u => u.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        builder.Property(u => u.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("NOW()")
            .IsRequired();

        // Unique index on email — login lookups and duplicate prevention
        builder.HasIndex(u => u.Email)
            .IsUnique()
            .HasDatabaseName("idx_users_email");

        // Navigation: one user → many wallets
        builder.HasMany(u => u.Wallets)
            .WithOne()
            .HasForeignKey(w => w.UserId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure backing field for the read-only Wallets collection
        builder.Navigation(u => u.Wallets)
            .HasField("_wallets")
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
