using System;
using System.Diagnostics;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Processing.Processors.Dithering;
using SixLabors.ImageSharp.Formats.Bmp;
using SixLabors.ImageSharp.PixelFormats;

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

        // todo:
        // 1. Resize images to fit.
        // 2. Black canvas: a) Convert to grayscale b) Floyd–Steinberg to 1-bit image
        // 3. Red canvas: a) take only red channel (red-scale) b) Floyd–Steinberg to 1-bit image

        // var input = "../in/sample-black.png";
        // var input = "../in/landscape-color.jpg";
        // var input = "../in/red-tulips.jpg";
        // var input = "../in/this-is-fine.jpg";
        var input = "../in/tank.png";


        using (var sourceImage = Image.Load(input))
        {
            // Resize to fit
            {
                var inputAspectRatio = ((double)sourceImage.Width) / sourceImage.Height;
                if (inputAspectRatio > AspectRatio)
                {
                    // Pad height
                    sourceImage.Mutate(x => x.Pad(sourceImage.Width, (int)(sourceImage.Width / AspectRatio), Color.White));
                }
                else
                {
                    // Pad width
                    sourceImage.Mutate(x => x.Pad((int)(sourceImage.Height * AspectRatio), sourceImage.Height, Color.White));
                }

                // Resize to target size (and now target aspect ratio)
                sourceImage.Mutate(x => x.Resize(Width, Height));
            }

            // Dither down to White, Black, Red
            {
                var palette = new ReadOnlyMemory<Color>([Color.White, Color.Black, Color.Red]);
                sourceImage.Mutate(x => x.ApplyProcessor(new PaletteDitherProcessor(ErrorDither.FloydSteinberg, palette)));

                sourceImage.SaveAsBmp(canvasFilename, new BmpEncoder() { BitsPerPixel = BmpBitsPerPixel.Pixel2 });
            }
        }

        // Create blacks canvas
        using (var canvas = Image.Load<Rgb24>(canvasFilename))
        {
            var black = Color.Black.ToPixel<Rgb24>();
            var white = Color.White.ToPixel<Rgb24>();

            for (int y = 0; y < canvas.Height; y++)
            {
                for (int x = 0; x < canvas.Width; x++)
                {
                    var pixel = canvas[x, y];

                    if (pixel != black)
                    {
                        canvas[x, y] = white;
                    }
                }
            }

            canvas.SaveAsBmp(blackCanvasFilename, new BmpEncoder() { BitsPerPixel = BmpBitsPerPixel.Pixel1 });
        }

        // Create reds canvas
        using (var canvas = Image.Load<Rgb24>(canvasFilename))
        {
            var red = Color.Red.ToPixel<Rgb24>();
            var white = Color.White.ToPixel<Rgb24>();

            for (int y = 0; y < canvas.Height; y++)
            {
                for (int x = 0; x < canvas.Width; x++)
                {
                    var pixel = canvas[x, y];

                    if (pixel != red)
                    {
                        canvas[x, y] = white;
                    }
                }
            }

            canvas.SaveAsBmp(redCanvasFilename, new BmpEncoder() { BitsPerPixel = BmpBitsPerPixel.Pixel1 });
        }

        return;

        // Display image
        {
            var pyProcArgs = $"display_interface.py --blacks {blackCanvasFilename} --reds {redCanvasFilename}";
            var pythonProcessStartInfo = new ProcessStartInfo
            {
                WorkingDirectory = "/home/pi/code/DisplayServer/Scripts/",
                FileName = "python",
                Arguments = pyProcArgs
            };

            var pyProc = Process.Start(pythonProcessStartInfo);
            // Console.WriteLine($"launching {pyProcArgs}");
            pyProc?.WaitForExit();
        }
    }
}