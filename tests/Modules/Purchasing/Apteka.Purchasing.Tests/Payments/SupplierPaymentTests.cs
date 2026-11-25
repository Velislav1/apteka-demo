using Apteka.Purchasing.Domain.Payments;

using Apteka.SharedKernel;

namespace Apteka.Purchasing.Tests.Payments;

public class SupplierPaymentTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 25, 9, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 11, 25);
    private static readonly Guid Old = Guid.CreateVersion7();
    private static readonly Guid Newer = Guid.CreateVersion7();

    private static Result<SupplierPayment> Pay(decimal amount, Guid? first = null) =>
        SupplierPayment.Create(Guid.CreateVersion7(), Today, amount, PaymentChannel.Bank, null, [(Old, 100.00m), (Newer, 50.00m)], first, null, Now);

    [Fact]
    public void A_payment_goes_to_the_oldest_invoice_first_and_the_chosen_one_before_all()
    {
        Assert.Equal([(Old, 100.00m), (Newer, 20.00m)], Pay(120m).Value.Allocations.Select(a => (a.DeliveryId, a.Amount)));
        Assert.Equal([(Newer, 50.00m), (Old, 10.00m)], Pay(60m, first: Newer).Value.Allocations.Select(a => (a.DeliveryId, a.Amount)));
    }

    [Fact]
    public void Zero_and_more_than_owed_are_refused()
    {
        Assert.Equal(PaymentErrors.AmountMustBePositive, Pay(0m).Error);
        Assert.Contains("150.00", Pay(150.01m).Error.Message);
    }
}
