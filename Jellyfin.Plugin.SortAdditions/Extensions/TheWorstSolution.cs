#pragma warning disable CS1591
#pragma warning disable CA2007
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Data.Enums;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.Movies;
using MediaBrowser.Controller.Entities.TV;
using MediaBrowser.Controller.Library;
using MediaBrowser.Model.Tasks;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SortAdditions.Extensions
{
    /// <summary>
    /// A class containing the worst solution to a problem.
    /// </summary>
    public class TheWorstSolution : IScheduledTask
    {
        private readonly ILibraryManager _libraryManager;
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly Logger<TheWorstSolution> _logger;

        public TheWorstSolution(
            ILibraryManager libraryManager,
            IHttpClientFactory httpClientFactory,
            Logger<TheWorstSolution> logger)
        {
            _libraryManager = libraryManager;
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public string Name => "Re-Tag Anime To Season";

        public string Key => "ReTagAnimeToSeason";

        public string Description => "A task that re-tags anime content to match their release season.";

        public string Category => "Sorting Additions";

        public async Task ExecuteAsync(IProgress<double> progress, CancellationToken cancellationToken)
        {
            string tagBase = "Anime Season: ";
            _logger.LogInformation("Starting anime Re-tagging task...");

            var allItems = _libraryManager.GetItemList(new InternalItemsQuery
            {
                IncludeItemTypes = new[] { BaseItemKind.Movie, BaseItemKind.Series },
                IsVirtualItem = false,
                Recursive = true,
                Tags = ["anime"],
            }).ToList();

            _logger.LogInformation("Found {Count} items in the library. (Counting movies and series)", allItems.Count);
            double percentPoint = 100.0 / allItems.Count;
            double currentProgress = 0;

            foreach (var item in allItems)
            {
                currentProgress += percentPoint;
                progress.Report(currentProgress);
                cancellationToken.ThrowIfCancellationRequested();
                if (item.ProductionYear == null || item.ProductionYear == 0)
                {
                    _logger.LogWarning("Skipping item '{Name}' (ID: {Id}) due to missing or invalid production year.", item.Name, item.Id);
                    continue;
                }

                if (item is Series series && !series.Children.Any())
                {
                    _logger.LogWarning("Skipping series '{Name}' (ID: {Id}) due to missing season information.", item.Name, item.Id);
                    continue;
                }

                if (item.Tags.Any(t => t.StartsWith(tagBase, StringComparison.OrdinalIgnoreCase)))
                {
                    _logger.LogInformation("Item '{Name}' (ID: {Id}) already has a season tag. Stripping...", item.Name, item.Id);
                    item.Tags = item.Tags.Where(t => !t.StartsWith(tagBase, StringComparison.OrdinalIgnoreCase)).ToArray();
                    await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken);
                }

                if (item is Series seriesItem)
                {
                    string addedTags = string.Empty;
                    foreach (Season season in seriesItem.Children.OfType<Season>())
                    {
                        if (season.ProductionYear == null || season.ProductionYear == 0 || season.GetEpisodes().Count == 0)
                        {
                            _logger.LogWarning("Skipping season '{Name}' (ID: {Id}) of series '{SeriesName}' (ID: {SeriesId}) due to missing or invalid production year or no episodes.", season.Name, season.Id, seriesItem.Name, seriesItem.Id);
                            continue;
                        }

                        DateTime? seasonRelationDate = season.PremiereDate;
                        if (seasonRelationDate == null)
                        {
                            _logger.LogWarning("Season '{Name}' (ID: {Id}) of series '{SeriesName}' (ID: {SeriesId}) is missing a premiere date. Skipping...", season.Name, season.Id, seriesItem.Name, seriesItem.Id);
                            continue;
                        }

                        string newSeasonTag = tagBase + AnimeSeasonHelper.GetAnimeSeasonFromDate(seasonRelationDate.Value);

                        item.AddTag(newSeasonTag);
                        addedTags += newSeasonTag + "; ";
                    }

                    await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken);
                    _logger.LogInformation("Added season tags '{Tags}' to series '{Name}' (ID: {Id}).", addedTags.TrimEnd(' ', ';'), item.Name, item.Id);
                }
                else if (item is Movie movieItem)
                {
                    DateTime? seasonRelationDate = movieItem.PremiereDate;
                    if (seasonRelationDate == null)
                    {
                        _logger.LogWarning("Movie '{Name}' (ID: {Id}) is missing a premiere date. Skipping...", movieItem.Name, movieItem.Id);
                        continue;
                    }

                    string newSeasonTag = tagBase + AnimeSeasonHelper.GetAnimeSeasonFromDate(seasonRelationDate.Value);
                    item.AddTag(newSeasonTag);
                    await item.UpdateToRepositoryAsync(ItemUpdateType.MetadataEdit, cancellationToken);
                    _logger.LogInformation("Added season tag '{Tag}' to movie '{Name}' (ID: {Id}).", newSeasonTag, item.Name, item.Id);
                }
            }

            progress.Report(100);
            _logger.LogInformation("Anime Re-tagging task completed.");
        }

        public IEnumerable<TaskTriggerInfo> GetDefaultTriggers()
        {
            // No default trigger!
            return Array.Empty<TaskTriggerInfo>();
        }
    }
}
