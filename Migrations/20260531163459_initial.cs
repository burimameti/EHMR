using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EHMR.Migrations
{
    /// <inheritdoc />
    public partial class initial : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Alerts",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Level = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    IsResolved = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Alerts", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AuditLogs",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Action = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EntityName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BeforeValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Data = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AfterValue = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EntityId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AuditLogs", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Medicines",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    GenericName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DosageForm = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DefaultDosage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Medicines", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Notifications",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Message = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsRead = table.Column<bool>(type: "bit", nullable: false),
                    Severity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Notifications", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Patients",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    NationalId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SSN = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BirthDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Phone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Email = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Address = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    City = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PostalCode = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmergencyContactName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmergencyContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EmergencyRelationship = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    BloodType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Allergies = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PrimaryDiagnosis = table.Column<string>(type: "nvarchar(300)", maxLength: 300, nullable: false),
                    ClinicalNotes = table.Column<string>(type: "nvarchar(4000)", maxLength: 4000, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    RegistrationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    Gender = table.Column<int>(type: "int", maxLength: 20, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Patients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TaskItems",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TreatmentPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    DueDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Priority = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaskItems", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "TherapyProtocols",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DiseaseCategory = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DurationInDays = table.Column<int>(type: "int", nullable: false),
                    CreatedByDoctor = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TherapyProtocols", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Username = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PasswordHash = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Role = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    Position = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Inventories",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InitialStock = table.Column<int>(type: "int", nullable: false),
                    CurrentStock = table.Column<int>(type: "int", nullable: false),
                    ReservedStock = table.Column<int>(type: "int", nullable: false),
                    MinimumStockLevel = table.Column<int>(type: "int", nullable: false),
                    MaximumStockLevel = table.Column<int>(type: "int", nullable: false),
                    LastUpdated = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MinimumStockAlert = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Inventories", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Inventories_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "Medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Diagnoses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EncounterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ClinicalDescription = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    System = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Severity = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsPrimary = table.Column<bool>(type: "bit", nullable: false),
                    DiagnosedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Diagnoses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Diagnoses_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PatientDocuments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    EncounterId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileUrl = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FileSizeInBytes = table.Column<long>(type: "bigint", nullable: false),
                    IsDeleted = table.Column<bool>(type: "bit", nullable: false),
                    IsCritical = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PatientDocuments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_PatientDocuments_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Prescriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Dosage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Medication = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Instructions = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Prescriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Prescriptions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Doctors",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    FirstName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LastName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    LicenseNumber = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Specialty = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ContactPhone = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Doctors", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Doctors_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserModules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ModuleKey = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IsEnabled = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserModules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserModules_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "UserScopes",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScopeType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TargetId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    IsPrimaryScope = table.Column<bool>(type: "bit", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserScopes", x => x.Id);
                    table.ForeignKey(
                        name: "FK_UserScopes_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "PrescriptionMedicines",
                columns: table => new
                {
                    PrescriptionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Dosage = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    Frequency = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                    DurationDays = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PrescriptionMedicines", x => new { x.PrescriptionId, x.MedicineId });
                    table.ForeignKey(
                        name: "FK_PrescriptionMedicines_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "Medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_PrescriptionMedicines_Prescriptions_PrescriptionId",
                        column: x => x.PrescriptionId,
                        principalTable: "Prescriptions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Encounters",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Date = table.Column<DateTime>(type: "datetime2", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AppointmentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PeriodStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PeriodEnd = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ChiefComplaint = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ClinicalNotes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    EncounterType = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Encounters", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Encounters_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Encounters_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TreatmentPlans",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ProtocolName = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    TherapyProtocolId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TreatmentPlans", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TreatmentPlans_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TreatmentPlans_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TreatmentPlans_TherapyProtocols_TherapyProtocolId",
                        column: x => x.TherapyProtocolId,
                        principalTable: "TherapyProtocols",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "TherapySchedules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TreatmentPlanId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    FrequencyInDays = table.Column<int>(type: "int", nullable: false),
                    GraceDays = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "bit", nullable: false),
                    StartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    EndDate = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TherapySchedules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TherapySchedules_TreatmentPlans_TreatmentPlanId",
                        column: x => x.TreatmentPlanId,
                        principalTable: "TreatmentPlans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ScheduleMedicineRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TherapyScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Dosage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    AdministrationDayOffset = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ScheduleMedicineRules", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ScheduleMedicineRules_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "Medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ScheduleMedicineRules_TherapySchedules_TherapyScheduleId",
                        column: x => x.TherapyScheduleId,
                        principalTable: "TherapySchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TherapyCycles",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TherapyScheduleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CycleNumber = table.Column<int>(type: "int", nullable: false),
                    PlannedStartDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlannedEndDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    StatusChangedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    ReasonForMissing = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    IsEscalatedToMissedGroup = table.Column<bool>(type: "bit", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TherapyCycles", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TherapyCycles_TherapySchedules_TherapyScheduleId",
                        column: x => x.TherapyScheduleId,
                        principalTable: "TherapySchedules",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Appointments",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ScheduledStart = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ScheduledEnd = table.Column<DateTime>(type: "datetime2", nullable: false),
                    ReasonForVisit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    TherapyCycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Appointments_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Appointments_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_Appointments_TherapyCycles_TherapyCycleId",
                        column: x => x.TherapyCycleId,
                        principalTable: "TherapyCycles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "CycleMedicationDoses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TherapyCycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TargetDosage = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    DosageValue = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    DosageUnit = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    PlannedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    PlannedAdministrationDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    AdministeredAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    AdministeredByNurseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    Status = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReasonIfSkipped = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Quantity = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CycleMedicationDoses", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CycleMedicationDoses_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "Medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_CycleMedicationDoses_TherapyCycles_TherapyCycleId",
                        column: x => x.TherapyCycleId,
                        principalTable: "TherapyCycles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "InventoryTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InventoryId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    MedicineId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TherapyDoseId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TherapyCycleId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    PatientId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    TransactionType = table.Column<int>(type: "int", nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TransactionDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    Notes = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    ReferenceNote = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedBy = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InventoryTransactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_CycleMedicationDoses_TherapyDoseId",
                        column: x => x.TherapyDoseId,
                        principalTable: "CycleMedicationDoses",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Inventories_InventoryId",
                        column: x => x.InventoryId,
                        principalTable: "Inventories",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Medicines_MedicineId",
                        column: x => x.MedicineId,
                        principalTable: "Medicines",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_Patients_PatientId",
                        column: x => x.PatientId,
                        principalTable: "Patients",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_InventoryTransactions_TherapyCycles_TherapyCycleId",
                        column: x => x.TherapyCycleId,
                        principalTable: "TherapyCycles",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_DoctorId",
                table: "Appointments",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_PatientId",
                table: "Appointments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_ScheduledStart",
                table: "Appointments",
                column: "ScheduledStart");

            migrationBuilder.CreateIndex(
                name: "IX_Appointments_TherapyCycleId",
                table: "Appointments",
                column: "TherapyCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_CycleMedicationDoses_MedicineId",
                table: "CycleMedicationDoses",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_CycleMedicationDoses_TherapyCycleId",
                table: "CycleMedicationDoses",
                column: "TherapyCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_Diagnoses_PatientId",
                table: "Diagnoses",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Doctors_UserId",
                table: "Doctors",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Encounters_DoctorId",
                table: "Encounters",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_Encounters_PatientId",
                table: "Encounters",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Inventories_MedicineId",
                table: "Inventories",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_CreatedAt",
                table: "InventoryTransactions",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_InventoryId",
                table: "InventoryTransactions",
                column: "InventoryId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_MedicineId",
                table: "InventoryTransactions",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_PatientId",
                table: "InventoryTransactions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_TherapyCycleId",
                table: "InventoryTransactions",
                column: "TherapyCycleId");

            migrationBuilder.CreateIndex(
                name: "IX_InventoryTransactions_TherapyDoseId",
                table: "InventoryTransactions",
                column: "TherapyDoseId");

            migrationBuilder.CreateIndex(
                name: "IX_Medicines_Name",
                table: "Medicines",
                column: "Name");

            migrationBuilder.CreateIndex(
                name: "IX_PatientDocuments_PatientId",
                table: "PatientDocuments",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_Patients_FirstName_LastName",
                table: "Patients",
                columns: new[] { "FirstName", "LastName" });

            migrationBuilder.CreateIndex(
                name: "IX_PrescriptionMedicines_MedicineId",
                table: "PrescriptionMedicines",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_Prescriptions_PatientId",
                table: "Prescriptions",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleMedicineRules_MedicineId",
                table: "ScheduleMedicineRules",
                column: "MedicineId");

            migrationBuilder.CreateIndex(
                name: "IX_ScheduleMedicineRules_TherapyScheduleId",
                table: "ScheduleMedicineRules",
                column: "TherapyScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_TherapyCycles_TherapyScheduleId",
                table: "TherapyCycles",
                column: "TherapyScheduleId");

            migrationBuilder.CreateIndex(
                name: "IX_TherapySchedules_TreatmentPlanId",
                table: "TherapySchedules",
                column: "TreatmentPlanId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlans_DoctorId",
                table: "TreatmentPlans",
                column: "DoctorId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlans_PatientId",
                table: "TreatmentPlans",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_TreatmentPlans_TherapyProtocolId",
                table: "TreatmentPlans",
                column: "TherapyProtocolId");

            migrationBuilder.CreateIndex(
                name: "IX_UserModules_UserId",
                table: "UserModules",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_UserScopes_UserId",
                table: "UserScopes",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Alerts");

            migrationBuilder.DropTable(
                name: "Appointments");

            migrationBuilder.DropTable(
                name: "AuditLogs");

            migrationBuilder.DropTable(
                name: "Diagnoses");

            migrationBuilder.DropTable(
                name: "Encounters");

            migrationBuilder.DropTable(
                name: "InventoryTransactions");

            migrationBuilder.DropTable(
                name: "Notifications");

            migrationBuilder.DropTable(
                name: "PatientDocuments");

            migrationBuilder.DropTable(
                name: "PrescriptionMedicines");

            migrationBuilder.DropTable(
                name: "ScheduleMedicineRules");

            migrationBuilder.DropTable(
                name: "TaskItems");

            migrationBuilder.DropTable(
                name: "UserModules");

            migrationBuilder.DropTable(
                name: "UserScopes");

            migrationBuilder.DropTable(
                name: "CycleMedicationDoses");

            migrationBuilder.DropTable(
                name: "Inventories");

            migrationBuilder.DropTable(
                name: "Prescriptions");

            migrationBuilder.DropTable(
                name: "TherapyCycles");

            migrationBuilder.DropTable(
                name: "Medicines");

            migrationBuilder.DropTable(
                name: "TherapySchedules");

            migrationBuilder.DropTable(
                name: "TreatmentPlans");

            migrationBuilder.DropTable(
                name: "Doctors");

            migrationBuilder.DropTable(
                name: "Patients");

            migrationBuilder.DropTable(
                name: "TherapyProtocols");

            migrationBuilder.DropTable(
                name: "Users");
        }
    }
}
