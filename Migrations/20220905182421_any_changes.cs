using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace moneytree.Migrations
{
    public partial class any_changes : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Ticks_PullDownId",
                table: "Ticks",
                column: "PullDownId");

            migrationBuilder.AddForeignKey(
                name: "FK_Ticks_PullDowns_PullDownId",
                table: "Ticks",
                column: "PullDownId",
                principalTable: "PullDowns",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Ticks_PullDowns_PullDownId",
                table: "Ticks");

            migrationBuilder.DropIndex(
                name: "IX_Ticks_PullDownId",
                table: "Ticks");
        }
    }
}
