using System;
using System.Collections.Generic;
using System.Globalization;
using Jellyfin.Plugin.SortAdditions.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.SortAdditions;

/// <summary>
/// The main plugin.
/// </summary>
public class SortingAdditions : BasePlugin<PluginConfiguration>, IHasWebPages
{
    private readonly Logger _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="SortingAdditions"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/> interface.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/> interface.</param>
    /// <param name="logger">Instance of the <see cref="Logger{Plugin}"/> class.</param>
    public SortingAdditions(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer, Logger logger)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        _logger = logger;
    }

    /// <inheritdoc />
    public override string Name => "SortingAdditions";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("144e0b9a-c908-416c-a11f-b495b6c52093");

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static SortingAdditions? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        return
        [
            new PluginPageInfo
            {
                Name = Name,
                EmbeddedResourcePath = string.Format(CultureInfo.InvariantCulture, "{0}.Configuration.configPage.html", GetType().Namespace)
            }
        ];
    }
}
