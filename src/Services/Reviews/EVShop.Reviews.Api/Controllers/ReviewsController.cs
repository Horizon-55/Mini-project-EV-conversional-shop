using EVShop.Reviews.Application.Common.Models;
using EVShop.Reviews.Application.DTOs;
using EVShop.Reviews.Application.Reviews.Commands.AddComment;
using EVShop.Reviews.Application.Reviews.Commands.CreateReview;
using EVShop.Reviews.Application.Reviews.Commands.DeleteReview;
using EVShop.Reviews.Application.Reviews.Commands.UpdateReview;
using EVShop.Reviews.Application.Reviews.Queries.GetAverageRating;
using EVShop.Reviews.Application.Reviews.Queries.GetReviewById;
using EVShop.Reviews.Application.Reviews.Queries.GetReviewsList;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace EVShop.Reviews.Api.Controllers;

[ApiController]
[Route("api/reviews")]
[Produces("application/json")]
public class ReviewsController : ControllerBase
{
    private readonly ISender _sender;

    public ReviewsController(ISender sender)
    {
        _sender = sender;
    }

    /// <summary>
    /// Retrieves a review by its MongoDB ObjectId.
    /// </summary>
    [HttpGet("{id}")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ReviewDto>> GetById(string id, CancellationToken cancellationToken)
    {
        var review = await _sender.Send(new GetReviewByIdQuery(id), cancellationToken);
        Response.Headers.ETag = $"\"{review.Version}\"";
        return Ok(review);
    }

    /// <summary>
    /// Retrieves a paginated list of reviews with optional filtering by product, rating, or text search.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(PagedResult<ReviewDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<PagedResult<ReviewDto>>> GetList(
        [FromQuery] long? productId,
        [FromQuery] int? rating,
        [FromQuery] string? searchTerm,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken cancellationToken = default)
    {
        var result = await _sender.Send(
            new GetReviewsListQuery(productId, rating, searchTerm, page, pageSize),
            cancellationToken
        );
        return Ok(result);
    }

    /// <summary>
    /// Computes and returns the average rating for a given product.
    /// </summary>
    [HttpGet("product/{productId:long}/average-rating")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    public async Task<ActionResult> GetAverageRating(long productId, CancellationToken cancellationToken)
    {
        var averageRating = await _sender.Send(new GetAverageRatingQuery(productId), cancellationToken);
        return Ok(new { productId, averageRating });
    }

    /// <summary>
    /// Creates a new review for an EV product.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReviewDto>> Create(
        [FromBody] CreateReviewDto dto,
        CancellationToken cancellationToken)
    {
        var command = new CreateReviewCommand(
            dto.ProductId,
            dto.ProductName,
            dto.Author,
            dto.Rating,
            dto.Text,
            dto.DiscussionId
        );

        var result = await _sender.Send(command, cancellationToken);
        Response.Headers.ETag = $"\"{result.Version}\"";

        return CreatedAtAction(nameof(GetById), new { id = result.Id }, result);
    }

    /// <summary>
    /// Updates an existing review rating and text with optimistic concurrency check.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(typeof(ReviewDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    public async Task<ActionResult<ReviewDto>> Update(
        string id,
        [FromBody] UpdateReviewDto dto,
        CancellationToken cancellationToken)
    {
        var command = new UpdateReviewCommand(id, dto.Rating, dto.Text, dto.ExpectedVersion);
        var result = await _sender.Send(command, cancellationToken);
        Response.Headers.ETag = $"\"{result.Version}\"";

        return Ok(result);
    }

    /// <summary>
    /// Adds a nested comment to an existing review.
    /// </summary>
    [HttpPost("{id}/comments")]
    [ProducesResponseType(typeof(CommentDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CommentDto>> AddComment(
        string id,
        [FromBody] AddCommentDto dto,
        CancellationToken cancellationToken)
    {
        var command = new AddCommentCommand(id, dto.Author, dto.Text);
        var result = await _sender.Send(command, cancellationToken);

        return CreatedAtAction(nameof(GetById), new { id }, result);
    }

    /// <summary>
    /// Deletes a review by its MongoDB ObjectId.
    /// </summary>
    [HttpDelete("{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(string id, CancellationToken cancellationToken)
    {
        await _sender.Send(new DeleteReviewCommand(id), cancellationToken);
        return NoContent();
    }
}
