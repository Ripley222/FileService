using Shared.SharedKernel.Errors;

namespace FileService.Domain.Shared;

public static class FileErrors
{
    public static Error BucketNotFound()
    {
        return Error.NotFound("no.such.bucket", "Бакет не найден!");
    }
    
    public static Error ObjectNotFound()
    {
        return Error.NotFound("no.such.object", "Объект не найден!");
    }
    
    public static Error InvalidObject()
    {
        return Error.Conflict("invalid.object.state", "Неверное состояние объекта!");
    }
    
    public static Error AccessDenied()
    {
        return Error.Conflict("access.denied", "Доступ запрещён!");
    }
    
    public static Error Unknown()
    {
        return Error.Failure("unknown.error", "Неизвестаня ошибка!");
    }
}