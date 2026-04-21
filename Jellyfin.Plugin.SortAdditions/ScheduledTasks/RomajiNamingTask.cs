#pragma warning disable CS1591
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using Jellyfin.Plugin.SortAdditions.Helpers;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Controller.Providers;
using MediaBrowser.Model.Entities;
using MediaBrowser.Model.Tasks;

namespace Jellyfin.Plugin.SortAdditions.ScheduledTasks
{
    public class RomajiNamingTask : IScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Logger _logger;
        private readonly IProviderManager _providerManager;

        public RomajiNamingTask(
            ILibraryManager libraryManager,
            IHttpClientFactory httpClientFactory,
            IProviderManager providerManager,
            Logger logger)
        {
            _libraryManager = libraryManager;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _providerManager = providerManager;
        }

        public string Name => "Append Romaji Name to Anime \"Original Title\"";

        public string Key => "AppendRomajiToAnime";

        public string Description => "A task that appends Romaji names to anime \"Original Title\" fields.";

        public string Category => "Sorting Additions";

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            string tagBase = "Anime Season: ";
            _logger.Info("Starting anime Re-tagging task...");

            var allItems = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
                IsVirtualItem = false,
                Recursive = true,
                Tags = ["anime"],  // Assume the library has tagged anime, this should be configureable
            }).ToList();

            _logger.Info($"Found {allItems.Count} items in the library. (Counting movies and series)");
            double percentPoint = 100.0 / allItems.Count;
            double currentProgress = 0;

            foreach (var item in allItems)
            {
                currentProgress += percentPoint;
                progress.Report(currentProgress);
                cancellationToken.ThrowIfCancellationRequested();

                Dictionary<string, int> providerIdPriorities = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase)
                {
                    { "AniDB", 1 },
                    { "MyAnimeList", 2 }
                };

                if (!item.ProviderIds.Any(x => providerIdPriorities.ContainsKey(x.Key)))
                {
                    _logger.Info($"Item '{item.Name}' (ID: {item.Id}) doesn't have a compatible metadata provider - Skipping...");
                    continue;
                }

                string providerToUse = item.ProviderIds.OrderBy(x => item.ProviderIds.ContainsKey(x.Key) ? providerIdPriorities[x.Key] : int.MaxValue).First().Key;
                if (item is Series)
                {
                    // MediaBrowser
                    RemoteSearchQuery<SeriesInfo> query = new RemoteSearchQuery<SeriesInfo>
                    {
                        ItemId = item.Id,
                        SearchProviderName = providerToUse,
                        SearchInfo = new SeriesInfo
                        {
                            IsAutomated = false,
                        },
                        IncludeDisabledProviders = false
                    };
                    var searchResults = await _providerManager.GetRemoteSearchResults<Series, SeriesInfo>(query, cancellationToken).ConfigureAwait(false);
                    if (searchResults == null || !searchResults.Any())
                    {
                        _logger.Info($"No search results found for series '{item.Name}' (ID: {item.Id}) using provider '{providerToUse}' - Skipping...");
                        continue;
                    }
                }

                if (item.ProductionYear == null || item.ProductionYear == 0)
                {
                    _logger.Warning($"Skipping item '{item.Name}' (ID: {item.Id}) due to missing or invalid production year.");
                    continue;
                }

                if (item is Series series && !series.Children.Any())
                {
                    _logger.Warning($"Skipping series '{item.Name}' (ID: {item.Id}) due to missing season information.");
                    continue;
                }

                if (item.Tags.Any(t => t.StartsWith(tagBase, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.Info($"Item '{item.Name}' (ID: {item.Id}) already has a season tag. Stripping...");
                    item.Tags = item.Tags.Where(t => !t.StartsWith(tagBase, StringComparison.OrdinalIgnoreCase)).ToArray();
                    await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
                }

                if (item is Series seriesItem)
                {
                    string addedTags = string.Empty;
                    foreach (Season season in seriesItem.Children.OfType<Season>())
                    {
                        if (season.IndexNumber == null || season.IndexNumber == 0)
                        {
                            _logger.Warning($"Skipping season '{season.Name}' (ID: {season.Id}) of series '{seriesItem.Name}' (ID: {seriesItem.Id}) Season is either a special or a custom entry");
                        }

                        if (season.ProductionYear == null || season.ProductionYear == 0 || season.GetEpisodes().Count == 0)
                        {
                            _logger.Warning($"Skipping season '{season.Name}' (ID: {season.Id}) of series '{seriesItem.Name}' (ID: {seriesItem.Id}) due to missing or invalid production year or no episodes.");
                            continue;
                        }

                        DateTime? seasonRelationDate = season.PremiereDate;
                        if (seasonRelationDate == null)
                        {
                            _logger.Warning($"Season '{season.Name}' (ID: {season.Id}) of series '{seriesItem.Name}' (ID: {seriesItem.Id}) is missing a premiere date. Skipping...");
                            continue;
                        }

                        string newSeasonTag = tagBase + AnimeSeasonHelper.GetAnimeSeasonFromDate(seasonRelationDate.Value);

                        item.AddTag(newSeasonTag);
                        addedTags += newSeasonTag + "; ";
                    }

                    await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
                    _logger.Info($"Added season tags '{addedTags.TrimEnd(' ', ';')}' to series '{item.Name}' (ID: {item.Id}).");
                }
                else if (item is Movie movieItem)
                {
                    DateTime? seasonRelationDate = movieItem.PremiereDate;
                    if (seasonRelationDate == null)
                    {
                        _logger.Warning($"Movie '{movieItem.Name}' (ID: {movieItem.Id}) is missing a premiere date. Skipping...");
                        continue;
                    }

                    string newSeasonTag = tagBase + AnimeSeasonHelper.GetAnimeSeasonFromDate(seasonRelationDate.Value);
                    item.AddTag(newSeasonTag);
                    await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
                    _logger.Info($"Added season tag '{newSeasonTag}' to movie '{item.Name}' (ID: {item.Id}).");
                }
            }

            progress.Report(100);
            _logger.Info("Anime Re-tagging task completed.");
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // No default trigger!
            return Array.Empty<TaskTriggerInfo>();
        }
    }
}
