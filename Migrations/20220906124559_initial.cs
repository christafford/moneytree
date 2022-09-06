using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace moneytree.Migrations
{
    public partial class initial : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Charts",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MinutesForMarketAnalysis = table.Column<int>(type: "int", nullable: false),
                    NumberOfHighestTradedForMarketAnalysis = table.Column<int>(type: "int", nullable: false),
                    DaysSymbolsMustExist = table.Column<int>(type: "int", nullable: false),
                    PercentagePlacementForSecurityPick = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ThresholdToRiseForSell = table.Column<decimal>(type: "decimal(65,30)", nullable: false),
                    ThresholdToDropForSell = table.Column<decimal>(type: "decimal(65,30)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Charts", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "PullDowns",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SymbolId = table.Column<int>(type: "int", nullable: false),
                    TickRequestTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TickResponseStart = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    TickResponseEnd = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RunTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    Finished = table.Column<bool>(type: "tinyint(1)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PullDowns", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Symbols",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Name = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MinTradeQuantity = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    QuantityStep = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    PriceStep = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    QuantityDecimals = table.Column<int>(type: "int", nullable: true),
                    PriceDecimals = table.Column<int>(type: "int", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Symbols", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "Ticks",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    SymbolId = table.Column<int>(type: "int", nullable: false),
                    OpenTime = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    OpenPrice = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    HighPrice = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    LowPrice = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    ClosePrice = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    Volume = table.Column<decimal>(type: "decimal(65,30)", nullable: true),
                    PullDownId = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Ticks", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Charts");

            migrationBuilder.DropTable(
                name: "PullDowns");

            migrationBuilder.DropTable(
                name: "Symbols");

            migrationBuilder.DropTable(
                name: "Ticks");
        }
    }
}
