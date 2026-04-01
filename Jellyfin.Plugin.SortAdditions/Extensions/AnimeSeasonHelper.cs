#pragma warning disable CS1591
using System;

namespace Jellyfin.Plugin.SortAdditions.Extensions
{
    /// <summary>
    /// A class containing the worst solution to a problem.
    /// </summary>
    public static class AnimeSeasonHelper
    {
        public static string GetAnimeSeasonFromDate(DateTime releaseDate)
        {
            int month = releaseDate.Month;
            int year = releaseDate.Year;

            string season = month switch
            {
                12 or 1 or 2 => "Winter",
                3 or 4 or 5 => "Spring",
                6 or 7 or 8 => "Summer",
                9 or 10 or 11 => "Fall",
                _ => "Unknown"
            };

            return $"{season} {year}";
        }
    }
}
