﻿// <auto-generated />
using BuildNotifier.Data.Context;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace BuildNotifier.Migrations
{
    [DbContext(typeof(AppDbContext))]
    [Migration("20250529074907_InitialCreate")]
    partial class InitialCreate
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "9.0.5");

            modelBuilder.Entity("BuildNotifier.Data.Models.DB.PlanChat", b =>
                {
                    b.Property<string>("PlanName")
                        .HasColumnType("TEXT");

                    b.Property<string>("ChatId")
                        .HasColumnType("TEXT");

                    b.HasKey("PlanName", "ChatId");

                    b.HasIndex("ChatId");

                    b.HasIndex("PlanName");

                    b.ToTable("PlanChats");
                });
#pragma warning restore 612, 618
        }
    }
}
