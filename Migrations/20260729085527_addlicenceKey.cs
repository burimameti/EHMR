using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EHMR.Migrations
{
    /// <inheritdoc />
    public partial class addlicenceKey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AppLicenses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    LicenseKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LicensedTo = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ValidUntil = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MaxAllowedPatients = table.Column<int>(type: "int", nullable: false),
                    IsActivated = table.Column<bool>(type: "bit", nullable: false),
                    DemoOffered = table.Column<bool>(type: "bit", nullable: false),
                    DemoDataSeeded = table.Column<bool>(type: "bit", nullable: false),
                    IsLocked = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AppLicenses", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AppLicenses");
        }
    }
}
