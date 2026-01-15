using CSharpFunctionalExtensions;
using FileService.Core.FileProviders;
using Shared.SharedKernel.Errors;

namespace FileService.Infrastructure.S3.FIleProviders;

public class ChunkSizeCalculator : IChunkSizeCalculator
{
    public Result<(long ChunkSize, int TotalChunks), Error> ChunksCalculator(
        long fileSize,
        int recommendedChunkSize,
        int maxChunks)
    {
        if (fileSize < recommendedChunkSize)
            return (fileSize, 1);

        int chunkSize = recommendedChunkSize;

        while (true)
        {
            int calculatedChunkSize = (int)Math.Ceiling((double)fileSize / recommendedChunkSize);
            if (calculatedChunkSize <= maxChunks)
                return (chunkSize, calculatedChunkSize);

            chunkSize *= 2;
        }
    }
}