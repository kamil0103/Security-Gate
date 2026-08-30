using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityGateway.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddWafEvents : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WafEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Timestamp = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    SourceIp = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RequestId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    RuleId = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    RuleMessage = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    Severity = table.Column<int>(type: "integer", nullable: false),
                    AttackType = table.Column<int>(type: "integer", nullable: false),
                    Method = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Uri = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: false),
                    Host = table.Column<string>(type: "character varying(253)", maxLength: 253, nullable: true),
                    Action = table.Column<int>(type: "integer", nullable: false),
                    RawLog = table.Column<string>(type: "text", nullable: true),
                    ReceivedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WafEvents", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_WafEvents_AttackType",
                table: "WafEvents",
                column: "AttackType");

            migrationBuilder.CreateIndex(
                name: "IX_WafEvents_Severity",
                table: "WafEvents",
                column: "Severity");

            migrationBuilder.CreateIndex(
                name: "IX_WafEvents_SourceIp",
                table: "WafEvents",
                column: "SourceIp");

            migrationBuilder.CreateIndex(
                name: "IX_WafEvents_Timestamp",
                table: "WafEvents",
                column: "Timestamp");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WafEvents");
        }
    }
}
