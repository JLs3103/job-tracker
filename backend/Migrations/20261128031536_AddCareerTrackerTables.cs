using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace backend.Migrations
{
    /// <inheritdoc />
    public partial class AddCareerTrackerTables : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CareerSites",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Url = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    LastJobUpdated = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    LastCheckedAt = table.Column<long>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CareerSites", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "JobApplications",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Company = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    JobTitle = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    AppliedVia = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    AppliedLink = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: true),
                    AppliedDate = table.Column<long>(type: "INTEGER", nullable: false),
                    LastUpdated = table.Column<long>(type: "INTEGER", nullable: false),
                    Status = table.Column<string>(type: "TEXT", maxLength: 120, nullable: false),
                    Result = table.Column<string>(type: "TEXT", nullable: false),
                    SourceJobPostingId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_JobApplications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "OnGoingJobs",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Company = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    JobTitle = table.Column<string>(type: "TEXT", maxLength: 300, nullable: false),
                    Link = table.Column<string>(type: "TEXT", maxLength: 2048, nullable: false),
                    Notes = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<long>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_OnGoingJobs", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_AppliedDate",
                table: "JobApplications",
                column: "AppliedDate");

            migrationBuilder.CreateIndex(
                name: "IX_JobApplications_SourceJobPostingId",
                table: "JobApplications",
                column: "SourceJobPostingId");

            migrationBuilder.CreateIndex(
                name: "IX_OnGoingJobs_CreatedAt",
                table: "OnGoingJobs",
                column: "CreatedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CareerSites");

            migrationBuilder.DropTable(
                name: "JobApplications");

            migrationBuilder.DropTable(
                name: "OnGoingJobs");
        }
    }
}
