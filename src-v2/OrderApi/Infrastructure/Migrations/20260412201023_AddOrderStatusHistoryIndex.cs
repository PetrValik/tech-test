using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderApi.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddOrderStatusHistoryIndex : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "IX_order_status_history_OrderId",
                table: "order_status_history",
                newName: "OrderStatusHistory_OrderId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameIndex(
                name: "OrderStatusHistory_OrderId",
                table: "order_status_history",
                newName: "IX_order_status_history_OrderId");
        }
    }
}
