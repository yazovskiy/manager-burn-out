using System.Security.Cryptography;
using System.Text;
using Bogus;
using CallWellbeing.Core.Domain.Entities;
using CallWellbeing.Core.Domain.Enums;
using CallWellbeing.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CallWellbeing.Infra.Seed;

public static class DataSeeder
{
  private static readonly string[] RiskLevels = ["низкий", "средний", "высокий"];

  public static async Task SeedAsync(CallWellbeingDbContext dbContext, int targetCalls, CancellationToken cancellationToken)
  {
    if (await dbContext.CallRecords.AnyAsync(cancellationToken))
    {
      return;
    }

    var managerIds = Enumerable.Range(1, 5).Select(_ => Guid.NewGuid()).ToArray();
    var faker = new Faker("ru");
    var metricsService = new Core.Domain.Services.MetricsService();

    for (var i = 0; i < targetCalls; i++)
    {
      var managerId = faker.PickRandom(managerIds);
      var startedAt = DateTimeOffset.UtcNow.AddDays(-faker.Random.Int(1, 30)).AddMinutes(-faker.Random.Int(0, 60));
      var externalCallId = Guid.NewGuid().ToString("N");
      var hashedCallId = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(externalCallId)));
      var callRecord = new CallRecord(managerId, hashedCallId, startedAt);

      var segmentCount = faker.Random.Int(6, 16);
      var offset = 0;
      for (var s = 0; s < segmentCount; s++)
      {
        var speaker = faker.Random.Bool(0.4f) ? SpeakerRole.Manager : SpeakerRole.Customer;
        var duration = faker.Random.Int(800, 4_000);
        var text = speaker == SpeakerRole.Manager
          ? faker.Company.CatchPhrase()
          : faker.Hacker.Phrase();

        var segment = new CallSegment(hashedCallId, speaker, offset, offset + duration, text);
        callRecord.AddSegment(segment);
        offset += duration + faker.Random.Int(250, 1_500);
      }

      var metrics = metricsService.Compute(callRecord.Id, managerId, callRecord.Segments.ToList(), startedAt);

      dbContext.CallRecords.Add(callRecord);
      dbContext.Metrics.Add(metrics);

      var risk = faker.Random.Bool(0.3f) ? faker.PickRandom(RiskLevels) : "низкий";
      var llmAssessment = new LlmAssessment(callRecord.Id, managerId, risk, faker.Lorem.Sentence(), faker.Lorem.Sentence());
      dbContext.Assessments.Add(llmAssessment);

      if (risk != "низкий" || metrics.PauseShare > 0.25)
      {
        var alert = new Alert(managerId, callRecord.Id, new[] { "seeded" }, risk, faker.Lorem.Sentence());
        dbContext.Alerts.Add(alert);
      }
    }

    await dbContext.SaveChangesAsync(cancellationToken);
  }
}
