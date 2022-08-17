﻿// <auto-generated />
using System;
using Aeolus;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Aeolus.Migrations
{
    [DbContext(typeof(AeolusDbContext))]
    [Migration("20220817043752_InitialCreate")]
    partial class InitialCreate
    {
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "6.0.8");

            modelBuilder.Entity("CrossLaunch.Models.ProjectDirectoryModel", b =>
                {
                    b.Property<string>("FullPath")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("RecordUpdateTime")
                        .HasColumnType("TEXT");

                    b.HasKey("FullPath");

                    b.ToTable("ProjectDirectories");
                });

            modelBuilder.Entity("CrossLaunch.Models.ProjectDirectoryProjectModel", b =>
                {
                    b.Property<string>("FullPath")
                        .HasColumnType("TEXT");

                    b.Property<string>("Framework")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Nickname")
                        .HasColumnType("TEXT");

                    b.Property<string>("ProjectDirectoryFullPath")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("ProjectEvaluatorType")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("RecordUpdateTime")
                        .HasColumnType("TEXT");

                    b.HasKey("FullPath");

                    b.HasIndex("ProjectDirectoryFullPath");

                    b.ToTable("ProjectDirectoryProjects");
                });

            modelBuilder.Entity("CrossLaunch.Models.RecentProjectModel", b =>
                {
                    b.Property<string>("FullPath")
                        .HasColumnType("TEXT");

                    b.Property<string>("Framework")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<string>("Nickname")
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("OpenedTime")
                        .HasColumnType("TEXT");

                    b.Property<string>("ProjectEvaluatorType")
                        .IsRequired()
                        .HasColumnType("TEXT");

                    b.Property<DateTime>("RecordUpdateTime")
                        .HasColumnType("TEXT");

                    b.HasKey("FullPath");

                    b.ToTable("RecentProjects");
                });

            modelBuilder.Entity("CrossLaunch.Models.ProjectDirectoryProjectModel", b =>
                {
                    b.HasOne("CrossLaunch.Models.ProjectDirectoryModel", "ProjectDirectory")
                        .WithMany("Projects")
                        .HasForeignKey("ProjectDirectoryFullPath")
                        .OnDelete(DeleteBehavior.Cascade)
                        .IsRequired();

                    b.Navigation("ProjectDirectory");
                });

            modelBuilder.Entity("CrossLaunch.Models.ProjectDirectoryModel", b =>
                {
                    b.Navigation("Projects");
                });
#pragma warning restore 612, 618
        }
    }
}
