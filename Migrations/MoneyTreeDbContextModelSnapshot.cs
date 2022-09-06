﻿// <auto-generated />
using System;
using CStafford.Moneytree.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace moneytree.Migrations
{
    [DbContext(typeof(MoneyTreeDbContext))]
    partial class MoneyTreeDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "6.0.8")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("CStafford.Moneytree.Models.Chart", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("DaysSymbolsMustExist")
                        .HasColumnType("int");

                    b.Property<int>("MinutesForMarketAnalysis")
                        .HasColumnType("int");

                    b.Property<int>("NumberOfHighestTradedForMarketAnalysis")
                        .HasColumnType("int");

                    b.Property<decimal>("PercentagePlacementForSecurityPick")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("ThresholdToDropForSell")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("ThresholdToRiseForSell")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("Id");

                    b.ToTable("Charts");
                });

            modelBuilder.Entity("CStafford.Moneytree.Models.PullDown", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<bool>("Finished")
                        .HasColumnType("tinyint(1)");

                    b.Property<DateTime>("RunTime")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("SymbolId")
                        .HasColumnType("int");

                    b.Property<DateTime>("TickRequestTime")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("TickResponseEnd")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("TickResponseStart")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.ToTable("PullDowns");
                });

            modelBuilder.Entity("CStafford.Moneytree.Models.Symbol", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<decimal?>("MinTradeQuantity")
                        .HasColumnType("decimal(65,30)");

                    b.Property<string>("Name")
                        .HasColumnType("longtext");

                    b.Property<int?>("PriceDecimals")
                        .HasColumnType("int");

                    b.Property<decimal?>("PriceStep")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int?>("QuantityDecimals")
                        .HasColumnType("int");

                    b.Property<decimal?>("QuantityStep")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("Id");

                    b.ToTable("Symbols");
                });

            modelBuilder.Entity("CStafford.Moneytree.Models.Tick", b =>
                {
                    b.Property<long>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("bigint");

                    b.Property<decimal?>("ClosePrice")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal?>("HighPrice")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal?>("LowPrice")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal?>("OpenPrice")
                        .HasColumnType("decimal(65,30)");

                    b.Property<DateTime>("OpenTime")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("PullDownId")
                        .HasColumnType("int");

                    b.Property<int>("SymbolId")
                        .HasColumnType("int");

                    b.Property<decimal?>("Volume")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("Id");

                    b.ToTable("Ticks");
                });
#pragma warning restore 612, 618
        }
    }
}
