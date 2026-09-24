using N3O.Umbraco.Extensions;
using N3O.Umbraco.Localization;
using N3O.Umbraco.Utilities;
using N3O.Umbraco.Video.YouTube.Criteria;
using N3O.Umbraco.Video.YouTube.Models;
using N3O.Umbraco.Video.YouTube.QueryFilters;
using NodaTime;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Umbraco.Cms.Core.Cache;
using Umbraco.Extensions;
using YoutubeExplode;
using YoutubeExplode.Common;
using YoutubeExplode.Playlists;

namespace N3O.Umbraco.Video.YouTube;

public class YouTube : IYouTube {
    private readonly IAppPolicyCache _appCache;
    private readonly ILocalClock _localClock;
    private readonly YouTubeVideoQueryFilter _videosQueryFilter;
    private readonly YoutubeClient _client = new();

    public YouTube(IAppPolicyCache appCache, ILocalClock localClock, YouTubeVideoQueryFilter videosQueryFilter) {
        _appCache = appCache;
        _localClock = localClock;
        _videosQueryFilter = videosQueryFilter;
    }

    public virtual async Task<IReadOnlyList<YouTubeVideo>> FindVideosAsync(string channelId,
                                                                           YouTubeVideoCriteria criteria,
                                                                           CancellationToken cancellationToken = default) {
        var channel = await GetChannelByIdAsync(channelId, cancellationToken);

        return _videosQueryFilter.Apply(channel.Videos, criteria).ToList();
    }

    public virtual async Task<YouTubeChannel> GetChannelByIdAsync(string id,
                                                                  CancellationToken cancellationToken = default) {
        var cacheKey = CacheKey.Generate<YouTube>(nameof(GetChannelByIdAsync), id);
        
        return await _appCache.GetCacheItem(cacheKey, async () => {
            var channel = await _client.Channels.GetAsync(id, cancellationToken);
            var uploads = await _client.Channels.GetUploadsAsync(id, cancellationToken);
            var videos = new List<YouTubeVideo>();

            foreach (var upload in uploads) {
                videos.Add(await GetVideoAsync(upload, cancellationToken));
            }

            return new YouTubeChannel(channel.Id, channel.Title, channel.Url, videos);
        }, TimeSpan.FromHours(1));
    }

    private async Task<YouTubeVideo> GetVideoAsync(PlaylistVideo playlistVideo,
                                                   CancellationToken cancellationToken) {
        var video = await _client.Videos.GetAsync(playlistVideo.Id, cancellationToken);

        return new YouTubeVideo(video.Id,
                                video.Title,
                                video.Description,
                                video.Keywords,
                                video.Url,
                                video.UploadDate.ToLocalDateTime(_localClock),
                                video.Duration == null ? null : Duration.FromTimeSpan(video.Duration.Value),
                                GetThumbnail(video.Thumbnails));
    }

    private YouTubeThumbnail GetThumbnail(IReadOnlyList<Thumbnail> youTubeThumbnails) {
        var youTubeThumbnail = youTubeThumbnails.OrderByDescending(x => x.Resolution.Area).FirstOrDefault();

        if (youTubeThumbnail != null) {
            var size = new Size(youTubeThumbnail.Resolution.Width, youTubeThumbnail.Resolution.Height);
            
            return new YouTubeThumbnail(size, youTubeThumbnail.Url);
        } else {
            return null;
        }
    }
}
