using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityGateway.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddRateLimiting : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "RateLimitRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ScopeType = table.Column<int>(type: "integer", nullable: false),
                    ScopeValue = table.Column<string>(type: "character varying(500)", maxLength: 500, nullable: true),
                    RequestsPerWindow = table.Column<int>(type: "integer", nullable: false),
                    WindowSeconds = table.Column<int>(type: "integer", nullable: false),
                    BurstAllowance = table.Column<int>(type: "integer", nullable: false),
                    IsEnabled = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RateLimitRules", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_RateLimitRules_ScopeType_ScopeValue",
                table: "RateLimitRules",
                columns: new[] { "ScopeType", "ScopeValue" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RateLimitRules");
        }
    }
}
