using System;
using System.Collections.Generic;

namespace CbetaTranslator.App.Services;

public interface IBloomSearchIndexService
{
    string ManifestPath(string root);
    string IndexPath(string root);

    void Build(string originalDir, string root, IProgress<(int done, int total)>? progress = null);
    List<string> Search(string root, string query);
}
