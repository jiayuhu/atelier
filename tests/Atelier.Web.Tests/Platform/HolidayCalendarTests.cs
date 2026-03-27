using Atelier.Web.Application.Platform;
using FluentAssertions;
using Xunit;

namespace Atelier.Web.Tests.Platform;

public sealed class HolidayCalendarTests
{
    [Fact]
    public void EffectiveDeadline_ShiftsToNextWorkingDayWhenHoliday()
    {
        var effectiveDate = HolidayCalendarService.ShiftToNextWorkingDay(
            new DateOnly(2026, 10, 1),
            new[] { new DateOnly(2026, 10, 1), new DateOnly(2026, 10, 2) });

        effectiveDate.Should().Be(new DateOnly(2026, 10, 5));
    }
}
