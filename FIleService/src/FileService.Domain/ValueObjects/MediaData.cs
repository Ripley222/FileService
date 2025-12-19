using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace FileService.Domain.ValueObjects;

public sealed record MediaData
{
    public FileName FileName { get; private set; } = null!;
    public ContentType ContentType { get; private set; } = null!;
    public long Size { get; private set; }
    public int ExpectedChunksCount { get; private set; }

    public MediaData(FileName fileName, ContentType contentType, long size, int expectedChunksCount)
    {
        FileName = fileName;
        ContentType = contentType;
        Size = size;
        ExpectedChunksCount = expectedChunksCount;
    }

    public static Result<MediaData, Error> Create(
        FileName fileName, ContentType contentType, long size, int expectedChunksCount)
    {
        if (size <= 0)
            return Error.Validation(
                "size.empty",
                "Размер файла должен быть больше нуля!",
                "MediaData");
        
        if (expectedChunksCount <= 0)
            return Error.Validation(
                "chunks.empty",
                "Количество чанков должно быть больше нуля!",
                "MediaData");

        return new MediaData(fileName, contentType, size, expectedChunksCount);
    }
}