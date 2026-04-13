using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class EnterpriseImprovements : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "order_item_oifk_1",
                table: "order_item");

            migrationBuilder.DropIndex(
                name: "CreatedDate",
                table: "order");

            migrationBuilder.AlterColumn<string>(
                name: "ConcurrencyStamp",
                table: "order",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValueSql: "(REPLACE(UUID(), '-', ''))",
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldMaxLength: 32)
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "CreatedDate",
                table: "order",
                column: "CreatedDate",
                descending: new[] { true });

            migrationBuilder.CreateIndex(
                name: "ResellerId",
                table: "order",
                column: "ResellerId");

            migrationBuilder.CreateIndex(
                name: "IdempotencyRecord_CreatedAt",
                table: "idempotency_record",
                column: "CreatedAt");

            migrationBuilder.AddForeignKey(
                name: "order_item_oifk_1",
                table: "order_item",
                column: "OrderId",
                principalTable: "order",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "order_item_oifk_1",
                table: "order_item");

            migrationBuilder.DropIndex(
                name: "CreatedDate",
                table: "order");

            migrationBuilder.DropIndex(
                name: "ResellerId",
                table: "order");

            migrationBuilder.DropIndex(
                name: "IdempotencyRecord_CreatedAt",
                table: "idempotency_record");

            migrationBuilder.AlterColumn<string>(
                name: "ConcurrencyStamp",
                table: "order",
                type: "varchar(32)",
                maxLength: 32,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "varchar(32)",
                oldMaxLength: 32,
                oldDefaultValueSql: "(REPLACE(UUID(), '-', ''))")
                .Annotation("MySql:CharSet", "utf8mb4")
                .OldAnnotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "CreatedDate",
                table: "order",
                column: "CreatedDate");

            migrationBuilder.AddForeignKey(
                name: "order_item_oifk_1",
                table: "order_item",
                column: "OrderId",
                principalTable: "order",
                principalColumn: "Id");
        }
    }
}
