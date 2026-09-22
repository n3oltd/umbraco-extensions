namespace N3O.Umbraco.Cloud.Models;

public class PublishedContentResult<T> {
    private PublishedContentResult(bool notFound, bool error, string path, T content) {
        NotFound = notFound;
        Error = error;
        Path = path;
        Content = content;
    }

    public T Content { get; }
    public bool Error { get; }
    public bool NotFound { get; }
    public string Path { get; }

    public static PublishedContentResult<T> ForError(string path) {
        return new PublishedContentResult<T>(false, true, path, default);
    }

    public static PublishedContentResult<T> ForFound(string path, T content) {
        return new PublishedContentResult<T>(false, false, path, content);
    }

    public static PublishedContentResult<T> ForNotFound(string path) {
        return new PublishedContentResult<T>(true, false, path, default);
    }
}
