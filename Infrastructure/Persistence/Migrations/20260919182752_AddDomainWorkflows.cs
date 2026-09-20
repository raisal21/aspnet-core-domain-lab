using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AspNetCoreDomainLab.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddDomainWorkflows : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "banking");

            migrationBuilder.EnsureSchema(
                name: "industrial");

            migrationBuilder.EnsureSchema(
                name: "healthcare");

            migrationBuilder.EnsureSchema(
                name: "logistics");

            migrationBuilder.CreateTable(
                name: "assets",
                schema: "industrial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    LastSeenAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_assets", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "customers",
                schema: "banking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerReference = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_customers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "delivery_routes",
                schema: "logistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_delivery_routes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "medical_inventory",
                schema: "healthcare",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Sku = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Name = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ReorderLevel = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_medical_inventory", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "patients",
                schema: "healthcare",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    MedicalRecordNumber = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    DisplayName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    BirthDate = table.Column<DateOnly>(type: "date", nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_patients", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "shipments",
                schema: "logistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TrackingNumber = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Origin = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Destination = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    CurrentLocation = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_shipments", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "warehouse_stock",
                schema: "logistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    WarehouseCode = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Sku = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    ReservedQuantity = table.Column<int>(type: "integer", nullable: false),
                    UpdatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_warehouse_stock", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "alarms",
                schema: "industrial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Severity = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Message = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    RaisedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    AcknowledgedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_alarms", x => x.Id);
                    table.ForeignKey(
                        name: "FK_alarms_assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "industrial",
                        principalTable: "assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "device_commands",
                schema: "industrial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    CommandType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    Status = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    IssuedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_device_commands", x => x.Id);
                    table.ForeignKey(
                        name: "FK_device_commands_assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "industrial",
                        principalTable: "assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "maintenance_work_orders",
                schema: "industrial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    Title = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Priority = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    CompletedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_maintenance_work_orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_maintenance_work_orders_assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "industrial",
                        principalTable: "assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "telemetry",
                schema: "industrial",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AssetId = table.Column<Guid>(type: "uuid", nullable: false),
                    ObservedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Value = table.Column<decimal>(type: "numeric(18,4)", precision: 18, scale: 4, nullable: false),
                    Unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_telemetry", x => x.Id);
                    table.ForeignKey(
                        name: "FK_telemetry_assets_AssetId",
                        column: x => x.AssetId,
                        principalSchema: "industrial",
                        principalTable: "assets",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "accounts",
                schema: "banking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CustomerId = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountReference = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    BalanceMinor = table.Column<long>(type: "bigint", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Version = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_accounts", x => x.Id);
                    table.ForeignKey(
                        name: "FK_accounts_customers_CustomerId",
                        column: x => x.CustomerId,
                        principalSchema: "banking",
                        principalTable: "customers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "route_stops",
                schema: "logistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    RouteId = table.Column<Guid>(type: "uuid", nullable: false),
                    StopCode = table.Column<string>(type: "character varying(40)", maxLength: 40, nullable: false),
                    AddressLabel = table.Column<string>(type: "character varying(160)", maxLength: 160, nullable: false),
                    Sequence = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_route_stops", x => x.Id);
                    table.ForeignKey(
                        name: "FK_route_stops_delivery_routes_RouteId",
                        column: x => x.RouteId,
                        principalSchema: "logistics",
                        principalTable: "delivery_routes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "appointments",
                schema: "healthcare",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    StartsAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Provider = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_appointments", x => x.Id);
                    table.ForeignKey(
                        name: "FK_appointments_patients_PatientId",
                        column: x => x.PatientId,
                        principalSchema: "healthcare",
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "lab_results",
                schema: "healthcare",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    PatientId = table.Column<Guid>(type: "uuid", nullable: false),
                    TestName = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: false),
                    ResultValue = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    Unit = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "character varying(32)", maxLength: 32, nullable: false),
                    CollectedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    ReviewedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_lab_results", x => x.Id);
                    table.ForeignKey(
                        name: "FK_lab_results_patients_PatientId",
                        column: x => x.PatientId,
                        principalSchema: "healthcare",
                        principalTable: "patients",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "carrier_events",
                schema: "logistics",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ShipmentId = table.Column<Guid>(type: "uuid", nullable: false),
                    ExternalReference = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    EventType = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false),
                    PayloadSummary = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    ReceivedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_carrier_events", x => x.Id);
                    table.ForeignKey(
                        name: "FK_carrier_events_shipments_ShipmentId",
                        column: x => x.ShipmentId,
                        principalSchema: "logistics",
                        principalTable: "shipments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "transfers",
                schema: "banking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    FromAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    ToAccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    AmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    Currency = table.Column<string>(type: "character varying(3)", maxLength: 3, nullable: false),
                    Status = table.Column<string>(type: "character varying(48)", maxLength: 48, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "character varying(80)", maxLength: 80, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_transfers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_transfers_accounts_FromAccountId",
                        column: x => x.FromAccountId,
                        principalSchema: "banking",
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_transfers_accounts_ToAccountId",
                        column: x => x.ToAccountId,
                        principalSchema: "banking",
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "account_transactions",
                schema: "banking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    AccountId = table.Column<Guid>(type: "uuid", nullable: false),
                    TransferId = table.Column<Guid>(type: "uuid", nullable: true),
                    AmountMinor = table.Column<long>(type: "bigint", nullable: false),
                    Direction = table.Column<string>(type: "character varying(16)", maxLength: 16, nullable: false),
                    Description = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_account_transactions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_account_transactions_accounts_AccountId",
                        column: x => x.AccountId,
                        principalSchema: "banking",
                        principalTable: "accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_account_transactions_transfers_TransferId",
                        column: x => x.TransferId,
                        principalSchema: "banking",
                        principalTable: "transfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "risk_reviews",
                schema: "banking",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    TransferId = table.Column<Guid>(type: "uuid", nullable: false),
                    Status = table.Column<string>(type: "character varying(24)", maxLength: 24, nullable: false),
                    Reason = table.Column<string>(type: "character varying(240)", maxLength: 240, nullable: false),
                    ReviewedBy = table.Column<string>(type: "character varying(120)", maxLength: 120, nullable: true),
                    CreatedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    DecidedAtUtc = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_risk_reviews", x => x.Id);
                    table.ForeignKey(
                        name: "FK_risk_reviews_transfers_TransferId",
                        column: x => x.TransferId,
                        principalSchema: "banking",
                        principalTable: "transfers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_account_transactions_AccountId",
                schema: "banking",
                table: "account_transactions",
                column: "AccountId");

            migrationBuilder.CreateIndex(
                name: "IX_account_transactions_TransferId",
                schema: "banking",
                table: "account_transactions",
                column: "TransferId");

            migrationBuilder.CreateIndex(
                name: "IX_accounts_AccountReference",
                schema: "banking",
                table: "accounts",
                column: "AccountReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_accounts_CustomerId",
                schema: "banking",
                table: "accounts",
                column: "CustomerId");

            migrationBuilder.CreateIndex(
                name: "IX_alarms_AssetId_Status",
                schema: "industrial",
                table: "alarms",
                columns: new[] { "AssetId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_appointments_PatientId_StartsAtUtc",
                schema: "healthcare",
                table: "appointments",
                columns: new[] { "PatientId", "StartsAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_assets_AssetCode",
                schema: "industrial",
                table: "assets",
                column: "AssetCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_carrier_events_ExternalReference",
                schema: "logistics",
                table: "carrier_events",
                column: "ExternalReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_carrier_events_ShipmentId",
                schema: "logistics",
                table: "carrier_events",
                column: "ShipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_customers_CustomerReference",
                schema: "banking",
                table: "customers",
                column: "CustomerReference",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_delivery_routes_RouteCode",
                schema: "logistics",
                table: "delivery_routes",
                column: "RouteCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_device_commands_AssetId",
                schema: "industrial",
                table: "device_commands",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_device_commands_IdempotencyKey",
                schema: "industrial",
                table: "device_commands",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_lab_results_PatientId",
                schema: "healthcare",
                table: "lab_results",
                column: "PatientId");

            migrationBuilder.CreateIndex(
                name: "IX_maintenance_work_orders_AssetId",
                schema: "industrial",
                table: "maintenance_work_orders",
                column: "AssetId");

            migrationBuilder.CreateIndex(
                name: "IX_medical_inventory_Sku",
                schema: "healthcare",
                table: "medical_inventory",
                column: "Sku",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_patients_MedicalRecordNumber",
                schema: "healthcare",
                table: "patients",
                column: "MedicalRecordNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_risk_reviews_TransferId_Status",
                schema: "banking",
                table: "risk_reviews",
                columns: new[] { "TransferId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_route_stops_RouteId_Sequence",
                schema: "logistics",
                table: "route_stops",
                columns: new[] { "RouteId", "Sequence" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_shipments_TrackingNumber",
                schema: "logistics",
                table: "shipments",
                column: "TrackingNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_telemetry_AssetId_ObservedAtUtc",
                schema: "industrial",
                table: "telemetry",
                columns: new[] { "AssetId", "ObservedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_transfers_FromAccountId",
                schema: "banking",
                table: "transfers",
                column: "FromAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_transfers_IdempotencyKey",
                schema: "banking",
                table: "transfers",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_transfers_ToAccountId",
                schema: "banking",
                table: "transfers",
                column: "ToAccountId");

            migrationBuilder.CreateIndex(
                name: "IX_warehouse_stock_WarehouseCode_Sku",
                schema: "logistics",
                table: "warehouse_stock",
                columns: new[] { "WarehouseCode", "Sku" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "account_transactions",
                schema: "banking");

            migrationBuilder.DropTable(
                name: "alarms",
                schema: "industrial");

            migrationBuilder.DropTable(
                name: "appointments",
                schema: "healthcare");

            migrationBuilder.DropTable(
                name: "carrier_events",
                schema: "logistics");

            migrationBuilder.DropTable(
                name: "device_commands",
                schema: "industrial");

            migrationBuilder.DropTable(
                name: "lab_results",
                schema: "healthcare");

            migrationBuilder.DropTable(
                name: "maintenance_work_orders",
                schema: "industrial");

            migrationBuilder.DropTable(
                name: "medical_inventory",
                schema: "healthcare");

            migrationBuilder.DropTable(
                name: "risk_reviews",
                schema: "banking");

            migrationBuilder.DropTable(
                name: "route_stops",
                schema: "logistics");

            migrationBuilder.DropTable(
                name: "telemetry",
                schema: "industrial");

            migrationBuilder.DropTable(
                name: "warehouse_stock",
                schema: "logistics");

            migrationBuilder.DropTable(
                name: "shipments",
                schema: "logistics");

            migrationBuilder.DropTable(
                name: "patients",
                schema: "healthcare");

            migrationBuilder.DropTable(
                name: "transfers",
                schema: "banking");

            migrationBuilder.DropTable(
                name: "delivery_routes",
                schema: "logistics");

            migrationBuilder.DropTable(
                name: "assets",
                schema: "industrial");

            migrationBuilder.DropTable(
                name: "accounts",
                schema: "banking");

            migrationBuilder.DropTable(
                name: "customers",
                schema: "banking");
        }
    }
}
