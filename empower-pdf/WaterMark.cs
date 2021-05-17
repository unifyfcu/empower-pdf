using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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

namespace empower_pdf
{
    public interface IWatermarkService
    {
        void DoThing(int number);
    }

    public class WatermarkService : IWatermarkService
    {
        private readonly IWatermarkService _watermarkService;
        public WatermarkService(IWatermarkService watermarkService)
        {
            _watermarkService = watermarkService;
        }

        public void DoThing(int number)
        {

        }
    }


    public class Watermark
    {
        /// <summary>
        /// Watermarks a given PDF file
        /// </summary>
        /// <param name="sourceFile"></param>
        /// <param name="destinationPath"></param>
        public static void WatermarkPdf(string sourceFile, string destinationPath)
        {
            const float watermarkTrimmingRectangleWidth = 600;
            const float watermarkTrimmingRectangleHeight = 600;

            const float formWidth = 300;
            const float formHeight = 300;
            const float formXOffset = 0;
            const float formYOffset = 0;

            const float xTranslation = 50;
            const float yTranslation = 25;

            const double rotationInRads = Math.PI / 3;

            try { FontCache.ClearSavedFonts(); }
            catch (Exception) { }   // ignored

            var font = PdfFontFactory.CreateFont(StandardFonts.COURIER);
            const float fontSize = 119;

            using var reader = new PdfReader(new MemoryStream(File.ReadAllBytes(sourceFile)));
            // using var file = File.CreateText(destinationPath);
            using var pdfDoc = new PdfDocument(reader, new PdfWriter(destinationPath));
            // using var pdfDoc = new PdfDocument(new PdfReader(sourceFile), new PdfWriter(destinationPath));
            var numberOfPages = pdfDoc.GetNumberOfPages();
            PdfPage page = null;

            for (var i = 1; i <= numberOfPages; i++)
            {
                page = pdfDoc.GetPage(i);
                var ps = page.GetPageSize();

                //Center the annotation
                var bottomLeftX = ps.GetWidth() / 2 - watermarkTrimmingRectangleWidth / 2;
                var bottomLeftY = ps.GetHeight() / 2 - watermarkTrimmingRectangleHeight / 2;
                var watermarkTrimmingRectangle = new Rectangle(bottomLeftX, bottomLeftY, watermarkTrimmingRectangleWidth, watermarkTrimmingRectangleHeight);

                var watermark = new PdfWatermarkAnnotation(watermarkTrimmingRectangle);

                //Apply linear algebra rotation math
                //Create identity matrix
                var transform = new AffineTransform();//No-args constructor creates the identity transform
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
                    .SetTextMatrix(transformValues[0], transformValues[1], transformValues[2], transformValues[3], transformValues[4], transformValues[5])
                    .SetFontAndSize(font, fontSize)
                    .ShowText("COPY")
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

    }
}
