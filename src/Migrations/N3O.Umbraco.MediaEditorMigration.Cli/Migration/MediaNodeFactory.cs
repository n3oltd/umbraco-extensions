using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace N3O.Umbraco.MediaEditorMigration.Cli;

// Registers a file that already exists on disk as an Umbraco media node, reusing its existing /media/{...} path
// so no file is moved or copied. Deduplicated by path, so the same file referenced from several properties
// becomes one media node.
//
// Only used by the --target mediapicker route: Umbraco.MediaPicker3 references the media library by GUID, so a
// node has to exist. The --target inline route needs none of this, because Umbraco.ImageCropper and
// Umbraco.UploadField keep the path on the property exactly as the retired N3O editors did.
public sealed class MediaNodeFactory {
    private static readonly Guid MediaObjectType = new("B796F64C-1F99-4FFB-B886-4BF4BC011A9C");

    private readonly SqlConnection _cn;
    private readonly SqlTransaction _tx;
    private readonly MediaTypes _mediaTypes;
    private readonly int _parentId;
    private readonly bool _verbose;
    private readonly Dictionary<string, Guid> _bySrc = new(StringComparer.OrdinalIgnoreCase);

    private string _parentPath;
    private int _parentLevel;

    public MediaNodeFactory(SqlConnection cn, SqlTransaction tx, MediaTypes mediaTypes, int parentId, bool verbose) {
        _cn = cn;
        _tx = tx;
        _mediaTypes = mediaTypes;
        _parentId = parentId;
        _verbose = verbose;

        ResolveParent();
    }

    public int Created { get; private set; }

    public Guid GetOrCreate(SourceFile file) {
        if (_bySrc.TryGetValue(file.Src, out var existing)) {
            return existing;
        }

        var mediaType = file.IsImage ? _mediaTypes.Image : _mediaTypes.File;
        var key = Guid.NewGuid();

        // The retired editors had no media node and therefore no name, so the alt text is the only human-readable
        // label available; the filename is the fallback.
        var name = file.AltText ?? file.Filename ?? "media";

        // path can only be set once the identity is known, so it is written with a placeholder then corrected.
        var nodeId = Db.ExecuteIdentity(_cn, _tx,
            "INSERT INTO umbracoNode (trashed, parentId, nodeUser, level, path, sortOrder, uniqueId, text, " +
            "nodeObjectType, createDate) " +
            "VALUES (0, @parentId, NULL, @level, @tmpPath, @sortOrder, @uniqueId, @text, @objType, SYSUTCDATETIME()); " +
            "SELECT CAST(SCOPE_IDENTITY() AS int);",
            ("@parentId", _parentId),
            ("@level", _parentLevel + 1),
            ("@tmpPath", _parentPath),
            ("@sortOrder", NextSortOrder()),
            ("@uniqueId", key),
            ("@text", name),
            ("@objType", MediaObjectType));

        Db.Execute(_cn, _tx, "UPDATE umbracoNode SET path = @path WHERE id = @id",
                   ("@path", $"{_parentPath},{nodeId}"), ("@id", nodeId));

        Db.Execute(_cn, _tx, "INSERT INTO umbracoContent (nodeId, contentTypeId) VALUES (@nodeId, @ctId)",
                   ("@nodeId", nodeId), ("@ctId", mediaType.ContentTypeId));

        var versionId = Db.ExecuteIdentity(_cn, _tx,
            "INSERT INTO umbracoContentVersion (nodeId, versionDate, userId, [current], text, preventCleanup) " +
            "VALUES (@nodeId, SYSUTCDATETIME(), NULL, 1, @text, 0); SELECT CAST(SCOPE_IDENTITY() AS int);",
            ("@nodeId", nodeId), ("@text", name));

        Db.Execute(_cn, _tx, "INSERT INTO umbracoMediaVersion (id, [path]) VALUES (@id, @path)",
                   ("@id", versionId), ("@path", (object) file.Src ?? DBNull.Value));

        InsertText(versionId, mediaType.FilePropertyTypeId, NativeValueBuilder.BuildUmbracoFileValue(file));

        if (file.Extension != null) {
            InsertVarchar(versionId, mediaType, "umbracoExtension", file.Extension.TrimStart('.'));
        }

        if (file.Bytes.HasValue) {
            InsertVarchar(versionId, mediaType, "umbracoBytes", file.Bytes.Value.ToString());
        }

        if (file.IsImage && file.Width is > 0 && file.Height is > 0) {
            InsertVarchar(versionId, mediaType, "umbracoWidth", file.Width.Value.ToString());
            InsertVarchar(versionId, mediaType, "umbracoHeight", file.Height.Value.ToString());
        }

        Created++;
        _bySrc[file.Src] = key;

        Log.Verbose(_verbose, $"Created {mediaType.Alias} media node {nodeId} (key {key}) for '{file.Src}'.");

        return key;
    }

    private void InsertText(int versionId, int propertyTypeId, string textValue) {
        Db.Execute(_cn, _tx,
            "INSERT INTO umbracoPropertyData (versionId, propertyTypeId, languageId, segment, textValue) " +
            "VALUES (@v, @p, NULL, NULL, @t)",
            ("@v", versionId), ("@p", propertyTypeId), ("@t", textValue));
    }

    private void InsertVarchar(int versionId, MediaTypeInfo type, string propertyAlias, string value) {
        if (!type.TryGetPropertyTypeId(propertyAlias, out var propertyTypeId)) {
            return;
        }

        Db.Execute(_cn, _tx,
            "INSERT INTO umbracoPropertyData (versionId, propertyTypeId, languageId, segment, varcharValue) " +
            "VALUES (@v, @p, NULL, NULL, @val)",
            ("@v", versionId), ("@p", propertyTypeId), ("@val", value));
    }

    private int NextSortOrder() {
        return Db.Scalar<int>(_cn, _tx,
            "SELECT ISNULL(MAX(sortOrder), -1) + 1 FROM umbracoNode WHERE parentId = @parentId " +
            "AND nodeObjectType = @objType",
            ("@parentId", _parentId), ("@objType", MediaObjectType));
    }

    private void ResolveParent() {
        // -1 is the Media section root, which has no umbracoNode row of its own.
        if (_parentId == -1) {
            _parentPath = "-1";
            _parentLevel = 0;

            return;
        }

        var rows = Db.Query(_cn, _tx,
            "SELECT [path], [level], nodeObjectType FROM umbracoNode WHERE id = @id",
            r => new { Path = r.GetString(0), Level = r.GetInt32(1), ObjType = r.GetGuid(2) },
            ("@id", _parentId));

        if (rows.Count == 0) {
            throw new InvalidOperationException($"--media-parent {_parentId} does not exist in umbracoNode.");
        }

        if (rows[0].ObjType != MediaObjectType) {
            throw new InvalidOperationException($"--media-parent {_parentId} is not a media node/folder.");
        }

        _parentPath = rows[0].Path;
        _parentLevel = rows[0].Level;
    }
}
