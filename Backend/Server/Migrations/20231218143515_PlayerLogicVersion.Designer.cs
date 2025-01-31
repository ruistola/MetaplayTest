﻿// <auto-generated />
using System;
using Game.Server.Database;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

#nullable disable

namespace Server.Migrations
{
    [DbContext(typeof(GameDbContext))]
    [Migration("20231218143515_PlayerLogicVersion")]
    partial class PlayerLogicVersion
    {
        /// <inheritdoc />
        protected override void BuildTargetModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder.HasAnnotation("ProductVersion", "7.0.14");

            modelBuilder.Entity("Metaplay.Server.AdminApi.AuditLog.PersistedAuditLogEvent", b =>
                {
                    b.Property<string>("EventId")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<byte[]>("CompressedPayload")
                        .IsRequired()
                        .HasColumnType("longblob");

                    b.Property<int>("CompressionAlgorithm")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("varchar(256)");

                    b.Property<string>("SourceCountryIsoCode")
                        .HasMaxLength(16)
                        .HasColumnType("varchar(16)");

                    b.Property<string>("SourceIpAddress")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("Target")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("varchar(256)");

                    b.HasKey("EventId");

                    b.HasIndex("Source");

                    b.HasIndex("Target");

                    b.ToTable("AuditLogEvents", (string)null);
                });

            modelBuilder.Entity("Metaplay.Server.Authentication.PersistedAuthenticationEntry", b =>
                {
                    b.Property<string>("AuthKey")
                        .HasMaxLength(160)
                        .HasColumnType("varchar(160)");

                    b.Property<string>("HashedAuthToken")
                        .HasColumnType("varchar(160)");

                    b.Property<string>("PlayerId")
                        .IsRequired()
                        .HasColumnType("varchar(64)");

                    b.HasKey("AuthKey");

                    b.ToTable("AuthEntries", (string)null);
                });

            modelBuilder.Entity("Metaplay.Server.Database.DatabaseMetaInfo", b =>
                {
                    b.Property<int>("Version")
                        .ValueGeneratedOnAdd()
                        .HasColumnType("INTEGER");

                    b.Property<int>("MasterVersion")
                        .HasColumnType("INTEGER");

                    b.Property<int>("NumShards")
                        .HasColumnType("INTEGER");

                    b.Property<DateTime>("Timestamp")
                        .HasColumnType("DateTime");

                    b.HasKey("Version");

                    b.ToTable("MetaInfo", (string)null);
                });

            modelBuilder.Entity("Metaplay.Server.DatabaseScan.PersistedDatabaseScanCoordinator", b =>
                {
                    b.Property<string>("EntityId")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<bool>("IsFinal")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("Payload")
                        .IsRequired()
                        .HasColumnType("longblob");

                    b.Property<DateTime>("PersistedAt")
                        .HasColumnType("DateTime");

                    b.Property<int>("SchemaVersion")
                        .HasColumnType("INTEGER");

                    b.HasKey("EntityId");

                    b.ToTable("DatabaseScanCoordinators", (string)null);
                });

            modelBuilder.Entity("Metaplay.Server.DatabaseScan.PersistedDatabaseScanWorker", b =>
                {
                    b.Property<string>("EntityId")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<bool>("IsFinal")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("Payload")
                        .IsRequired()
                        .HasColumnType("longblob");

                    b.Property<DateTime>("PersistedAt")
                        .HasColumnType("DateTime");

                    b.Property<int>("SchemaVersion")
                        .HasColumnType("INTEGER");

                    b.HasKey("EntityId");

                    b.ToTable("DatabaseScanWorkers", (string)null);
                });

