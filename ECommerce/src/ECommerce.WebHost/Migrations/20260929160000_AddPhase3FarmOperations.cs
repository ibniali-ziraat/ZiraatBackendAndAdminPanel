using System;
using ECommerce.Module.Core.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace ECommerce.WebHost.Migrations
{
    [DbContext(typeof(SimplDbContext))]
    [Migration("20260929160000_AddPhase3FarmOperations")]
    public class AddPhase3FarmOperations : Migration
    {
        protected override void Up(MigrationBuilder m)
        {
            m.AddColumn<long>("OrchardProfileId", "Core_FarmOperation", "bigint", nullable: false, defaultValue: 0L);
            m.AddColumn<long>("FarmerId", "Core_FarmOperation", "bigint", nullable: false, defaultValue: 0L);
            m.AddColumn<string>("Status", "Core_FarmOperation", "nvarchar(16)", maxLength: 16, nullable: false, defaultValue: "posted");
            m.AddColumn<decimal>("CoverageValue", "Core_FarmOperation", "decimal(18,4)", nullable: false, defaultValue: 0m);
            m.AddColumn<string>("CoverageUnit", "Core_FarmOperation", "nvarchar(16)", maxLength: 16, nullable: false, defaultValue: "ha");
            m.AddColumn<decimal>("LabourHours", "Core_FarmOperation", "decimal(18,4)", nullable: true);
            m.AddColumn<int>("NumberOfLabourers", "Core_FarmOperation", "int", nullable: true);
            m.AddColumn<string>("EquipmentJson", "Core_FarmOperation", "nvarchar(max)", nullable: true);
            m.AddColumn<decimal>("EquipmentHours", "Core_FarmOperation", "decimal(18,4)", nullable: true);
            m.AddColumn<decimal>("Cost", "Core_FarmOperation", "decimal(18,2)", nullable: true);
            m.AddColumn<string>("Currency", "Core_FarmOperation", "nvarchar(3)", maxLength: 3, nullable: true);
            m.AddColumn<string>("MaterialName", "Core_FarmOperation", "nvarchar(200)", maxLength: 200, nullable: true);
            m.AddColumn<decimal>("MaterialQuantity", "Core_FarmOperation", "decimal(18,4)", nullable: true);
            m.AddColumn<string>("MaterialUnit", "Core_FarmOperation", "nvarchar(16)", maxLength: 16, nullable: true);
            m.AddColumn<string>("Target", "Core_FarmOperation", "nvarchar(200)", maxLength: 200, nullable: true);
            m.AddColumn<decimal>("DoseValue", "Core_FarmOperation", "decimal(18,4)", nullable: true);
            m.AddColumn<string>("DoseUnit", "Core_FarmOperation", "nvarchar(16)", maxLength: 16, nullable: true);
            m.AddColumn<decimal>("MixtureTotalLitres", "Core_FarmOperation", "decimal(18,4)", nullable: true);
            m.AddColumn<decimal>("IrrigationTotalLitres", "Core_FarmOperation", "decimal(18,4)", nullable: true);
            m.AddColumn<string>("PruningType", "Core_FarmOperation", "nvarchar(32)", maxLength: 32, nullable: true);
            m.AddColumn<decimal>("HarvestQuantityKg", "Core_FarmOperation", "decimal(18,4)", nullable: true);
            m.AddColumn<string>("Notes", "Core_FarmOperation", "nvarchar(2000)", maxLength: 2000, nullable: true);
            m.AddColumn<DateTimeOffset>("UpdatedOn", "Core_FarmOperation", "datetimeoffset", nullable: false, defaultValueSql: "SYSUTCDATETIME()");
            m.AddColumn<DateTimeOffset>("VoidedOn", "Core_FarmOperation", "datetimeoffset", nullable: true);
            m.AddColumn<long>("VoidedById", "Core_FarmOperation", "bigint", nullable: true);
            m.AddColumn<string>("VoidReason", "Core_FarmOperation", "nvarchar(1000)", maxLength: 1000, nullable: true);
            m.CreateIndex("IX_Core_FarmOperation_OrchardProfileId_OperationDate", "Core_FarmOperation", new[] { "OrchardProfileId", "OperationDate" });
            m.CreateIndex("IX_Core_FarmOperation_FarmerId_Status", "Core_FarmOperation", new[] { "FarmerId", "Status" });
            m.CreateTable("Core_FarmHarvestSaleLot", table => new
            {
                Id = table.Column<Guid>("uniqueidentifier", nullable: false), OperationId = table.Column<Guid>("uniqueidentifier", nullable: false),
                QuantityKg = table.Column<decimal>("decimal(18,4)", nullable: false), PricePerKg = table.Column<decimal>("decimal(18,4)", nullable: true),
                Income = table.Column<decimal>("decimal(18,2)", nullable: true), IsSold = table.Column<bool>("bit", nullable: false),
                BuyerOrMandi = table.Column<string>("nvarchar(200)", maxLength: 200, nullable: true), CreatedOn = table.Column<DateTimeOffset>("datetimeoffset", nullable: false)
            }, constraints: table => { table.PrimaryKey("PK_Core_FarmHarvestSaleLot", x => x.Id); table.ForeignKey("FK_Core_FarmHarvestSaleLot_Core_FarmOperation_OperationId", x => x.OperationId, "Core_FarmOperation", "Id", onDelete: ReferentialAction.Restrict); });
            m.CreateIndex("IX_Core_FarmHarvestSaleLot_OperationId", "Core_FarmHarvestSaleLot", "OperationId");
        }
        protected override void Down(MigrationBuilder m)
        {
            m.DropTable("Core_FarmHarvestSaleLot");
            m.DropIndex("IX_Core_FarmOperation_OrchardProfileId_OperationDate", "Core_FarmOperation"); m.DropIndex("IX_Core_FarmOperation_FarmerId_Status", "Core_FarmOperation");
            foreach (var name in new[] { "OrchardProfileId", "FarmerId", "Status", "CoverageValue", "CoverageUnit", "LabourHours", "NumberOfLabourers", "EquipmentJson", "EquipmentHours", "Cost", "Currency", "MaterialName", "MaterialQuantity", "MaterialUnit", "Target", "DoseValue", "DoseUnit", "MixtureTotalLitres", "IrrigationTotalLitres", "PruningType", "HarvestQuantityKg", "Notes", "UpdatedOn", "VoidedOn", "VoidedById", "VoidReason" }) m.DropColumn(name, "Core_FarmOperation");
        }
    }
}
