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
            _logger.Info("Starting anime Romaji Naming task...");

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

                string providerToUse = providerIdPriorities.First(x => item.ProviderIds.ContainsKey(x.Key)).Key;
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
                    IEnumerable<MediaBrowser.Model.Providers.RemoteSearchResult>? searchResults = await _providerManager.GetRemoteSearchResults<Series, SeriesInfo>(query, cancellationToken).ConfigureAwait(false);
                    if (searchResults == null || !searchResults.Any())
                    {
                        _logger.Info($"No search results found for series '{item.Name}' (ID: {item.Id}) using provider '{providerToUse}' - Skipping...");
                        continue;
                    }

                    if (!searchResults.Any())
                    {
                        _logger.Info($"No search results found for series '{item.Name}' (ID: {item.Id}) using provider '{providerToUse}' - Skipping...");
                        continue;
                    }

                    _logger.Info($"Assuming (Hoping) '{item.Name}' (ID: {item.Id}) is: '{searchResults.First().Name}' using provider '{providerToUse}'.");

                    if (item.OriginalTitle.Contains(searchResults.First().Name, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.Info($"Series '{item.Name}' (ID: {item.Id}) has romaji in original title - Assuming it's the correct entry!");
                    }

                    if (item.SortName.Contains(searchResults.First().Name, StringComparison.OrdinalIgnoreCase))
                    {
                        _logger.Info($"Series '{item.Name}' (ID: {item.Id}) has romaji in sort name - Skipping...");
                        continue;
                    }

                    item.SortName = $"{item.SortName} ({searchResults.First().Name})";
                    await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken).ConfigureAwait(false);
                    _logger.Info($"Updated sort name for series '{item.Name}' (ID: {item.Id}) to '{item.SortName}'.");
                    continue;
                }
            }

            progress.Report(100);
            _logger.Info("Anime Romaji Naming task completed.");
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // No default trigger!
            return Array.Empty<TaskTriggerInfo>();
        }
    }
}
