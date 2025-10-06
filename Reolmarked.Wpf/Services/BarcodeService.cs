using System.IO;
using QRCoder;

namespace Reolmarked.Services;

public static class BarcodeService
{
    // Gemmer en QR-label som PNG (kan printes fra valgfri billedfremviser)
    public static void SaveQrPng(string content, string filePath, int pixelsPerModule = 6)
    {
        using var generator = new QRCodeGenerator();
        using var data = generator.CreateQrCode(content, QRCodeGenerator.ECCLevel.M);
        using var qr = new PngByteQRCode(data);
        var pngBytes = qr.GetGraphic(pixelsPerModule);
        File.WriteAllBytes(filePath, pngBytes);
    }
}
