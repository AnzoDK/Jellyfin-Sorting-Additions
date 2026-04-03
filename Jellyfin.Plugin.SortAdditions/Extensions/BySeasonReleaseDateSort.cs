/*#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
using System.Collections.Generic;
using System.Linq;
using MediaBrowser.Controller.Entities;
using MediaBrowser.Controller.Entities.TV;

namespace Jellyfin.Plugin.SortAdditions.Extensions
{
    /// <summary>
    /// A class containing the worst solution to a problem.
    /// </summary>
    public static class BySeasonReleaseDateSort : IComparer<BaseItem>
    {
        // this is where the worst solution to the problem will be implemented
        public int Compare(BaseItem? x, BaseItem? y)
        {
            if (x == null && y == null)
            {
                return 0; // If either item is null, consider them equal (or you could choose to sort nulls first/last)
            }

            if (x == null || y == null)
            {
                return y == null ? 1 : -1; // If one item is null, sort it after the non-null item
            }

            if (x is Series series)
            {
                if (series.Children.OfType<Season>().Any())
                {
                    var orderedSeasons = series.Children.OfType<Season>().OrderBy(s => s.PremiereDate).ToList();
                    var firstSeason = orderedSeasons.FirstOrDefault();
                    if(firstSeason == null || firstSeason.)
                }
            }

            throw new System.NotImplementedException();
        }
    }
}
*/
