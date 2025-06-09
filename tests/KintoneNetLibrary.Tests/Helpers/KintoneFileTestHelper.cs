using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using ClosedXML.Excel;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

namespace KintoneNetLibrary.Tests.Helpers;

public static class KintoneFileTestHelper {
    public static string ComputeSha256Hash(string data) {
        var encoding = Encoding.GetEncoding("UTF-8");
        return ComputeSha256Hash(encoding.GetBytes(data));
    }
    public static string ComputeSha256Hash(byte[] data) {
        var hashBytes = SHA256.HashData(data);
        return Convert.ToHexStringLower(hashBytes);
    }
    public static string CreateSamplePdfFile() {
        var filePath = "Files/sample.pdf";
        Directory.CreateDirectory("Files");

        if (File.Exists(filePath)) {
            return filePath;
        }

        using var fs = new FileStream(filePath, FileMode.Create, FileAccess.Write);
        using var writer = new StreamWriter(fs, Encoding.ASCII);
        writer.WriteLine("%PDF-1.1");
        writer.WriteLine("1 0 obj << /Type /Catalog /Pages 2 0 R >> endobj");
        writer.WriteLine("2 0 obj << /Type /Pages /Kids [3 0 R] /Count 1 >> endobj");
        writer.WriteLine("3 0 obj << /Type /Page /Parent 2 0 R /MediaBox [0 0 300 144] /Contents 4 0 R >> endobj");
        writer.WriteLine("4 0 obj << /Length 44 >> stream");
        writer.WriteLine("BT /F1 24 Tf 100 100 Td (Hello PDF) Tj ET");
        writer.WriteLine("endstream endobj");
        writer.WriteLine("xref 0 5");
        writer.WriteLine("0000000000 65535 f ");
        writer.WriteLine("0000000010 00000 n ");
        writer.WriteLine("0000000060 00000 n ");
        writer.WriteLine("0000000117 00000 n ");
        writer.WriteLine("0000000220 00000 n ");
        writer.WriteLine("trailer << /Size 5 /Root 1 0 R >>");
        writer.WriteLine("startxref");
        writer.WriteLine("320");
        writer.WriteLine("%%EOF");
        writer.Flush();

        return filePath;
    }
    public static string CreateSampleCsvFile() {
        var filePath = "Files/sample.csv";
        Directory.CreateDirectory("Files");

        if (File.Exists(filePath)) {
            return filePath;
        }

        var lines = new List<string>
        {
        "ID,Name,Price",
        "1,Book A,1200",
        "2,Book B,1500",
        "3,Notebook,800"
    };

        File.WriteAllLines(filePath, lines, Encoding.UTF8);
        return filePath;
    }
    public static string CreateSamplePngFile() {
        var filePath = "Files/sample.png";
        Directory.CreateDirectory("Files");

        if (File.Exists(filePath)) {
            return filePath;
        }

        const int width = 200;
        const int height = 100;
        var backgroundColor = Color.White;
        var textColor = Color.Black;
        var text = "Hello PNG";

        // デフォルトフォントを使用（OSに依存せず動作）
        var fontCollection = new FontCollection();
        var fontFamily = SystemFonts.Families.First(); // 最初に見つかったフォントを使う
        var font = fontFamily.CreateFont(24);

        using var image = new Image<Rgba32>(width, height);
        image.Mutate(ctx => {
            ctx.Fill(backgroundColor);
            ctx.DrawText(text, font, textColor, new PointF(10, 40));
        });

        image.Save(filePath); // 拡張子から PNG として保存される

        return filePath;
    }
    public static string CreateSampleExcelFile() {
        var filePath = "Files/sample.xlsx";
        Directory.CreateDirectory("Files");

        if (File.Exists(filePath)) {
            return filePath;
        }

        using var workbook = new XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sheet1");

        worksheet.Cell(1, 1).Value = "ID";
        worksheet.Cell(1, 2).Value = "Name";
        worksheet.Cell(1, 3).Value = "Price";

        worksheet.Cell(2, 1).Value = 1;
        worksheet.Cell(2, 2).Value = "Book A";
        worksheet.Cell(2, 3).Value = 1200;

        worksheet.Cell(3, 1).Value = 2;
        worksheet.Cell(3, 2).Value = "Book B";
        worksheet.Cell(3, 3).Value = 1500;

        worksheet.Cell(4, 1).Value = 3;
        worksheet.Cell(4, 2).Value = "Notebook";
        worksheet.Cell(4, 3).Value = 800;

        workbook.SaveAs(filePath);

        return filePath;
    }
    // public static string CreateSampleTxtFile() {
    //     var filePath = "Files/test1.txt";
    //     Directory.CreateDirectory("Files");

    //     if (!File.Exists(filePath)) {
    //         File.WriteAllText(filePath, "This is a sample text file for upload/download test.", Encoding.UTF8);
    //     }

    //     return filePath;
    // }
    public static string CreateSampleTxtFile() {
        var filePath = "Files/sample.txt";
        Directory.CreateDirectory("Files");

        if (File.Exists(filePath)) {
            return filePath;
        }

        var content = "This is a sample text file for upload/download test.";
        var utf8WithoutBom = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
        File.WriteAllText(filePath, content, utf8WithoutBom);

        return filePath;
    }

    public static string CreateSampleZipFile() {
        var filePath = "Files/sample.zip";
        Directory.CreateDirectory("Files");

        if (File.Exists(filePath)) {
            return filePath;
        }

        // 一時ディレクトリを作って中にテキストファイルを作成
        var tempDir = Path.Combine("Files", "temp_zip");
        if (Directory.Exists(tempDir)) {
            Directory.Delete(tempDir, true);
        }
        Directory.CreateDirectory(tempDir);

        var textFilePath = Path.Combine(tempDir, "test.txt");
        File.WriteAllText(textFilePath, "This is a test inside the ZIP file.", Encoding.UTF8);

        // ZIPファイル作成
        ZipFile.CreateFromDirectory(tempDir, filePath);

        // 一時ディレクトリ削除
        Directory.Delete(tempDir, true);

        return filePath;
    }

}