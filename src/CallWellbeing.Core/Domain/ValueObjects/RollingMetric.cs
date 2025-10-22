namespace CallWellbeing.Core.Domain.ValueObjects;

public sealed class RollingMetric
{
  public static RollingMetric Empty => new();

  public int Count { get; private set; }

  public double Mean { get; private set; }

  private double _m2;

  public double StandardDeviation => Count > 1 ? Math.Sqrt(_m2 / (Count - 1)) : 0;

  public double VarianceAggregate
  {
    get => _m2;
    private set => _m2 = value;
  }

  public RollingMetric()
  {
  }

  public RollingMetric(int count, double mean, double m2)
  {
    Count = count;
    Mean = mean;
    _m2 = m2;
  }

  public RollingMetric Update(double value)
  {
    var newMetric = new RollingMetric
    {
      Count = Count,
      Mean = Mean,
      VarianceAggregate = VarianceAggregate
    };

    newMetric.Push(value);
    return newMetric;
  }

  public void Push(double value)
  {
    Count++;
    var delta = value - Mean;
    Mean += delta / Count;
    var delta2 = value - Mean;
    _m2 += delta * delta2;
  }

  public bool IsReady => Count > 0;
}
