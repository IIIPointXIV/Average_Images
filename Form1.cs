using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Drawing.Imaging;
public class Form1 : Form
{
    List<DirectBitmap> images;
    bool AvgOnlyExistingPixels = false;
    Stopwatch time = new Stopwatch();

    public void RunForm(string path, bool AvgOnlyExistingPixelsArg)
    {
       MakeAverageImg(path);
    }

    private void MakeAverageImg(string givenPath)
    {
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
        Console.WriteLine("Found " + images.Count + " images");
        
        int maxHeight = 0;
        int maxWidth = 0;
        foreach(DirectBitmap curentImage in images)
        {
            maxHeight = (maxHeight < curentImage.Height ? curentImage.Height : maxHeight);
            maxWidth = (maxWidth < curentImage.Width ? curentImage.Width : maxWidth);
        }
        Console.WriteLine($"Images have a max height of {maxHeight} and a max width of {maxWidth}");

        DirectBitmap finalImage = new DirectBitmap(maxWidth, maxHeight);

        int threadsCount = 14;
        List<Task> threadList = new List<Task>();
        int amount = (int)MathF.Floor(maxWidth/threadsCount);
        int endingOffset = maxWidth - (amount*threadsCount);

        for(int i = 0; i<threadsCount; i++)
        {
            int startX = i*amount;
            int endX = (i+1)*amount + ((i+1)==threadsCount ? endingOffset :0)-1;
            threadList.Add(Task.Factory.StartNew(()=>RunByThread(finalImage, startX, endX)));
        }
        Task.WaitAll(threadList.ToArray());
        
        foreach(Task thread in threadList)
            thread.Dispose();

        finalImage.Bitmap.Save(Path.Combine(givenPath, "final_image.png"));
        finalImage.Dispose();
        Console.WriteLine("Done. Made image \"final_image.png\"");
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
                countOffset += AvgOnlyExistingPixels ? 1 : 0; //if image does not have pixel at that point, do not add it
                continue;
            }
            thisColor = image.GetPixel(dx, dy);

            if(thisColor.A == 0)
            {
                countOffset += AvgOnlyExistingPixels ? 1 : 0; 
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