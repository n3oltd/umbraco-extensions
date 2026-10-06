namespace N3O.Umbraco.Email;

public interface IFluentEmailBuilder<T> {
    IFluentEmailBuilder<T> From(string email, string name = null);
    IFluentEmailBuilder<T> To(string email, string name = null);
    IFluentEmailBuilder<T> Cc(string email, string name = null);
    IFluentEmailBuilder<T> Bcc(string email, string name = null);

    IFluentEmailBuilder<T> Subject(string text);
    IFluentEmailBuilder<T> Body(string content);
    IFluentEmailBuilder<T> Model(T model);
    IFluentEmailBuilder<T> Attach(string name, string contentType, byte[] bytes);

    void Queue();
}
