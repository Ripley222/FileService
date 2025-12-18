using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace FileService.Domain.ValueObjects;

public sealed record MediaOwner
{
    private const int MAX_CONTEXT_LENGTH = 50;

    private static readonly string[] AllowedContexts = ["department", "lesson"];

    public string Context { get; private set; }
    public Guid EntityId { get; private set; }

    private MediaOwner(string context, Guid entityId)
    {
        Context = context.ToLower();
        EntityId = entityId;
    }

    public static Result<MediaOwner, Error> Create(string context, Guid entityId)
    {
        if (string.IsNullOrWhiteSpace(context) || context.Length > MAX_CONTEXT_LENGTH)
            return Error.Validation(
                "context.length",
                $"Допустимая длина контекста не более {MAX_CONTEXT_LENGTH} символов!",
                "MediaOwner");

        if (AllowedContexts.Contains(context) is false)
            return Error.Validation(
                "context.whitelist",
                $"Ожидается один из следующих контекстов: {string.Join(", ", AllowedContexts)}!",
                "MediaOwner");

        if (entityId == Guid.Empty)
            return Error.Validation(
                "entityId.length",
                "ID сущности не должен быть пустым!",
                "MediaOwner");

        return new MediaOwner(context, entityId);
    }

    public static Result<MediaOwner, Error> ForDepartments(Guid entityId)
    {
        const string CONTEXT = "department";

        if (entityId == Guid.Empty)
            return Error.Validation(
                "entityId.length",
                "ID сущности не должен быть пустым!",
                "MediaOwner");

        return new MediaOwner(CONTEXT, entityId);
    }
    
    public static Result<MediaOwner, Error> ForLessons(Guid entityId)
    {
        const string CONTEXT = "lesson";

        if (entityId == Guid.Empty)
            return Error.Validation(
                "entityId.length",
                "ID сущности не должен быть пустым!",
                "MediaOwner");

        return new MediaOwner(CONTEXT, entityId);
    }
}