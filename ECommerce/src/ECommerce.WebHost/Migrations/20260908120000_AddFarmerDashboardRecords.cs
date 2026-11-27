using System;
using ECommerce.Module.Core.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.WebHost.Migrations
{
    [DbContext(typeof(SimplDbContext))]
    [Migration("20260908120000_AddFarmerDashboardRecords")]
    public class AddFarmerDashboardRecords : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Core_FarmActivity",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    OrchardProfileId = table.Column<long>(type: "bigint", nullable: false),
                    RecordedById = table.Column<long>(type: "bigint", nullable: false),
                    Kind = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    OccurredOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    ScheduledFor = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    IsCompleted = table.Column<bool>(type: "bit", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: true),
                    ExpenseCategory = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Core_FarmActivity", x => x.Id);
                    table.ForeignKey("FK_Core_FarmActivity_Core_OrchardProfile_OrchardProfileId", x => x.OrchardProfileId,
                        "Core_OrchardProfile", "Id", onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.CreateIndex("IX_Core_FarmActivity_OrchardProfileId_OccurredOn", "Core_FarmActivity", new[] { "OrchardProfileId", "OccurredOn" });
            migrationBuilder.CreateTable(
                name: "Core_FarmAssessment",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false).Annotation("SqlServer:Identity", "1, 1"),
                    OrchardProfileId = table.Column<long>(type: "bigint", nullable: false),
                    RecordedById = table.Column<long>(type: "bigint", nullable: false),
                    ObservedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    TreesAssessed = table.Column<int>(type: "int", nullable: true),
                    LivingTrees = table.Column<int>(type: "int", nullable: true),
                    TreesMonitored = table.Column<int>(type: "int", nullable: true),
                    PestAffectedTrees = table.Column<int>(type: "int", nullable: true),
                    DiseaseAffectedTrees = table.Column<int>(type: "int", nullable: true),
                    DeficiencySymptomsCount = table.Column<int>(type: "int", nullable: true),
                    AverageTreeAgeYears = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    YieldKgPerTree = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    YieldKgPerHectare = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    SoilPh = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    SoilEc = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    OrganicMatterPercent = table.Column<decimal>(type: "decimal(18,4)", nullable: true),
                    RiskLevel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    NitrogenLevel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    PhosphorusLevel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    PotassiumLevel = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: true),
                    PesticideResistanceSuspected = table.Column<bool>(type: "bit", nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CreatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                    UpdatedOn = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Core_FarmAssessment", x => x.Id);
                    table.ForeignKey("FK_Core_FarmAssessment_Core_OrchardProfile_OrchardProfileId", x => x.OrchardProfileId,
                        "Core_OrchardProfile", "Id", onDelete: ReferentialAction.Cascade);
                });
            migrationBuilder.CreateIndex("IX_Core_FarmAssessment_OrchardProfileId_ObservedOn", "Core_FarmAssessment", new[] { "OrchardProfileId", "ObservedOn" });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable("Core_FarmAssessment");
            migrationBuilder.DropTable("Core_FarmActivity");
        }
    }
}
