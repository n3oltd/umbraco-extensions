using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Primitives;
using N3O.Umbraco.Bundling.Models;
using N3O.Umbraco.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using Umbraco.Cms.Core.Serialization;

namespace N3O.Umbraco.Bundling;

public class AssetManifest : IAssetManifest {
    private readonly IFileProvider _fileProvider;
    private readonly IJsonSerializer _jsonSerializer;
    private readonly ILogger<AssetManifest> _logger;
    private readonly BundlingSettings _settings;
    private readonly object _lock = new();
    private readonly ConcurrentDictionary<string, bool> _warnedBundles = new();
    private volatile IReadOnlyDictionary<string, AssetBundle> _bundles;

    public AssetManifest(IWebHostEnvironment webHostEnvironment,
                         IJsonSerializer jsonSerializer,
                         ILogger<AssetManifest> logger,
                         BundlingSettings settings) {
        _fileProvider = webHostEnvironment.WebRootFileProvider;
        _jsonSerializer = jsonSerializer;
        _logger = logger;
        _settings = settings;

        ChangeToken.OnChange(() => _fileProvider.Watch(_settings.ManifestPath), Invalidate);
    }

    public IReadOnlyList<AssetReference> GetCss(string bundle) {
        return Get(bundle, x => x.Css);
    }

    public IReadOnlyList<AssetReference> GetJs(string bundle) {
        return Get(bundle, x => x.Js);
    }

    private IReadOnlyList<AssetReference> Get(string bundle, Func<AssetBundle, List<AssetReference>> getReferences) {
        var bundles = GetBundles();

        if (!bundles.TryGetValue(bundle, out var assetBundle)) {
            WarnUnknownBundle(bundle, bundles);

            return [];
        }

        return getReferences(assetBundle);
    }

    private IReadOnlyDictionary<string, AssetBundle> GetBundles() {
        var bundles = _bundles;

        if (bundles == null) {
            lock (_lock) {
                bundles = _bundles;

                if (bundles == null) {
                    bundles = Load();

                    _bundles = bundles;
                }
            }
        }

        return bundles;
    }

    private IReadOnlyDictionary<string, AssetBundle> Load() {
        var file = _fileProvider.GetFileInfo(_settings.ManifestPath);

        if (!file.Exists) {
            return new Dictionary<string, AssetBundle>();
        }

        string json;

        using (var stream = file.CreateReadStream()) {
            using (var reader = new StreamReader(stream)) {
                json = reader.ReadToEnd();
            }
        }

        var bundles = _jsonSerializer.Deserialize<Dictionary<string, AssetBundle>>(json);

        if (bundles == null) {
            return new Dictionary<string, AssetBundle>();
        }

        if (_settings.BaseUrl.HasValue()) {
            foreach (var bundle in bundles.Values) {
                ApplyBaseUrl(bundle.Css);
                ApplyBaseUrl(bundle.Js);
            }
        }

        return bundles;
    }

    private void ApplyBaseUrl(IEnumerable<AssetReference> references) {
        var baseUrl = _settings.BaseUrl.TrimEnd('/');

        foreach (var reference in references) {
            reference.Url = $"{baseUrl}/{reference.Url.TrimStart('/')}";
        }
    }

    // A misspelled bundle name renders nothing, which is indistinguishable from a bundle that is
    // deliberately empty. An empty manifest is the documented "not built yet" case and stays quiet.
    private void WarnUnknownBundle(string bundle, IReadOnlyDictionary<string, AssetBundle> bundles) {
        if (bundles.Count == 0 || !_warnedBundles.TryAdd(bundle, true)) {
            return;
        }

        _logger.LogWarning("Asset bundle {Bundle} is not in the manifest at {ManifestPath}, so nothing " +
                           "will be rendered for it. Known bundles: {KnownBundles}",
                           bundle,
                           _settings.ManifestPath,
                           string.Join(", ", bundles.Keys));
    }

    private void Invalidate() {
        lock (_lock) {
            _bundles = null;

            _warnedBundles.Clear();
        }
    }
}
