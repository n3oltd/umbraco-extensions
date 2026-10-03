using FluentValidation;
using N3O.Umbraco.Content;
using N3O.Umbraco.Extensions;
using N3O.Umbraco.Localization;
using N3O.Umbraco.Validation;
using System;
using System.Linq;
using uSync.Publisher.Models;
using uSync.Publisher.Services;

namespace N3O.Umbraco.Sync.Extensions.Models;

public class SyncContentReqValidator : ModelValidator<SyncContentReq> {
    private readonly IContentLocator _contentLocator;
    private readonly SyncServerService _serverService;
    
    public SyncContentReqValidator(IFormatter formatter, IContentLocator contentLocator, SyncServerService serverService) 
        : base(formatter) {
        _serverService = serverService;
        _contentLocator = contentLocator;

        RuleFor(x => x.ContentId)
           .NotNull()
           .WithMessage(Get<Strings>(s => s.ContentIdMustBeSpecified));
        
        RuleFor(x => x.ContentId)
           .Must(ContentExists)
           .WithMessage(Get<Strings>(s => s.ContentNotFound));
        
        RuleFor(x => x.ServerAlias)
           .NotNull()
           .WithMessage(Get<Strings>(s => s.ServerAliasBeSpecified));
        
        RuleFor(x => x.ServerAlias)
           .Must(ServerExists)
           .WithMessage(Get<Strings>(s => s.ServerAliasBeSpecified));
    }

    private bool ContentExists(Guid? contentId) {
        return _contentLocator.ById(contentId.GetValueOrThrow()).HasValue();
    }
    
    private bool ServerExists(string serverAlias) {
        var allowedServers = _serverService.GetAllowedServers(PublishMode.Push, _serverService.GetDefaultServer());
        
        return allowedServers.Any(x => x.Alias == serverAlias);
    }

    public class Strings : ValidationStrings {
        public string ContentIdMustBeSpecified => "Content id must be specified";
        public string ContentNotFound => "No content can be found with specified id";
        public string ServerAliasBeSpecified => "Server alias must be specified";
        public string ServerNotFound => "No server can be found with specified alias";
    }
}