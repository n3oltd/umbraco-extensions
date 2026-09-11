namespace N3O.Umbraco.Blocks;

public interface IBlocksCloner {
    bool CanClone(string propertyEditorAlias);
    string Clone(string value);
}
