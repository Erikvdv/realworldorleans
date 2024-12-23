using Realworlddotnet.Api.Features.Articles;
using Realworlddotnet.Core.Dto;
using Realworlddotnet.Core.Repositories;
using Comment = Realworlddotnet.Api.Features.Articles.Comment;

namespace Realworlddotnet.Api.Grains;


public interface IArticleGrain : IGrainWithStringKey
{
    Task<ArticleResponse> GetArticle(string? username);
    Task<ArticleResponse> CreateArticleAsync(NewArticleDto newArticle, string username);
    Task<Article> UpdateArticleAsync(ArticleUpdateDto update, string username);
    Task DeleteArticleAsync(string username);
    Task<List<Comment>> GetCommentsAsync(string? username);
    Task<Comment> AddCommentAsync(string username, CommentDto commentDto);
    Task RemoveCommentAsync(int commentId, string username);
}
public sealed class ArticleGrain(IConduitRepository repository) : Grain, IArticleGrain
{
    private Article? _article;
    public async Task<ArticleResponse> GetArticle(string? username)
    {
        var cancellationToken = CancellationToken.None;
        if (_article != null)
        {
            return _article.MapToArticleResponse();
        }

        var article = await repository.GetArticleBySlugAsync(this.GetPrimaryKeyString(), false, cancellationToken) ??
                      throw new KeyNotFoundException("Article not found");

        var comments = await repository.GetCommentsBySlugAsync(
            this.GetPrimaryKeyString(), username, cancellationToken);
        
        article.Comments = comments;
        _article = article;
        return _article.MapToArticleResponse();
    }
    
    public async Task<ArticleResponse> CreateArticleAsync(
        NewArticleDto newArticle, string username)
    {
        var cancellationToken = CancellationToken.None;
        var user = await repository.GetUserByUsernameAsync(username, cancellationToken);
        var tags = await repository.UpsertTagsAsync(newArticle.TagList, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);

        var article = new Article(
                newArticle.Title,
                newArticle.Description,
                newArticle.Body
            ) { Author = user, Tags = tags.ToList() }
            ;

        repository.AddArticle(article);
        await repository.SaveChangesAsync(cancellationToken);
        _article = article;
        var result = article.MapToArticleResponse();
        return result;
    }

    public async Task<Article> UpdateArticleAsync(
        ArticleUpdateDto update, string username)
    {
        var cancellationToken = CancellationToken.None;
        var article = await repository.GetArticleBySlugAsync(this.GetPrimaryKeyString(), false, cancellationToken);

        if (article == null)
        {
            throw new KeyNotFoundException("ArticleNotFound");
        }

        if (username != article.Author.Username)
        {
            throw new UnauthorizedAccessException($"{username} is not the author");
        }

        article.UpdateArticle(update);
        await repository.SaveChangesAsync(cancellationToken);
        _article = article;
        return article;
    }
    
    public async Task DeleteArticleAsync(string username)
    {
        var cancellationToken = CancellationToken.None;
        var slug = this.GetPrimaryKeyString();
        var article = await repository.GetArticleBySlugAsync(slug, false, cancellationToken) ??
                      throw new KeyNotFoundException("Article not found");

        if (username != article.Author.Username)
        {
            throw new UnauthorizedAccessException($"{username} is not the author");
        }

        repository.DeleteArticle(article);
        await repository.SaveChangesAsync(cancellationToken);
        _article = null;
    }

    public async Task RemoveCommentAsync(int commentId, string username)
    {
        var cancellationToken = CancellationToken.None;
        var slug = this.GetPrimaryKeyString();
        var article = await repository.GetArticleBySlugAsync(slug, false, cancellationToken) ??
            throw new KeyNotFoundException("Article not found");

        var comments = await repository.GetCommentsBySlugAsync(slug, username, cancellationToken);
        var comment = comments.Find(x => x.Id == commentId)
                      ?? throw new KeyNotFoundException("Comment not found");;


        if (comment.Author.Username != username)
        {
            throw new UnauthorizedAccessException("User does not own Article");
        }

        comments.Remove(comment);
        await repository.SaveChangesAsync(cancellationToken);
        article.Comments = comments;
        _article = article;
    }
    
    public async Task<Comment> AddCommentAsync(string username, CommentDto commentDto)
    {
        var cancellationToken = CancellationToken.None;
        var slug = this.GetPrimaryKeyString();
        var user = await repository.GetUserByUsernameAsync(username, cancellationToken);
        var article = await repository.GetArticleBySlugAsync(slug, false, cancellationToken) ??
                      throw new KeyNotFoundException("Article not found");

        var comment = new Core.Entities.Comment(commentDto.Body, user.Username, article.Id);
        repository.AddArticleComment(comment);

        await repository.SaveChangesAsync(cancellationToken);
        article.Comments.Add(comment);
        _article = article;
        return comment.MapToCommentModel();
    }
    
    public async Task<List<Comment>> GetCommentsAsync(string? username)
    {
        await GetArticle(username);
        var comments = _article.Comments.Select(x => x.MapToCommentModel());
        return comments.ToList();
    }
}
