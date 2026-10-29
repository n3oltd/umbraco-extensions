using N3O.Umbraco.Json;
using N3O.Umbraco.Scheduler;
using N3O.Umbraco.Storage;

namespace N3O.Umbraco.Email;

public class EmailBuilder : IEmailBuilder {
    private readonly IBackgroundJob _backgroundJob;
    private readonly IJsonProvider _jsonProvider;
    private readonly IVolume _volume;

    public EmailBuilder(IBackgroundJob backgroundJob, IJsonProvider jsonProvider, IVolume volume) {
        _backgroundJob = backgroundJob;
        _jsonProvider = jsonProvider;
        _volume = volume;
    }

    public IFluentEmailBuilder<T> Create<T>() {
        return new FluentEmailBuilder<T>(_backgroundJob, _jsonProvider, _volume);
    }
}
