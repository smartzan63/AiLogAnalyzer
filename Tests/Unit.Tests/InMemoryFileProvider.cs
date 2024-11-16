using System.Text;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Primitives;

namespace Unit.Tests;

public class InMemoryFileProvider : IFileProvider
{
    private readonly Dictionary<string, string> _files;

    public InMemoryFileProvider(Dictionary<string, string> files)
    {
        _files = files;
    }

    public IFileInfo GetFileInfo(string subpath)
    {
        if (_files.TryGetValue(subpath, out var content))
        {
            return new InMemoryFileInfo(subpath, content);
        }
        return new NotFoundFileInfo(subpath);
    }

    public IDirectoryContents GetDirectoryContents(string subpath)
    {
        return NotFoundDirectoryContents.Singleton;
    }

    public IChangeToken Watch(string filter)
    {
        return NullChangeToken.Singleton;
    }
}

public class InMemoryFileInfo : IFileInfo
{
    private readonly string _content;

    public InMemoryFileInfo(string name, string content)
    {
        Name = name;
        _content = content;
    }

    public bool Exists => true;
    public long Length => _content.Length;
    public string PhysicalPath => null;
    public string Name { get; }
    public DateTimeOffset LastModified => DateTimeOffset.UtcNow;
    public bool IsDirectory => false;

    public Stream CreateReadStream()
    {
        return new MemoryStream(Encoding.UTF8.GetBytes(_content));
    }
}

