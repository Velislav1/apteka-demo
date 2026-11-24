using Apteka.Purchasing.Domain.Deliveries;
using Apteka.SharedKernel;

namespace Apteka.Purchasing.Tests.Deliveries;

public class DeliveryTests
{
    private static readonly DateOnly Today = new(2026, 11, 24);
    private static readonly DateTimeOffset Now = new(2026, 11, 24, 9, 0, 0, TimeSpan.Zero);

    private static DeliveryLineSpec Line(int quantity = 3, int factor = 30, decimal purchase = 4.1234m, decimal sale = 6.49m, string batch = "L001", int expiresInDays = 365, Guid? id = null) =>
        new(id, Guid.CreateVersion7(), Guid.CreateVersion7(), factor, quantity, purchase, 25m, sale, batch, Today.AddDays(expiresInDays));

    private static Delivery Draft(params DeliveryLineSpec[] lines)
    {
        var delivery = Delivery.Create(Guid.CreateVersion7(), " 0000123 ", Today, Now, null).Value;
        Assert.True(delivery.SetLines(lines).IsSuccess);
        return delivery;
    }

    [Fact]
    public void Draft_has_trimmed_invoice_number_and_lines_in_order()
    {
        var delivery = Draft(Line(batch: "A"), Line(batch: "B"));

        Assert.Equal("0000123", delivery.InvoiceNo);
        Assert.Equal(DeliveryStatus.Draft, delivery.Status);
        Assert.Equal(["A", "B"], delivery.Lines.Select(l => l.BatchNo));
        Assert.Equal([1, 2], delivery.Lines.Select(l => l.Position));
    }

    [Fact]
    public void Quantity_is_converted_to_base_units_by_the_factor_and_totals_are_in_cents()
    {
        var delivery = Draft(Line(quantity: 3, factor: 30, purchase: 4.1234m), Line(quantity: 2, factor: 1, purchase: 0.105m));

        Assert.Equal(90, delivery.Lines[0].BaseQuantity);
        Assert.Equal(Money.Of(12.37m), delivery.Lines[0].PurchaseTotal);
        Assert.Equal(Money.Of(0.21m), delivery.Lines[1].PurchaseTotal);
        Assert.Equal(Money.Of(12.58m), delivery.PurchaseTotal);
    }

    [Fact]
    public void Supplier_discount_lowers_the_purchase_price_and_the_total()
    {
        var delivery = Draft(Line(quantity: 3, factor: 30, purchase: 10.00m) with { SupplierDiscountPercent = 12.5m });

        var line = delivery.Lines[0];
        Assert.Equal(10.00m, line.UnitPurchasePrice);
        Assert.Equal(8.7500m, line.NetUnitPurchasePrice);
        Assert.Equal(Money.Of(26.25m), line.PurchaseTotal);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(100.01)]
    public void Supplier_discount_outside_0_to_100_is_rejected(decimal discount)
    {
        var delivery = Delivery.Create(Guid.CreateVersion7(), "1", Today, Now, null).Value;

        var result = delivery.SetLines([Line() with { SupplierDiscountPercent = discount }]);

        Assert.Equal("purchasing.invalid_supplier_discount", result.Error.Code);
    }

    [Fact]
    public void Due_date_is_the_invoice_date_plus_the_payment_term()
    {
        var delivery = Delivery.Create(Guid.CreateVersion7(), "1", Today, Now, null, paymentTermDays: 30).Value;

        Assert.Equal(Today.AddDays(30), delivery.DueDate);
        Assert.Equal(DeliveryErrors.InvalidPaymentTerm, delivery.UpdateHeader(delivery.SupplierId, "1", Today, -1).Error);
        Assert.Equal(DeliveryErrors.InvalidPaymentTerm, Delivery.Create(Guid.CreateVersion7(), "1", Today, Now, null, 366).Error);
    }

    [Theory]
    [InlineData(0, 4, 6, "L1", "purchasing.quantity_positive")]
    [InlineData(1, -1, 6, "L1", "purchasing.invalid_purchase_price")]
    [InlineData(1, 4, 0, "L1", "purchasing.invalid_sale_price")]
    [InlineData(1, 4, 6, " ", "purchasing.batch_no_required")]
    public void Invalid_line_is_rejected_with_its_number(int quantity, decimal purchase, decimal sale, string batch, string code)
    {
        var delivery = Delivery.Create(Guid.CreateVersion7(), "1", Today, Now, null).Value;

        var result = delivery.SetLines([Line(), Line(quantity: quantity, purchase: purchase, sale: sale, batch: batch)]);

        Assert.Equal(code, result.Error.Code);
        Assert.Contains("Ред 2", result.Error.Message);
        Assert.Empty(delivery.Lines);
    }

    [Fact]
    public void Removed_line_is_marked_not_deleted_and_kept_lines_are_updated()
    {
        var delivery = Draft(Line(batch: "A"), Line(batch: "B"));
        var keep = delivery.Lines[1];

        Assert.True(delivery.SetLines([Line(batch: "B2", id: keep.Id), Line(batch: "C")]).IsSuccess);

        Assert.Equal(["B2", "C"], delivery.Lines.Select(l => l.BatchNo));
        Assert.Equal(3, delivery.AllLines.Count);
        Assert.True(delivery.AllLines.Single(l => l.BatchNo == "A").IsRemoved);
    }

    [Fact]
    public void Line_of_another_delivery_is_rejected()
    {
        var delivery = Draft(Line());

        Assert.Equal(DeliveryErrors.UnknownLine, delivery.SetLines([Line(id: Guid.CreateVersion7())]).Error);
    }

    [Fact]
    public void Posting_locks_the_delivery()
    {
        var delivery = Draft(Line());

        Assert.True(delivery.Post(Today, Now).IsSuccess);

        Assert.Equal(DeliveryStatus.Posted, delivery.Status);
        Assert.Equal(Now, delivery.PostedAt);
        Assert.Equal(DeliveryErrors.NotDraft, delivery.SetLines([Line()]).Error);
        Assert.Equal(DeliveryErrors.NotDraft, delivery.UpdateHeader(delivery.SupplierId, "2", Today, 0).Error);
        Assert.Equal(DeliveryErrors.NotDraft, delivery.Post(Today, Now).Error);
    }

    [Fact]
    public void Empty_delivery_cannot_be_posted() =>
        Assert.Equal(DeliveryErrors.NoLines, Draft().Post(Today, Now).Error);

    [Fact]
    public void Expired_line_cannot_be_posted_but_expiring_today_can()
    {
        Assert.Equal("purchasing.expired", Draft(Line(), Line(expiresInDays: -1)).Post(Today, Now).Error.Code);
        Assert.True(Draft(Line(expiresInDays: 0)).Post(Today, Now).IsSuccess);
    }

    [Theory]
    [InlineData(10, 25, 20, 15.00)]
    [InlineData(4.1234, 25, 20, 6.19)]
    [InlineData(3.33, 0, 9, 3.63)]
    [InlineData(1, 10, 0, 1.10)]
    public void Sale_price_from_markup_adds_markup_then_VAT_and_rounds_to_cents(decimal purchase, decimal markup, decimal vat, decimal expected) =>
        Assert.Equal(expected, SalePriceCalculator.FromMarkup(purchase, markup, vat));
}
