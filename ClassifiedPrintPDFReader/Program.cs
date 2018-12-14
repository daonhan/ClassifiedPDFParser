using ClassifiedPrintPDFReader.PdfHelper;
using iTextSharp.text.pdf;
using iTextSharp.text.pdf.parser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using static ClassifiedPrintPDFReader.LocationTextExtractionStrategyEx;
using static ClassifiedPrintPDFReader.PdfHelper.LocationTextExtractionStrategyEx2;

namespace ClassifiedPrintPDFReader
{
    class Program
    {
        const string searchText = @"Baán gêëp àêët nïìn P.15, Q.8, DT 6x20m, 56tr/m2, àêët trong KDC múái, coá GPXD, àiïån nûúác êm, gêìn chúå, trûúâng, coá CQ; Àêët P.11, Q.Bònh Thaånh, gêìn ngaä 4 NVÀ-LQÀ, DT 320m2, trïn àêët coá 4 nhaâ, GTHL, 65tr/m2. Tel. 0989960039 A.Têm";

        public static string SearchText => searchText;
        //static const Regex.Replace(s, @"\t|\n|\r", "");
        static void Main(string[] args)
        {
            string fileName = @"D:\mobi.pdf";
            //Open PDF document
            using (PdfReader reader = new PdfReader(fileName))
            {
                var sb = new TextRenderEx();
                //var parser = new PdfReaderContentParser(reader);
                for(int page = 1; page <= reader.NumberOfPages; page++)
                {
                    var size = reader.GetCropBox(page);
                    Console.WriteLine(size.Width);
                    Console.WriteLine(size.Height);

                    PdfDictionary pdfDictionary = reader.GetPageN(page);
                    IRenderListener listener = new SBTextRenderer(sb);
                    PdfContentStreamProcessor processor = new PdfContentStreamProcessor(listener);
                    PdfDictionary pageDic = reader.GetPageN(page);
                    PdfDictionary resourcesDic = pageDic.GetAsDict(PdfName.RESOURCES);
                    processor.ProcessContent(ContentByteUtils.GetContentBytesForPage(reader, page), resourcesDic);

                    //Create an instance of our strategy
                    //var t2 = new MyLocationTextExtractionStrategy(searchText, System.Globalization.CompareOptions.IgnoreCase);
                    //var ex = PdfTextExtractor.GetTextFromPage(reader, page, t2);
                    ////Loop through each chunk found
                    //foreach (var p in t2.myPoints)
                    //{
                    //    Console.WriteLine(string.Format("Found text {0} at {1}x{2}", p.Text, p.Rect.Left, p.Rect.Bottom));
                    //}
                    //var strategy = parser.ProcessContent(i, new LocationTextExtractionStrategyEx());

                    //var res = strategy.GetLocations();
                    var its = new LocationTextExtractionStrategyEx2();
                    String s = PdfTextExtractor.GetTextFromPage(reader, page, its);
                    var result = new StringBuilder();
                   
                    foreach (var t in its.Columbs.Values){
                        string rs = t.ToString();
                        Console.WriteLine(rs);
                    }

                    string str = result.ToString();
                    //Console.Write(str);                   
                    //if (!string.IsNullOrWhiteSpace(str) && (str.IndexOf(SearchText) != -1)||searchText.IndexOf(str) != -1) {
                    //    Console.Write(str);
                    //}

                    //Console.WriteLine(pageResult.ToString());

                    // System.Diagnostics.Debug.WriteLine(s);
                    //var its2 = new LocationTextExtractionStrategyEx(searchText, page);
                    //String ss = PdfTextExtractor.GetTextFromPage(reader, page, its2);
                    //for (int i1 = 0; i1 < its2.m_SearchResultsList.Count; i1++)
                    //{
                    //    SearchResult t = its2.m_SearchResultsList[i1];
                    //    Console.WriteLine(string.Format("text:{2}; x:{0},y:{1}", t.iPosX, t.iPosY, t.Text));
                    //    Console.WriteLine(string.Format("topleft: x:{0},y:{1}", t.TopLeft[Vector.I1], t.TopLeft[Vector.I2]));
                    //}
                    var bbb = sb.sb.ToString();
                    var asdf = "";
                }
            }

        }

    }

    public class TextRenderEx
    {
        public StringBuilder sb = new StringBuilder();

        private List<TextChunk> m_locationResult = new List<TextChunk>();

        //public List<TextChunk> GetResult()
        //{
        //    return m_locationResult;
        //}
    }
    public class SBTextRenderer : IRenderListener
    {

        private TextRenderEx obj;
        public SBTextRenderer(TextRenderEx builder)
        {
            obj = builder;
        }
        #region IRenderListener Members

        public void BeginTextBlock()
        {
        }

        public void EndTextBlock()
        {
        }

        public void RenderImage(ImageRenderInfo renderInfo)
        {
        }

        public void RenderText(TextRenderInfo renderInfo)
        {
            obj.sb.AppendLine(renderInfo.GetText());
            //LineSegment segment = renderInfo.GetBaseline();
            //TextChunk location = new TextChunk(renderInfo.GetText(), segment.GetStartPoint(), segment.GetEndPoint(), renderInfo.GetSingleSpaceWidth(), renderInfo.GetAscentLine(), renderInfo.GetDescentLine());
            //obj.GetResult().Add(location);
        }

        #endregion
    }
}
