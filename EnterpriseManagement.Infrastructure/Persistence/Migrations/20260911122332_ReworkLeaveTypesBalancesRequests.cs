using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class ReworkLeaveTypesBalancesRequests : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "DefaultDays",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "TotalDays",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "AllocatedDays",
                table: "LeaveBalances");

            migrationBuilder.DropColumn(
                name: "RemainingDays",
                table: "LeaveBalances");

            migrationBuilder.DropColumn(
                name: "UsedDays",
                table: "LeaveBalances");

            migrationBuilder.AddColumn<decimal>(
                name: "AccrualAmount",
                table: "LeaveTypes",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "AccrualPeriod",
                table: "LeaveTypes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "AccrualUnit",
                table: "LeaveTypes",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AlterColumn<DateTime>(
                name: "StartDate",
                table: "LeaveRequests",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AlterColumn<DateTime>(
                name: "EndDate",
                table: "LeaveRequests",
                type: "datetime2",
                nullable: false,
                oldClrType: typeof(DateOnly),
                oldType: "date");

            migrationBuilder.AddColumn<string>(
                name: "Session",
                table: "LeaveRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "TotalTime",
                table: "LeaveRequests",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "LeaveRequests",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "AllocatedTime",
                table: "LeaveBalances",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<int>(
                name: "Month",
                table: "LeaveBalances",
                type: "int",
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingTime",
                table: "LeaveBalances",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<string>(
                name: "Unit",
                table: "LeaveBalances",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<decimal>(
                name: "UsedTime",
                table: "LeaveBalances",
                type: "decimal(6,2)",
                precision: 6,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AccrualAmount",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "AccrualPeriod",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "AccrualUnit",
                table: "LeaveTypes");

            migrationBuilder.DropColumn(
                name: "Session",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "TotalTime",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "LeaveRequests");

            migrationBuilder.DropColumn(
                name: "AllocatedTime",
                table: "LeaveBalances");

            migrationBuilder.DropColumn(
                name: "Month",
                table: "LeaveBalances");

            migrationBuilder.DropColumn(
                name: "RemainingTime",
                table: "LeaveBalances");

            migrationBuilder.DropColumn(
                name: "Unit",
                table: "LeaveBalances");

            migrationBuilder.DropColumn(
                name: "UsedTime",
                table: "LeaveBalances");

            migrationBuilder.AddColumn<decimal>(
                name: "DefaultDays",
                table: "LeaveTypes",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: true);

            migrationBuilder.AlterColumn<DateOnly>(
                name: "StartDate",
                table: "LeaveRequests",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AlterColumn<DateOnly>(
                name: "EndDate",
                table: "LeaveRequests",
                type: "date",
                nullable: false,
                oldClrType: typeof(DateTime),
                oldType: "datetime2");

            migrationBuilder.AddColumn<decimal>(
                name: "TotalDays",
                table: "LeaveRequests",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "AllocatedDays",
                table: "LeaveBalances",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "RemainingDays",
                table: "LeaveBalances",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);

            migrationBuilder.AddColumn<decimal>(
                name: "UsedDays",
                table: "LeaveBalances",
                type: "decimal(5,2)",
                precision: 5,
                scale: 2,
                nullable: false,
                defaultValue: 0m);
        }
    }
}
