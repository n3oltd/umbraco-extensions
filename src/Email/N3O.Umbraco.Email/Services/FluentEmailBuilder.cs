using Humanizer.Bytes;
using N3O.Umbraco.Email.Commands;
using N3O.Umbraco.Email.Models;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Json;
using N3O.Umbraco.Scheduler;
using N3O.Umbraco.Storage;
using N3O.Umbraco.Types;
using System;
using System.Collections.Generic;
using System.IO;

namespace N3O.Umbraco.Email;

public class FluentEmailBuilder<T> : IFluentEmailBuilder<T> {
    private readonly IBackgroundJob _backgroundJob;
    private readonly IJsonProvider _jsonProvider;
    private readonly IVolume _volume;
    private readonly List<string> _recipientEmails = [];
    private readonly string _storageFolderName;

    private EmailIdentityReq _from;
    private readonly List<EmailIdentityReq> _to = [];
    private readonly List<EmailIdentityReq> _cc = [];
    private readonly List<EmailIdentityReq> _bcc = [];
    private string _subject;
    private string _body;
    private T _model;
    private readonly List<EmailAttachmentReq> _attachments = [];

    public FluentEmailBuilder(IBackgroundJob backgroundJob, IJsonProvider jsonProvider, IVolume volume) {
        _backgroundJob = backgroundJob;
        _jsonProvider = jsonProvider;
        _volume = volume;
        _storageFolderName = Path.Join(EmailConstants.StorageFolders.Attachments, Guid.NewGuid().ToString());
    }

    public IFluentEmailBuilder<T> From(string email, string name = null) {
        _from = new EmailIdentityReq();

        _from.Email = email;
        _from.Name = name;

        return this;
    }

    public IFluentEmailBuilder<T> To(string email, string name = null) {
        return AddRecipient(_to, email, name);
    }

    public IFluentEmailBuilder<T> Cc(string email, string name = null) {
        return AddRecipient(_cc, email, name);
    }

    public IFluentEmailBuilder<T> Bcc(string email, string name = null) {
        return AddRecipient(_bcc, email, name);
    }

    public IFluentEmailBuilder<T> Subject(string text) {
        _subject = text;

        return this;
    }

    public IFluentEmailBuilder<T> Body(string content) {
        _body = content;

        return this;
    }

    public IFluentEmailBuilder<T> Model(T model) {
        _model = model;

        return this;
    }

    public IFluentEmailBuilder<T> Attach(string name, string contentType, byte[] bytes) {
        var filename = Guid.NewGuid().ToString();
        var storageFolder = _volume.GetStorageFolderAsync(_storageFolderName).GetAwaiter().GetResult();
        storageFolder.AddFileAsync(filename, bytes).GetAwaiter().GetResult();

        var attachment = new EmailAttachmentReq();
        attachment.Name = name;
        attachment.ContentType = contentType;
        attachment.Size = ByteSize.FromBytes(bytes.Length);
        attachment.StorageFolderName = _storageFolderName;
        attachment.Filename = filename;

        _attachments.Add(attachment);

        return this;
    }

    public void Queue() {
        var req = Build();
        var jobName = $"Send {req.Subject} to {_recipientEmails.Join("; ")}";
        
        _backgroundJob.Enqueue<SendEmailCommand, SendEmailReq>(jobName, req);
    }
    
    private SendEmailReq Build() {
        var req = new SendEmailReq();
        req.From = _from;
        req.To = _to;
        req.Cc = _cc;
        req.Bcc = _bcc;
        req.Subject = _subject;
        req.Body = _body;
        req.Attachments = _attachments;

        if (_model.HasValue()) {
            req.ModelType = TypeResolver.PersistedName(typeof(T));
            req.ModelJson = _jsonProvider.SerializeObject(_model);
        }

        return req;
    }

    private IFluentEmailBuilder<T> AddRecipient(List<EmailIdentityReq> list, string email, string name) {
        if (list.None(x => x.Email.EqualsInvariant(email))) {
            var identity = new EmailIdentityReq();
            identity.Name = name;
            identity.Email = email;

            list.Add(identity);
            
            _recipientEmails.AddIfNotExists(email);
        }

        return this;
    }
}
