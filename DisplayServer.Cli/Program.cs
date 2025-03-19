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

        const string blackCanvasFilename = "/home/pi/code/DisplayServer/canvas-black.bmp";
        const string redCanvasFilename = "/home/pi/code/DisplayServer/canvas-red.bmp";

        // todo:
        // 1. Resize images to fit.
        // 2. Black canvas: a) Convert to grayscale b) Floyd–Steinberg to 1-bit image
        // 3. Red canvas: a) take only red channel (red-scale) b) Floyd–Steinberg to 1-bit image

        // var input = "/home/pi/code/DisplayServer/DisplayServer.Cli/sample-black.png";
        // var input = "/home/pi/code/DisplayServer/DisplayServer.Cli/landscape-color.jpg";
        // var input = "/home/pi/code/DisplayServer/DisplayServer.Cli/red-tulips.jpg";
        // var input = "/home/pi/code/DisplayServer/DisplayServer.Cli/this-is-fine.jpg";
        var input = "/home/pi/code/DisplayServer/DisplayServer.Cli/tank.png";

        using (var image = Image.Load(input))
        {
            // Resize to fit
            {
                var inputAspectRatio = ((double)image.Width) / image.Height;
                if (inputAspectRatio > AspectRatio)
                {
                    // Pad height
                    image.Mutate(x => x.Pad(image.Width, (int)(image.Width / AspectRatio), Color.White));
                }
                else
                {
                    // Pad width
                    image.Mutate(x => x.Pad((int)(image.Height * AspectRatio), image.Height, Color.White));
                }

                // Resize to target size (and now target aspect ratio)
                image.Mutate(x => x.Resize(Width, Height));
            }

            using var inputStream = new MemoryStream();
            image.Save(inputStream, new BmpEncoder());

            // Convert to monochrome bitmap
            {
                inputStream.Position = 0;
                var blackImage = Image.Load<Rgb24>(inputStream);

                // blackImage.Mutate(x => x.Contrast(0));
                // blackImage.Mutate(x => x.Brightness(1.75f));

                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        var pixel = blackImage[x, y];
                        // var average = (byte)((pixel.R + pixel.G + pixel.B) / 3);
                        // var average = (byte)((255 + pixel.G + pixel.B) / 3);
                        // var average = (byte)((pixel.G + pixel.B) / 2);
                        var average = (byte)Math.Min(255, ((pixel.R * 2.5f) + (pixel.G * 0.9f) + (pixel.B * 0.9f)) / 3);

                        var redToDivide = 0.67f * pixel.R;

                        // var newRed = (byte)Math.Max(255, 1.33f * average);
                        var newRed = (byte)average;
                        var newGreen = (byte)(Math.Min(255, pixel.G + redToDivide));
                        var newBlue = (byte)(Math.Min(255, pixel.B + redToDivide));
                        blackImage[x, y] = new Rgb24(average, average, average);
                    }
                }

                // blackImage.Mutate(x => x.Brightness(1.75f));

                blackImage.Mutate(x => x.BinaryDither(ErrorDither.FloydSteinberg));
                blackImage.SaveAsBmp(blackCanvasFilename, new BmpEncoder() { BitsPerPixel = BmpBitsPerPixel.Pixel1 });
            }

            // Convert reds to monochrome bitmap
            {
                inputStream.Position = 0;
                var redImage = Image.Load<Rgb24>(inputStream);

                // redImage.Mutate(x => x.Contrast(1.2f));
                // redImage.Mutate(x => x.Brightness(1.05f));

                for (int y = 0; y < image.Height; y++)
                {
                    for (int x = 0; x < image.Width; x++)
                    {
                        var pixel = redImage[x, y];
                        var average = (byte)((pixel.G + pixel.B) / 2);
                        var newValue = (byte)(255 - Math.Max(0, pixel.R - (1.5f * average)));
                        redImage[x, y] = new Rgb24(newValue, newValue, newValue);
                    }
                }

                redImage.Mutate(x => x.BinaryDither(ErrorDither.FloydSteinberg));
                redImage.SaveAsBmp(redCanvasFilename, new BmpEncoder() { BitsPerPixel = BmpBitsPerPixel.Pixel1 });
            }
        }

        // return;

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