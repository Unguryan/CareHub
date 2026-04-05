using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CareHub.Laboratory.Migrations
{
    /// <inheritdoc />
    public partial class Initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "LabOrders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    BranchId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    SampleReceivedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResultSummary = table.Column<string>(type: "character varying(4000)", maxLength: 4000, nullable: true),
                    ResultEnteredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ResultEnteredByUserId = table.Column<Guid>(type: "uuid", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LabOrders", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_LabOrders_AppointmentId",
                table: "LabOrders",
                column: "AppointmentId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LabOrders_BranchId",
                table: "LabOrders",
                column: "BranchId");

            migrationBuilder.CreateIndex(
                name: "IX_LabOrders_CreatedAt",
                table: "LabOrders",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_LabOrders_PatientId",
                table: "LabOrders",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_LabOrders_Status",
                table: "LabOrders",
                column: "Status");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LabOrders");
        }
    }
}
