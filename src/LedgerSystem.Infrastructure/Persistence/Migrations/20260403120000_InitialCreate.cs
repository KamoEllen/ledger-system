using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace LedgerSystem.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // ── users ────────────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "users",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false,
                        defaultValueSql: "gen_random_uuid()"),
                    email = table.Column<string>(type: "character varying(255)",
                        maxLength: 255, nullable: false),
                    password_hash = table.Column<string>(type: "text", nullable: false),
                    role = table.Column<string>(type: "character varying(50)",
                        maxLength: 50, nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone",
                        nullable: false, defaultValueSql: "NOW()"),
                    updated_at = table.Column<DateTime>(type: "timestamp with time zone",
                        nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_users", x => x.id);
                });

            migrationBuilder.CreateIndex(
                name: "idx_users_email",
                table: "users",
                column: "email",
                unique: true);

            // ── wallets ──────────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "wallets",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false,
                        defaultValueSql: "gen_random_uuid()"),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    currency = table.Column<string>(type: "character(3)",
                        maxLength: 3, nullable: false),
                    balance = table.Column<decimal>(type: "numeric(19,4)",
                        nullable: false, defaultValue: 0m),
                    is_active = table.Column<bool>(type: "boolean",
                        nullable: false, defaultValue: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone",
                        nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_wallets", x => x.id);
                    table.CheckConstraint("ck_wallets_balance_non_negative", "balance >= 0");
                    table.ForeignKey(
                        name: "FK_wallets_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_wallets_user_id",
                table: "wallets",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "idx_wallets_user_currency",
                table: "wallets",
                columns: new[] { "user_id", "currency" });

            // ── transfers ────────────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "transfers",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false,
                        defaultValueSql: "gen_random_uuid()"),
                    source_wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    destination_wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    amount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    currency = table.Column<string>(type: "character(3)",
                        maxLength: 3, nullable: false),
                    status = table.Column<string>(type: "character varying(20)",
                        maxLength: 20, nullable: false),
                    idempotency_key = table.Column<string>(type: "character varying(255)",
                        maxLength: 255, nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone",
                        nullable: false, defaultValueSql: "NOW()"),
                    completed_at = table.Column<DateTime>(type: "timestamp with time zone",
                        nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfers", x => x.id);
                    table.CheckConstraint("ck_transfers_amount_positive", "amount > 0");
                    table.CheckConstraint("ck_transfers_different_wallets",
                        "source_wallet_id != destination_wallet_id");
                    table.ForeignKey(
                        name: "FK_transfers_wallets_source_wallet_id",
                        column: x => x.source_wallet_id,
                        principalTable: "wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transfers_wallets_destination_wallet_id",
                        column: x => x.destination_wallet_id,
                        principalTable: "wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_transfers_idempotency_key",
                table: "transfers",
                column: "idempotency_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "idx_transfers_source_wallet_id",
                table: "transfers",
                column: "source_wallet_id");

            migrationBuilder.CreateIndex(
                name: "idx_transfers_destination_wallet_id",
                table: "transfers",
                column: "destination_wallet_id");

            migrationBuilder.CreateIndex(
                name: "idx_transfers_created_at",
                table: "transfers",
                column: "created_at");

            // ── ledger_entries ───────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "ledger_entries",
                columns: table => new
                {
                    id = table.Column<Guid>(type: "uuid", nullable: false,
                        defaultValueSql: "gen_random_uuid()"),
                    wallet_id = table.Column<Guid>(type: "uuid", nullable: false),
                    transfer_id = table.Column<Guid>(type: "uuid", nullable: true),
                    entry_type = table.Column<string>(type: "character varying(10)",
                        maxLength: 10, nullable: false),
                    amount = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    balance_after = table.Column<decimal>(type: "numeric(19,4)", nullable: false),
                    description = table.Column<string>(type: "text", nullable: true),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone",
                        nullable: false, defaultValueSql: "NOW()")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ledger_entries", x => x.id);
                    table.CheckConstraint("ck_ledger_entries_amount_positive", "amount > 0");
                    table.ForeignKey(
                        name: "FK_ledger_entries_wallets_wallet_id",
                        column: x => x.wallet_id,
                        principalTable: "wallets",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ledger_entries_transfers_transfer_id",
                        column: x => x.transfer_id,
                        principalTable: "transfers",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "idx_ledger_entries_wallet_created_at",
                table: "ledger_entries",
                columns: new[] { "wallet_id", "created_at" });

            migrationBuilder.CreateIndex(
                name: "idx_ledger_entries_created_at",
                table: "ledger_entries",
                column: "created_at");

            // ── idempotency_keys ─────────────────────────────────────────────────
            migrationBuilder.CreateTable(
                name: "idempotency_keys",
                columns: table => new
                {
                    key = table.Column<string>(type: "character varying(255)",
                        maxLength: 255, nullable: false),
                    user_id = table.Column<Guid>(type: "uuid", nullable: false),
                    request_path = table.Column<string>(type: "text", nullable: false),
                    response_status = table.Column<int>(type: "integer", nullable: false),
                    response_body = table.Column<string>(type: "jsonb", nullable: false),
                    created_at = table.Column<DateTime>(type: "timestamp with time zone",
                        nullable: false, defaultValueSql: "NOW()"),
                    expires_at = table.Column<DateTime>(type: "timestamp with time zone",
                        nullable: false, defaultValueSql: "NOW() + INTERVAL '24 hours'")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_idempotency_keys", x => new { x.key, x.user_id });
                    table.ForeignKey(
                        name: "FK_idempotency_keys_users_user_id",
                        column: x => x.user_id,
                        principalTable: "users",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "idx_idempotency_keys_expires_at",
                table: "idempotency_keys",
                column: "expires_at");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "idempotency_keys");
            migrationBuilder.DropTable(name: "ledger_entries");
            migrationBuilder.DropTable(name: "transfers");
            migrationBuilder.DropTable(name: "wallets");
            migrationBuilder.DropTable(name: "users");
        }
    }
}
