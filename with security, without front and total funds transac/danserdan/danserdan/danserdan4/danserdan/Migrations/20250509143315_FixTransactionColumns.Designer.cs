﻿// <auto-generated />
using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using danserdan.Services;

#nullable disable

namespace danserdan.Migrations
{
    [DbContext(typeof(ApplicationDBContext))]
    [Migration("20250509143315_FixTransactionColumns")]
    partial class FixTransactionColumns
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasAnnotation("ProductVersion", "7.0.0")
                .HasAnnotation("Relational:MaxIdentifierLength", 128);

            SqlServerModelBuilderExtensions.UseIdentityColumns(modelBuilder);

            modelBuilder.Entity("danserdan.Models.Stocks", b =>
                {
                    b.Property<int>("stock_id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("stock_id"));

                    b.Property<string>("company_name")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.Property<DateTime>("last_updated")
                        .HasColumnType("datetime2");

                    b.Property<decimal>("market_price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<decimal?>("open_price")
                        .HasColumnType("decimal(18,2)");

                    b.Property<DateTime?>("open_price_time")
                        .HasColumnType("datetime2");

                    b.Property<string>("symbol")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)");

                    b.HasKey("stock_id");

                    b.ToTable("Stocks");
                });

            modelBuilder.Entity("danserdan.Models.Transaction", b =>
                {
                    b.Property<int>("transaction_id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("transaction_id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("transaction_id"));

                    b.Property<decimal>("amount")
                        .HasColumnType("decimal(18, 2)")
                        .HasColumnName("amount");

                    b.Property<int>("quantity")
                        .HasColumnType("int")
                        .HasColumnName("quantity");

                    b.Property<string>("stock_symbol")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("stock_symbol");

                    b.Property<DateTime>("transaction_date")
                        .HasColumnType("datetime2")
                        .HasColumnName("transaction_date");

                    b.Property<string>("transaction_type")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("transaction_type");

                    b.Property<int>("user_id")
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    b.HasKey("transaction_id");

                    b.HasIndex("user_id");

                    b.ToTable("transactions", (string)null);
                });

            modelBuilder.Entity("danserdan.Models.Users", b =>
                {
                    b.Property<int>("user_id")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("int")
                        .HasColumnName("user_id");

                    SqlServerPropertyBuilderExtensions.UseIdentityColumn(b.Property<int>("user_id"));

                    b.Property<decimal?>("balance")
                        .HasColumnType("decimal(18, 2)")
                        .HasColumnName("balance");

                    b.Property<DateTime>("created_at")
                        .HasColumnType("datetime2")
                        .HasColumnName("created_at");

                    b.Property<string>("email")
                        .IsRequired()
                        .HasColumnType("nvarchar(max)")
                        .HasColumnName("email");

                    b.Property<string>("firstName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("lastName")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)");

                    b.Property<string>("password_hash")
                        .IsRequired()
                        .HasMaxLength(100)
                        .HasColumnType("nvarchar(100)")
                        .HasColumnName("password_hash");

                    b.Property<string>("username")
                        .IsRequired()
                        .HasMaxLength(50)
                        .HasColumnType("nvarchar(50)")
                        .HasColumnName("username");

                    b.HasKey("user_id");

                    b.ToTable("users", (string)null);
                });

            modelBuilder.Entity("danserdan.Models.Transaction", b =>
                {
                    b.HasOne("danserdan.Models.Users", "User")
                        .WithMany()
                        .HasForeignKey("user_id")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("User");
                });
#pragma warning restore 612, 618
        }
    }
}
