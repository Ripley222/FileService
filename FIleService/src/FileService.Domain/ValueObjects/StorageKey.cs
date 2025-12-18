using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace FileService.Domain.ValueObjects;

public sealed record StorageKey
{
    public string Key { get; private set; }
    public string? Prefix { get; private set; }
    public string Bucket { get; private set; }
    public string Value { get; private set; }
    public string FullPath { get; private set; }

    private StorageKey(string key, string? prefix, string bucket, string value, string fullPath)
    {
        Key = key;
        Prefix = prefix;
        Bucket = bucket;
        Value = value;
        FullPath = fullPath;
    }

    public static Result<StorageKey, Error> Create(string bucket, string? prefix, string key)
    {
        if (string.IsNullOrWhiteSpace(bucket))
            return Error.Validation(
                "empty.bucket",
                "Необходимо указать корзину!");

        if (string.IsNullOrWhiteSpace(key))
            return Error.Validation(
                "empty.key",
                "Необходимо указать имя файла или конечного сегмента!");

        var value = string.IsNullOrWhiteSpace(prefix)
            ? NormalizePath(key)
            : NormalizePath(prefix) + "/" + NormalizePath(key);

        var fullPath = NormalizePath(bucket) + "/" + value;

        return new StorageKey(key, prefix, bucket, value, fullPath);
    }

    public Result<StorageKey, Error> AppendSegment(string segment)
    {
        if (string.IsNullOrWhiteSpace(segment))
            return Error.Validation(
                "empty.segment",
                "Необходимо указать имя конечного сегмента!");

        var normalizedSegment = NormalizePath(segment);

        var value = Value + "/" + normalizedSegment;
        var fullPath = FullPath + "/" + normalizedSegment;

        return new StorageKey(Key, Prefix, Bucket, value, fullPath);
    }

    private static string NormalizePath(string value)
    {
        var normalizedValue = value
            .Replace(" ", "")
            .Replace("\\", "/")
            .TrimStart(['\\', '/'])
            .TrimEnd(['\\', '/']);

        return normalizedValue;
    }
}