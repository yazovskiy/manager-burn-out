using CallWellbeing.Core.Domain.Entities;
using CallWellbeing.Core.Domain.Enums;
using CallWellbeing.Core.Domain.Services;
using FluentAssertions;

namespace CallWellbeing.Core.Tests;

public sealed class MetricsServiceTests
{
  private readonly MetricsService _sut = new();

  [Fact]
  public void Compute_ShouldCalculateTalkSharesAndDuration()
  {
    var callId = Guid.NewGuid();
    var managerId = Guid.NewGuid();
    var hashedCallId = Guid.NewGuid().ToString("N");
    var segments = new List<CallSegment>
    {
      new(hashedCallId, SpeakerRole.Manager, 0, 3_000, "Здравствуйте, коллеги?"),
      new(hashedCallId, SpeakerRole.Customer, 3_500, 7_000, "Добрый день"),
      new(hashedCallId, SpeakerRole.Manager, 7_200, 10_200, "Как идут дела?")
    };

    var metrics = _sut.Compute(callId, managerId, segments, DateTimeOffset.UtcNow);

    metrics.DurationMinutes.Should().BeApproximately(10_200 / 60_000d, 0.001);
    metrics.ManagerTalkShare.Should().BeApproximately(6_000d / 10_500, 0.01);
    metrics.PauseShare.Should().BeGreaterThan(0.04);
    metrics.ManagerActionCount.Should().Be(2);
    metrics.CustomerActionCount.Should().Be(1);
    metrics.UnansweredShare.Should().BeGreaterThan(0);
    metrics.SilentPeriodCount.Should().Be(0);
  }

  [Fact]
  public void Compute_ShouldHandleEmptySegments()
  {
    var metrics = _sut.Compute(Guid.NewGuid(), Guid.NewGuid(), Array.Empty<CallSegment>(), DateTimeOffset.UtcNow);

    metrics.DurationMinutes.Should().Be(0);
    metrics.ManagerTalkShare.Should().Be(0);
    metrics.PauseShare.Should().Be(0);
    metrics.ManagerActionCount.Should().Be(0);
    metrics.CustomerActionCount.Should().Be(0);
  }

  [Fact]
  public void Compute_ShouldDetectSilentPeriods()
  {
    var hashedCallId = Guid.NewGuid().ToString("N");
    var segments = new List<CallSegment>
    {
      new(hashedCallId, SpeakerRole.Manager, 0, 2_000, "Начнем"),
      new(hashedCallId, SpeakerRole.Customer, 5_500, 7_500, "Готов"),
      new(hashedCallId, SpeakerRole.Manager, 12_800, 14_000, "Поехали")
    };

    var metrics = _sut.Compute(Guid.NewGuid(), Guid.NewGuid(), segments, DateTimeOffset.UtcNow);

    metrics.SilentPeriodCount.Should().Be(2);
    metrics.PauseShare.Should().BeGreaterThan(0.4);
  }
}
