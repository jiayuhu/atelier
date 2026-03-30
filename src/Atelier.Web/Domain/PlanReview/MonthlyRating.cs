namespace Atelier.Web.Domain.PlanReview;

public enum MonthlyRating
{
    ExceedsExpectations = 1,
    MeetsExpectations = 2,
    NeedsImprovement = 3,
}

public static class MonthlyRatingExtensions
{
    public static string ToStorageValue(this MonthlyRating rating)
    {
        return rating switch
        {
            MonthlyRating.ExceedsExpectations => "Exceeds Expectations",
            MonthlyRating.MeetsExpectations => "Meets Expectations",
            MonthlyRating.NeedsImprovement => "Needs Improvement",
            _ => throw new ArgumentOutOfRangeException(nameof(rating), rating, "Unsupported monthly rating."),
        };
    }
}
