<h1 align="center">Average Your Images!</h1>
This project allows you to take a directory of images and average them together. This can create some weird looking images!

# Features
- Average images

# How To Use
1. Download exe from the latest [release](https://github.com/IIIPointXIV/Average_Images/releases).
2. Open command prompt where the exe is.
3. Run Average_Images.exe -d "Directory where your images are"
4. Wait!
5. The averaged image should be put in the directory provided with the name average-image.png (If you did not change it)

# Command Line Arguments
| Command | Description | Example |
| --- | --- | --- |
| -d or -directory | Provide the directory of images. (Required) | -d "C:\image-directory" | 
| -t or -threads | The amount of threads the program will use. Defaults to 4. | -t 16 |
| -o or -output | The location and name of the final image. Defaults to [the image directory]\average-image.png | -o "C:\image.png" |
| -r or -resize | If present, will resize images to the size of the biggest image provided. | -r |
| -existing | Only averages the pixels that are present on an image. | -existing |
