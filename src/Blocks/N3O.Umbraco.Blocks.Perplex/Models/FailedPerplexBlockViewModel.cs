using Perplex.ContentBlocks.Rendering;
using System;
using Umbraco.Cms.Core.Models.PublishedContent;

namespace N3O.Umbraco.Blocks.Perplex;

public class FailedPerplexBlockViewModel<TBlock> : IFailedPerplexBlockViewModel, IContentBlockViewModel<TBlock>
    where TBlock : IPublishedElement {
    public FailedPerplexBlockViewModel(TBlock content,
                                       Guid id,
                                       Guid definitionId,
                                       Guid layoutId,
                                       Exception exception) {
        Content = content;
        Id = id;
        DefinitionId = definitionId;
        LayoutId = layoutId;
        Exception = exception;
    }

    public TBlock Content { get; }
    public Guid DefinitionId { get; }
    public Exception Exception { get; }
    public Guid Id { get; }
    public Guid LayoutId { get; }

    IPublishedElement IContentBlockViewModel.Content => Content;
}
