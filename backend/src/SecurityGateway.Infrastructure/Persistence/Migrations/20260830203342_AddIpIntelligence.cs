using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace SecurityGateway.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddIpIntelligence : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "IpAddresses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Ip = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CountryCode = table.Column<string>(type: "character varying(8)", maxLength: 8, nullable: true),
                    Country = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Region = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    City = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    Latitude = table.Column<double>(type: "double precision", nullable: true),
                    Longitude = table.Column<double>(type: "double precision", nullable: true),
                    Isp = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Organization = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: true),
                    Asn = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: true),
                    IsVpn = table.Column<bool>(type: "boolean", nullable: false),
                    IsProxy = table.Column<bool>(type: "boolean", nullable: false),
                    IsTor = table.Column<bool>(type: "boolean", nullable: false),
                    IsDatacenter = table.Column<bool>(type: "boolean", nullable: false),
                    ThreatScore = table.Column<int>(type: "integer", nullable: false),
                    ThreatLevel = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: true),
                    ReputationSource = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RequestCount = table.Column<long>(type: "bigint", nullable: false),
                    AttackCount = table.Column<long>(type: "bigint", nullable: false),
                    BlockCount = table.Column<long>(type: "bigint", nullable: false),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpAddresses", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "IpDeviceAssociations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IpAddressId = table.Column<Guid>(type: "uuid", nullable: false),
                    DeviceId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RequestCount = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpDeviceAssociations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IpDeviceAssociations_Devices_DeviceId",
                        column: x => x.DeviceId,
                        principalTable: "Devices",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IpDeviceAssociations_IpAddresses_IpAddressId",
                        column: x => x.IpAddressId,
                        principalTable: "IpAddresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "IpUserAssociations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    IpAddressId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    FirstSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    LastSeenAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    RequestCount = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_IpUserAssociations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_IpUserAssociations_IpAddresses_IpAddressId",
                        column: x => x.IpAddressId,
                        principalTable: "IpAddresses",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_IpUserAssociations_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_IpAddresses_Ip",
                table: "IpAddresses",
                column: "Ip",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IpDeviceAssociations_DeviceId",
                table: "IpDeviceAssociations",
                column: "DeviceId");

            migrationBuilder.CreateIndex(
                name: "IX_IpDeviceAssociations_IpAddressId_DeviceId",
                table: "IpDeviceAssociations",
                columns: new[] { "IpAddressId", "DeviceId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IpUserAssociations_IpAddressId_UserId",
                table: "IpUserAssociations",
                columns: new[] { "IpAddressId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_IpUserAssociations_UserId",
                table: "IpUserAssociations",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "IpDeviceAssociations");

            migrationBuilder.DropTable(
                name: "IpUserAssociations");

            migrationBuilder.DropTable(
                name: "IpAddresses");
        }
    }
}
