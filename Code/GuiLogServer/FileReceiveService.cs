using System.Security.Cryptography;
using System.Text;

namespace GuiLogServer;

public sealed record ReceivedFileResult(string FileName, string FullPath, long Size, string Sha256);

public sealed class InvalidReceivedFileNameException(string message) : Exception(message);

public sealed class DuplicateReceivedFileException(string fileName)
    : IOException($"Tệp '{fileName}' đã tồn tại trong thư mục nhận.");

public sealed class ReceivedFileTooLargeException(long maximumSize)
    : IOException($"Tệp vượt giới hạn nhận là {maximumSize} byte.");

public sealed class FileReceiveService
{
    public const long MaximumFileSize = 2L * 1024 * 1024 * 1024;

    private static readonly HashSet<string> ReservedDeviceNames = CreateReservedDeviceNames();

    public async Task<ReceivedFileResult> SaveAsync(
        Stream source,
        string destinationDirectory,
        string fileName,
        long? expectedLength = null,
        CancellationToken cancellationToken = default)
    {
        ValidateFileName(fileName);
        if (expectedLength is < 0)
            throw new ArgumentOutOfRangeException(nameof(expectedLength));
        if (expectedLength > MaximumFileSize)
            throw new ReceivedFileTooLargeException(MaximumFileSize);

        var fullDirectory = Path.GetFullPath(destinationDirectory);
        Directory.CreateDirectory(fullDirectory);
        var destinationPath = Path.GetFullPath(Path.Combine(fullDirectory, fileName));
        var directoryPrefix = Path.EndsInDirectorySeparator(fullDirectory)
            ? fullDirectory
            : fullDirectory + Path.DirectorySeparatorChar;
        if (!destinationPath.StartsWith(directoryPrefix, StringComparison.OrdinalIgnoreCase))
            throw new InvalidReceivedFileNameException("Tên tệp không được phép trỏ ra ngoài thư mục nhận.");
        if (File.Exists(destinationPath))
            throw new DuplicateReceivedFileException(fileName);

        var temporaryPath = Path.Combine(fullDirectory, $".upload-{Guid.NewGuid():N}.tmp");
        long size = 0;
        try
        {
            using var hash = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            await using (var output = new FileStream(
                temporaryPath,
                FileMode.CreateNew,
                FileAccess.Write,
                FileShare.None,
                bufferSize: 128 * 1024,
                FileOptions.Asynchronous | FileOptions.SequentialScan))
            {
                var buffer = new byte[128 * 1024];
                while (expectedLength is null || size < expectedLength.Value)
                {
                    var requestedBytes = expectedLength is null
                        ? buffer.Length
                        : (int)Math.Min(buffer.Length, expectedLength.Value - size);
                    var bytesRead = await source.ReadAsync(buffer.AsMemory(0, requestedBytes), cancellationToken);
                    if (bytesRead == 0)
                    {
                        if (expectedLength is not null && size != expectedLength.Value)
                            throw new EndOfStreamException("Luồng nguồn kết thúc trước khi nhận đủ kích thước tệp.");
                        break;
                    }

                    if (size + bytesRead > MaximumFileSize)
                        throw new ReceivedFileTooLargeException(MaximumFileSize);

                    await output.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    hash.AppendData(buffer, 0, bytesRead);
                    size += bytesRead;
                }

                await output.FlushAsync(cancellationToken);
            }

            var sha256 = Convert.ToHexString(hash.GetHashAndReset());
            try
            {
                File.Move(temporaryPath, destinationPath, overwrite: false);
            }
            catch (IOException) when (File.Exists(destinationPath))
            {
                throw new DuplicateReceivedFileException(fileName);
            }

            return new ReceivedFileResult(fileName, destinationPath, size, sha256);
        }
        finally
        {
            if (File.Exists(temporaryPath))
                File.Delete(temporaryPath);
        }
    }

    private static void ValidateFileName(string fileName)
    {
        if (string.IsNullOrWhiteSpace(fileName) ||
            fileName is "." or ".." ||
            fileName.Length > 240 ||
            fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            fileName.Contains('/') ||
            fileName.Contains('\\') ||
            fileName.Any(char.IsControl) ||
            fileName.EndsWith(' ') ||
            fileName.EndsWith('.'))
        {
            throw new InvalidReceivedFileNameException("Tên tệp không hợp lệ.");
        }

        var baseName = Path.GetFileNameWithoutExtension(fileName).TrimEnd(' ', '.');
        if (ReservedDeviceNames.Contains(baseName))
            throw new InvalidReceivedFileNameException("Tên tệp trùng với tên thiết bị hệ thống của Windows.");
    }

    private static HashSet<string> CreateReservedDeviceNames()
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "CON", "PRN", "AUX", "NUL"
        };
        for (var index = 1; index <= 9; index++)
        {
            names.Add($"COM{index}");
            names.Add($"LPT{index}");
        }
        return names;
    }
}
