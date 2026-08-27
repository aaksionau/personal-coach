using Coach.Application.Formatters;
using Coach.Domain.Entities;

namespace Coach.Application.Tests;

public class ValuesContextFormatterTests
{
    [Fact]
    public void Format_SaysNotSetUp_WhenProfileIsNull()
    {
        Assert.Contains("not set up yet", ValuesContextFormatter.Format(null));
    }

    [Fact]
    public void Format_IncludesTheProfileContent_WhenPresent()
    {
        var profile = ValuesProfile.Create("Autonomy over status. Time with the kids is non-negotiable.");

        var formatted = ValuesContextFormatter.Format(profile);

        Assert.Contains("Autonomy over status. Time with the kids is non-negotiable.", formatted);
    }
}
