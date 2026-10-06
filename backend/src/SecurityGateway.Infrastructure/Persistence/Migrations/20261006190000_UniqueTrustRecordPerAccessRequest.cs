using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityGateway.Infrastructure.Persistence.Migrations;

[DbContext(typeof(ApplicationDbContext))]
[Migration("20261006190000_UniqueTrustRecordPerAccessRequest")]
public sealed class UniqueTrustRecordPerAccessRequest : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_TrustRecords_AccessRequestId",
            table: "TrustRecords");

        migrationBuilder.CreateIndex(
            name: "IX_TrustRecords_AccessRequestId",
            table: "TrustRecords",
            column: "AccessRequestId",
            unique: true,
            filter: "\"AccessRequestId\" IS NOT NULL");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropIndex(
            name: "IX_TrustRecords_AccessRequestId",
            table: "TrustRecords");

        migrationBuilder.CreateIndex(
            name: "IX_TrustRecords_AccessRequestId",
            table: "TrustRecords",
            column: "AccessRequestId");
    }
}
