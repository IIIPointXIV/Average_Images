using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
using System.Drawing;
public class Form1 : Form
{
    List<DirectBitmap> images;
    bool avgOnlyExistingPixels = false;
    bool resizeImages = false;
    Stopwatch time = new Stopwatch();
    Dictionary<args, string> argDict = new Dictionary<args, string>();
    int threadCount = 4;
    enum args
    {
        directoryOfImgs,
        threads,
        directoryOfFinishedImg,
        resizeImags,
        avgOnlyExistingPixels,
    };

    public void RunForm(string[] args)
    {
        if(args.Length == 0)
        {
            Console.WriteLine("No path of images given. Exiting.");
            return;
        }
        ParseArgs(args);
        MakeAverageImg(argDict[Form1.args.directoryOfImgs]);
    }

    private void ParseArgs(string[] args)
    {
        argDict.Add(Form1.args.directoryOfImgs, null);
        argDict.Add(Form1.args.threads, null);
        argDict.Add(Form1.args.directoryOfFinishedImg, null);
        argDict.Add(Form1.args.resizeImags, "false");
        argDict.Add(Form1.args.avgOnlyExistingPixels, "false");

        for(int i = 0; i < args.Length; i++)
        {
            if(args[i] == "-d" || args[i] == "-directory")
                argDict[Form1.args.directoryOfImgs] = args[i+1];
            else if(args[i] == "-t" || args[i] == "-threads")
            {
                argDict[Form1.args.threads] = args[i+1];
                threadCount = int.Parse(args[i+1]);
            }
            else if(args[i] == "-o" || args[i] == "-output")
                argDict[Form1.args.directoryOfFinishedImg] = args[i+1];
            else if(args[i] == "-r" || args[i] == "-resize")
            {
                argDict[Form1.args.resizeImags] = "true";
                resizeImages = true;
            }
            else if(args[i] == "-existing")
            {
                argDict[Form1.args.avgOnlyExistingPixels] = "true";
                avgOnlyExistingPixels = true;
            }
        }

        if(string.IsNullOrWhiteSpace(argDict[Form1.args.directoryOfFinishedImg]))
        {
            argDict[Form1.args.directoryOfFinishedImg] = Path.Combine(argDict[Form1.args.directoryOfImgs], "average-image.png");
        }
    }
   
    private void MakeAverageImg(string givenPath)
    {
        Console.WriteLine($"Using {threadCount} threads");
        time.Restart();
        Console.WriteLine("Discovering Images");

        images = new List<DirectBitmap>();
        DirectoryInfo di = new DirectoryInfo(givenPath);
        foreach (FileInfo file in di.GetFiles())
        {
            if(file.Name.Contains(".png") || file.Name.Contains(".jpg"))
            {
                using(Image image = Bitmap.FromFile(file.FullName))
                {
                    images.Add(ConvertToDirectBitmap(image));
                }
            }
        }

        if(images.Count == 0)
        {
            Console.WriteLine("No valid imaged found in directory. Exiting");
            return;
        }

        Console.WriteLine("Found " + images.Count + " images");
        
        int maxHeight = 0;
        int maxWidth = 0;
        foreach(DirectBitmap curentImage in images)
        {
            maxHeight = (maxHeight < curentImage.Height ? curentImage.Height : maxHeight);
            maxWidth = (maxWidth < curentImage.Width ? curentImage.Width : maxWidth);
        }
        Console.WriteLine($"Images have a max height of {maxWidth} and a max width of {maxHeight}");

        if(resizeImages)
        {
            Console.WriteLine($"Resizing images to {maxWidth}x{maxHeight}");
            for(int i = 0; i < images.Count; i++)
            {
                images[i] = ResizeDirectBitmap(images[i], maxWidth, maxHeight);
            }
            Console.WriteLine("Finished resizing images");
        }

        DirectBitmap finalImage = new DirectBitmap(maxWidth, maxHeight);

        List<Task> threadList = new List<Task>();
        int amount = (int)MathF.Floor(maxWidth/threadCount);
        int endingOffset = maxWidth - (amount*threadCount);

        Console.WriteLine("Averaging images");
        for(int i = 0; i<threadCount; i++)
        {
            int startX = i*amount;
            int endX = (i+1)*amount + ((i+1)==threadCount ? endingOffset :0)-1;
            threadList.Add(Task.Factory.StartNew(()=>RunByThread(finalImage, startX, endX)));
        }
        Task.WaitAll(threadList.ToArray());
        
        foreach(Task thread in threadList)
            thread.Dispose();

        finalImage.Bitmap.Save(argDict[Form1.args.directoryOfFinishedImg]);
        finalImage.Dispose();
        Console.WriteLine($"Finished. Saved image to {argDict[Form1.args.directoryOfFinishedImg]}");
        Console.WriteLine($"Took {time.ElapsedMilliseconds}ms to run");
    }

