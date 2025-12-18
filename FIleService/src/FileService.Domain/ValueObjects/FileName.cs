using CSharpFunctionalExtensions;
using Shared.SharedKernel.Errors;

namespace FileService.Domain.ValueObjects;

public sealed record FileName
{
    public string Name { get; private set; }
    public string Extension { get; private set; }

    private FileName(string name, string extension)
    {
        Name = name;
        Extension = extension;
    }

    public static Result<FileName, Error> Create(string fileName)
    {
        var name = Path.GetFileNameWithoutExtension(fileName);
        var extension = Path.GetExtension(fileName).ToLower();
        
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation(
                "empty.file.name", 
                "Имя файла не может быть пустым!", 
                "FileName");
        
        if (string.IsNullOrWhiteSpace(extension))
            return Error.Validation(
                "empty.file.extension", 
                "Расширение файла не может быть пустым!", 
                "FileName");

        return new FileName(name, extension);
    }
};