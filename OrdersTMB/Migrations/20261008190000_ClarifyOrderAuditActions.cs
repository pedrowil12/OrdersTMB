using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OrdersTMB.Data;

#nullable disable

namespace OrdersTMB.Migrations;

[DbContext(typeof(AppDbContext))]
[Migration("20261008190000_ClarifyOrderAuditActions")]
public partial class ClarifyOrderAuditActions : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE "LogsAudit" AS log
               SET "UserId" = orders."UserId"
              FROM "Orders" AS orders
             WHERE log."Table" = 'Orders'
               AND log."EntityId" = orders."Id"
               AND log."UserId" IS NULL;

            UPDATE "LogsAudit"
               SET "Action" = CASE "Action"
                   WHEN 'OrderCreated' THEN 'Pedido criado pelo usuário'
                   WHEN 'OrderStatusChanged' THEN 'Status alterado pelo Worker'
                   WHEN 'OrderHistoryImported' THEN 'Histórico importado pela migration'
                   ELSE "Action"
               END
             WHERE "Action" IN ('OrderCreated', 'OrderStatusChanged', 'OrderHistoryImported');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            """
            UPDATE "LogsAudit"
               SET "Action" = CASE "Action"
                   WHEN 'Pedido criado pelo usuário' THEN 'OrderCreated'
                   WHEN 'Status alterado pelo Worker' THEN 'OrderStatusChanged'
                   WHEN 'Histórico importado pela migration' THEN 'OrderHistoryImported'
                   ELSE "Action"
               END
             WHERE "Action" IN (
                 'Pedido criado pelo usuário',
                 'Status alterado pelo Worker',
                 'Histórico importado pela migration');

            UPDATE "LogsAudit"
               SET "UserId" = NULL
             WHERE "Action" IN ('OrderStatusChanged', 'OrderHistoryImported');
            """);
    }
}
