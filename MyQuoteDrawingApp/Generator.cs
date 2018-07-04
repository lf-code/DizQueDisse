//#define DEBUG DEFINED in project properties

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Drawing.Text;
using System.IO;
using System.Drawing.Imaging;
using System.Diagnostics;

namespace MyQuoteDrawingApp
{
    /// <summary>
    /// Auxiliary program that creates an image for a given quote, 
    /// based on a default backgroung image, and a custom font.
    /// Input data (the quote) is provided by calling process, via Console.Input 
    /// </summary>
    static public class Generator
    {
        static string path = System.IO.Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location);

        static void Main(string[] args)
        {
            //1) Get the source process StandardInput encoding (process.StandardInput.Encoding.CodePage)
            //the first line of input is the encoding code
            int InputEncodingCodePage = int.Parse(Console.In.ReadLine()); 
            Encoding sourceConsoleEncoding = Encoding.GetEncoding(InputEncodingCodePage);

            //2) Correct string encoding, to get the original intended strings
            //if process.StandardInput.Encoding !=  Console.InputEncoding, string will contain errors
            string quote = MyCorrectEncoding(Console.ReadLine(), sourceConsoleEncoding);
            string author = MyCorrectEncoding(Console.ReadLine(), sourceConsoleEncoding);

            //3) Generate the image and send back its bytes to the calling process:
            byte[] b = Generator.GetImage(quote, author);
            Console.OpenStandardOutput().Write(b, 0, b.Length);
        }

        /// <summary>
        /// Corrects the text received through Console.ReadLine.
        /// </summary>
        /// <param name="stringReadFromConsole">Text to be corrected</param>
        /// <param name="sourceConsoleEncoding">The Process.StandardInput.Encoding of the calling process</param>
        /// <returns>The corrected text.</returns>
        static public string MyCorrectEncoding(string stringReadFromConsole, Encoding sourceConsoleEncoding)
        {
            //1) Get the original bytes received, by reverting the convertion from Console.InputEncoding to Unicode (from string type)
            byte[] originalBytesReceived = Encoding.Convert(Encoding.Unicode, Console.InputEncoding, Encoding.Unicode.GetBytes(stringReadFromConsole));

            //2) As process.StandardInput.Encoding may differ from Console.InputEncoding, use the former to obtain the original string
            // that the calling process intended to send
            string stringWithCorrectEncoding = Encoding.Unicode.GetString(Encoding.Convert(sourceConsoleEncoding, Encoding.Unicode, originalBytesReceived));

            return stringWithCorrectEncoding;
        }


        /// <summary>
        /// Generates an image, writing the quote text and author name 
        /// with a custom font on a given background image.
        /// </summary>
        /// <param name="quote">The quote's text</param>
        /// <param name="author">The name of the author of the quote.</param>
        /// <returns>The array of bytes that make up the image.</returns>
        static public byte[] GetImage(string quote, string author)
        {
            //Default background image:
            Bitmap backgroundImage = new Bitmap(path+@"\MyFiles\citbg.png");
            Graphics g = Graphics.FromImage(backgroundImage);

            //Custom font: 
            FontFamily[] fontFamilies;
            PrivateFontCollection privateFontCollection = new PrivateFontCollection();
            privateFontCollection.AddFontFile(path + @"\MyFiles\TravelingTypewriter.ttf");
            fontFamilies = privateFontCollection.Families;
            string familyName = fontFamilies[0].Name;

            //Write quote text onto the background image, using custom font:
            Font regFont = new Font(familyName, 28, FontStyle.Regular, GraphicsUnit.Pixel);
            SolidBrush drawBrush = new SolidBrush(Color.FromArgb(255, 63, 37, 11));
            StringFormat stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Center;
            stringFormat.LineAlignment = StringAlignment.Center;
            RectangleF rectF1 = new RectangleF(31, 25, 550, 320);
            string citText = $"\"{quote}\"";
            g.DrawString(citText, regFont, drawBrush, rectF1, stringFormat);

            //Write author text onto the background image, using custom font:
            regFont = new Font(familyName, 20, FontStyle.Regular, GraphicsUnit.Pixel);
            drawBrush = new SolidBrush(Color.FromArgb(255, 63, 37, 11));
            stringFormat = new StringFormat();
            stringFormat.Alignment = StringAlignment.Far;
            stringFormat.LineAlignment = StringAlignment.Center;
            rectF1 = new RectangleF(31, 325, 519, 50);
            string autorText = $"- {author}";
            g.DrawString(autorText, regFont, drawBrush, rectF1, stringFormat);

            //save resulting image to a memory stream as a byte array:
            MemoryStream m = new MemoryStream();
            backgroundImage.Save(m, ImageFormat.Png);
            byte[] imageBytes = m.ToArray();

            //Dispose:
            regFont.Dispose();
            drawBrush.Dispose();
            g.Dispose();
            m.Dispose();

            return imageBytes;
        }

    }
}
