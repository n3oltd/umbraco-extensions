using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace N3O.Umbraco.MediaEditorMigration.Cli;

public class AltTextProperties {
    public const string AliasSuffix = "AltText";
    public const string EditorAlias = "Umbraco.TextBox";
    public const int MaxLength = 512;

    private const string NameSuffix = " Alt Text";
    private static readonly Guid TextstringDataTypeKey = new("0cc0eba1-9960-42c9-bf9b-60e150b429ae");

    private readonly SqlConnection _cn;
    private readonly SqlTransaction _tx;
    private readonly bool _verbose;

    public AltTextProperties(SqlConnection cn, SqlTransaction tx, bool verbose) {
        _cn = cn;
        _tx = tx;
        _verbose = verbose;
    }

    public int Created { get; private set; }

    public static string AliasFor(string imageAlias) {
        return imageAlias + AliasSuffix;
    }

    public Dictionary<int, int> CreateFor(string editorAlias) {
        var dataTypeId = Db.Scalar<int>(_cn,
                                        _tx,
                                        "SELECT id FROM umbracoNode WHERE uniqueId = @key",
                                        ("@key", TextstringDataTypeKey));

        if (dataTypeId == 0) {
            throw new InvalidOperationException($"The built-in Textstring data type ({TextstringDataTypeKey}) " +
                                                "was not found, so no alt-text properties can be created.");
        }

        // Highest sort order first, so shifting the properties below one image never moves an image still to come.
        var imageProperties = Db.Query(_cn,
                                       _tx,
                                       "SELECT pt.id, pt.contentTypeId, pt.propertyTypeGroupId, pt.Alias, pt.Name, " +
                                       "pt.sortOrder, pt.variations " +
                                       "FROM cmsPropertyType pt " +
                                       "INNER JOIN umbracoDataType dt ON dt.nodeId = pt.dataTypeId " +
                                       "WHERE dt.propertyEditorAlias = @alias " +
                                       "ORDER BY pt.contentTypeId, pt.sortOrder DESC",
                                       ReadImageProperty,
                                       ("@alias", editorAlias));

        var altTextIds = new Dictionary<int, int>();

        foreach (var image in imageProperties) {
            altTextIds[image.Id] = GetOrCreate(image, dataTypeId);
        }

        return altTextIds;
    }

    private int GetOrCreate(ImageProperty image, int dataTypeId) {
        var alias = AliasFor(image.Alias);

        var existingId = Db.Scalar<int>(_cn,
                                        _tx,
                                        "SELECT id FROM cmsPropertyType WHERE contentTypeId = @ct AND Alias = @alias",
                                        ("@ct", image.ContentTypeId),
                                        ("@alias", alias));

        if (existingId != 0) {
            Log.Verbose(_verbose, $"Alt-text property '{alias}' already exists on content type {image.ContentTypeId}.");

            return existingId;
        }

        Db.Execute(_cn,
                   _tx,
                   "UPDATE cmsPropertyType SET sortOrder = sortOrder + 1 " +
                   "WHERE contentTypeId = @ct AND sortOrder > @sortOrder " +
                   "AND (propertyTypeGroupId = @group OR (@group IS NULL AND propertyTypeGroupId IS NULL))",
                   ("@ct", image.ContentTypeId),
                   ("@sortOrder", image.SortOrder),
                   ("@group", image.GroupId));

        var id = Db.ExecuteIdentity(_cn,
                                    _tx,
                                    "INSERT INTO cmsPropertyType (dataTypeId, contentTypeId, propertyTypeGroupId, " +
                                    "Alias, Name, sortOrder, mandatory, labelOnTop, variations, UniqueID) " +
                                    "VALUES (@dataTypeId, @ct, @group, @alias, @name, @sortOrder, 0, 0, " +
                                    "@variations, NEWID()); SELECT CAST(SCOPE_IDENTITY() AS int);",
                                    ("@dataTypeId", dataTypeId),
                                    ("@ct", image.ContentTypeId),
                                    ("@group", image.GroupId),
                                    ("@alias", alias),
                                    ("@name", image.Name + NameSuffix),
                                    ("@sortOrder", image.SortOrder + 1),
                                    ("@variations", image.Variations));

        Created++;
        Log.Verbose(_verbose, $"Created alt-text property '{alias}' below '{image.Alias}' on content type " +
                              $"{image.ContentTypeId}.");

        return id;
    }

    private static ImageProperty ReadImageProperty(SqlDataReader reader) {
        var image = new ImageProperty();
        image.Id = reader.GetInt32(0);
        image.ContentTypeId = reader.GetInt32(1);
        image.GroupId = reader.IsDBNull(2) ? null : reader.GetInt32(2);
        image.Alias = reader.GetString(3);
        image.Name = reader.IsDBNull(4) ? reader.GetString(3) : reader.GetString(4);
        image.SortOrder = reader.GetInt32(5);
        image.Variations = reader.GetInt32(6);

        return image;
    }

    private class ImageProperty {
        public int Id { get; set; }
        public int ContentTypeId { get; set; }
        public int? GroupId { get; set; }
        public string Alias { get; set; }
        public string Name { get; set; }
        public int SortOrder { get; set; }
        public int Variations { get; set; }
    }
}
