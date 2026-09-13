using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class RedesignKpiPlanLevel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SalesCommissions_SalesKpiTargets_KpiTargetId",
                table: "SalesCommissions");

            migrationBuilder.DropTable(
                name: "SalesKpiTargets");

            migrationBuilder.RenameColumn(
                name: "KpiTargetId",
                table: "SalesCommissions",
                newName: "KpiLevelId");

            migrationBuilder.RenameIndex(
                name: "IX_SalesCommissions_KpiTargetId",
                table: "SalesCommissions",
                newName: "IX_SalesCommissions_KpiLevelId");

            migrationBuilder.AddColumn<long>(
                name: "KpiPlanId",
                table: "Employees",
                type: "bigint",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "KpiPlans",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PlanName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiPlans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "KpiLevels",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    KpiPlanId = table.Column<long>(type: "bigint", nullable: false),
                    LevelOrder = table.Column<int>(type: "int", nullable: false),
                    MinimumRevenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    CommissionRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_KpiLevels", x => x.Id);
                    table.ForeignKey(
                        name: "FK_KpiLevels_KpiPlans_KpiPlanId",
                        column: x => x.KpiPlanId,
                        principalTable: "KpiPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Employees_KpiPlanId",
                table: "Employees",
                column: "KpiPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_KpiLevels_KpiPlanId",
                table: "KpiLevels",
                column: "KpiPlanId");

            migrationBuilder.AddForeignKey(
                name: "FK_Employees_KpiPlans_KpiPlanId",
                table: "Employees",
                column: "KpiPlanId",
                principalTable: "KpiPlans",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_SalesCommissions_KpiLevels_KpiLevelId",
                table: "SalesCommissions",
                column: "KpiLevelId",
                principalTable: "KpiLevels",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Employees_KpiPlans_KpiPlanId",
                table: "Employees");

            migrationBuilder.DropForeignKey(
                name: "FK_SalesCommissions_KpiLevels_KpiLevelId",
                table: "SalesCommissions");

            migrationBuilder.DropTable(
                name: "KpiLevels");

            migrationBuilder.DropTable(
                name: "KpiPlans");

            migrationBuilder.DropIndex(
                name: "IX_Employees_KpiPlanId",
                table: "Employees");

            migrationBuilder.DropColumn(
                name: "KpiPlanId",
                table: "Employees");

            migrationBuilder.RenameColumn(
                name: "KpiLevelId",
                table: "SalesCommissions",
                newName: "KpiTargetId");

            migrationBuilder.RenameIndex(
                name: "IX_SalesCommissions_KpiLevelId",
                table: "SalesCommissions",
                newName: "IX_SalesCommissions_KpiTargetId");

            migrationBuilder.CreateTable(
                name: "SalesKpiTargets",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CommissionRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EffectiveFrom = table.Column<DateOnly>(type: "date", nullable: false),
                    EffectiveTo = table.Column<DateOnly>(type: "date", nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    MinimumRevenue = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    TargetName = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SalesKpiTargets", x => x.Id);
                });

            migrationBuilder.AddForeignKey(
                name: "FK_SalesCommissions_SalesKpiTargets_KpiTargetId",
                table: "SalesCommissions",
                column: "KpiTargetId",
                principalTable: "SalesKpiTargets",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
