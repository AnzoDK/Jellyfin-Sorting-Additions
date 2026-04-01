#pragma warning disable CA2007
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Database.Implementations;
using Jellyfin.Database.Implementations.Entities;
using Microsoft.EntityFrameworkCore;

namespace Jellyfin.Plugin.SortAdditions.Extensions
{
    /// <summary>
    /// Extension methods for the JellyfinDbContext.
    /// </summary>
    public static class JellyfinDbContextExtensions
    {
        /// <summary>
        /// Extension methods for the JellyfinDbContext.
        /// </summary>
        /// <param name="dbContextFactory">The database context factory.</param>
        /// <param name="seasonTags">The season tags to filter by.</param>
        /// <param name="ct">Cancellation token.</param>
        /// <returns>A dictionary mapping season tags and values to item IDs.</returns>
        public static async Task<Dictionary<(string SeasonTag, string Value), Guid>> GetItemIdsBySeasonTagAsync(
                this IDbContextFactory<JellyfinDbContext> dbContextFactory,
                IReadOnlyCollection<(string SeasonTag, string Value)> seasonTags,
                CancellationToken ct = default)
        {
            var result = new Dictionary<(string SeasonTag, string Value), Guid>();
            if (seasonTags.Count == 0)
            {
                return new Dictionary<(string SeasonTag, string Value), Guid>();
            }

            await using var db = await dbContextFactory.CreateDbContextAsync(ct);

            var seasonTagsGroups = seasonTags
                .GroupBy(sT => sT.SeasonTag)
                .ToDictionary(g => g.Key, g => g.Select(x => x.Value).ToList());

            var results = new List<BaseItemProvider>();

            foreach (var sT in seasonTagsGroups)
            {
                var seasonTag = sT.Key;
                var values = sT.Value;
                var items = await db.BaseItemProviders
                    .Where(bip => bip.ProviderId == seasonTag && values.Contains(bip.ProviderValue))
                    .ToListAsync(ct);

                results.AddRange(items);
            }

            return results.DistinctBy(s => (s.ProviderId, s.ProviderValue))
                .ToDictionary(s => (s.ProviderId, s.ProviderValue), s => s.ItemId);
        }
    }
}
