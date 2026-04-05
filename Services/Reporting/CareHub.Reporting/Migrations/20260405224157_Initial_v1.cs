using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareHub.Reporting.Migrations
{
    /// <inheritdoc />
    public partial class Initial_v1 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ReportAppointmentFacts",
                columns: table => new
                {
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: true),
                    ScheduledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancelledAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CancellationReason = table.Column<string>(type: "character varying(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportAppointmentFacts", x => x.AppointmentId);
                });

            migrationBuilder.CreateTable(
                name: "ReportPatientFacts",
                columns: table => new
                {
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportPatientFacts", x => x.PatientId);
                });

            migrationBuilder.CreateTable(
                name: "ReportPaymentFacts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MessageId = table.Column<string>(type: "text", nullable: false),
                    InvoiceId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Amount = table.Column<decimal>(type: "numeric", nullable: false),
                    Currency = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    IsRefund = table.Column<bool>(type: "boolean", nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    ActorUserId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ReportPaymentFacts", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ReportAppointmentFacts_BranchId",
                table: "ReportAppointmentFacts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportAppointmentFacts_CancelledAt",
                table: "ReportAppointmentFacts",
                column: "CancelledAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReportAppointmentFacts_CompletedAt",
                table: "ReportAppointmentFacts",
                column: "CompletedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReportAppointmentFacts_DoctorId",
                table: "ReportAppointmentFacts",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportAppointmentFacts_ScheduledAt",
                table: "ReportAppointmentFacts",
                column: "ScheduledAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReportPatientFacts_BranchId",
                table: "ReportPatientFacts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportPatientFacts_CreatedAt",
                table: "ReportPatientFacts",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ReportPaymentFacts_BranchId",
                table: "ReportPaymentFacts",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportPaymentFacts_InvoiceId",
                table: "ReportPaymentFacts",
                column: "InvoiceId");

            migrationBuilder.CreateIndex(
                name: "IX_ReportPaymentFacts_MessageId",
                table: "ReportPaymentFacts",
                column: "MessageId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ReportPaymentFacts_OccurredAt",
                table: "ReportPaymentFacts",
                column: "OccurredAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ReportAppointmentFacts");

            migrationBuilder.DropTable(
                name: "ReportPatientFacts");

            migrationBuilder.DropTable(
                name: "ReportPaymentFacts");
        }
    }
}
