using AutoMapper;
using EVShop.Reviews.Application.DTOs;
using EVShop.Reviews.Domain.Exceptions;
using EVShop.Reviews.Domain.Interfaces;
using EVShop.Reviews.Domain.ValueObjects;
using MediatR;
using MongoDB.Bson;

namespace EVShop.Reviews.Application.Reviews.Commands.AddComment;

public class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, CommentDto>
{
    private readonly IReviewRepository _repository;
    private readonly IMapper _mapper;

    public AddCommentCommandHandler(IReviewRepository repository, IMapper mapper)
    {
        _repository = repository;
        _mapper = mapper;
    }

    public async Task<CommentDto> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        var objectId = ObjectId.Parse(request.ReviewId);
        var review = await _repository.GetByIdAsync(objectId, cancellationToken);

        if (review == null)
            throw new NotFoundException("Review", request.ReviewId);

        var author = new Author(
            request.Author.UserId,
            request.Author.DisplayName,
            request.Author.IsVerifiedBuyer
        );

        review.AddComment(author, request.Text);

        await _repository.UpdateAsync(review, cancellationToken);

        var addedComment = review.Comments.Last();
        return _mapper.Map<CommentDto>(addedComment);
    }
}
