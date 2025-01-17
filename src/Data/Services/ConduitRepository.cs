﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Hellang.Middleware.ProblemDetails;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Realworlddotnet.Core.Dto;
using Realworlddotnet.Core.Entities;
using Realworlddotnet.Core.Repositories;
using Realworlddotnet.Data.Contexts;

namespace Realworlddotnet.Data.Services;

public class ConduitRepository(ConduitContext context) : IConduitRepository
{
    public void AddUser(User user)
    {
        context.Users.Add(user);
    }

    public async Task<bool> UserExistsAsync(string username)
    {
       return await context.Users.AnyAsync(x => x.Username == username);
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await context.Users.AnyAsync(x => x.Email == email);
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await context.Users.AsNoTracking().FirstOrDefaultAsync(x => x.Email == email);
    }


    public Task<User> GetUserByUsernameAsync(string username, CancellationToken cancellationToken)
    {
        return context.Users.FirstAsync(x => x.Username == username, cancellationToken);
    }

    public async Task<IEnumerable<Tag>> UpsertTagsAsync(IEnumerable<string> tags,
        CancellationToken cancellationToken)
    {
        var dbTags = await context.Tags.Where(x => tags.Contains(x.Id)).ToListAsync(cancellationToken);

        foreach (var tag in tags)
        {
            if (!dbTags.Exists(x => x.Id == tag))
            {
                context.Tags.Add(new Tag(tag));
            }
        }

        return context.Tags;
    }

    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ArticlesResponseDto> GetArticlesAsync(
        ArticlesQueryDto articlesQueryDto,
        string? username,
        bool isFeed,
        CancellationToken cancellationToken)
    {
        var query = context.Articles.Select(x => x);

        if (!string.IsNullOrWhiteSpace(articlesQueryDto.Author))
        {
            query = query.Where(x => x.Author.Username == articlesQueryDto.Author);
        }

        if (!string.IsNullOrWhiteSpace(articlesQueryDto.Tag))
        {
            query = query.Where(x => x.Tags.Any(tag => tag.Id == articlesQueryDto.Tag));
        }

        query = query.Include(x => x.Author);

        if (username is not null)
        {
            query = query.Include(x => x.Author)
                .ThenInclude(x => x.Followers.Where(fu => fu.FollowerUsername == username))
                .AsSplitQuery();
        }

        if (isFeed)
        {
            query = query.Where(x => x.Author.Followers.Count != 0);
        }

        query = query.OrderByDescending(x => x.UpdatedAt);

        var total = await query.CountAsync(cancellationToken);
        var pageQuery = query
            .Skip(articlesQueryDto.Offset).Take(articlesQueryDto.Limit)
            .Include(x => x.Author)
            .Include(x => x.Tags)
            .Include(x => x.ArticleFavorites)
            .AsNoTracking();

        var page = await pageQuery.ToListAsync(cancellationToken);
        foreach (var article in page)
        {
            article.FavoritesCount = article.ArticleFavorites.Count;
        }

        return new ArticlesResponseDto(page, total);
    }

    public async Task<Article?> GetArticleBySlugAsync(string slug, bool asNoTracking,
        CancellationToken cancellationToken)
    {
        var query = context.Articles
            .Include(x => x.Author)
            .Include(x => x.Tags);

        if (asNoTracking)
        {
            query.AsNoTracking();
        }

        var article = await query
            .FirstOrDefaultAsync(x => x.Slug == slug, cancellationToken);

        if (article == null)
        {
            return article;
        }

        var favoriteCount =
            await context.ArticleFavorites.CountAsync(x => x.ArticleId == article.Id, cancellationToken);
        article.Favorited = favoriteCount > 0;
        article.FavoritesCount = favoriteCount;
        return article;
    }

    public void AddArticle(Article article)
    {
        context.Articles.Add(article);
    }

    public void DeleteArticle(Article article)
    {
        context.Articles.Remove(article);
    }

    public async Task<ArticleFavorite?> GetArticleFavoriteAsync(string username, Guid articleId)
    {
        return await context.ArticleFavorites.FirstOrDefaultAsync(x =>
            x.Username == username && x.ArticleId == articleId);
    }

    public void AddArticleFavorite(ArticleFavorite articleFavorite)
    {
        context.ArticleFavorites.Add(articleFavorite);
    }

    public void AddArticleComment(Comment comment)
    {
        context.Comments.Add(comment);
    }

    public void RemoveArticleComment(Comment comment)
    {
        context.Comments.Remove(comment);
    }

    public async Task<List<Comment>> GetCommentsBySlugAsync(string slug, string? username,
        CancellationToken cancellationToken)
    {
        return await context.Comments.Where(x => x.Article.Slug == slug)
            .Include(x => x.Author)
            .ThenInclude(x => x.Followers.Where(fu => fu.FollowerUsername == username))
            .ToListAsync(cancellationToken);
    }

    public void RemoveArticleFavorite(ArticleFavorite articleFavorite)
    {
        context.ArticleFavorites.Remove(articleFavorite);
    }

    public Task<List<Tag>> GetTagsAsync(CancellationToken cancellationToken)
    {
        return context.Tags.AsNoTracking().ToListAsync(cancellationToken);
    }

    public Task<bool> IsFollowingAsync(string username, string followerUsername, CancellationToken cancellationToken)
    {
        return context.FollowedUsers.AnyAsync(
            x => x.Username == username && x.FollowerUsername == followerUsername,
            cancellationToken);
    }

    public void Follow(string username, string followerUsername)
    {
        context.FollowedUsers.Add(new UserLink(username, followerUsername));
    }

    public void UnFollow(string username, string followerUsername)
    {
        context.FollowedUsers.Remove(new UserLink(username, followerUsername));
    }
}
