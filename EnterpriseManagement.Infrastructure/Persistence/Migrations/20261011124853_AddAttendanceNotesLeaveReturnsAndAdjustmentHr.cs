using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnterpriseManagement.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAttendanceNotesLeaveReturnsAndAdjustmentHr : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "ProcessedAt",
                table: "AttendanceAdjustments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "ProcessedBy",
                table: "AttendanceAdjustments",
                type: "bigint",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "RejectionReason",
                table: "AttendanceAdjustments",
                type: "nvarchar(500)",
                maxLength: 500,
                nullable: true);

            migrationBuilder.CreateTable(
                name: "AttendanceSessionNotes",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Session = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Note = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    CreatedBy = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RevokedBy = table.Column<long>(type: "bigint", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AttendanceSessionNotes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AttendanceSessionNotes_Employees_CreatedBy",
                        column: x => x.CreatedBy,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceSessionNotes_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AttendanceSessionNotes_Employees_RevokedBy",
                        column: x => x.RevokedBy,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveReturnRequests",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    EmployeeId = table.Column<long>(type: "bigint", nullable: false),
                    FromDate = table.Column<DateOnly>(type: "date", nullable: false),
                    FromSession = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(30)", maxLength: 30, nullable: false),
                    ManagerId = table.Column<long>(type: "bigint", nullable: true),
                    ManagerApprovedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ProcessedBy = table.Column<long>(type: "bigint", nullable: true),
                    ProcessedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    RejectionReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveReturnRequests", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveReturnRequests_Employees_EmployeeId",
                        column: x => x.EmployeeId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveReturnRequests_Employees_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveReturnRequests_Employees_ProcessedBy",
                        column: x => x.ProcessedBy,
                        principalTable: "Employees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LeaveReturnSessions",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ReturnRequestId = table.Column<long>(type: "bigint", nullable: false),
                    LeaveRequestId = table.Column<long>(type: "bigint", nullable: false),
                    Date = table.Column<DateOnly>(type: "date", nullable: false),
                    Session = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    State = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    WorkedStatus = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LeaveReturnSessions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LeaveReturnSessions_LeaveRequests_LeaveRequestId",
                        column: x => x.LeaveRequestId,
                        principalTable: "LeaveRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LeaveReturnSessions_LeaveReturnRequests_ReturnRequestId",
                        column: x => x.ReturnRequestId,
                        principalTable: "LeaveReturnRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceAdjustments_ProcessedBy",
                table: "AttendanceAdjustments",
                column: "ProcessedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessionNotes_CreatedBy",
                table: "AttendanceSessionNotes",
                column: "CreatedBy");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessionNotes_EmployeeId_Date_Session",
                table: "AttendanceSessionNotes",
                columns: new[] { "EmployeeId", "Date", "Session" },
                unique: true,
                filter: "[RevokedAt] IS NULL");

            migrationBuilder.CreateIndex(
                name: "IX_AttendanceSessionNotes_RevokedBy",
                table: "AttendanceSessionNotes",
                column: "RevokedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveReturnRequests_EmployeeId_Status",
                table: "LeaveReturnRequests",
                columns: new[] { "EmployeeId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveReturnRequests_ManagerId",
                table: "LeaveReturnRequests",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveReturnRequests_ProcessedBy",
                table: "LeaveReturnRequests",
                column: "ProcessedBy");

            migrationBuilder.CreateIndex(
                name: "IX_LeaveReturnSessions_LeaveRequestId_Date_Session",
                table: "LeaveReturnSessions",
                columns: new[] { "LeaveRequestId", "Date", "Session" });

            migrationBuilder.CreateIndex(
                name: "IX_LeaveReturnSessions_ReturnRequestId",
                table: "LeaveReturnSessions",
                column: "ReturnRequestId");

            migrationBuilder.AddForeignKey(
                name: "FK_AttendanceAdjustments_Employees_ProcessedBy",
                table: "AttendanceAdjustments",
                column: "ProcessedBy",
                principalTable: "Employees",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AttendanceAdjustments_Employees_ProcessedBy",
                table: "AttendanceAdjustments");

            migrationBuilder.DropTable(
                name: "AttendanceSessionNotes");

            migrationBuilder.DropTable(
                name: "LeaveReturnSessions");

            migrationBuilder.DropTable(
                name: "LeaveReturnRequests");

            migrationBuilder.DropIndex(
                name: "IX_AttendanceAdjustments_ProcessedBy",
                table: "AttendanceAdjustments");

            migrationBuilder.DropColumn(
                name: "ProcessedAt",
                table: "AttendanceAdjustments");

            migrationBuilder.DropColumn(
                name: "ProcessedBy",
                table: "AttendanceAdjustments");

            migrationBuilder.DropColumn(
                name: "RejectionReason",
                table: "AttendanceAdjustments");
        }
    }
}
