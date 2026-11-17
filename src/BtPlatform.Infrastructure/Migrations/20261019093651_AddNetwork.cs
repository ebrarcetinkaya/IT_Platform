using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BtPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddNetwork : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Switches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: false),
                    Location = table.Column<string>(type: "TEXT", nullable: false),
                    ManagementIp = table.Column<string>(type: "TEXT", nullable: false),
                    CpuPercent = table.Column<int>(type: "INTEGER", nullable: false),
                    MemoryPercent = table.Column<int>(type: "INTEGER", nullable: false),
                    UptimeHours = table.Column<int>(type: "INTEGER", nullable: false),
                    LastPolledAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ConfigText = table.Column<string>(type: "TEXT", nullable: false),
                    ConfigHash = table.Column<string>(type: "TEXT", nullable: false),
                    ConfigChangedAt = table.Column<DateTime>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Switches", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Title = table.Column<string>(type: "TEXT", nullable: false),
                    Message = table.Column<string>(type: "TEXT", nullable: false),
                    Severity = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ResolvedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SwitchId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Alerts_Switches_SwitchId",
                        column: x => x.SwitchId,
                        principalTable: "Switches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "SwitchPorts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SwitchId = table.Column<int>(type: "INTEGER", nullable: false),
                    Number = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    Vlan = table.Column<int>(type: "INTEGER", nullable: false),
                    SpeedMbps = table.Column<int>(type: "INTEGER", nullable: false),
                    TrafficInMbps = table.Column<double>(type: "REAL", nullable: false),
                    TrafficOutMbps = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SwitchPorts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SwitchPorts_Switches_SwitchId",
                        column: x => x.SwitchId,
                        principalTable: "Switches",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Alerts_SwitchId",
                table: "Alerts",
                column: "SwitchId");

            migrationBuilder.CreateIndex(
                name: "IX_Switches_Name",
                table: "Switches",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SwitchPorts_SwitchId",
                table: "SwitchPorts",
                column: "SwitchId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "SwitchPorts");

            migrationBuilder.DropTable(
                name: "Switches");
        }
    }
}
