using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace FileService.Core.FileProviders;

public interface IChunkSizeCalculator
{
    Result<(long ChunkSize, int TotalChunks), Error> ChunksCalculator(
        long fileSize, 
        int recommendedChunkSize, 
        int maxChunks);
}