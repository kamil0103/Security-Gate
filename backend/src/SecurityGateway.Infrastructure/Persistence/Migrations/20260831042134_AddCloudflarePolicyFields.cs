using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityGateway.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddCloudflarePolicyFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "AllowedCloudflareCountries",
                table: "ApplicationPolicies",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BlockedCloudflareCountries",
                table: "ApplicationPolicies",
                type: "character varying(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "BypassAuthenticationPaths",
                table: "ApplicationPolicies",
                type: "character varying(2000)",
                maxLength: 2000,
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowedCloudflareCountries",
                table: "ApplicationPolicies");

            migrationBuilder.DropColumn(
                name: "BlockedCloudflareCountries",
                table: "ApplicationPolicies");

            migrationBuilder.DropColumn(
                name: "BypassAuthenticationPaths",
                table: "ApplicationPolicies");
        }
    }
}
