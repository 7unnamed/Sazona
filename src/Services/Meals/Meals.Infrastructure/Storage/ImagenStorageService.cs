using Microsoft.AspNetCore.Http;

namespace Meals.Infrastructure.Storage;

public class ImagenStorageService
{
    private static readonly HashSet<string> TiposPermitidos = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp"
    };

    private const long TamanoMaximoBytes = 5 * 1024 * 1024;

    private readonly string _wwwRootPath;

    public ImagenStorageService(string wwwRootPath)
    {
        _wwwRootPath = wwwRootPath;
    }

    public async Task<string> GuardarAsync(IFormFile archivo, string subcarpeta, CancellationToken cancellationToken = default)
    {
        if (archivo.Length == 0)
        {
            throw new InvalidOperationException("El archivo está vacío.");
        }

        if (archivo.Length > TamanoMaximoBytes)
        {
            throw new InvalidOperationException("La imagen no puede superar los 5 MB.");
        }

        if (!TiposPermitidos.Contains(archivo.ContentType))
        {
            throw new InvalidOperationException("Solo se permiten imágenes JPEG, PNG o WebP.");
        }

        var carpetaDestino = Path.Combine(_wwwRootPath, "uploads", subcarpeta);
        Directory.CreateDirectory(carpetaDestino);

        var extension = Path.GetExtension(archivo.FileName);
        var nombreArchivo = $"{Guid.NewGuid():N}{extension}";
        var rutaCompleta = Path.Combine(carpetaDestino, nombreArchivo);

        await using (var stream = new FileStream(rutaCompleta, FileMode.Create))
        {
            await archivo.CopyToAsync(stream, cancellationToken);
        }

        return $"/uploads/{subcarpeta}/{nombreArchivo}";
    }
}
