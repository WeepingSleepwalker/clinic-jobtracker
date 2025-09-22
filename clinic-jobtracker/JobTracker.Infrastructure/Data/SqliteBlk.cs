using Microsoft.EntityFrameworkCore;
using System.Text;

namespace JobTracker.Infrastructure.Data;

/// <summary>
/// SQLite has no stored procedures; this helper does set-based bulk UPDATEs
/// using a VALUES CTE inside a single statement/transaction.
/// </summary>
public static class SqliteBulk
{
    /// <summary>
    /// Bulk update invoice statuses in one statement (BLOB GUID storage).
    /// Each row = (id, status). Status example: "Pending" | "Paid" | "Failed".
    /// Uses x'GUIDHEX' literal form, which expects Invoices.Id stored as BLOB (EF Core default when configured for BLOB).
    /// </summary>
    public static Task<int> BulkUpdateInvoicesAsync(
        AppDb db,
        IEnumerable<(Guid id, string status)> rows,
        CancellationToken ct = default)
    {
        var list = rows.ToList();
        if (list.Count == 0) return Task.FromResult(0);

        // Build VALUES tuples: (x'GUIDHEX','Paid'),(...)
        var sb = new StringBuilder();
        for (int i = 0; i < list.Count; i++)
        {
            var (id, status) = list[i];
            var idHex = id.ToString("N");                   // 32 hex chars, no dashes
            var escStatus = status.Replace("'", "''");      // basic escape
            if (i > 0) sb.Append(',');
            sb.Append($"(x'{idHex}','{escStatus}')");
        }

        var sql = $@"
WITH data(id, newStatus) AS (
  VALUES {sb}
)
UPDATE Invoices
SET Status = (SELECT newStatus FROM data WHERE data.id = Invoices.Id),
    PaidAt = CASE (SELECT newStatus FROM data WHERE data.id = Invoices.Id)
               WHEN 'Paid' THEN CURRENT_TIMESTAMP
               ELSE PaidAt END
WHERE Id IN (SELECT id FROM data);";

        return db.Database.ExecuteSqlRawAsync(sql, ct);
    }

    /// <summary>
    /// Alternate version if your Invoices.Id is stored as TEXT (36-char GUID with dashes).
    /// Swap to this if you see mismatches when using x'…' literals.
    /// </summary>
    public static Task<int> BulkUpdateInvoicesTextGuidAsync(
        AppDb db,
        IEnumerable<(Guid id, string status)> rows,
        CancellationToken ct = default)
    {
        var list = rows.ToList();
        if (list.Count == 0) return Task.FromResult(0);

        // Build VALUES tuples: ('5d2f...-...','Paid'),(...)
        var sb = new StringBuilder();
        for (int i = 0; i < list.Count; i++)
        {
            var (id, status) = list[i];
            var escId = id.ToString();                      // with dashes
            var escStatus = status.Replace("'", "''");
            if (i > 0) sb.Append(',');
            sb.Append($"('{escId}','{escStatus}')");
        }

        var sql = $@"
WITH data(id, newStatus) AS (
  VALUES {sb}
)
UPDATE Invoices
SET Status = (SELECT newStatus FROM data WHERE data.id = Invoices.Id),
    PaidAt = CASE (SELECT newStatus FROM data WHERE data.id = Invoices.Id)
               WHEN 'Paid' THEN CURRENT_TIMESTAMP
               ELSE PaidAt END
WHERE Id IN (SELECT id FROM data);";

        return db.Database.ExecuteSqlRawAsync(sql, ct);
    }
}
