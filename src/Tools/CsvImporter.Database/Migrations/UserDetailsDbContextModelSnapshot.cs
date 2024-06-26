﻿// <auto-generated />
using System;
using EntraMfaPrefillinator.Tools.CsvImporter.Database.Contexts;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace EntraMfaPrefillinator.Tools.CsvImporter.Database.Migrations
{
    [DbContext(typeof(UserDetailsDbContext))]
    partial class UserDetailsDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "8.0.4");

            modelBuilder.Entity("EntraMfaPrefillinator.Lib.Models.UserDetails", b =>
                {
                    b.Property<string>("EmployeeNumber")
                        .HasColumnType("TEXT")
                        .HasColumnName("EmployeeNumber");

                    b.Property<string>("HomePhoneNumber")
                        .HasColumnType("TEXT")
                        .HasColumnName("HomePhoneNumber");

                    b.Property<DateTimeOffset?>("LastUpdated")
                        .HasColumnType("TEXT")
                        .HasColumnName("LastUpdated");

                    b.Property<string>("PhoneNumber")
                        .HasColumnType("TEXT")
                        .HasColumnName("PhoneNumber");

                    b.Property<string>("SecondaryEmail")
                        .HasColumnType("TEXT")
                        .HasColumnName("SecondaryEmail");

                    b.Property<string>("UserName")
                        .IsRequired()
                        .HasColumnType("TEXT")
                        .HasColumnName("UserName");

                    b.HasKey("EmployeeNumber");

                    b.ToTable("UserDetails");
                });
#pragma warning restore 612, 618
        }
    }
}
