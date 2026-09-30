using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace N3O.Umbraco.MediaEditorMigration.Cli;

// Resolves the built-in Image and File media types (their content-type node id + the property-type ids of the
// metadata properties) once, so the MediaNodeFactory can insert property data rows by id.
// Only used by the --target mediapicker route, which is the only one that creates media nodes.
public sealed class MediaTypes {
    // Umbraco.Cms.Core Constants.ObjectTypes.MediaType. cmsContentType holds document types, media types and
    // member types alike, all keyed by alias, so the object type is what distinguishes them: without it a
    // DOCUMENT type aliased "Image" or "File" would resolve here and media nodes would be created against it.
    private static readonly Guid MediaTypeObjectType = new("4EA4382B-2F5A-4C2B-9587-AE9B3CF3602E");

    public MediaTypeInfo Image { get; private set; }
    public MediaTypeInfo File { get; private set; }

    public static MediaTypes Resolve(SqlConnection cn, SqlTransaction tx) {
        return new MediaTypes {
            Image = ResolveOne(cn, tx, "Image"),
            File = ResolveOne(cn, tx, "File")
        };
    }

    private static MediaTypeInfo ResolveOne(SqlConnection cn, SqlTransaction tx, string alias) {
        // The media type's content-type id is its umbracoNode id.
        var contentTypeIds = Db.Query(cn, tx,
            "SELECT ct.nodeId FROM cmsContentType ct " +
            "INNER JOIN umbracoNode n ON n.id = ct.nodeId " +
            "WHERE ct.alias = @alias AND n.nodeObjectType = @objType",
            r => r.GetInt32(0),
            ("@alias", alias), ("@objType", MediaTypeObjectType));

        if (contentTypeIds.Count == 0) {
            throw new InvalidOperationException(
                $"Built-in media type '{alias}' was not found in this database (no cmsContentType row with that " +
                $"alias is a media type). Cannot create media nodes without it.");
        }

        if (contentTypeIds.Count > 1) {
            throw new InvalidOperationException(
                $"Found {contentTypeIds.Count} media types aliased '{alias}' " +
                $"(ids {string.Join(", ", contentTypeIds)}). Cannot tell which one new media nodes belong to; " +
                $"resolve the duplicate first.");
        }

        var contentTypeId = contentTypeIds[0];

        var propertyTypeIds = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        var rows = Db.Query(cn, tx,
            "SELECT pt.Alias, pt.id FROM cmsPropertyType pt WHERE pt.contentTypeId = @ctId",
            r => new { Alias = r.GetString(0), Id = r.GetInt32(1) },
            ("@ctId", contentTypeId));

        foreach (var row in rows) {
            propertyTypeIds[row.Alias] = row.Id;
        }

        if (!propertyTypeIds.ContainsKey("umbracoFile")) {
            throw new InvalidOperationException(
                $"Media type '{alias}' has no 'umbracoFile' property — unexpected schema; cannot continue.");
        }

        return new MediaTypeInfo {
            Alias = alias,
            ContentTypeId = contentTypeId,
            PropertyTypeIds = propertyTypeIds
        };
    }
}

public sealed class MediaTypeInfo {
    public string Alias { get; set; }
    public int ContentTypeId { get; set; }
    public Dictionary<string, int> PropertyTypeIds { get; set; }

    public int FilePropertyTypeId => PropertyTypeIds["umbracoFile"];

    public bool TryGetPropertyTypeId(string alias, out int id) => PropertyTypeIds.TryGetValue(alias, out id);
}
