using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OrdersTMB.Data;

#nullable disable

namespace OrdersTMB.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261008160000_AddLogAuditEntityId")]
public partial class AddLogAuditEntityId : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<Guid>(
            name: "EntityId",
            table: "LogsAudit",
            type: "uuid",
            nullable: true);

        migrationBuilder.CreateIndex(
            name: "IX_LogsAudit_Table_EntityId_CreateDate",
            table: "LogsAudit",
            columns: new[] { "Table", "EntityId", "CreateDate" });

        migrationBuilder.Sql(
            """
            INSERT INTO "LogsAudit"
                ("Id", "UserId", "EntityId", "Action", "Table", "Dado", "CreateDate")
            SELECT
                gen_random_uuid(),
                NULL,
                "Id",
                'OrderHistoryImported',
                'Orders',
                json_build_object(
                    'OrderId', "Id",
                    'PreviousStatus', NULL,
                    'NewStatus', "Status")::text,
                NOW()
            FROM "Orders";
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            DELETE FROM "LogsAudit"
            WHERE "Action" = 'OrderHistoryImported';
            """);

        migrationBuilder.DropIndex(
            name: "IX_LogsAudit_Table_EntityId_CreateDate",
            table: "LogsAudit");

        migrationBuilder.DropColumn(
            name: "EntityId",
            table: "LogsAudit");
    }
}
