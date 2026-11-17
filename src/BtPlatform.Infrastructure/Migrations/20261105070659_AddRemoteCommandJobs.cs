using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BtPlatform.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddRemoteCommandJobs : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RemoteCommandJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ComputerId = table.Column<int>(type: "INTEGER", nullable: true),
                    Hostname = table.Column<string>(type: "TEXT", nullable: false),
                    IsRealTarget = table.Column<bool>(type: "INTEGER", nullable: false),
                    CatalogKey = table.Column<string>(type: "TEXT", nullable: true),
                    CommandText = table.Column<string>(type: "TEXT", nullable: false),
                    Reason = table.Column<string>(type: "TEXT", nullable: false),
                    Status = table.Column<string>(type: "TEXT", nullable: false),
                    RequestedBy = table.Column<string>(type: "TEXT", nullable: false),
                    RequestedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    StartedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    Output = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RemoteCommandJobs", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RemoteCommandJobs_Computers_ComputerId",
                        column: x => x.ComputerId,
                        principalTable: "Computers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RemoteCommandJobs_ComputerId",
                table: "RemoteCommandJobs",
                column: "ComputerId");

            migrationBuilder.CreateIndex(
                name: "IX_RemoteCommandJobs_Status",
                table: "RemoteCommandJobs",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RemoteCommandJobs");
        }
    }
}
