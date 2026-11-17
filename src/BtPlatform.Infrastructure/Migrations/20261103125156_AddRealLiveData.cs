using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BtPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRealLiveData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "CpuPercent",
                table: "Computers",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DiskFreeGb",
                table: "Computers",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "DiskTotalGb",
                table: "Computers",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "MetricsAt",
                table: "Computers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RamTotalGb",
                table: "Computers",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "RamUsedGb",
                table: "Computers",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ScanAt",
                table: "Computers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ScanError",
                table: "Computers",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "UptimeHours",
                table: "Computers",
                type: "REAL",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "RealUpgradeFindings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ComputerId = table.Column<int>(type: "INTEGER", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    PackageId = table.Column<string>(type: "TEXT", nullable: false),
                    CurrentVersion = table.Column<string>(type: "TEXT", nullable: false),
                    AvailableVersion = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    FoundAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RealUpgradeFindings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RealUpgradeFindings_Computers_ComputerId",
                        column: x => x.ComputerId,
                        principalTable: "Computers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RealUpgradeFindings_ComputerId",
                table: "RealUpgradeFindings",
                column: "ComputerId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RealUpgradeFindings");

            migrationBuilder.DropColumn(
                name: "CpuPercent",
                table: "Computers");

            migrationBuilder.DropColumn(
                name: "DiskFreeGb",
                table: "Computers");

            migrationBuilder.DropColumn(
                name: "DiskTotalGb",
                table: "Computers");

            migrationBuilder.DropColumn(
                name: "MetricsAt",
                table: "Computers");

            migrationBuilder.DropColumn(
                name: "RamTotalGb",
                table: "Computers");

            migrationBuilder.DropColumn(
                name: "RamUsedGb",
                table: "Computers");

            migrationBuilder.DropColumn(
                name: "ScanAt",
                table: "Computers");

            migrationBuilder.DropColumn(
                name: "ScanError",
                table: "Computers");

            migrationBuilder.DropColumn(
                name: "UptimeHours",
                table: "Computers");
        }
    }
}
