using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace PrestexaAPI.Migrations
{
    /// <inheritdoc />
    public partial class AddMismoImportDomainFirstPass : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "BorrowerAssets",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyNmlsNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BorrowerId = table.Column<int>(type: "integer", nullable: false),
                    AssetType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AssetCashOrMarketValueAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BorrowerAssets", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BorrowerAssets_Borrowers_BorrowerId",
                        column: x => x.BorrowerId,
                        principalTable: "Borrowers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BorrowerAssets_Companies_CompanyNmlsNumber",
                        column: x => x.CompanyNmlsNumber,
                        principalTable: "Companies",
                        principalColumn: "NmlsNumber",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BorrowerDeclarations",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyNmlsNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LoanId = table.Column<int>(type: "integer", nullable: false),
                    BorrowerId = table.Column<int>(type: "integer", nullable: false),
                    DeclarationType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    IsAffirmative = table.Column<bool>(type: "boolean", nullable: false),
                    Explanation = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BorrowerDeclarations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BorrowerDeclarations_Borrowers_BorrowerId",
                        column: x => x.BorrowerId,
                        principalTable: "Borrowers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BorrowerDeclarations_Companies_CompanyNmlsNumber",
                        column: x => x.CompanyNmlsNumber,
                        principalTable: "Companies",
                        principalColumn: "NmlsNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_BorrowerDeclarations_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BorrowerEmployments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyNmlsNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BorrowerId = table.Column<int>(type: "integer", nullable: false),
                    EmployerName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    EmploymentStatusType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    EmploymentStartDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    EmploymentPositionDescription = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BorrowerEmployments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BorrowerEmployments_Borrowers_BorrowerId",
                        column: x => x.BorrowerId,
                        principalTable: "Borrowers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BorrowerEmployments_Companies_CompanyNmlsNumber",
                        column: x => x.CompanyNmlsNumber,
                        principalTable: "Companies",
                        principalColumn: "NmlsNumber",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BorrowerLiabilities",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyNmlsNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BorrowerId = table.Column<int>(type: "integer", nullable: false),
                    LiabilityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MonthlyPaymentAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    UPBAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BorrowerLiabilities", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BorrowerLiabilities_Borrowers_BorrowerId",
                        column: x => x.BorrowerId,
                        principalTable: "Borrowers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BorrowerLiabilities_Companies_CompanyNmlsNumber",
                        column: x => x.CompanyNmlsNumber,
                        principalTable: "Companies",
                        principalColumn: "NmlsNumber",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "GovernmentMonitorings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyNmlsNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LoanId = table.Column<int>(type: "integer", nullable: false),
                    BorrowerId = table.Column<int>(type: "integer", nullable: false),
                    EthnicityType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    RaceType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    SexType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CollectionMethodType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GovernmentMonitorings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_GovernmentMonitorings_Borrowers_BorrowerId",
                        column: x => x.BorrowerId,
                        principalTable: "Borrowers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GovernmentMonitorings_Companies_CompanyNmlsNumber",
                        column: x => x.CompanyNmlsNumber,
                        principalTable: "Companies",
                        principalColumn: "NmlsNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_GovernmentMonitorings_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "HousingExpenses",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyNmlsNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LoanId = table.Column<int>(type: "integer", nullable: false),
                    BorrowerId = table.Column<int>(type: "integer", nullable: true),
                    HousingExpenseType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    PresentHousingExpenseAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    ProposedHousingExpenseAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_HousingExpenses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_HousingExpenses_Borrowers_BorrowerId",
                        column: x => x.BorrowerId,
                        principalTable: "Borrowers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_HousingExpenses_Companies_CompanyNmlsNumber",
                        column: x => x.CompanyNmlsNumber,
                        principalTable: "Companies",
                        principalColumn: "NmlsNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_HousingExpenses_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoanConditions",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyNmlsNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LoanId = table.Column<int>(type: "integer", nullable: false),
                    ConditionCategory = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    ConditionStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    Description = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: true),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanConditions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanConditions_Companies_CompanyNmlsNumber",
                        column: x => x.CompanyNmlsNumber,
                        principalTable: "Companies",
                        principalColumn: "NmlsNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanConditions_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoanMilestones",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyNmlsNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LoanId = table.Column<int>(type: "integer", nullable: false),
                    MilestoneType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    MilestoneStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    OccurredAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanMilestones", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanMilestones_Companies_CompanyNmlsNumber",
                        column: x => x.CompanyNmlsNumber,
                        principalTable: "Companies",
                        principalColumn: "NmlsNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanMilestones_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "LoanOfficerAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyNmlsNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LoanId = table.Column<int>(type: "integer", nullable: false),
                    UserId = table.Column<int>(type: "integer", nullable: false),
                    IsPrimary = table.Column<bool>(type: "boolean", nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanOfficerAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanOfficerAssignments_Companies_CompanyNmlsNumber",
                        column: x => x.CompanyNmlsNumber,
                        principalTable: "Companies",
                        principalColumn: "NmlsNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanOfficerAssignments_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoanOfficerAssignments_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoanTerms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyNmlsNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LoanId = table.Column<int>(type: "integer", nullable: false),
                    NoteAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    NoteRatePercent = table.Column<decimal>(type: "numeric", nullable: false),
                    LoanAmortizationPeriodCount = table.Column<int>(type: "integer", nullable: false),
                    LoanAmortizationType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LoanPurposeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    MortgageType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    LienPriorityType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanTerms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanTerms_Companies_CompanyNmlsNumber",
                        column: x => x.CompanyNmlsNumber,
                        principalTable: "Companies",
                        principalColumn: "NmlsNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanTerms_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Realtors",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyNmlsNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    FullName = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    Email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                    PhoneNumber = table.Column<string>(type: "character varying(30)", maxLength: 30, nullable: true),
                    LicenseNumber = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Realtors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Realtors_Companies_CompanyNmlsNumber",
                        column: x => x.CompanyNmlsNumber,
                        principalTable: "Companies",
                        principalColumn: "NmlsNumber",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "SubjectProperties",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyNmlsNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LoanId = table.Column<int>(type: "integer", nullable: false),
                    AddressLineText = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    CityName = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    StateCode = table.Column<string>(type: "character varying(2)", maxLength: 2, nullable: false),
                    PostalCode = table.Column<string>(type: "character varying(10)", maxLength: 10, nullable: false),
                    PropertyUsageType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    FinancedUnitCount = table.Column<int>(type: "integer", nullable: false),
                    PropertyEstimatedValueAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SubjectProperties", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SubjectProperties_Companies_CompanyNmlsNumber",
                        column: x => x.CompanyNmlsNumber,
                        principalTable: "Companies",
                        principalColumn: "NmlsNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SubjectProperties_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BorrowerIncomes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyNmlsNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    BorrowerId = table.Column<int>(type: "integer", nullable: false),
                    BorrowerEmploymentId = table.Column<int>(type: "integer", nullable: true),
                    IncomeType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    CurrentIncomeMonthlyTotalAmount = table.Column<decimal>(type: "numeric", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BorrowerIncomes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BorrowerIncomes_BorrowerEmployments_BorrowerEmploymentId",
                        column: x => x.BorrowerEmploymentId,
                        principalTable: "BorrowerEmployments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_BorrowerIncomes_Borrowers_BorrowerId",
                        column: x => x.BorrowerId,
                        principalTable: "Borrowers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BorrowerIncomes_Companies_CompanyNmlsNumber",
                        column: x => x.CompanyNmlsNumber,
                        principalTable: "Companies",
                        principalColumn: "NmlsNumber",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "LoanTasks",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyNmlsNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LoanId = table.Column<int>(type: "integer", nullable: false),
                    LoanConditionId = table.Column<int>(type: "integer", nullable: true),
                    AssignedUserId = table.Column<int>(type: "integer", nullable: true),
                    TaskType = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                    TaskStatus = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    DueDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanTasks", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanTasks_Companies_CompanyNmlsNumber",
                        column: x => x.CompanyNmlsNumber,
                        principalTable: "Companies",
                        principalColumn: "NmlsNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanTasks_LoanConditions_LoanConditionId",
                        column: x => x.LoanConditionId,
                        principalTable: "LoanConditions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LoanTasks_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoanTasks_Users_AssignedUserId",
                        column: x => x.AssignedUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "LoanRealtorAssignments",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    CompanyNmlsNumber = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                    LoanId = table.Column<int>(type: "integer", nullable: false),
                    RealtorId = table.Column<int>(type: "integer", nullable: false),
                    AssignmentType = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    AssignedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoanRealtorAssignments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_LoanRealtorAssignments_Companies_CompanyNmlsNumber",
                        column: x => x.CompanyNmlsNumber,
                        principalTable: "Companies",
                        principalColumn: "NmlsNumber",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_LoanRealtorAssignments_Loans_LoanId",
                        column: x => x.LoanId,
                        principalTable: "Loans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LoanRealtorAssignments_Realtors_RealtorId",
                        column: x => x.RealtorId,
                        principalTable: "Realtors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BorrowerAssets_BorrowerId",
                table: "BorrowerAssets",
                column: "BorrowerId");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowerAssets_CompanyNmlsNumber_BorrowerId_AssetType",
                table: "BorrowerAssets",
                columns: new[] { "CompanyNmlsNumber", "BorrowerId", "AssetType" });

            migrationBuilder.CreateIndex(
                name: "IX_BorrowerDeclarations_BorrowerId",
                table: "BorrowerDeclarations",
                column: "BorrowerId");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowerDeclarations_CompanyNmlsNumber_LoanId_BorrowerId_De~",
                table: "BorrowerDeclarations",
                columns: new[] { "CompanyNmlsNumber", "LoanId", "BorrowerId", "DeclarationType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BorrowerDeclarations_LoanId",
                table: "BorrowerDeclarations",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowerEmployments_BorrowerId",
                table: "BorrowerEmployments",
                column: "BorrowerId");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowerEmployments_CompanyNmlsNumber_BorrowerId_Employment~",
                table: "BorrowerEmployments",
                columns: new[] { "CompanyNmlsNumber", "BorrowerId", "EmploymentStatusType" });

            migrationBuilder.CreateIndex(
                name: "IX_BorrowerIncomes_BorrowerEmploymentId",
                table: "BorrowerIncomes",
                column: "BorrowerEmploymentId");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowerIncomes_BorrowerId",
                table: "BorrowerIncomes",
                column: "BorrowerId");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowerIncomes_CompanyNmlsNumber_BorrowerId_IncomeType",
                table: "BorrowerIncomes",
                columns: new[] { "CompanyNmlsNumber", "BorrowerId", "IncomeType" });

            migrationBuilder.CreateIndex(
                name: "IX_BorrowerLiabilities_BorrowerId",
                table: "BorrowerLiabilities",
                column: "BorrowerId");

            migrationBuilder.CreateIndex(
                name: "IX_BorrowerLiabilities_CompanyNmlsNumber_BorrowerId_LiabilityT~",
                table: "BorrowerLiabilities",
                columns: new[] { "CompanyNmlsNumber", "BorrowerId", "LiabilityType" });

            migrationBuilder.CreateIndex(
                name: "IX_GovernmentMonitorings_BorrowerId",
                table: "GovernmentMonitorings",
                column: "BorrowerId");

            migrationBuilder.CreateIndex(
                name: "IX_GovernmentMonitorings_CompanyNmlsNumber_LoanId_BorrowerId",
                table: "GovernmentMonitorings",
                columns: new[] { "CompanyNmlsNumber", "LoanId", "BorrowerId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_GovernmentMonitorings_LoanId",
                table: "GovernmentMonitorings",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_HousingExpenses_BorrowerId",
                table: "HousingExpenses",
                column: "BorrowerId");

            migrationBuilder.CreateIndex(
                name: "IX_HousingExpenses_CompanyNmlsNumber_LoanId_HousingExpenseType",
                table: "HousingExpenses",
                columns: new[] { "CompanyNmlsNumber", "LoanId", "HousingExpenseType" });

            migrationBuilder.CreateIndex(
                name: "IX_HousingExpenses_LoanId",
                table: "HousingExpenses",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanConditions_CompanyNmlsNumber_LoanId_ConditionStatus",
                table: "LoanConditions",
                columns: new[] { "CompanyNmlsNumber", "LoanId", "ConditionStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_LoanConditions_LoanId",
                table: "LoanConditions",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanMilestones_CompanyNmlsNumber_LoanId_MilestoneType",
                table: "LoanMilestones",
                columns: new[] { "CompanyNmlsNumber", "LoanId", "MilestoneType" });

            migrationBuilder.CreateIndex(
                name: "IX_LoanMilestones_LoanId",
                table: "LoanMilestones",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanOfficerAssignments_CompanyNmlsNumber_LoanId_UserId",
                table: "LoanOfficerAssignments",
                columns: new[] { "CompanyNmlsNumber", "LoanId", "UserId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanOfficerAssignments_LoanId",
                table: "LoanOfficerAssignments",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanOfficerAssignments_UserId",
                table: "LoanOfficerAssignments",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanRealtorAssignments_CompanyNmlsNumber_LoanId_RealtorId",
                table: "LoanRealtorAssignments",
                columns: new[] { "CompanyNmlsNumber", "LoanId", "RealtorId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanRealtorAssignments_LoanId",
                table: "LoanRealtorAssignments",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanRealtorAssignments_RealtorId",
                table: "LoanRealtorAssignments",
                column: "RealtorId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanTasks_AssignedUserId",
                table: "LoanTasks",
                column: "AssignedUserId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanTasks_CompanyNmlsNumber_LoanId_TaskStatus",
                table: "LoanTasks",
                columns: new[] { "CompanyNmlsNumber", "LoanId", "TaskStatus" });

            migrationBuilder.CreateIndex(
                name: "IX_LoanTasks_LoanConditionId",
                table: "LoanTasks",
                column: "LoanConditionId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanTasks_LoanId",
                table: "LoanTasks",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_LoanTerms_CompanyNmlsNumber_LoanId",
                table: "LoanTerms",
                columns: new[] { "CompanyNmlsNumber", "LoanId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_LoanTerms_LoanId",
                table: "LoanTerms",
                column: "LoanId");

            migrationBuilder.CreateIndex(
                name: "IX_Realtors_CompanyNmlsNumber_Email",
                table: "Realtors",
                columns: new[] { "CompanyNmlsNumber", "Email" });

            migrationBuilder.CreateIndex(
                name: "IX_SubjectProperties_CompanyNmlsNumber_LoanId",
                table: "SubjectProperties",
                columns: new[] { "CompanyNmlsNumber", "LoanId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_SubjectProperties_LoanId",
                table: "SubjectProperties",
                column: "LoanId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BorrowerAssets");

            migrationBuilder.DropTable(
                name: "BorrowerDeclarations");

            migrationBuilder.DropTable(
                name: "BorrowerIncomes");

            migrationBuilder.DropTable(
                name: "BorrowerLiabilities");

            migrationBuilder.DropTable(
                name: "GovernmentMonitorings");

            migrationBuilder.DropTable(
                name: "HousingExpenses");

            migrationBuilder.DropTable(
                name: "LoanMilestones");

            migrationBuilder.DropTable(
                name: "LoanOfficerAssignments");

            migrationBuilder.DropTable(
                name: "LoanRealtorAssignments");

            migrationBuilder.DropTable(
                name: "LoanTasks");

            migrationBuilder.DropTable(
                name: "LoanTerms");

            migrationBuilder.DropTable(
                name: "SubjectProperties");

            migrationBuilder.DropTable(
                name: "BorrowerEmployments");

            migrationBuilder.DropTable(
                name: "Realtors");

            migrationBuilder.DropTable(
                name: "LoanConditions");
        }
    }
}
