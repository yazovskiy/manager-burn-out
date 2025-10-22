using CallWellbeing.Core.Abstractions;
using CallWellbeing.Core.Domain.Entities;
using CallWellbeing.Infra.Persistence;
using Microsoft.EntityFrameworkCore;

namespace CallWellbeing.Infra.Repositories;

internal sealed class LlmAssessmentRepository : ILlmAssessmentRepository
{
  private readonly CallWellbeingDbContext _dbContext;

  public LlmAssessmentRepository(CallWellbeingDbContext dbContext)
  {
    _dbContext = dbContext;
  }

  public async Task UpsertAsync(LlmAssessment assessment, CancellationToken cancellationToken = default)
  {
    ArgumentNullException.ThrowIfNull(assessment);

    var existing = await _dbContext.Assessments
      .FirstOrDefaultAsync(x => x.CallRecordId == assessment.CallRecordId, cancellationToken);

    if (existing is null)
    {
      await _dbContext.Assessments.AddAsync(assessment, cancellationToken);
    }
    else
    {
      existing.Update(assessment.Risk, assessment.Why, assessment.Advice);
    }

    await _dbContext.SaveChangesAsync(cancellationToken);
  }
}
