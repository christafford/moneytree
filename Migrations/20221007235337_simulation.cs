using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace moneytree.Migrations
{
    public partial class simulation : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Simulations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    ChartId = table.Column<int>(type: "int", nullable: false),
                    DepositFrequency = table.Column<int>(type: "int", nullable: false),
                    SimulationStart = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    SimulationEnd = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RunTimeStart = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RunTimeEnd = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ResultGainPercentage = table.Column<decimal>(type: "decimal(65,30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Simulations", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Ticks_OpenTime",
                table: "Ticks",
                column: "OpenTime");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Simulations");

            migrationBuilder.DropIndex(
                name: "IX_Ticks_OpenTime",
                table: "Ticks");
        }
    }
}
