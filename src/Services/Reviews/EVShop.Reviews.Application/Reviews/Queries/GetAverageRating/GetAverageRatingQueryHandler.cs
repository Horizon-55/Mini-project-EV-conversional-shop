using EVShop.Reviews.Application.Common.Interfaces;
using MediatR;

namespace EVShop.Reviews.Application.Reviews.Queries.GetAverageRating;

public class GetAverageRatingQueryHandler : IRequestHandler<GetAverageRatingQuery, double>
{
    private readonly IReviewQueries _queries;

    public GetAverageRatingQueryHandler(IReviewQueries queries)
    {
        _queries = queries;
    }

    public async Task<double> Handle(GetAverageRatingQuery request, CancellationToken cancellationToken)
    {
        return await _queries.GetAverageRatingAsync(request.ProductId, cancellationToken);
    }
}
