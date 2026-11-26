using Apteka.Identity.Contracts;
using Apteka.Purchasing.Application.Accounting;
using Apteka.Sales.Application.Accounting;
using Apteka.Web.Features.Shared;
using Microsoft.AspNetCore.Authorization;

namespace Apteka.Web.Features.Reports.Accounting;

/// <summary>„Експорт към счетоводство“: the purchase and sales ledgers of a period as CSV files (manager only).</summary>
public static class AccountingExport
{
    public static string PurchasesUrl(DateOnly from, DateOnly to) => $"reports/accounting/purchases.csv?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";

    public static string SalesUrl(DateOnly from, DateOnly to) => $"reports/accounting/sales.csv?from={from:yyyy-MM-dd}&to={to:yyyy-MM-dd}";

    public static byte[] PurchasesCsv(IReadOnlyList<PurchaseLedgerRow> rows) => Csv.Build(
        ["Дата", "Вид документ", "Номер", "Доставчик", "ЕИК", "СР №", "Консигнация", "ДДС %", "Данъчна основа", "ДДС", "Общо"],
        rows.Select(r => (IReadOnlyList<object?>)[r.Date, r.Kind, r.DocumentNo, r.SupplierName, r.SupplierEik, r.DeliveryNumber, r.IsConsignment, r.VatRate, r.TaxBase, r.Vat, r.Total]));

    public static byte[] SalesCsv(IReadOnlyList<SalesLedgerRow> rows) => Csv.Build(
        ["Дата", "Вид", "ДДС %", "Брой документи", "Данъчна основа", "ДДС", "Общо"],
        rows.Select(r => (IReadOnlyList<object?>)[r.Day, r.KindName, r.VatRate, r.Documents, r.TaxBase, r.Vat, r.Total]));

    public static void MapAccountingExport(this WebApplication app)
    {
        var group = app.MapGroup("/reports/accounting").RequireAuthorization(new AuthorizeAttribute { Roles = Roles.Manager });

        group.MapGet("/purchases.csv", async (DateOnly from, DateOnly to, PurchaseLedgerHandler ledger, CancellationToken ct) =>
            Results.File(PurchasesCsv(await ledger.HandleAsync(from, to, ct)), "text/csv; charset=utf-8", $"dnevnik-pokupki-{from:yyyyMMdd}-{to:yyyyMMdd}.csv"));

        group.MapGet("/sales.csv", async (DateOnly from, DateOnly to, SalesLedgerHandler ledger, CancellationToken ct) =>
            Results.File(SalesCsv(await ledger.HandleAsync(from, to, ct)), "text/csv; charset=utf-8", $"dnevnik-prodazhbi-{from:yyyyMMdd}-{to:yyyyMMdd}.csv"));
    }
}
