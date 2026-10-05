using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

using JetBrains.Annotations;

namespace Rasa.Migrations.MySqlAuth
{
    using Context.Auth;

    /// <summary>
    /// account.password holds a PBKDF2 hash string now (PasswordHasher:
    /// pbkdf2-sha256$iterations$pepper$hash, about 90 characters), not 64 hex digits of SHA-256,
    /// so the column is widened to varchar(255). Existing hashes are untouched; each account is
    /// rehashed at its next login.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    [UsedImplicitly]
    [DbContext(typeof(MySqlAuthContext))]
    [Migration("20261001000000_Widen_password_hash")]
    public partial class Widen_password_hash : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "password",
                table: "account",
                type: "varchar(255)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(64)");
        }

        /// <summary>Only while every hash still fits: an account rehashed since would be cut short.</summary>
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "password",
                table: "account",
                type: "varchar(64)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(255)");
        }
    }
}
