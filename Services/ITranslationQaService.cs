using System.Collections.Generic;
using ReadZen.App.Models;

namespace ReadZen.App.Services;

public interface ITranslationQaService
{
    List<QaIssue> Check(CurrentSegmentContext ctx, List<TermHit> terms);
}