    private DirectBitmap ConvertToDirectBitmap(Image toConvertImage)
    {
        DirectBitmap directBitmap = new DirectBitmap(toConvertImage.Width, toConvertImage.Height);
        Rectangle rectSize = new Rectangle(0, 0, toConvertImage.Width, toConvertImage.Height);

        using(Graphics graph = Graphics.FromImage(directBitmap.Bitmap))
            graph.DrawImage(toConvertImage, rectSize);
        
        toConvertImage.Dispose();
        return directBitmap;
    }

    private void RunByThread(DirectBitmap finalImage, int startX, int endX)
    {
        for (int dx = startX; dx <= endX; dx++)
        {
            for (int dy = 0; dy < finalImage.Height; dy++)
            {
                finalImage.SetPixel(dx, dy, GetColorAverage(dx, dy));
            }
        }
    }

    private Color GetColorAverage(int dx, int dy)
    {
        //int origDX = dx;
        //int origDY = dy;
        int countOffset = 0;
        int colorA=0;
        int colorR=0;
        int colorG=0;
        int colorB=0;
        
        Color thisColor;
        int imagesCount = images.Count();
        for(int thisImageIndex = 0; thisImageIndex < imagesCount; thisImageIndex++)
        {
            DirectBitmap image = images[thisImageIndex];
            //dx = origDX;
            //dy = origDY;

            if(image.Height < dy+1 || image.Width < dx+1 || dx<0 || dy<0)
            {
                countOffset += avgOnlyExistingPixels ? 1 : 0; //if image does not have pixel at that point, do not add it
                continue;
            }
            thisColor = image.GetPixel(dx, dy);

            if(thisColor.A == 0)
            {
                countOffset += avgOnlyExistingPixels ? 1 : 0; 
                continue;
            }
            colorA += thisColor.A;
            colorR += thisColor.R;
            colorG += thisColor.G;
            colorB += thisColor.B;
        }
        int divisor = images.Count()==countOffset ? 1 : images.Count()-countOffset; //make sure to not divide by zero

        return Color.FromArgb(colorA/divisor, colorR/divisor, colorG/divisor, colorB/divisor);
    }

    private DirectBitmap ResizeDirectBitmap(DirectBitmap image, int width, int height)
    {
        var destRect = new Rectangle(0, 0, width, height);
        var destImage = new Bitmap(width, height);

        destImage.SetResolution(image.Bitmap.HorizontalResolution, image.Bitmap.VerticalResolution);

        using (var graphics = Graphics.FromImage(destImage))
        {
            //graphics.CompositingMode = CompositingMode.SourceCopy;
            graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
            graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
            graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
            graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

            using (var wrapMode = new ImageAttributes())
            {
                wrapMode.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                graphics.DrawImage(image.Bitmap, destRect, 0, 0, image.Width,image.Height, GraphicsUnit.Pixel, wrapMode);
            }
        }
        image.Dispose();
        return ConvertToDirectBitmap(destImage);
    }
}

public class DirectBitmap : IDisposable
{
    public Bitmap Bitmap { get; private set; }
    public Int32[] Bits { get; private set; }
    public bool Disposed { get; private set; }
    public int Height { get; private set; }
    public int Width { get; private set; }

    protected GCHandle BitsHandle { get; private set; }

    public DirectBitmap(int width, int height)
    {
        Width = width;
        Height = height;
        Bits = new Int32[width * height];
        BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
        Bitmap = new Bitmap(width, height, width * 4, PixelFormat.Format32bppPArgb, BitsHandle.AddrOfPinnedObject());
    }

     /*public void SetNewBitmap(Bitmap image)
    {
        Width = image.Width;
        Height = image.Height;
        Bits = new Int32[Width * Height];
        BitsHandle = GCHandle.Alloc(Bits, GCHandleType.Pinned);
        Bitmap = new Bitmap(Width, Height, Width*4, PixelFormat.Format32bppPArgb, BitsHandle.AddrOfPinnedObject());
    } */

    public void SetPixel(int x, int y, Color colour)
    {
        int index = x + (y * Width);
        int col = colour.ToArgb();

        Bits[index] = col;
    }

    public Color GetPixel(int x, int y)
    {
        int index = x + (y * Width);
        int col = Bits[index];
        Color result = Color.FromArgb(col);

        return result;
    }

    public void Dispose()
    {
        if (Disposed) return;
        Disposed = true;
        Bitmap.Dispose();
        BitsHandle.Free();
    }
}