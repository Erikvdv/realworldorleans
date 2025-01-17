﻿using CommentEntity = Realworlddotnet.Core.Entities.Comment;
using CommentModel = Realworlddotnet.Api.Features.Articles.Comment;

namespace Realworlddotnet.Api.Features.Articles;

public static class CommentMapper
{
    public static CommentModel MapToCommentModel(this CommentEntity commentEntity)
    {
        var author = new Author(
            commentEntity.Author.Username,
            commentEntity.Author.Image,
            commentEntity.Author.Bio,
            commentEntity.Author.Followers.Count != 0);
        return new CommentModel(commentEntity.Id,
            commentEntity.CreatedAt,
            commentEntity.UpdatedAt,
            commentEntity.Body,
            author);
    }
}
