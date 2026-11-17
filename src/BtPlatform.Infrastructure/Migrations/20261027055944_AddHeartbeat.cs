using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BtPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddHeartbeat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "IsOnline",
                table: "Computers",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTime>(
                name: "LastSeenAt",
                table: "Computers",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsOnline",
                table: "Computers");

            migrationBuilder.DropColumn(
                name: "LastSeenAt",
                table: "Computers");
        }
    }
}
