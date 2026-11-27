using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ECommerce.Module.Core.Migrations
{
    /// <inheritdoc />
    public partial class AddOrchardProfileCascadeDeleteConstraints : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Core_OrchardLocation_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardLocation");

            migrationBuilder.DropForeignKey(
                name: "FK_Core_OrchardVariety_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardVariety");

            migrationBuilder.DropForeignKey(
                name: "FK_Core_OrchardSoilAndWater_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardSoilAndWater");

            migrationBuilder.DropForeignKey(
                name: "FK_Core_OrchardCropNutrition_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardCropNutrition");

            migrationBuilder.DropForeignKey(
                name: "FK_Core_OrchardPlantHealth_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardPlantHealth");

            migrationBuilder.DropForeignKey(
                name: "FK_Core_OrchardInfrastructure_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardInfrastructure");

            migrationBuilder.DropForeignKey(
                name: "FK_Core_OrchardProduction_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardProduction");

            migrationBuilder.DropForeignKey(
                name: "FK_Core_OrchardInputCost_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardInputCost");

            migrationBuilder.DropForeignKey(
                name: "FK_Core_OrchardOptionalFields_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardOptionalFields");

            migrationBuilder.AddForeignKey(
                name: "FK_Core_OrchardLocation_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardLocation",
                column: "OrchardProfileId",
                principalTable: "Core_OrchardProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Core_OrchardVariety_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardVariety",
                column: "OrchardProfileId",
                principalTable: "Core_OrchardProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Core_OrchardSoilAndWater_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardSoilAndWater",
                column: "OrchardProfileId",
                principalTable: "Core_OrchardProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Core_OrchardCropNutrition_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardCropNutrition",
                column: "OrchardProfileId",
                principalTable: "Core_OrchardProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Core_OrchardPlantHealth_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardPlantHealth",
                column: "OrchardProfileId",
                principalTable: "Core_OrchardProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Core_OrchardInfrastructure_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardInfrastructure",
                column: "OrchardProfileId",
                principalTable: "Core_OrchardProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Core_OrchardProduction_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardProduction",
                column: "OrchardProfileId",
                principalTable: "Core_OrchardProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Core_OrchardInputCost_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardInputCost",
                column: "OrchardProfileId",
                principalTable: "Core_OrchardProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);

            migrationBuilder.AddForeignKey(
                name: "FK_Core_OrchardOptionalFields_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardOptionalFields",
                column: "OrchardProfileId",
                principalTable: "Core_OrchardProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Core_OrchardLocation_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardLocation");

            migrationBuilder.DropForeignKey(
                name: "FK_Core_OrchardVariety_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardVariety");

            migrationBuilder.DropForeignKey(
                name: "FK_Core_OrchardSoilAndWater_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardSoilAndWater");

            migrationBuilder.DropForeignKey(
                name: "FK_Core_OrchardCropNutrition_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardCropNutrition");

            migrationBuilder.DropForeignKey(
                name: "FK_Core_OrchardPlantHealth_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardPlantHealth");

            migrationBuilder.DropForeignKey(
                name: "FK_Core_OrchardInfrastructure_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardInfrastructure");

            migrationBuilder.DropForeignKey(
                name: "FK_Core_OrchardProduction_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardProduction");

            migrationBuilder.DropForeignKey(
                name: "FK_Core_OrchardInputCost_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardInputCost");

            migrationBuilder.DropForeignKey(
                name: "FK_Core_OrchardOptionalFields_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardOptionalFields");

            migrationBuilder.AddForeignKey(
                name: "FK_Core_OrchardLocation_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardLocation",
                column: "OrchardProfileId",
                principalTable: "Core_OrchardProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Core_OrchardVariety_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardVariety",
                column: "OrchardProfileId",
                principalTable: "Core_OrchardProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Core_OrchardSoilAndWater_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardSoilAndWater",
                column: "OrchardProfileId",
                principalTable: "Core_OrchardProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Core_OrchardCropNutrition_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardCropNutrition",
                column: "OrchardProfileId",
                principalTable: "Core_OrchardProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Core_OrchardPlantHealth_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardPlantHealth",
                column: "OrchardProfileId",
                principalTable: "Core_OrchardProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Core_OrchardInfrastructure_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardInfrastructure",
                column: "OrchardProfileId",
                principalTable: "Core_OrchardProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Core_OrchardProduction_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardProduction",
                column: "OrchardProfileId",
                principalTable: "Core_OrchardProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Core_OrchardInputCost_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardInputCost",
                column: "OrchardProfileId",
                principalTable: "Core_OrchardProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_Core_OrchardOptionalFields_Core_OrchardProfile_OrchardProfileId",
                table: "Core_OrchardOptionalFields",
                column: "OrchardProfileId",
                principalTable: "Core_OrchardProfile",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
