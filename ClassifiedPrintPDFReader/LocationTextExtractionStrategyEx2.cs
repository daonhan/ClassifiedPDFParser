using System;
using System.Collections.Generic;
using System.Text;

namespace ClassifiedPrintPDFReader
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    using iTextSharp.text.pdf.parser;

    namespace PdfHelper
    {
        

        /// <summary>
        /// Taken from http://www.java-frameworks.com/java/itext/com/itextpdf/text/pdf/parser/LocationTextExtractionStrategy.java.html
        /// </summary>
        class LocationTextExtractionStrategyEx2 : LocationTextExtractionStrategy
        {
            const float MIN_COL1 = 0;
            //const double MAX_COL1 = 156.4094;
            const float MIN_COL2 = 164.4095f;
            //const double MAX_COL2 = 320.8188;
            const float MIN_COL3 = 320.8181f;
            //const double MAX_COL3 = 485.2282;
            const float MIN_COL4 = 485.2283f;
            //const double MAX_COL4 = 649.6376;
            const float MIN_COL5 = 649.6377f;
            //const double MAX_COL5 = 822.047;
            public Dictionary<int, StringBuilder> Columbs = new Dictionary<int, StringBuilder>();
            private List<TextChunk> m_locationResult = new List<TextChunk>();
            private List<TextInfo> m_TextLocationInfo = new List<TextInfo>();
            public List<TextChunk> LocationResult
            {
                get { return m_locationResult; }
            }
            public List<TextInfo> TextLocationInfo
            {
                get { return m_TextLocationInfo; }
            }

            /// <summary>
            /// Creates a new LocationTextExtracationStrategyEx
            /// </summary>
            public LocationTextExtractionStrategyEx2()
            {
            }

            /// <summary>
            /// Returns the result so far
            /// </summary>
            /// <returns>a String with the resulting text</returns>
            public override String GetResultantText()
            {
                //return string.Empty;
                //m_locationResult.Sort();

                StringBuilder sb = new StringBuilder();
                TextChunk lastChunk = null;
                TextInfo lastTextInfo = null;
                StringBuilder sbColumb1 = new StringBuilder();
                StringBuilder sbColumb2 = new StringBuilder();
                StringBuilder sbColumb3 = new StringBuilder();
                StringBuilder sbColumb4 = new StringBuilder();
                StringBuilder sbColumb5 = new StringBuilder();
                foreach (TextChunk chunk in m_locationResult)
                {
                    if (lastChunk == null)
                    {
                        sb.Append(chunk.Text);
                        lastTextInfo = new TextInfo(chunk);
                        m_TextLocationInfo.Add(lastTextInfo);
                    }
                    else
                    {
                        float col = chunk.AscentLine.GetStartPoint()[Vector.I1];

                        if (chunk.sameLine(lastChunk))
                        {
                            float dist = chunk.distanceFromEndOf(lastChunk);

                            if (dist < -chunk.CharSpaceWidth)
                            {
                                //sb.Append(' ');
                                //lastTextInfo.addSpace();
                            }
                            //append a space if the trailing char of the prev string wasn't a space && the 1st char of the current string isn't a space
                            else if (dist > chunk.CharSpaceWidth / 2.0f && chunk.Text[0] != ' ' && lastChunk.Text[lastChunk.Text.Length - 1] != ' ')
                            {
                                //sb.Append(' ');
                                //lastTextInfo.addSpace();
                            }
                            
                            if (col < MIN_COL2)
                                sbColumb1.Append(chunk.Text);
                            else if (col >= MIN_COL2 && col < MIN_COL3)
                                sbColumb2.Append(chunk.Text);
                            else if (col >= MIN_COL3 && col < MIN_COL4)
                                sbColumb3.Append(chunk.Text);
                            else if (col >= MIN_COL4 && col < MIN_COL5)
                                sbColumb4.Append(chunk.Text);
                            else if (col >= MIN_COL5)
                                sbColumb5.Append(chunk.Text);

                            sb.Append(chunk.Text);
                            lastTextInfo.appendText(chunk);
                        }
                        else
                        {
                            sb.Append('\n');
                            sb.AppendFormat("#{0} {1}", chunk.AscentLine.GetStartPoint()[Vector.I1], chunk.Text);

                            if (col < MIN_COL2)
                            {
                                sbColumb1.Append(' ');
                                sbColumb1.Append(chunk.Text);
                            }
                            else if (col >= MIN_COL2 && col < MIN_COL3)
                            {
                                sbColumb2.Append(' ');
                                sbColumb2.Append(chunk.Text);
                            }
                            else if (col >= MIN_COL3 && col < MIN_COL4)
                            {
                                sbColumb3.Append(' ');
                                sbColumb3.Append(chunk.Text);
                            }
                            else if (col >= MIN_COL4 && col < MIN_COL5)
                            {
                                sbColumb4.Append(' ');
                                sbColumb4.Append(chunk.Text);
                            }
                            else if (col >= MIN_COL5)
                            {
                                sbColumb5.Append(' ');
                                sbColumb5.Append(chunk.Text);
                            }
                                

                            lastTextInfo = new TextInfo(chunk);                           
                            m_TextLocationInfo.Add(lastTextInfo);
                        }
                    }
                    lastChunk = chunk;
                }
                Columbs[1] = sbColumb1;
                Columbs[2] = sbColumb2;
                Columbs[3] = sbColumb3;
                Columbs[4] = sbColumb4;
                Columbs[5] = sbColumb5;
                return sb.ToString();
            }

            /// <summary>
            /// 
            /// </summary>
            /// <param name="renderInfo"></param>
            public override void RenderText(TextRenderInfo renderInfo)
            {
                LineSegment segment = renderInfo.GetBaseline();                
                TextChunk location = new TextChunk(renderInfo.GetText(), segment.GetStartPoint(), segment.GetEndPoint(), renderInfo.GetSingleSpaceWidth(), renderInfo.GetAscentLine(), renderInfo.GetDescentLine());
                m_locationResult.Add(location);
            }

            public class TextChunk : IComparable, ICloneable
            {
                string m_text;
                Vector m_startLocation;
                Vector m_endLocation;
                Vector m_orientationVector;
                int m_orientationMagnitude;
                int m_distPerpendicular;
                float m_distParallelStart;
                float m_distParallelEnd;
                float m_charSpaceWidth;

                public LineSegment AscentLine;
                public LineSegment DecentLine;

                public object Clone()
                {
                    TextChunk copy = new TextChunk(m_text, m_startLocation, m_endLocation, m_charSpaceWidth, AscentLine, DecentLine);
                    return copy;
                }

                public string Text
                {
                    get { return m_text; }
                    set { m_text = value; }
                }
                public float CharSpaceWidth
                {
                    get { return m_charSpaceWidth; }
                    set { m_charSpaceWidth = value; }
                }
                public Vector StartLocation
                {
                    get { return m_startLocation; }
                    set { m_startLocation = value; }
                }
                public Vector EndLocation
                {
                    get { return m_endLocation; }
                    set { m_endLocation = value; }
                }

                /// <summary>
                /// Represents a chunk of text, it's orientation, and location relative to the orientation vector
                /// </summary>
                /// <param name="txt"></param>
                /// <param name="startLoc"></param>
                /// <param name="endLoc"></param>
                /// <param name="charSpaceWidth"></param>
                public TextChunk(string txt, Vector startLoc, Vector endLoc, float charSpaceWidth, LineSegment ascentLine, LineSegment decentLine)
                {
                    m_text = txt;
                    m_startLocation = startLoc;
                    m_endLocation = endLoc;
                    m_charSpaceWidth = charSpaceWidth;
                    AscentLine = ascentLine;
                    DecentLine = decentLine;
                    Vector oVector = endLoc.Subtract(startLoc);
                    if (oVector.Length == 0)
                    {
                        oVector = new Vector(1, 0, 0);
                    }
                    m_orientationVector = oVector.Normalize();
                    m_orientationMagnitude = (int)(Math.Atan2(m_orientationVector[Vector.I2], m_orientationVector[Vector.I1]) * 1000);

                    //m_orientationVector = m_endLocation.Subtract(m_startLocation).Normalize();
                    //m_orientationMagnitude = (int)(Math.Atan2(m_orientationVector[Vector.I2], m_orientationVector[Vector.I1]) * 1000);

                    // see http://mathworld.wolfram.com/Point-LineDistance2-Dimensional.html
                    // the two vectors we are crossing are in the same plane, so the result will be purely
                    // in the z-axis (out of plane) direction, so we just take the I3 component of the result
                    Vector origin = new Vector(0, 0, 1);
                    m_distPerpendicular = (int)(m_startLocation.Subtract(origin)).Cross(m_orientationVector)[Vector.I3];

                    m_distParallelStart = m_orientationVector.Dot(m_startLocation);
                    m_distParallelEnd = m_orientationVector.Dot(m_endLocation);
                }

                /// <summary>
                /// true if this location is on the the same line as the other text chunk
                /// </summary>
                /// <param name="textChunkToCompare">the location to compare to</param>
                /// <returns>true if this location is on the the same line as the other</returns>
                public bool sameLine(TextChunk textChunkToCompare)
                {
                    if (m_orientationMagnitude != textChunkToCompare.m_orientationMagnitude) return false;
                    if (m_distPerpendicular != textChunkToCompare.m_distPerpendicular) return false;
                    return true;
                }

                /// <summary>
                /// Computes the distance between the end of 'other' and the beginning of this chunk
                /// in the direction of this chunk's orientation vector.  Note that it's a bad idea
                /// to call this for chunks that aren't on the same line and orientation, but we don't
                /// explicitly check for that condition for performance reasons.
                /// </summary>
                /// <param name="other"></param>
                /// <returns>the number of spaces between the end of 'other' and the beginning of this chunk</returns>
                public float distanceFromEndOf(TextChunk other)
                {
                    float distance = m_distParallelStart - other.m_distParallelEnd;
                    return distance;
                }

                /// <summary>
                /// Compares based on orientation, perpendicular distance, then parallel distance
                /// </summary>
                /// <param name="obj"></param>
                /// <returns></returns>
                public int CompareTo(object obj)
                {
                    if (obj == null) throw new ArgumentException("Object is now a TextChunk");

                    TextChunk rhs = obj as TextChunk;
                    if (rhs != null)
                    {
                        if (this == rhs) return 0;

                        int rslt;
                        rslt = m_orientationMagnitude - rhs.m_orientationMagnitude;
                        if (rslt != 0) return rslt;

                        rslt = m_distPerpendicular - rhs.m_distPerpendicular;
                        if (rslt != 0) return rslt;

                        // note: it's never safe to check floating point numbers for equality, and if two chunks
                        // are truly right on top of each other, which one comes first or second just doesn't matter
                        // so we arbitrarily choose this way.
                        rslt = m_distParallelStart < rhs.m_distParallelStart ? -1 : 1;

                        return rslt;
                    }
                    else
                    {
                        throw new ArgumentException("Object is now a TextChunk");
                    }
                }
            }

            public class TextInfo
            {
                public Vector TopLeft;
                public Vector BottomRight;
                private string m_Text;

                public string Text
                {
                    get { return m_Text; }
                }

                /// <summary>
                /// Create a TextInfo.
                /// </summary>
                /// <param name="initialTextChunk"></param>
                public TextInfo(TextChunk initialTextChunk)
                {
                    TopLeft = initialTextChunk.AscentLine.GetStartPoint();
                    BottomRight = initialTextChunk.DecentLine.GetEndPoint();
                    m_Text = initialTextChunk.Text;
                }

                /// <summary>
                /// Add more text to this TextInfo.
                /// </summary>
                /// <param name="additionalTextChunk"></param>
                public void appendText(TextChunk additionalTextChunk)
                {
                    BottomRight = additionalTextChunk.DecentLine.GetEndPoint();
                    m_Text += additionalTextChunk.Text;
                }

                /// <summary>
                /// Add a space to the TextInfo.  This will leave the endpoint out of sync with the text.
                /// The assumtion is that you will add more text after the space which will correct the endpoint.
                /// </summary>
                public void addSpace()
                {
                    m_Text += ' ';
                }


            }
        }
    }
}
