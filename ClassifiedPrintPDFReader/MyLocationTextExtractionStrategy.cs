using iTextSharp.text.pdf.parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace ClassifiedPrintPDFReader
{
    //Helper class that stores our rectangle and text
    public class RectAndText
    {
        public iTextSharp.text.Rectangle Rect;
        public String Text;
        public RectAndText(iTextSharp.text.Rectangle rect, String text)
        {
            this.Rect = rect;
            this.Text = text;
        }
    }

    public class MyLocationTextExtractionStrategy : LocationTextExtractionStrategy
    {
        //Hold each coordinate 
        public List<RectAndText> myPoints = new List<RectAndText>();

        //The string that we're searching for 
        public String TextToSearchFor { get; set; }

        //How to compare strings 
        public System.Globalization.CompareOptions CompareOptions { get; set; }

        public MyLocationTextExtractionStrategy(String textToSearchFor, System.Globalization.CompareOptions compareOptions = System.Globalization.CompareOptions.None)
        {
            this.TextToSearchFor = textToSearchFor;
            this.CompareOptions = compareOptions;
        }

        public MyLocationTextExtractionStrategy()
        {
        }

        //Automatically called for each chunk of text in the PDF 
        public override void RenderText(TextRenderInfo renderInfo)
        {
            base.RenderText(renderInfo);

            //See if the current chunk contains the text 
            var startPosition = System.Globalization.CultureInfo.CurrentCulture.CompareInfo.IndexOf(renderInfo.GetText(), this.TextToSearchFor, this.CompareOptions);

            //If not found bail 
            if (startPosition < 0)
            {
                return;
            }

            //Grab the individual characters 
            var chars = renderInfo.GetCharacterRenderInfos().Skip(startPosition).Take(this.TextToSearchFor.Length).ToList();

            //Grab the first and last character 
            var firstChar = chars.First();
            var lastChar = chars.Last();


            //Get the bounding box for the chunk of text 
            var bottomLeft = firstChar.GetDescentLine().GetStartPoint();
            var topRight = lastChar.GetAscentLine().GetEndPoint();

            //Create a rectangle from it 
            var rect = new iTextSharp.text.Rectangle(
                      bottomLeft[Vector.I1],
                      bottomLeft[Vector.I2],
                      topRight[Vector.I1],
                      topRight[Vector.I2]
                      );

            //Add this to our main collection 
            this.myPoints.Add(new RectAndText(rect, this.TextToSearchFor));
        }
    }
}
