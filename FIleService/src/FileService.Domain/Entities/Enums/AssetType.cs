namespace FileService.Domain.Entities.Enums;

public enum AssetType
{
    Video,
    Preview
}

public static class AssetTypeExtensions
{
    public static AssetType ToAssetType(this string value)
    {
        return value switch
        {
            _ when value.Contains("video", StringComparison.InvariantCultureIgnoreCase) => AssetType.Video,
            _ when value.Contains("image", StringComparison.InvariantCultureIgnoreCase) => AssetType.Preview,
            _ => throw new ArgumentException("Неверный AssetType!")
        };
    }
}