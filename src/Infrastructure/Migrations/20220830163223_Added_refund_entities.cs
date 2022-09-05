using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    public partial class Added_refund_entities : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Refunds",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Identifier = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    RefundReference = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    PaymentReference = table.Column<string>(type: "nvarchar(36)", maxLength: 36, nullable: true),
                    Amount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    PaymentId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    RefundId = table.Column<string>(type: "nvarchar(255)", maxLength: 255, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    Finished = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Refunds", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "RefundStatusHistory",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    RefundId = table.Column<int>(type: "int", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    Finished = table.Column<bool>(type: "bit", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_RefundStatusHistory", x => x.Id);
                    table.ForeignKey(
                        name: "FK_RefundStatusHistory_Refunds_RefundId",
                        column: x => x.RefundId,
                        principalTable: "Refunds",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Payments_Reference",
                table: "Payments",
                column: "Reference");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_Finished",
                table: "Refunds",
                column: "Finished");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_PaymentReference",
                table: "Refunds",
                column: "PaymentReference");

            migrationBuilder.CreateIndex(
                name: "IX_Refunds_RefundReference",
                table: "Refunds",
                column: "RefundReference");

            migrationBuilder.CreateIndex(
                name: "IX_RefundStatusHistory_RefundId",
                table: "RefundStatusHistory",
                column: "RefundId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "RefundStatusHistory");

            migrationBuilder.DropTable(
                name: "Refunds");

            migrationBuilder.DropIndex(
                name: "IX_Payments_Reference",
                table: "Payments");
        }
    }
}
