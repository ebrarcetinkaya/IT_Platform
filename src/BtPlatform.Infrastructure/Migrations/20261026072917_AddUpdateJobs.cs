using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BtPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddUpdateJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "UpdateJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ComputerId = table.Column<int>(type: "INTEGER", nullable: true),
                    InstalledAppId = table.Column<int>(type: "INTEGER", nullable: true),
                    Hostname = table.Column<string>(type: "TEXT", nullable: false),
                    AppName = table.Column<string>(type: "TEXT", nullable: false),
                    FromVersion = table.Column<string>(type: "TEXT", nullable: false),
                    ToVersion = table.Column<string>(type: "TEXT", nullable: false),
                    PackageId = table.Column<string>(type: "TEXT", nullable: false),
                    Command = table.Column<string>(type: "TEXT", nullable: false),
                    InstallerSha256 = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    RequestedBy = table.Column<string>(type: "TEXT", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Output = table.Column<string>(type: "TEXT", nullable: false),
                    BatchId = table.Column<Guid>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UpdateJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UpdateJobs_Computers_ComputerId",
                        column: x => x.ComputerId,
                        principalTable: "Computers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_UpdateJobs_InstalledApps_InstalledAppId",
                        column: x => x.InstalledAppId,
                        principalTable: "InstalledApps",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UpdateJobs_BatchId",
                table: "UpdateJobs",
                column: "BatchId");

            migrationBuilder.CreateIndex(
                name: "IX_UpdateJobs_ComputerId",
                table: "UpdateJobs",
                column: "ComputerId");

            migrationBuilder.CreateIndex(
                name: "IX_UpdateJobs_InstalledAppId",
                table: "UpdateJobs",
                column: "InstalledAppId");

            migrationBuilder.CreateIndex(
                name: "IX_UpdateJobs_Status",
                table: "UpdateJobs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UpdateJobs");
        }
    }
}
