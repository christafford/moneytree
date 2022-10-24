﻿// <auto-generated />
using System;
using CStafford.MoneyTree.Infrastructure;
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
                .HasAnnotation("ProductVersion", "6.0.10")
                .HasAnnotation("Relational:MaxIdentifierLength", 64);

            modelBuilder.Entity("CStafford.MoneyTree.Models.Chart", b =>
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

            modelBuilder.Entity("CStafford.MoneyTree.Models.Simulation", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<int>("ChartId")
                        .HasColumnType("int");

                    b.Property<int>("DepositFrequency")
                        .HasColumnType("int");

                    b.Property<int>("EndEpoch")
                        .HasColumnType("int");

                    b.Property<decimal>("ResultGainPercentage")
                        .HasColumnType("decimal(65,30)");

                    b.Property<DateTime>("RunTimeEnd")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("RunTimeStart")
                        .HasColumnType("datetime(6)");

                    b.Property<int>("StartEpoch")
                        .HasColumnType("int");

                    b.HasKey("Id");

                    b.ToTable("Simulations");
                });

            modelBuilder.Entity("CStafford.MoneyTree.Models.SimulationLog", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<string>("Action")
                        .HasColumnType("longtext");

                    b.Property<string>("Message")
                        .HasColumnType("longtext");

                    b.Property<int>("SimulationId")
                        .HasColumnType("int");

                    b.Property<DateTime>("Time")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("SimulationId");

                    b.ToTable("SimulationLogs");
                });

            modelBuilder.Entity("CStafford.MoneyTree.Models.Symbol", b =>
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

            modelBuilder.Entity("CStafford.MoneyTree.Models.Tick", b =>
                {
                    b.Property<int>("TickEpoch")
                        .HasColumnType("int");

                    b.Property<int>("SymbolId")
                        .HasColumnType("int");

                    b.Property<decimal>("ClosePrice")
                        .HasColumnType("decimal(65,30)");

                    b.Property<decimal>("VolumeUsd")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("TickEpoch", "SymbolId");

                    b.HasIndex("TickEpoch");

                    b.HasIndex("SymbolId", "TickEpoch");

                    b.ToTable("Ticks");
                });
#pragma warning restore 612, 618
        }
    }
}
