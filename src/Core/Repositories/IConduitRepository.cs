﻿using Realworlddotnet.Core.Dto;
using Realworlddotnet.Core.Entities;

namespace Realworlddotnet.Core.Repositories;

public interface IConduitRepository
{
    public void AddUser(User user);
    
    public Task<bool> UserExistsAsync(string username);
    
    public Task<bool> EmailExistsAsync(string email);

    public Task<User?> GetUserByEmailAsync(string email);

    public Task<User> GetUserByUsernameAsync(string username, CancellationToken cancellationToken);

    public Task<IEnumerable<Tag>> UpsertTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken);

    public Task<ArticlesResponseDto> GetArticlesAsync(ArticlesQueryDto articlesQueryDto, string? username, bool isFeed,
        CancellationToken cancellationToken);

    public Task<Article?> GetArticleBySlugAsync(string slug, bool asNoTracking, CancellationToken cancellationToken);

    public void AddArticle(Article article);

    public void DeleteArticle(Article article);
    public void AddArticleComment(Comment comment);
    public void RemoveArticleComment(Comment comment);

    public Task<List<Comment>>
        GetCommentsBySlugAsync(string slug, string? username, CancellationToken cancellationToken);

    public Task<ArticleFavorite?> GetArticleFavoriteAsync(string username, Guid articleId);

    public void AddArticleFavorite(ArticleFavorite articleFavorite);

    public void RemoveArticleFavorite(ArticleFavorite articleFavorite);

    public Task<List<Tag>> GetTagsAsync(CancellationToken cancellationToken);

    public Task<bool> IsFollowingAsync(string username, string followerUsername, CancellationToken cancellationToken);

    public void Follow(string username, string followerUsername);

    public void UnFollow(string username, string followerUsername);
}
