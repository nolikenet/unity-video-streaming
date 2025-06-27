using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

public class FFmpegEncoder
{
    private Process ffmpeg;
    private Stream ffmpegInput;

    public void StartEncoder(int width, int height, int fps, string outputFilePath)
    {
        string args = $"-f rawvideo -pixel_format rgb24 -video_size {width}x{height} -framerate {fps} -i - " +
                      $"-c:v libx264 -preset ultrafast -pix_fmt yuv420p \"{outputFilePath}\"";

        ffmpeg = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = "ffmpeg",
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardInput = true,
                CreateNoWindow = true
            }
        };

        ffmpeg.Start();
        ffmpegInput = ffmpeg.StandardInput.BaseStream;
    }

    public async Task EncodeFrame(byte[] frameData)
    {
        await ffmpegInput.WriteAsync(frameData, 0, frameData.Length);
    }

    public void StopEncoder()
    {
        ffmpegInput.Flush();
        ffmpegInput.Close();
        ffmpeg.WaitForExit();
        ffmpeg.Dispose();
    }
}