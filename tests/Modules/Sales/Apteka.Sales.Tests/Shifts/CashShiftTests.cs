using Apteka.Sales.Domain.Shifts;

namespace Apteka.Sales.Tests.Shifts;

public class CashShiftTests
{
    private static readonly DateTimeOffset Now = new(2026, 11, 26, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Movements_need_a_reason_and_a_closed_shift_no_longer_changes()
    {
        var shift = CashShift.Open(Guid.CreateVersion7(), 50.004m, Now).Value;
        Assert.Equal(50.00m, shift.OpeningCash);

        Assert.Equal(ShiftErrors.ReasonRequired, shift.AddMovement(20m, " ", Now).Error);
        Assert.Equal(ShiftErrors.ZeroAmount, shift.AddMovement(0m, "x", Now).Error);
        Assert.True(shift.AddMovement(20m, "разменни", Now).IsSuccess);
        Assert.True(shift.AddMovement(-35.5m, "плащане на доставчик", Now).IsSuccess);
        Assert.Equal((20m, 35.5m), (shift.CashIn, shift.CashOut));

        Assert.True(shift.Close(100m, 101.20m, "липсва 1,20", Now).IsSuccess);
        Assert.Equal(-1.20m, shift.Difference);
        Assert.False(shift.IsOpen);
        Assert.Equal(ShiftErrors.Closed, shift.AddMovement(1m, "x", Now).Error);
        Assert.Equal(ShiftErrors.Closed, shift.Close(1m, 1m, null, Now).Error);
        Assert.Equal(ShiftErrors.NegativeAmount, CashShift.Open(Guid.CreateVersion7(), -1m, Now).Error);
    }
}
