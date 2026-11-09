using System.Security.Cryptography;

namespace GuiLogServer;

public sealed record FileComparisonResult(
    string SourcePath,
    string DestinationPath,
    long SourceSize,
    long DestinationSize,
    string SourceSha256,
    string DestinationSha256,
    bool SizesMatch,
    bool HashesMatch)
{
    public bool FilesMatch => SizesMatch && HashesMatch;
}

public static class FileIntegrityComparer
{
    public static async Task<FileComparisonResult> CompareAsync(
        string sourcePath,
        string destinationPath,
        CancellationToken cancellationToken = default)
    {
        var sourceInfo = new FileInfo(sourcePath);
        var destinationInfo = new FileInfo(destinationPath);
        if (!sourceInfo.Exists)
            throw new FileNotFoundException("Không tìm thấy tệp nguồn.", sourcePath);
        if (!destinationInfo.Exists)
            throw new FileNotFoundException("Không tìm thấy tệp đích.", destinationPath);

        var sourceSize = sourceInfo.Length;
        var destinationSize = destinationInfo.Length;
        var sourceHash = await ComputeSha256Async(sourcePath, cancellationToken);
        var destinationHash = await ComputeSha256Async(destinationPath, cancellationToken);

        return new FileComparisonResult(
            sourcePath,
            destinationPath,
            sourceSize,
            destinationSize,
            Convert.ToHexString(sourceHash),
            Convert.ToHexString(destinationHash),
            sourceSize == destinationSize,
            CryptographicOperations.FixedTimeEquals(sourceHash, destinationHash));
    }

    private static async Task<byte[]> ComputeSha256Async(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(
            path,
            FileMode.Open,
            FileAccess.Read,
            FileShare.Read,
            bufferSize: 128 * 1024,
            FileOptions.Asynchronous | FileOptions.SequentialScan);
        return await SHA256.HashDataAsync(stream, cancellationToken);
    }
}
