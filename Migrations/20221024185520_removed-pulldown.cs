using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace moneytree.Migrations
{
    public partial class removedpulldown : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PullDowns");

            migrationBuilder.DropColumn(
                name: "PullDownId",
                table: "Ticks");

            migrationBuilder.AddForeignKey(
                name: "FK_SimulationLogs_Simulations_SimulationId",
                table: "SimulationLogs",
                column: "SimulationId",
                principalTable: "Simulations",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_SimulationLogs_Simulations_SimulationId",
                table: "SimulationLogs");

            migrationBuilder.AddColumn<int>(
                name: "PullDownId",
                table: "Ticks",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.CreateTable(
                name: "PullDowns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Finished = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    RunTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SymbolId = table.Column<int>(type: "int", nullable: false),
                    TickEndEpoch = table.Column<int>(type: "int", nullable: true),
                    TickStartEpoch = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PullDowns", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }
    }
}
