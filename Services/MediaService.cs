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
        /// Decodifica el códec directo al tamaño final reducido cuando el códec lo soporta
        /// (en vez de decodificar a resolución completa y luego reescalar), para evitar el pico
        /// de memoria de la imagen original que puede causar OutOfMemoryException en equipos con
        /// poca RAM.
        ///
        /// FIX: no todos los códecs (PNG, HEIC/HEIF de iPhone, algunos WEBP) soportan decodificar
        /// a CUALQUIER tamaño reducido -- solo a los tamaños "nativos" de escalado que el propio
        /// códec reporta en GetScaledDimensions(). Antes se le pedía directo el tamaño calculado
        /// por regla de tres (ancho/alto * escala), y si el códec no soportaba ese tamaño exacto,
        /// GetPixels regresaba un resultado distinto de Success/IncompleteInput -> se interpretaba
        /// como "no se pudo decodificar" y tronaba la app (ver DecodificarATamanoSoportado).
        /// Ahora se pide el tamaño que el códec sí soporta, y si ni así funciona, se cae a
        /// decodificar a resolución completa; el ajuste al tamaño final exacto se hace después
        /// con Resize sobre el bitmap ya chico (barato en memoria).
        /// </summary>
        private static SKBitmap? DecodificarRedimensionado(SKCodec codec, int ladoMaximo)
        {
            var infoOriginal = codec.Info;

            if (infoOriginal.Width <= 0 || infoOriginal.Height <= 0)
                return null;

            float escala = Math.Min(
                1f,
                (float)ladoMaximo / Math.Max(infoOriginal.Width, infoOriginal.Height));

            int anchoDeseado = Math.Max(1, (int)(infoOriginal.Width * escala));
            int altoDeseado = Math.Max(1, (int)(infoOriginal.Height * escala));

            using var bitmapDecodificado = DecodificarATamanoSoportado(codec, escala, infoOriginal);
            if (bitmapDecodificado is null)
                return null;

            // Si el códec ya entregó justo el tamaño deseado (caso común: JPEG con escalado
            // nativo), no hace falta reescalar de nuevo.
            if (bitmapDecodificado.Width == anchoDeseado && bitmapDecodificado.Height == altoDeseado)
                return bitmapDecodificado.Copy();

            var infoFinal = new SKImageInfo(anchoDeseado, altoDeseado, SKColorType.Rgba8888, SKAlphaType.Premul);
            var bitmapFinal = new SKBitmap(infoFinal);

            var opcionesMuestreo = new SKSamplingOptions(SKFilterMode.Linear, SKMipmapMode.None);

            if (!bitmapDecodificado.ScalePixels(bitmapFinal, opcionesMuestreo))
            {
                bitmapFinal.Dispose();
                // Fallback final: si el reescalado in-memory fallara por alguna razón, se
                // regresa el bitmap ya decodificado (tamaño soportado por el códec) tal cual,
                // en vez de perder la foto por completo.
                return bitmapDecodificado.Copy();
            }

            return bitmapFinal;
        }

        /// <summary>
        /// Intenta decodificar directo al tamaño "nativo" que el códec reporta como soportado
        /// para la escala pedida (GetScaledDimensions). Si eso falla (formato sin escalado nativo,
        /// o el tamaño reportado tampoco es aceptado por GetPixels en este dispositivo/versión),
        /// cae a decodificar a resolución completa como último recurso antes de dar por perdida
        /// la imagen.
        /// </summary>
        private static SKBitmap? DecodificarATamanoSoportado(SKCodec codec, float escala, SKImageInfo infoOriginal)
        {
            SKSizeI tamanoSoportado;
            try
            {
                tamanoSoportado = codec.GetScaledDimensions(escala);
            }
            catch (Exception)
            {
                tamanoSoportado = new SKSizeI(infoOriginal.Width, infoOriginal.Height);
            }

            var bitmap = IntentarDecodificar(codec, tamanoSoportado.Width, tamanoSoportado.Height);
            if (bitmap != null)
                return bitmap;

            // Ni el tamaño sugerido por el propio códec funcionó -> se intenta a resolución
            // completa. Si esto también falla, sí es un formato realmente no decodificable.
            if (tamanoSoportado.Width != infoOriginal.Width || tamanoSoportado.Height != infoOriginal.Height)
                return IntentarDecodificar(codec, infoOriginal.Width, infoOriginal.Height);

            return null;
        }

        private static SKBitmap? IntentarDecodificar(SKCodec codec, int ancho, int alto)
        {
            if (ancho <= 0 || alto <= 0) return null;

            var info = new SKImageInfo(ancho, alto, SKColorType.Rgba8888, SKAlphaType.Premul);
            var bitmap = new SKBitmap(info);
            var resultado = codec.GetPixels(info, bitmap.GetPixels());

            if (resultado == SKCodecResult.Success || resultado == SKCodecResult.IncompleteInput)
                return bitmap;

            bitmap.Dispose();
            return null;
        }
    }
}