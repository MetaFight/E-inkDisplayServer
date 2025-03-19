using System;
using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Dithering;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;
using Iot.Device.BuildHat.Sensors;
using SixLabors.ImageSharp.Formats.Png;

namespace DisplayServer.Cli;

class Program
{
    const int Width = 640;
    const int Height = 384;
    const double AspectRatio = ((double)Width) / Height;

    static void Main(string[] args)
    {
        // const string blackCanvasFilename = "/home/pi/code/DisplayServer/DisplayServer.Cli/sample-black-small.bmp";
        // const string redCanvasFilename = "/home/pi/code/DisplayServer/DisplayServer.Cli/sample-red-small.bmp";

        // const string blackCanvasFilename = "/home/pi/.displayServer/canvas-black.bmp";
        // const string redCanvasFilename = "/home/pi/.displayServer/canvas-red.bmp";

        const string canvasFilename = "../out/canvas.bmp";
        const string blackCanvasFilename = "../out/canvas-black.bmp";
        const string redCanvasFilename = "../out/canvas-red.bmp";

        var sourceImage = "../in/landscape-color.jpg";
        // var sourceImage = "../in/red-tulips.jpg";
        // var sourceImage = "../in/this-is-fine.jpg";
        // var sourceImage = "../in/tank.png";

        var renderImage = true;
        // renderImage = false;

        const string input = "../in/input.png";

        var black = Color.Black;
        var white = Color.White;
        var red = Color.Red;
        // var red = new Color(new Rgb24(255, 64, 64));
        // var red = new Color(new Rgb24(255 - 128, 0, 0));

        var blackPixel = black.ToPixel<Rgb24>();
        var whitePixel = white.ToPixel<Rgb24>();
        var redPixel = red.ToPixel<Rgb24>();

        // Preprocessing
        using (var canvas = Image.Load<Rgb24>(sourceImage))
        {
            // Resize to fit
            var inputAspectRatio = ((double)canvas.Width) / canvas.Height;
            if (inputAspectRatio > AspectRatio)
            {
                // Pad height
                canvas.Mutate(x => x.Pad(canvas.Width, (int)(canvas.Width / AspectRatio), white));
            }
            else
            {
                // Pad width
                canvas.Mutate(x => x.Pad((int)(canvas.Height * AspectRatio), canvas.Height, white));
            }

            // Resize to target size (and now target aspect ratio)
            canvas.Mutate(x => x.Resize(Width, Height));

            // Lighten up greens and blues
            for (int y = 0; y < canvas.Height; y++)
            {
                for (int x = 0; x < canvas.Width; x++)
                {
                    var pixel = canvas[x, y];
                    var r = pixel.R;
                    var g = pixel.G;
                    var b = pixel.B;

                    var gScale = 1.4f;
                    var bScale = 1.6f;
                    // r = (byte)Math.Min(255, (int)r * scale);
                    g = (byte)Math.Min(255, g * gScale);
                    b = (byte)Math.Min(255, b * bScale);

                    canvas[x, y] = new Rgb24(r, g, b);
                }
            }

            canvas.Mutate(x => x.Saturate(0.6f));
            // canvas.Mutate(x => x.Contrast(0.9f));
            // canvas.Mutate(x => x.Lightness(1.1f));
            // canvas.Mutate(x => x.Brightness(1.1f));

            canvas.SaveAsPng(input);
        }

        // Dithering
        using (var canvas = Image.Load(input))
        {
            var palette = new ReadOnlyMemory<Color>([white, black, red]);
            canvas.Mutate(x => x.ApplyProcessor(new PaletteDitherProcessor(ErrorDither.FloydSteinberg, palette)));

            canvas.SaveAsBmp(canvasFilename, new BmpEncoder()
            {
                BitsPerPixel = BmpBitsPerPixel.Pixel2,
            });
        }

        // Create blacks canvas
        using (var canvas = Image.Load<Rgb24>(canvasFilename))
        {
            for (int y = 0; y < canvas.Height; y++)
            {
                for (int x = 0; x < canvas.Width; x++)
                {
                    var pixel = canvas[x, y];

                    canvas[x, y] = pixel == blackPixel ? blackPixel : whitePixel;
                }
            }

            canvas.SaveAsBmp(blackCanvasFilename, new BmpEncoder() { BitsPerPixel = BmpBitsPerPixel.Pixel1 });
        }

        // Create reds canvas
        using (var canvas = Image.Load<Rgb24>(canvasFilename))
        {
            for (int y = 0; y < canvas.Height; y++)
            {
                for (int x = 0; x < canvas.Width; x++)
                {
                    var pixel = canvas[x, y];

                    canvas[x, y] = pixel == redPixel ? blackPixel : whitePixel;
                }
            }

            canvas.SaveAsBmp(redCanvasFilename, new BmpEncoder() { BitsPerPixel = BmpBitsPerPixel.Pixel1 });
        }

        // Display image
        if (renderImage)
        {
            var pyProcArgs = $"display_interface.py --blacks {blackCanvasFilename} --reds {redCanvasFilename}";
            var pythonProcessStartInfo = new ProcessStartInfo
            {
                WorkingDirectory = "../Scripts/",
                FileName = "python",
                Arguments = pyProcArgs
            };

            var pyProc = Process.Start(pythonProcessStartInfo);
            // Console.WriteLine($"launching {pyProcArgs}");
            pyProc?.WaitForExit();
        }
    }
}