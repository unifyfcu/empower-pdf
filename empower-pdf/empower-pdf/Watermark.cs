using Ghostscript.NET;
using Ghostscript.NET.Processor;
using iText.IO.Font;
using iText.IO.Font.Constants;
using iText.Kernel.Colors;
using iText.Kernel.Font;
using iText.Kernel.Geom;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Annot;
using iText.Kernel.Pdf.Canvas;
using iText.Kernel.Pdf.Extgstate;
using iText.Kernel.Pdf.Xobject;
using Serilog;
using System;
using System.Collections.Generic;
using System.IO;

namespace empower_pdf
{
    public interface IWatermarkContract
    {
        void ProcessFiles(string file);
    }

    public class WatermarkService : IWatermarkContract
    {
        private readonly IArguments _operation;
        private const string PathWatermarked = "1_Watermarked";
        private const string PathGreyscale = "2_Greyscale";
        private const string PathFinished = "3_Finished";

        public WatermarkService(IArguments operation)
        {
            _operation = operation ?? throw new ArgumentNullException(nameof(operation));
        }

        public void ProcessFiles(string file)
        {
            var information = new[]
            {
                $"Processing ...............: {file}",
                $"Watermarking .............: {file}",
                $"Removing color ...........: {file}",
                $"Copying file to Finished .: {file}"
            };

            Log.Information(information[0]);
            Log.Information(information[1]);
            WatermarkPdf(file);
            Log.Information(information[2]);
            PdfToGreyScale(file);
            Log.Information(information[3]);
            PdfCompare(file);
        }

        /// <summary>
        /// Watermarks a PDF file.
        /// </summary>
        /// <paramref name="fileName"/>
        private void WatermarkPdf(string fileName)
        {
            var watermarkText = _operation.WatermarkText;
            var sourceFile = $"{_operation.SourcePath}\\{fileName}";
            var destinationPath = $"{_operation.DestinationPath}\\{PathWatermarked}";
            var destinationFile = $"{destinationPath}\\{fileName}";

            if (!Directory.Exists(_operation.SourcePath))
            {
                var m = $"Source directory: '{_operation.SourcePath}', does not exist!";
                Log.Error(m);
                throw new Exception(m);
            }

            if (!Directory.Exists(destinationPath))
                Directory.CreateDirectory(destinationPath);

            const float watermarkTrimmingRectangleWidth = 600;
            const float watermarkTrimmingRectangleHeight = 600;

            const float formWidth = 300;
            const float formHeight = 300;
            const float formXOffset = 0;
            const float formYOffset = 0;

            const float xTranslation = 50;
            const float yTranslation = 25;

            const double rotationInRads = Math.PI / 3;

            try
            {
                FontCache.ClearSavedFonts();
            }
            catch (Exception exception)
            {
                Log.Error(exception.Message);
            }

            var font = PdfFontFactory.CreateFont(StandardFonts.COURIER);
            const float fontSize = 119;

            using var reader = new PdfReader(new MemoryStream(File.ReadAllBytes(sourceFile)));
            using var pdfDoc = new PdfDocument(reader, new PdfWriter(destinationFile));
            var numberOfPages = pdfDoc.GetNumberOfPages();
            PdfPage page = null;

            for (var i = 1; i <= numberOfPages; i++)
            {
                page = pdfDoc.GetPage(i);

                var ps = page.GetPageSize();

                //Center the annotation
                var bottomLeftX = ps.GetWidth() / 2 - watermarkTrimmingRectangleWidth / 2;
                var bottomLeftY = ps.GetHeight() / 2 - watermarkTrimmingRectangleHeight / 2;
                var watermarkTrimmingRectangle = new Rectangle(bottomLeftX, bottomLeftY,
                    watermarkTrimmingRectangleWidth, watermarkTrimmingRectangleHeight);

                var watermark = new PdfWatermarkAnnotation(watermarkTrimmingRectangle);

                //Apply linear algebra rotation math
                //Create identity matrix
                var transform = new AffineTransform(); //No-args constructor creates the identity transform
                //Apply translation
                transform.Translate(xTranslation, yTranslation);
                //Apply rotation
                transform.Rotate(rotationInRads);

                var fixedPrint = new PdfFixedPrint();
                watermark.SetFixedPrint(fixedPrint);
                //Create appearance
                var formRectangle = new Rectangle(formXOffset, formYOffset, formWidth, formHeight);

                //Observation: font XObject will be resized to fit inside the watermark rectangle
                var form = new PdfFormXObject(formRectangle);
                var gs1 = new PdfExtGState().SetFillOpacity(0.6f);
                var canvas = new PdfCanvas(form, pdfDoc);

                var transformValues = new float[6];
                transform.GetMatrix(transformValues);

                canvas.SaveState()
                    .BeginText().SetColor(ColorConstants.GRAY, true).SetExtGState(gs1)
                    .SetTextMatrix(transformValues[0], transformValues[1], transformValues[2], transformValues[3],
                        transformValues[4], transformValues[5])
                    .SetFontAndSize(font, fontSize)
                    .ShowText(watermarkText)
                    .EndText()
                    .RestoreState();

                canvas.Release();

                watermark.SetAppearance(PdfName.N, new PdfAnnotationAppearance(form.GetPdfObject()));
                watermark.SetFlags(PdfAnnotation.PRINT);

                page.AddAnnotation(watermark);
            }

            page?.Flush();
            pdfDoc.Close();
        }

