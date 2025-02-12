using System;
using System.IO;
using System.Collections.Generic;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using iText.Kernel.Colors;
using iText.Kernel.Pdf.Canvas.Parser.Data;

class Program
{
    static void Main(string[] args)
    {
        string? pdfPath = GetFilePathFromArgs(args);

        if (string.IsNullOrWhiteSpace(pdfPath) || !File.Exists(pdfPath))
        {
            Console.WriteLine("File does not exist.");
            return;
        }

        bool hasHighKBlack = CheckPdfForHighKBlack(pdfPath);
        Console.WriteLine(hasHighKBlack ? "True" : "False");
    }

    static string? GetFilePathFromArgs(string[] args)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (args[i] == "--file" && i + 1 < args.Length)
            {
                return args[i + 1];
            }
        }
        Console.WriteLine("Usage: dotnet run --file \"myfile.pdf\"");
        return null;
    }

    static bool CheckPdfForHighKBlack(string pdfPath)
    {
        using (PdfReader reader = new PdfReader(pdfPath))
        using (PdfDocument pdfDoc = new PdfDocument(reader))
        {
            for (int pageNum = 1; pageNum <= pdfDoc.GetNumberOfPages(); pageNum++)
            {
                var strategy = new HighKBlackDetectionStrategy();
                PdfCanvasProcessor processor = new PdfCanvasProcessor(strategy);
                processor.ProcessPageContent(pdfDoc.GetPage(pageNum));

                if (strategy.FoundHighKBlack)
                {
                    return true; // Stop checking if at least one case is found
                }
            }
        }
        return false;
    }
}

public class HighKBlackDetectionStrategy : IEventListener
{
    public bool FoundHighKBlack { get; private set; } = false;

    public void EventOccurred(IEventData data, EventType type)
    {
        if (type == EventType.RENDER_PATH)
        {
            PathRenderInfo pathInfo = (PathRenderInfo)data;

            // Extract fill and stroke colors
            Color? fillColor = pathInfo.GetFillColor();
            Color? strokeColor = pathInfo.GetStrokeColor();

            // Check if the color is CMYK and has K > 95%
            if (fillColor is DeviceCmyk cmykFill && CheckBlackPercentage(cmykFill))
                FoundHighKBlack = true;

            if (strokeColor is DeviceCmyk cmykStroke && CheckBlackPercentage(cmykStroke))
                FoundHighKBlack = true;
        }
    }

    private bool CheckBlackPercentage(DeviceCmyk color)
    {
        float[] cmykValues = color.GetColorValue(); // Get full CMYK array
        float k = cmykValues[3]; // Extract K (black) component
        return k > 0.95f; // True if K > 95%
    }

    public ICollection<EventType> GetSupportedEvents()
    {
        return new HashSet<EventType> { EventType.RENDER_PATH };
    }
}
