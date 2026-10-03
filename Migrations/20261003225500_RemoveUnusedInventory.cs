using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EHMR.Migrations;

public partial class RemoveUnusedInventory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(
            name: "Inventories");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Inventories",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false),
                UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                CurrentStock = table.Column<int>(type: "int", nullable: false),
                InitialStock = table.Column<int>(type: "int", nullable: false),
                LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                MaximumStockLevel = table.Column<int>(type: "int", nullable: false),
                MedicineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                MinimumStockAlert = table.Column<int>(type: "int", nullable: false),
                MinimumStockLevel = table.Column<int>(type: "int", nullable: false),
                ReservedStock = table.Column<int>(type: "int", nullable: false),
                Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Inventories", x => x.Id);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Inventories_MedicineId",
            table: "Inventories",
            column: "MedicineId");
    }
}