using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace NuGetServer
{
    public static class StreamExtensions
    {
        public static async Task<FileStream> ToTemporaryFileStreamAsync(
            this Stream stream,
            CancellationToken cancellation)
        {
            var fileStream = new FileStream(
                Path.GetTempFileName(),
                FileMode.Create,
                FileAccess.ReadWrite,
                FileShare.None,
                bufferSize: 81920,
                FileOptions.DeleteOnClose);

            using (stream)
            {
                await stream.CopyToAsync(fileStream, cancellation);
                await stream.FlushAsync();
            }

            fileStream.Position = 0;

            return fileStream;
        }
    }
}
