using CSharpFunctionalExtensions;
using FileService.Domain.ValueObjects.Enums;
using Shared.SharedKernel.Errors;

namespace FileService.Domain.ValueObjects;

public sealed record ContentType
{
    public string Value { get; private set; }
    public MediaType Category { get; private set; }

    private ContentType(string value, MediaType category)
    {
        Value = value;
        Category = category;
    }

    public static Result<ContentType, Error> Create(string contentType)
    {
        if (string.IsNullOrWhiteSpace(contentType))
            return Error.Validation(
                "empty.content.type",
                "Тип контента не может быть пустым!",
                "ContentType");

        var category = contentType switch
        {
            _ when contentType.Contains("video", StringComparison.InvariantCultureIgnoreCase) => MediaType.Video,
            _ when contentType.Contains("image", StringComparison.InvariantCultureIgnoreCase) => MediaType.Image,
            _ when contentType.Contains("audio", StringComparison.InvariantCultureIgnoreCase) => MediaType.Audio,
            _ => MediaType.Unknown,
        };

        return new ContentType(contentType, category);
    }
}