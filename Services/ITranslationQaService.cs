using System.Collections.Generic;
using CbetaTranslator.App.Models;

namespace CbetaTranslator.App.Services;

public interface ITranslationQaService
{
    List<QaIssue> Check(CurrentSegmentContext ctx, List<TermHit> terms);
}
