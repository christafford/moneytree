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

            modelBuilder.Entity("CStafford.Moneytree.Models.PullDown", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    b.Property<DateTime>("RunTime")
                        .HasColumnType("datetime(6)");

                    b.Property<string>("SymbolName")
                        .HasColumnType("varchar(255)");

                    b.Property<DateTime>("TickRequestTime")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("TickResponseEnd")
                        .HasColumnType("datetime(6)");

                    b.Property<DateTime>("TickResponseStart")
                        .HasColumnType("datetime(6)");

                    b.HasKey("Id");

                    b.HasIndex("SymbolName", "TickResponseEnd");

                    b.ToTable("PullDowns");
                });

            modelBuilder.Entity("CStafford.Moneytree.Models.Symbol", b =>
                {
                    b.Property<string>("Name")
                        .HasColumnType("varchar(255)");

                    b.Property<decimal?>("MinTradeQuantity")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int?>("PriceDecimals")
                        .HasColumnType("int");

                    b.Property<decimal?>("PriceStep")
                        .HasColumnType("decimal(65,30)");

                    b.Property<int?>("QuantityDecimals")
                        .HasColumnType("int");

                    b.Property<decimal?>("QuantityStep")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("Name");

                    b.ToTable("Symbols");
                });

            modelBuilder.Entity("CStafford.Moneytree.Models.Tick", b =>
                {
                    b.Property<int>("Id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

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

                    b.Property<string>("SymbolName")
                        .HasColumnType("longtext");

                    b.Property<decimal?>("Volume")
                        .HasColumnType("decimal(65,30)");

                    b.HasKey("Id");

                    b.ToTable("Ticks");
                });

            modelBuilder.Entity("CStafford.Moneytree.Models.PullDown", b =>
                {
                    b.HasOne("CStafford.Moneytree.Models.Symbol", "Symbol")
                        .WithMany()
                        .HasForeignKey("SymbolName");

                    b.Navigation("Symbol");
                });
#pragma warning restore 612, 618
        }
    }
}
