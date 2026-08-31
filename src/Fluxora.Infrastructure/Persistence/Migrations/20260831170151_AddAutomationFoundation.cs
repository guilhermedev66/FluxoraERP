using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Fluxora.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAutomationFoundation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DashboardSnapshots",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    BusinessDate = table.Column<DateOnly>(type: "date", nullable: false),
                    PreparedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    CurrentBalance = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: false),
                    MonthRevenue = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: false),
                    MonthExpenses = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: false),
                    OverdueReceivablesCount = table.Column<int>(type: "integer", nullable: false),
                    OverdueReceivablesAmount = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: false),
                    OverduePayablesCount = table.Column<int>(type: "integer", nullable: false),
                    OverduePayablesAmount = table.Column<decimal>(type: "numeric(19,2)", precision: 19, scale: 2, nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DashboardSnapshots", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DashboardSnapshots_BusinessDate",
                table: "DashboardSnapshots",
                column: "BusinessDate",
                unique: true);

            // Quartz.NET 3.20 PostgreSQL AdoJobStore schema. Kept in the application's
            // migration chain so a fresh Fluxora database is scheduler-ready before startup.
            migrationBuilder.Sql(
                """
                CREATE TABLE qrtz_job_details
                (
                    sched_name TEXT NOT NULL,
                    job_name TEXT NOT NULL,
                    job_group TEXT NOT NULL,
                    description TEXT NULL,
                    job_class_name TEXT NOT NULL,
                    is_durable BOOL NOT NULL,
                    is_nonconcurrent BOOL NOT NULL,
                    is_update_data BOOL NOT NULL,
                    requests_recovery BOOL NOT NULL,
                    job_data BYTEA NULL,
                    PRIMARY KEY (sched_name, job_name, job_group)
                );

                CREATE TABLE qrtz_triggers
                (
                    sched_name TEXT NOT NULL,
                    trigger_name TEXT NOT NULL,
                    trigger_group TEXT NOT NULL,
                    job_name TEXT NOT NULL,
                    job_group TEXT NOT NULL,
                    description TEXT NULL,
                    next_fire_time BIGINT NULL,
                    prev_fire_time BIGINT NULL,
                    priority INTEGER NULL,
                    trigger_state TEXT NOT NULL,
                    trigger_type TEXT NOT NULL,
                    start_time BIGINT NOT NULL,
                    end_time BIGINT NULL,
                    calendar_name TEXT NULL,
                    misfire_instr SMALLINT NULL,
                    misfire_orig_fire_time BIGINT NULL,
                    execution_group VARCHAR(200) NULL,
                    preferred_node VARCHAR(200) NULL,
                    preferred_node_auto BOOL NOT NULL DEFAULT FALSE,
                    job_data BYTEA NULL,
                    PRIMARY KEY (sched_name, trigger_name, trigger_group),
                    FOREIGN KEY (sched_name, job_name, job_group)
                        REFERENCES qrtz_job_details (sched_name, job_name, job_group)
                );

                CREATE TABLE qrtz_simple_triggers
                (
                    sched_name TEXT NOT NULL,
                    trigger_name TEXT NOT NULL,
                    trigger_group TEXT NOT NULL,
                    repeat_count BIGINT NOT NULL,
                    repeat_interval BIGINT NOT NULL,
                    times_triggered BIGINT NOT NULL,
                    PRIMARY KEY (sched_name, trigger_name, trigger_group),
                    FOREIGN KEY (sched_name, trigger_name, trigger_group)
                        REFERENCES qrtz_triggers (sched_name, trigger_name, trigger_group)
                        ON DELETE CASCADE
                );

                CREATE TABLE qrtz_simprop_triggers
                (
                    sched_name TEXT NOT NULL,
                    trigger_name TEXT NOT NULL,
                    trigger_group TEXT NOT NULL,
                    str_prop_1 TEXT NULL,
                    str_prop_2 TEXT NULL,
                    str_prop_3 TEXT NULL,
                    int_prop_1 INTEGER NULL,
                    int_prop_2 INTEGER NULL,
                    long_prop_1 BIGINT NULL,
                    long_prop_2 BIGINT NULL,
                    dec_prop_1 NUMERIC NULL,
                    dec_prop_2 NUMERIC NULL,
                    bool_prop_1 BOOL NULL,
                    bool_prop_2 BOOL NULL,
                    time_zone_id TEXT NULL,
                    PRIMARY KEY (sched_name, trigger_name, trigger_group),
                    FOREIGN KEY (sched_name, trigger_name, trigger_group)
                        REFERENCES qrtz_triggers (sched_name, trigger_name, trigger_group)
                        ON DELETE CASCADE
                );

                CREATE TABLE qrtz_cron_triggers
                (
                    sched_name TEXT NOT NULL,
                    trigger_name TEXT NOT NULL,
                    trigger_group TEXT NOT NULL,
                    cron_expression TEXT NOT NULL,
                    time_zone_id TEXT NULL,
                    PRIMARY KEY (sched_name, trigger_name, trigger_group),
                    FOREIGN KEY (sched_name, trigger_name, trigger_group)
                        REFERENCES qrtz_triggers (sched_name, trigger_name, trigger_group)
                        ON DELETE CASCADE
                );

                CREATE TABLE qrtz_blob_triggers
                (
                    sched_name TEXT NOT NULL,
                    trigger_name TEXT NOT NULL,
                    trigger_group TEXT NOT NULL,
                    blob_data BYTEA NULL,
                    PRIMARY KEY (sched_name, trigger_name, trigger_group),
                    FOREIGN KEY (sched_name, trigger_name, trigger_group)
                        REFERENCES qrtz_triggers (sched_name, trigger_name, trigger_group)
                        ON DELETE CASCADE
                );

                CREATE TABLE qrtz_calendars
                (
                    sched_name TEXT NOT NULL,
                    calendar_name TEXT NOT NULL,
                    calendar BYTEA NOT NULL,
                    PRIMARY KEY (sched_name, calendar_name)
                );

                CREATE TABLE qrtz_paused_trigger_grps
                (
                    sched_name TEXT NOT NULL,
                    trigger_group TEXT NOT NULL,
                    PRIMARY KEY (sched_name, trigger_group)
                );

                CREATE TABLE qrtz_fired_triggers
                (
                    sched_name TEXT NOT NULL,
                    entry_id TEXT NOT NULL,
                    trigger_name TEXT NOT NULL,
                    trigger_group TEXT NOT NULL,
                    instance_name TEXT NOT NULL,
                    fired_time BIGINT NOT NULL,
                    sched_time BIGINT NOT NULL,
                    priority INTEGER NOT NULL,
                    state TEXT NOT NULL,
                    job_name TEXT NULL,
                    job_group TEXT NULL,
                    is_nonconcurrent BOOL NOT NULL,
                    requests_recovery BOOL NULL,
                    execution_group VARCHAR(200) NULL,
                    PRIMARY KEY (sched_name, entry_id)
                );

                CREATE TABLE qrtz_scheduler_state
                (
                    sched_name TEXT NOT NULL,
                    instance_name TEXT NOT NULL,
                    last_checkin_time BIGINT NOT NULL,
                    checkin_interval BIGINT NOT NULL,
                    PRIMARY KEY (sched_name, instance_name)
                );

                CREATE TABLE qrtz_locks
                (
                    sched_name TEXT NOT NULL,
                    lock_name TEXT NOT NULL,
                    PRIMARY KEY (sched_name, lock_name)
                );

                CREATE INDEX idx_qrtz_j_req_recovery ON qrtz_job_details (sched_name, requests_recovery);
                CREATE INDEX idx_qrtz_j_g_n ON qrtz_job_details (sched_name, job_group, job_name);
                CREATE INDEX idx_qrtz_t_j ON qrtz_triggers (sched_name, job_name, job_group);
                CREATE INDEX idx_qrtz_t_c ON qrtz_triggers (sched_name, calendar_name);
                CREATE INDEX idx_qrtz_t_g_n ON qrtz_triggers (sched_name, trigger_group, trigger_name);
                CREATE INDEX idx_qrtz_t_next_fire_time ON qrtz_triggers (sched_name, next_fire_time);
                CREATE INDEX idx_qrtz_t_nft_st ON qrtz_triggers (sched_name, trigger_state, next_fire_time);
                CREATE INDEX idx_qrtz_ft_inst_job_req_rcvry
                    ON qrtz_fired_triggers (sched_name, instance_name, requests_recovery);
                CREATE INDEX idx_qrtz_ft_j_g ON qrtz_fired_triggers (sched_name, job_name, job_group);
                CREATE INDEX idx_qrtz_ft_t_g ON qrtz_fired_triggers (sched_name, trigger_name, trigger_group);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DROP TABLE qrtz_fired_triggers;
                DROP TABLE qrtz_paused_trigger_grps;
                DROP TABLE qrtz_scheduler_state;
                DROP TABLE qrtz_locks;
                DROP TABLE qrtz_simprop_triggers;
                DROP TABLE qrtz_simple_triggers;
                DROP TABLE qrtz_cron_triggers;
                DROP TABLE qrtz_blob_triggers;
                DROP TABLE qrtz_triggers;
                DROP TABLE qrtz_job_details;
                DROP TABLE qrtz_calendars;
                """);

            migrationBuilder.DropTable(
                name: "DashboardSnapshots");
        }
    }
}
