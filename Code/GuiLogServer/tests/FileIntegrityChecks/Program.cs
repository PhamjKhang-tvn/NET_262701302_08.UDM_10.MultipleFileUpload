using GuiLogServer;

var root = Path.Combine(Path.GetTempPath(), $"gui-log-file-tests-{Guid.NewGuid():N}");
var sourceDirectory = Path.Combine(root, "source");
var destinationDirectory = Path.Combine(root, "received");
Directory.CreateDirectory(sourceDirectory);
Directory.CreateDirectory(destinationDirectory);

var receiver = new FileReceiveService();
var passed = 0;

try
{
    await CheckTextFileAsync("notes.txt", "Nội dung kiểm tra tiếng Việt.");
    await CheckByteFileAsync("ảnh tiếng Việt.png", Convert.FromBase64String(
        "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO+/r9sAAAAASUVORK5CYII="));
    await CheckByteFileAsync("empty.bin", Array.Empty<byte>());
    await CheckTextFileAsync("tệp có dấu - số 2.txt", "Tên tệp tiếng Việt");

    var largePath = Path.Combine(sourceDirectory, "large.bin");
    await using (var largeFile = new FileStream(largePath, FileMode.CreateNew, FileAccess.Write, FileShare.None))
    {
        var block = Enumerable.Range(0, 128 * 1024).Select(value => (byte)(value % 251)).ToArray();
        for (var index = 0; index < 512; index++)
            await largeFile.WriteAsync(block);
    }
    await CheckCopiedFileAsync(largePath, "large-copy.bin");

    var differentSource = Path.Combine(sourceDirectory, "different-source.txt");
    var differentDestination = Path.Combine(destinationDirectory, "different-destination.txt");
    await File.WriteAllTextAsync(differentSource, "abc");
    await File.WriteAllTextAsync(differentDestination, "abd");
    var differentResult = await FileIntegrityComparer.CompareAsync(differentSource, differentDestination);
    Assert(!differentResult.FilesMatch && differentResult.SizesMatch && !differentResult.HashesMatch,
        "cùng kích thước nhưng nội dung khác");

    await AssertThrowsAsync<InvalidReceivedFileNameException>(
        () => receiver.SaveAsync(new MemoryStream([1]), destinationDirectory, "..\\outside.txt"),
        "chặn đường dẫn thoát thư mục");
    await AssertThrowsAsync<DuplicateReceivedFileException>(
        () => receiver.SaveAsync(new MemoryStream([1]), destinationDirectory, "empty.bin"),
        "không ghi đè tệp trùng tên");
    await AssertThrowsAsync<EndOfStreamException>(
        () => receiver.SaveAsync(new MemoryStream([1]), destinationDirectory, "truncated.bin", expectedLength: 2),
        "phát hiện luồng kết thúc trước kích thước đã khai báo");

    Console.WriteLine($"PASS: {passed} lượt kiểm tra.");
    Console.WriteLine("PASS: tệp cùng kích thước nhưng khác nội dung không bị nhận nhầm.");
    Console.WriteLine("PASS: chặn đường dẫn thoát thư mục và từ chối ghi đè tệp trùng tên.");
}
finally
{
    Directory.Delete(root, recursive: true);
}

async Task CheckTextFileAsync(string sourceName, string content)
{
    var bytes = System.Text.Encoding.UTF8.GetBytes(content);
    await CheckByteFileAsync(sourceName, bytes);
}

async Task CheckByteFileAsync(string sourceName, byte[] bytes)
{
    var sourcePath = Path.Combine(sourceDirectory, sourceName);
    await File.WriteAllBytesAsync(sourcePath, bytes);
    await CheckCopiedFileAsync(sourcePath, sourceName);
}

async Task CheckCopiedFileAsync(string sourcePath, string receivedName)
{
    await using var source = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read);
    var received = await receiver.SaveAsync(source, destinationDirectory, receivedName, source.Length);
    var comparison = await FileIntegrityComparer.CompareAsync(sourcePath, received.FullPath);
    Assert(received.Size == new FileInfo(sourcePath).Length, $"kích thước {receivedName}");
    Assert(received.Sha256 == comparison.SourceSha256, $"SHA-256 nhận {receivedName}");
    Assert(comparison.FilesMatch, $"đối chiếu {receivedName}");
}

void Assert(bool condition, string description)
{
    if (!condition)
        throw new InvalidOperationException($"FAIL: {description}");
    passed++;
    Console.WriteLine($"PASS: {description}");
}

async Task AssertThrowsAsync<TException>(Func<Task> action, string description) where TException : Exception
{
    try
    {
        await action();
    }
    catch (TException)
    {
        Assert(true, description);
        return;
    }

    throw new InvalidOperationException($"FAIL: {description} (không có exception mong đợi)");
}
