using EVShop.Reviews.Domain.Exceptions;
using EVShop.Reviews.Domain.Interfaces;
using MediatR;
using MongoDB.Bson;

namespace EVShop.Reviews.Application.Reviews.Commands.DeleteReview;

public class DeleteReviewCommandHandler : IRequestHandler<DeleteReviewCommand>
{
    private readonly IReviewRepository _repository;

    public DeleteReviewCommandHandler(IReviewRepository repository)
    {
        _repository = repository;
    }

    public async Task Handle(DeleteReviewCommand request, CancellationToken cancellationToken)
    {
        var objectId = ObjectId.Parse(request.Id);
        var review = await _repository.GetByIdAsync(objectId, cancellationToken);

        if (review == null)
            throw new NotFoundException("Review", request.Id);

        await _repository.DeleteAsync(objectId, cancellationToken);
    }
}
