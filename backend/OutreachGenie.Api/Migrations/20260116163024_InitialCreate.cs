using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable
#pragma warning disable SA1137 // Elements should have the same indentation
#pragma warning disable CA1861 // Prefer static readonly fields over constant array arguments

namespace OutreachGenie.Api.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        migrationBuilder.CreateTable(
                name: "Campaigns",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                    Phase = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    Metadata = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Campaigns", x => x.Id);
                });

        migrationBuilder.CreateTable(
            name: "Events",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                EventType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                CampaignId = table.Column<Guid>(type: "TEXT", nullable: true),
                Timestamp = table.Column<DateTime>(type: "TEXT", nullable: false),
                Actor = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                Payload = table.Column<string>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Events", x => x.Id);
            });

        migrationBuilder.CreateTable(
            name: "AgentThreads",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                CampaignId = table.Column<Guid>(type: "TEXT", nullable: false),
                ThreadId = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                State = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                LastAccessedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                CampaignId1 = table.Column<Guid>(type: "TEXT", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_AgentThreads", x => x.Id);
                table.ForeignKey(
                    name: "FK_AgentThreads_Campaigns_CampaignId",
                    column: x => x.CampaignId,
                    principalTable: "Campaigns",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_AgentThreads_Campaigns_CampaignId1",
                    column: x => x.CampaignId1,
                    principalTable: "Campaigns",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Artifacts",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                CampaignId = table.Column<Guid>(type: "TEXT", nullable: false),
                FileName = table.Column<string>(type: "TEXT", maxLength: 500, nullable: false),
                FilePath = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false),
                MimeType = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
                Version = table.Column<int>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                DeletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Artifacts", x => x.Id);
                table.ForeignKey(
                    name: "FK_Artifacts_Campaigns_CampaignId",
                    column: x => x.CampaignId,
                    principalTable: "Campaigns",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "CampaignTasks",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                CampaignId = table.Column<Guid>(type: "TEXT", nullable: false),
                Title = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Description = table.Column<string>(type: "TEXT", nullable: false),
                Status = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false),
                OrderIndex = table.Column<int>(type: "INTEGER", nullable: false),
                RequiresApproval = table.Column<bool>(type: "INTEGER", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_CampaignTasks", x => x.Id);
                table.ForeignKey(
                    name: "FK_CampaignTasks_Campaigns_CampaignId",
                    column: x => x.CampaignId,
                    principalTable: "Campaigns",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateTable(
            name: "Leads",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "TEXT", nullable: false),
                CampaignId = table.Column<Guid>(type: "TEXT", nullable: false),
                Source = table.Column<string>(type: "TEXT", maxLength: 200, nullable: false),
                Score = table.Column<decimal>(type: "decimal(5,2)", nullable: true),
                ScoringRationale = table.Column<string>(type: "TEXT", nullable: true),
                Data = table.Column<string>(type: "TEXT", nullable: false),
                CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                ScoredAt = table.Column<DateTime>(type: "TEXT", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Leads", x => x.Id);
                table.ForeignKey(
                    name: "FK_Leads_Campaigns_CampaignId",
                    column: x => x.CampaignId,
                    principalTable: "Campaigns",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_AgentThreads_CampaignId",
            table: "AgentThreads",
            column: "CampaignId");

        migrationBuilder.CreateIndex(
            name: "IX_AgentThreads_CampaignId1",
            table: "AgentThreads",
            column: "CampaignId1");

        migrationBuilder.CreateIndex(
            name: "IX_AgentThreads_ThreadId",
            table: "AgentThreads",
            column: "ThreadId",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Artifacts_CampaignId",
            table: "Artifacts",
            column: "CampaignId");

        migrationBuilder.CreateIndex(
            name: "IX_CampaignTasks_CampaignId_OrderIndex",
            table: "CampaignTasks",
            columns: new[] { "CampaignId", "OrderIndex" });

        migrationBuilder.CreateIndex(
            name: "IX_Events_CampaignId",
            table: "Events",
            column: "CampaignId");

        migrationBuilder.CreateIndex(
            name: "IX_Events_Timestamp",
            table: "Events",
            column: "Timestamp");

        migrationBuilder.CreateIndex(
            name: "IX_Leads_CampaignId",
            table: "Leads",
            column: "CampaignId");
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        ArgumentNullException.ThrowIfNull(migrationBuilder);

        migrationBuilder.DropTable(
                name: "AgentThreads");

        migrationBuilder.DropTable(
            name: "Artifacts");

        migrationBuilder.DropTable(
            name: "CampaignTasks");

        migrationBuilder.DropTable(
            name: "Events");

        migrationBuilder.DropTable(
            name: "Leads");

        migrationBuilder.DropTable(
            name: "Campaigns");
    }
}