            modelBuilder.Entity("Metaplay.Server.InAppPurchase.PersistedInAppPurchase", b =>
                {
                    b.Property<string>("TransactionId")
                        .HasMaxLength(512)
                        .HasColumnType("varchar(512)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("DateTime");

                    b.Property<byte[]>("Event")
                        .IsRequired()
                        .HasColumnType("longblob");

                    b.Property<bool>("IsValidReceipt")
                        .HasColumnType("INTEGER");

                    b.Property<string>("PlayerId")
                        .IsRequired()
                        .HasColumnType("varchar(64)");

                    b.HasKey("TransactionId");

                    b.ToTable("InAppPurchases", (string)null);
                });

            modelBuilder.Entity("Metaplay.Server.InAppPurchase.PersistedInAppPurchaseSubscription", b =>
                {
                    b.Property<string>("PlayerAndOriginalTransactionId")
                        .HasMaxLength(530)
                        .HasColumnType("varchar(530)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("DateTime");

                    b.Property<string>("OriginalTransactionId")
                        .IsRequired()
                        .HasMaxLength(512)
                        .HasColumnType("varchar(512)");

                    b.Property<string>("PlayerId")
                        .IsRequired()
                        .HasColumnType("varchar(64)");

                    b.Property<byte[]>("SubscriptionInfo")
                        .IsRequired()
                        .HasColumnType("longblob");

                    b.HasKey("PlayerAndOriginalTransactionId");

                    b.HasIndex("OriginalTransactionId");

                    b.HasIndex("PlayerId");

                    b.ToTable("InAppPurchaseSubscriptions", (string)null);
                });

            modelBuilder.Entity("Metaplay.Server.PersistedGlobalState", b =>
                {
                    b.Property<string>("EntityId")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<bool>("IsFinal")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("Payload")
                        .IsRequired()
                        .HasColumnType("longblob");

                    b.Property<DateTime>("PersistedAt")
                        .HasColumnType("DateTime");

                    b.Property<int>("SchemaVersion")
                        .HasColumnType("INTEGER");

                    b.HasKey("EntityId");

                    b.ToTable("GlobalStates", (string)null);
                });

            modelBuilder.Entity("Metaplay.Server.PersistedPlayerBase", b =>
                {
                    b.Property<string>("EntityId")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<bool>("IsFinal")
                        .HasColumnType("INTEGER");

                    b.Property<int>("LogicVersion")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("Payload")
                        .HasColumnType("longblob");

                    b.Property<DateTime>("PersistedAt")
                        .HasColumnType("DateTime");

                    b.Property<int>("SchemaVersion")
                        .HasColumnType("INTEGER");

                    b.HasKey("EntityId");

                    b.ToTable("Players", (string)null);
                });

            modelBuilder.Entity("Metaplay.Server.PersistedPlayerEventLogSegment", b =>
                {
                    b.Property<string>("GlobalId")
                        .HasMaxLength(128)
                        .HasColumnType("varchar(128)");

                    b.Property<DateTime>("CreatedAt")
                        .HasColumnType("DateTime");

                    b.Property<DateTime>("FirstEntryTimestamp")
                        .HasColumnType("DateTime");

                    b.Property<DateTime>("LastEntryTimestamp")
                        .HasColumnType("DateTime");

                    b.Property<string>("OwnerId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<byte[]>("Payload")
                        .IsRequired()
                        .HasColumnType("longblob");

                    b.Property<int>("SegmentSequentialId")
                        .HasColumnType("INTEGER");

                    b.HasKey("GlobalId");

                    b.HasIndex("OwnerId");

                    b.ToTable("PlayerEventLogSegments", (string)null);
                });

            modelBuilder.Entity("Metaplay.Server.PersistedPlayerIncident", b =>
                {
                    b.Property<string>("IncidentId")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<int>("Compression")
                        .HasColumnType("INTEGER");

                    b.Property<string>("Fingerprint")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<byte[]>("Payload")
                        .IsRequired()
                        .HasColumnType("longblob");

                    b.Property<DateTime>("PersistedAt")
                        .HasColumnType("DateTime");

                    b.Property<string>("PlayerId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("Reason")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("varchar(256)");

                    b.Property<string>("SubType")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("varchar(128)");

                    b.Property<string>("Type")
                        .IsRequired()
                        .HasMaxLength(128)
                        .HasColumnType("varchar(128)");

                    b.HasKey("IncidentId");

                    b.HasIndex("PersistedAt");

                    b.HasIndex("PlayerId");

                    b.HasIndex("Fingerprint", "PersistedAt");

                    b.ToTable("PlayerIncidents", (string)null);
                });

            modelBuilder.Entity("Metaplay.Server.PersistedPlayerSearch", b =>
                {
                    b.Property<string>("EntityId")
                        .IsRequired()
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("NamePart")
                        .IsRequired()
                        .HasColumnType("varchar(32)");

                    b.HasIndex("EntityId");

                    b.HasIndex("NamePart", "EntityId");

                    b.ToTable("PlayerNameSearches", (string)null);
                });

            modelBuilder.Entity("Metaplay.Server.PersistedSegmentEstimateState", b =>
                {
                    b.Property<string>("EntityId")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<bool>("IsFinal")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("Payload")
                        .IsRequired()
                        .HasColumnType("longblob");

                    b.Property<DateTime>("PersistedAt")
                        .HasColumnType("DateTime");

                    b.Property<int>("SchemaVersion")
                        .HasColumnType("INTEGER");

                    b.HasKey("EntityId");

                    b.ToTable("SegmentEstimates", (string)null);
                });

            modelBuilder.Entity("Metaplay.Server.PersistedStaticGameConfig", b =>
                {
                    b.Property<string>("Id")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<DateTime>("ArchiveBuiltAt")
                        .HasColumnType("DateTime");

                    b.Property<byte[]>("ArchiveBytes")
                        .HasColumnType("longblob");

                    b.Property<string>("Description")
                        .HasMaxLength(512)
                        .HasColumnType("varchar(512)");

                    b.Property<string>("FailureInfo")
                        .HasColumnType("TEXT");

                    b.Property<bool>("IsArchived")
                        .HasColumnType("tinyint");

                    b.Property<DateTime>("LastModifiedAt")
                        .HasColumnType("DateTime");

                    b.Property<byte[]>("MetaDataBytes")
                        .HasColumnType("longblob");

                    b.Property<string>("Name")
                        .IsRequired()
                        .HasMaxLength(256)
                        .HasColumnType("varchar(256)");

                    b.Property<DateTime?>("PublishedAt")
                        .HasColumnType("DateTime");

                    b.Property<string>("Source")
                        .IsRequired()
                        .HasColumnType("varchar(128)");

                    b.Property<string>("TaskId")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<DateTime?>("UnpublishedAt")
                        .HasColumnType("DateTime");

                    b.Property<string>("VersionHash")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.HasKey("Id");

                    b.ToTable("StaticGameConfigs", (string)null);
                });

            modelBuilder.Entity("Metaplay.Server.PersistedStatsCollector", b =>
                {
                    b.Property<string>("EntityId")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<bool>("IsFinal")
                        .HasColumnType("INTEGER");

                    b.Property<byte[]>("Payload")
                        .IsRequired()
                        .HasColumnType("longblob");

                    b.Property<DateTime>("PersistedAt")
                        .HasColumnType("DateTime");

                    b.Property<int>("SchemaVersion")
                        .HasColumnType("INTEGER");

                    b.HasKey("EntityId");

                    b.ToTable("StatsCollectors", (string)null);
                });

            modelBuilder.Entity("Metaplay.Server.PlayerDeletion.PlayerDeletionRecords+PersistedPlayerDeletionRecord", b =>
                {
                    b.Property<string>("PlayerId")
                        .HasMaxLength(64)
                        .HasColumnType("varchar(64)");

                    b.Property<string>("DeletionSource")
                        .HasColumnType("varchar(128)");

                    b.Property<DateTime>("ScheduledDeletionAt")
                        .HasColumnType("DateTime");

                    b.HasKey("PlayerId");

                    b.ToTable("PlayerDeletionRecords", (string)null);
                });
#pragma warning restore 612, 618
        }
    }
}