        /// <summary>
        /// Converts a Pdf to greyscale.
        /// </summary>
        /// <paramref name="fileName"/>
        private void PdfToGreyScale(string fileName)
        {
            var sourceFile = $"{_operation.DestinationPath}\\{PathWatermarked}\\{fileName}";
            var destinationPath = $"{_operation.DestinationPath}\\{PathGreyscale}";
            var destinationFile = $"{destinationPath}\\{fileName}";

            if (!File.Exists(sourceFile))
            {
                var m = $"Watermarked file: '{sourceFile}', does not exist!";
                Log.Error(m);
                throw new Exception(m);
            }

            if (!Directory.Exists(destinationPath))
                Directory.CreateDirectory(destinationPath);

            //GhostscriptPipedOutput gsPipedOutput = new GhostscriptPipedOutput();
            using var gsPipedOutput = new GhostscriptPipedOutput();

            string outputPipeHandle = "%handle%" + int.Parse(gsPipedOutput.ClientHandle).ToString("X2");
            using GhostscriptProcessor processor = new GhostscriptProcessor();

            List<string> switches = new List<string>();
            switches.Add("-empty");
            switches.Add("-dQUIET");
            switches.Add("-dSAFER");
            switches.Add("-dBATCH");
            switches.Add("-dNOPAUSE");
            switches.Add("-dNOPROMPT");
            switches.Add("-sProcessColorModel=DeviceGray");
            switches.Add("-sColorConversionStrategy=Gray");
            switches.Add("-dOverrideICC");
            switches.Add("-sDEVICE=pdfwrite");
            switches.Add("-o" + outputPipeHandle);
            switches.Add("-q");
            switches.Add("-f");
            switches.Add(sourceFile);

            try
            {
                processor.StartProcessing(switches.ToArray(), null);

                byte[] rawDocumentData = gsPipedOutput.Data;

                File.WriteAllBytes(destinationFile, rawDocumentData);
            }
            catch (Exception ex)
            {
                Log.Error(ex.Message);
            }
            finally
            {
                gsPipedOutput.Dispose();
            }
        }

        /// <summary>
        /// Compare two Pdf files and copies the smaller one to the destination directory.
        /// </summary>
        /// <paramref name="fileName"/>
        private void PdfCompare(string fileName)
        {
            var source1 = $"{_operation.DestinationPath}\\{PathWatermarked}\\{fileName}";
            var source2 = $"{_operation.DestinationPath}\\{PathGreyscale}\\{fileName}";
            var destinationPath = $"{_operation.DestinationPath}\\{PathFinished}";
            var destination = $"{destinationPath}\\{fileName}";

            bool.TryParse((!File.Exists(source1)).ToString(), out var isSource1);
            bool.TryParse((!File.Exists(source2)).ToString(), out var isSource2);

            if (isSource1 || isSource2)
            {
                var m = "";

                if (isSource1)
                    m = $"Watermarked file: '{source1}', does not exist!";
                if (isSource2)
                    m = $"Greyscale file: '{source2}', does not exist!";

                Log.Error(m);
                throw new Exception(m);
            }

            if (!Directory.Exists(destinationPath))
                Directory.CreateDirectory(destinationPath);

            var attributesSource1 = new FileInfo(source1).Length;
            var attributesSource2 = new FileInfo(source2).Length;
            var source = attributesSource1 < attributesSource2 ? source1 : source2;
            File.Copy(source, destination, true);
        }
    }
}