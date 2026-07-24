using Microsoft.Maui.Storage;
using Microsoft.Maui.Media;
using SkiaSharp;
using System;
using System.IO;
using System.Threading.Tasks;

namespace agaverosActividades.Services
{
    public class MediaService : IMediaService
    {
        private const int LadoMaximo = 1600; // px, suficiente para pantalla completa sin gastar memoria
        private const int CalidadJpeg = 85;  // 0-100

        public async Task<string?> TomarFotoAsync()
        {
            if (!MediaPicker.Default.IsCaptureSupported) return null;

            var foto = await MediaPicker.Default.CapturePhotoAsync();
            return foto is null ? null : await GuardarLocalAsync(foto);
        }

        public async Task<string?> ElegirDeGaleriaAsync()
        {
            var foto = await MediaPicker.Default.PickPhotoAsync();
            return foto is null ? null : await GuardarLocalAsync(foto);
        }

        private async Task<string> GuardarLocalAsync(FileResult foto)
        {
            try
            {
                var nombreDestino = $"{Guid.NewGuid():N}.jpg";

                // AppDataDirectory (no CacheDirectory): la imagen se queda "pendiente" hasta que
                // el usuario presiona Guardar, y el SO puede purgar CacheDirectory sin avisar
                // bajo presión de almacenamiento mientras tanto.
                var destino = Path.Combine(FileSystem.AppDataDirectory, nombreDestino);

                using var origen = await foto.OpenReadAsync();
                using var streamMemoria = new MemoryStream();
                await origen.CopyToAsync(streamMemoria);
                streamMemoria.Position = 0;

                // SKCodec.Create + GetPixels respeta el tag EXIF de orientación automáticamente
                // al decodificar (a diferencia de PlatformImage en Android), así que las fotos
                // tomadas en vertical no salen de lado. Además, se decodifica DIRECTO al tamaño
                // reducido (en vez de a resolución completa y luego Resize) para no picar la
                // memoria con fotos de 12-50 MP de cámaras modernas, que en equipos de gama
                // baja/media puede provocar OutOfMemoryException y cerrar la app.
                using var codec = SKCodec.Create(streamMemoria);
                if (codec is null)
                    throw new InvalidOperationException("Formato de imagen no soportado.");

                using var bitmapFinal = DecodificarRedimensionado(codec, LadoMaximo);
                if (bitmapFinal is null)
                    throw new InvalidOperationException("No fue posible decodificar la imagen capturada.");

                using var imagenSkia = SKImage.FromBitmap(bitmapFinal);
                using var datosJpeg = imagenSkia.Encode(SKEncodedImageFormat.Jpeg, CalidadJpeg);
                using var destinoStream = File.Create(destino);
                datosJpeg.SaveTo(destinoStream);

                return destino;
            }
            catch (OutOfMemoryException)
            {
                throw new InvalidOperationException("La imagen es demasiado grande para procesarse en este dispositivo. Intenta con una foto de menor resolución.");
            }
            catch (Exception ex) when (ex is not InvalidOperationException)
            {
                throw new InvalidOperationException($"Error al procesar la imagen: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Decodifica el códec directo al tamaño final reducido (en vez de decodificar a
        /// resolución completa y luego reescalar), para evitar el pico de memoria de la
        /// imagen original que puede causar OutOfMemoryException en equipos con poca RAM.
        /// </summary>
        private static SKBitmap? DecodificarRedimensionado(SKCodec codec, int ladoMaximo)
        {
            var infoOriginal = codec.Info;

            if (infoOriginal.Width <= 0 || infoOriginal.Height <= 0)
                return null;

            float escala = Math.Min(
                1f,
                (float)ladoMaximo / Math.Max(infoOriginal.Width, infoOriginal.Height));

            int anchoDestino = Math.Max(1, (int)(infoOriginal.Width * escala));
            int altoDestino = Math.Max(1, (int)(infoOriginal.Height * escala));

            var infoDestino = new SKImageInfo(anchoDestino, altoDestino, SKColorType.Rgba8888, SKAlphaType.Premul);
            var bitmap = new SKBitmap(infoDestino);

            var resultado = codec.GetPixels(infoDestino, bitmap.GetPixels());

            if (resultado != SKCodecResult.Success && resultado != SKCodecResult.IncompleteInput)
            {
                bitmap.Dispose();
                return null;
            }

            return bitmap;
        }
    }
}