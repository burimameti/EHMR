using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Win32;

namespace EHMR.Abstraction
{
    public class MauiFileDialogService : IFileDialogService
    {
        // Per-platform file type filters. CSV/TSV don't have a single universal MIME type/UTI
        // across platforms, so each platform lists what it actually recognizes for these exports.
        private static readonly FilePickerFileType Mkb10FileTypes = new(
            new Dictionary<DevicePlatform, IEnumerable<string>>
            {
            { DevicePlatform.iOS, new[] { "public.comma-separated-values-text", "public.plain-text" } },
            { DevicePlatform.Android, new[] { "text/csv", "text/plain", "text/tab-separated-values" } },
            { DevicePlatform.WinUI, new[] { ".xls", ".xlsx", ".txt" } },
            { DevicePlatform.MacCatalyst, new[] { "csv", "tsv", "txt" } },
            });

        public async Task<string?> PickOpenFileAsync(string title)
        {
            var result = await FilePicker.Default.PickAsync(new PickOptions
            {
                PickerTitle=title,
                FileTypes=Mkb10FileTypes
            });

            return result?.FullPath;
        }
    }

    public interface IFileDialogService
    {
        /// <summary>
        /// Shows an "open file" dialog. Returns the picked path, or null if the user cancelled.
        /// </summary>
        /// <param name="title">Dialog title bar text.</param>
        /// <param name="filter">Win32-style filter string, e.g. "CSV files (*.csv)|*.csv|All files (*.*)|*.*".</param>
        Task<string?> PickOpenFileAsync(string title);
    }
}