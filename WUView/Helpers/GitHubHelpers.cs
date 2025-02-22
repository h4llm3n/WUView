﻿// Copyright (c) Tim Kennedy. All Rights Reserved. Licensed under the MIT License.

using Octokit;

namespace WUView.Helpers;

/// <summary>
/// Class for methods that check GitHub for releases
/// </summary>
internal static class GitHubHelpers
{
    #region MainWindow Instance
    private static readonly MainWindow _mainWindow = System.Windows.Application.Current.MainWindow as MainWindow;
    #endregion MainWindow Instance

    #region NLog Instance
    private static readonly Logger _log = LogManager.GetLogger("logTemp");
    #endregion NLog Instance

    #region Check for newer release
    /// <summary>
    /// Checks to see if a newer release is available.
    /// </summary>
    /// <remarks>
    /// If the release version is greater than the current version
    /// a message box will be shown asking to go to the releases page.
    /// </remarks>
    public static async Task CheckRelease()
    {
        try
        {
            SnackbarMsg.ClearAndQueueMessage("Checking for updates");
            Release release = await GetLatestReleaseAsync(AppConstString.RepoOwner, AppConstString.RepoName);
            if (release == null)
            {
                CheckFailed();
                return;
            }

            string tag = release.TagName;
            if (string.IsNullOrEmpty(tag))
            {
                CheckFailed();
                return;
            }

            if (tag.StartsWith("v", StringComparison.InvariantCultureIgnoreCase))
            {
                tag = tag.ToLower().TrimStart('v');
            }

            Version latestVersion = new(tag);
            if (latestVersion == null)
            {
                CheckFailed();
                return;
            }

            _log.Debug($"Latest version is {latestVersion} released on {release.PublishedAt.Value.UtcDateTime} UTC");

            if (latestVersion <= AppInfo.AppVersionVer)
            {
                _log.Debug("No newer releases were found.");
                _ = new MDCustMsgBox("No newer releases were found.",
                    "Windows Update Viewer",
                    ButtonType.Ok,
                    false,
                    true,
                    _mainWindow,
                    false).ShowDialog();
            }
            else
            {
                _log.Debug($"A newer release ({latestVersion}) has been found.");
                _ = new MDCustMsgBox($"A newer release ({latestVersion}) has been found.\n\n" +
                                 "Do you want to go to the release page?\n",
                    "Windows Update Viewer",
                    ButtonType.YesNo,
                    false,
                    true,
                    _mainWindow,
                    false).ShowDialog();

                if (MDCustMsgBox.CustResult == CustResultType.Yes)
                {
                    _log.Debug($"Opening {release.HtmlUrl}");
                    string url = release.HtmlUrl;
                    Process p = new();
                    p.StartInfo.FileName = url;
                    p.StartInfo.UseShellExecute = true;
                    p.Start();
                }
            }
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Error encountered while checking version");
            CheckFailed();
        }
    }
    #endregion Check for newer release

    #region Get latest release
    /// <summary>
    /// Gets the latest release.
    /// </summary>
    /// <param name="repoOwner">The repository owner.</param>
    /// <param name="repoName">Name of the repository.</param>
    /// <returns>Release object</returns>
    internal static async Task<Release> GetLatestReleaseAsync(string repoOwner, string repoName)
    {
        try
        {
            GitHubClient client = new(new ProductHeaderValue(repoName));
            _log.Debug("Checking GitHub for latest release.");

            return await client.Repository.Release.GetLatest(repoOwner, repoName);
        }
        catch (Exception ex)
        {
            _log.Error(ex, "Get latest release from GitHub failed.");
            return null;
        }
    }
    #endregion Get latest release

    #region Check failed message
    /// <summary>
    /// Display a message box stating that the release check failed.
    /// </summary>
    internal static void CheckFailed()
    {
        _ = new MDCustMsgBox("Check for update failed.\nSee the log for more information.",
            "Windows Update Viewer",
            ButtonType.Ok,
            false,
            true,
            _mainWindow,
            true).ShowDialog();
    }
    #endregion Check failed message
}
