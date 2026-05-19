using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Workvert.Migrations
{
    /// <inheritdoc />
    public partial class WorkvertProfessionalCore : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ClientServiceRequests",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    ServiceArea = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    ProfessionalTypeNeeded = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Complexity = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Location = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    RemoteAllowed = table.Column<bool>(type: "bit", nullable: false),
                    BudgetMin = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    BudgetMax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    Urgency = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    RequiredSkills = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientServiceRequests", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "ProfessionalProfiles",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    UserId = table.Column<string>(type: "nvarchar(450)", maxLength: 450, nullable: false),
                    DisplayName = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    CurrentProfession = table.Column<string>(type: "nvarchar(140)", maxLength: 140, nullable: false),
                    Headline = table.Column<string>(type: "nvarchar(220)", maxLength: 220, nullable: true),
                    ExperienceSummary = table.Column<string>(type: "nvarchar(3000)", maxLength: 3000, nullable: true),
                    TechnicalSkills = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SoftSkills = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Tools = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Education = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Languages = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    DesiredLocation = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    WorkMode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    EngagementType = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    CompensationGoal = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    InterestAreas = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    PortfolioUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProfilePhotoPath = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    ProfilePhotoPurposeNote = table.Column<string>(type: "nvarchar(260)", maxLength: 260, nullable: false),
                    IsOpenToEmployment = table.Column<bool>(type: "bit", nullable: false),
                    IsOpenToFreelance = table.Column<bool>(type: "bit", nullable: false),
                    IsAvailableForServices = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfessionalProfiles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "WorkOpportunities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Source = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: false),
                    ExternalId = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: true),
                    SourceUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Title = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: false),
                    Organization = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    OpportunityType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    WorkMode = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RequiredSkills = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: true),
                    NiceToHaveSkills = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: true),
                    CompensationMin = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    CompensationMax = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: true),
                    CompensationPeriod = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastSeenAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WorkOpportunities", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CareerActionPlans",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfessionalProfileId = table.Column<int>(type: "int", nullable: false),
                    TargetRole = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(2500)", maxLength: 2500, nullable: true),
                    SkillGaps = table.Column<string>(type: "nvarchar(1500)", maxLength: 1500, nullable: true),
                    RecommendedLearning = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    CvAdvice = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    LinkedInAdvice = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SalaryInsight = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    NextSteps = table.Column<string>(type: "nvarchar(2500)", maxLength: 2500, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerActionPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CareerActionPlans_ProfessionalProfiles_ProfessionalProfileId",
                        column: x => x.ProfessionalProfileId,
                        principalTable: "ProfessionalProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "FreelancerServiceListings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfessionalProfileId = table.Column<int>(type: "int", nullable: false),
                    ServiceName = table.Column<string>(type: "nvarchar(160)", maxLength: 160, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2500)", maxLength: 2500, nullable: true),
                    Skills = table.Column<string>(type: "nvarchar(1200)", maxLength: 1200, nullable: true),
                    HourlyRate = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ProjectRateFrom = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    ProjectRateTo = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: true),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    Location = table.Column<string>(type: "nvarchar(180)", maxLength: 180, nullable: true),
                    RemoteAvailable = table.Column<bool>(type: "bit", nullable: false),
                    Availability = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    PortfolioUrl = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FreelancerServiceListings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FreelancerServiceListings_ProfessionalProfiles_ProfessionalProfileId",
                        column: x => x.ProfessionalProfileId,
                        principalTable: "ProfessionalProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "GeneratedProfessionalAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfessionalProfileId = table.Column<int>(type: "int", nullable: false),
                    AssetType = table.Column<string>(type: "nvarchar(60)", maxLength: 60, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Content = table.Column<string>(type: "nvarchar(max)", maxLength: 8000, nullable: false),
                    Language = table.Column<string>(type: "nvarchar(80)", maxLength: 80, nullable: true),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GeneratedProfessionalAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GeneratedProfessionalAssets_ProfessionalProfiles_ProfessionalProfileId",
                        column: x => x.ProfessionalProfileId,
                        principalTable: "ProfessionalProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfessionalSkills",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfessionalProfileId = table.Column<int>(type: "int", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Category = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    ProficiencyLevel = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: true),
                    YearsExperience = table.Column<int>(type: "int", nullable: true),
                    IsAiSuggested = table.Column<bool>(type: "bit", nullable: false),
                    IsConfirmedByUser = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfessionalSkills", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfessionalSkills_ProfessionalProfiles_ProfessionalProfileId",
                        column: x => x.ProfessionalProfileId,
                        principalTable: "ProfessionalProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProfessionalOpportunityMatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProfessionalProfileId = table.Column<int>(type: "int", nullable: false),
                    WorkOpportunityId = table.Column<int>(type: "int", nullable: false),
                    CompatibilityScore = table.Column<int>(type: "int", nullable: false),
                    MatchType = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    RecommendationReasons = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    MatchedSkills = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    MissingSkills = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    SuggestedNextStep = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    NotifiedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DismissedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProfessionalOpportunityMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProfessionalOpportunityMatches_ProfessionalProfiles_ProfessionalProfileId",
                        column: x => x.ProfessionalProfileId,
                        principalTable: "ProfessionalProfiles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ProfessionalOpportunityMatches_WorkOpportunities_WorkOpportunityId",
                        column: x => x.WorkOpportunityId,
                        principalTable: "WorkOpportunities",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ClientServiceMatches",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ClientServiceRequestId = table.Column<int>(type: "int", nullable: false),
                    FreelancerServiceListingId = table.Column<int>(type: "int", nullable: false),
                    CompatibilityScore = table.Column<int>(type: "int", nullable: false),
                    MatchReasons = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    SuggestedBrief = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    EstimatedBudget = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: true),
                    Status = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ContactedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true),
                    DismissedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ClientServiceMatches", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ClientServiceMatches_ClientServiceRequests_ClientServiceRequestId",
                        column: x => x.ClientServiceRequestId,
                        principalTable: "ClientServiceRequests",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ClientServiceMatches_FreelancerServiceListings_FreelancerServiceListingId",
                        column: x => x.FreelancerServiceListingId,
                        principalTable: "FreelancerServiceListings",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CareerActionPlans_ProfessionalProfileId_CreatedAtUtc",
                table: "CareerActionPlans",
                columns: new[] { "ProfessionalProfileId", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientServiceMatches_ClientServiceRequestId_CompatibilityScore",
                table: "ClientServiceMatches",
                columns: new[] { "ClientServiceRequestId", "CompatibilityScore" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientServiceMatches_ClientServiceRequestId_FreelancerServiceListingId",
                table: "ClientServiceMatches",
                columns: new[] { "ClientServiceRequestId", "FreelancerServiceListingId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ClientServiceMatches_FreelancerServiceListingId",
                table: "ClientServiceMatches",
                column: "FreelancerServiceListingId");

            migrationBuilder.CreateIndex(
                name: "IX_ClientServiceRequests_ServiceArea_Location_RemoteAllowed",
                table: "ClientServiceRequests",
                columns: new[] { "ServiceArea", "Location", "RemoteAllowed" });

            migrationBuilder.CreateIndex(
                name: "IX_ClientServiceRequests_UserId_Status_CreatedAtUtc",
                table: "ClientServiceRequests",
                columns: new[] { "UserId", "Status", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_FreelancerServiceListings_Category_Location_RemoteAvailable",
                table: "FreelancerServiceListings",
                columns: new[] { "Category", "Location", "RemoteAvailable" });

            migrationBuilder.CreateIndex(
                name: "IX_FreelancerServiceListings_ProfessionalProfileId_IsActive",
                table: "FreelancerServiceListings",
                columns: new[] { "ProfessionalProfileId", "IsActive" });

            migrationBuilder.CreateIndex(
                name: "IX_GeneratedProfessionalAssets_ProfessionalProfileId_AssetType_CreatedAtUtc",
                table: "GeneratedProfessionalAssets",
                columns: new[] { "ProfessionalProfileId", "AssetType", "CreatedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionalOpportunityMatches_ProfessionalProfileId_Status_CompatibilityScore",
                table: "ProfessionalOpportunityMatches",
                columns: new[] { "ProfessionalProfileId", "Status", "CompatibilityScore" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionalOpportunityMatches_WorkOpportunityId_ProfessionalProfileId",
                table: "ProfessionalOpportunityMatches",
                columns: new[] { "WorkOpportunityId", "ProfessionalProfileId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionalProfiles_CurrentProfession_DesiredLocation",
                table: "ProfessionalProfiles",
                columns: new[] { "CurrentProfession", "DesiredLocation" });

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionalProfiles_UserId",
                table: "ProfessionalProfiles",
                column: "UserId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProfessionalSkills_ProfessionalProfileId_Category_Name",
                table: "ProfessionalSkills",
                columns: new[] { "ProfessionalProfileId", "Category", "Name" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_WorkOpportunities_OpportunityType_WorkMode_Location",
                table: "WorkOpportunities",
                columns: new[] { "OpportunityType", "WorkMode", "Location" });

            migrationBuilder.CreateIndex(
                name: "IX_WorkOpportunities_Source_ExternalId",
                table: "WorkOpportunities",
                columns: new[] { "Source", "ExternalId" },
                unique: true,
                filter: "[ExternalId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_WorkOpportunities_Status_LastSeenAtUtc",
                table: "WorkOpportunities",
                columns: new[] { "Status", "LastSeenAtUtc" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CareerActionPlans");

            migrationBuilder.DropTable(
                name: "ClientServiceMatches");

            migrationBuilder.DropTable(
                name: "GeneratedProfessionalAssets");

            migrationBuilder.DropTable(
                name: "ProfessionalOpportunityMatches");

            migrationBuilder.DropTable(
                name: "ProfessionalSkills");

            migrationBuilder.DropTable(
                name: "ClientServiceRequests");

            migrationBuilder.DropTable(
                name: "FreelancerServiceListings");

            migrationBuilder.DropTable(
                name: "WorkOpportunities");

            migrationBuilder.DropTable(
                name: "ProfessionalProfiles");
        }
    }
}
