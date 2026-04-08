namespace ReadZen.App.Services;

public static class SearchIndexSettings
{
    // Total memory-ish budget per document.
    // 512 bytes is tiny, 4 KB is good, 8 KB is "serious researcher".
    // You can expose this in the UI later.
    public static int BloomBytesPerDocument { get; set; } = 4096;

    // Good default for CJK. 2-gram works better than 3-gram for Chinese queries.
    public static int NGramSize { get; set; } = 2;

    // More hashes = fewer false positives (but a bit slower to build/check).
    public static int HashCount { get; set; } = 6;
}
