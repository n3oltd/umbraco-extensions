using Perplex.ContentBlocks.Rendering;
using System;

namespace N3O.Umbraco.Blocks.Perplex;

public interface IFailedPerplexBlockViewModel : IContentBlockViewModel {
    Exception Exception { get; }
}
