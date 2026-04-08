using System;
using System.IO;
using ReadZen.App.Models;
using ReadZen.App.Services;
using Xunit;

namespace ReadZen.Tests.Services;

public class TranslationStatusServiceTests
{
    [Fact]
    public void ComputeStatus_GreenWhenOnlyRemainingCjkIsInsideMulu()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var orig = Path.Combine(root, "orig.xml");
        var tran = Path.Combine(root, "tran.xml");

        try
        {
            File.WriteAllText(orig, @"<TEI xmlns:cb=""urn:cbeta""><text><body><cb:mulu>目錄項</cb:mulu><p>佛法</p></body></text></TEI>");
            File.WriteAllText(tran, @"<TEI xmlns:cb=""urn:cbeta""><text><body><cb:mulu>目錄項</cb:mulu><p>dharma</p></body></text></TEI>");

            var status = TranslationStatusService.ComputeStatus(orig, tran, root, "T/T48/T48n2005.xml", verboseLog: false);

            Assert.Equal(TranslationStatus.Green, status);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ComputeStatus_YellowWhenBodyStillContainsNonMuluCjk()
    {
        var root = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var orig = Path.Combine(root, "orig.xml");
        var tran = Path.Combine(root, "tran.xml");

        try
        {
            File.WriteAllText(orig, @"<TEI xmlns:cb=""urn:cbeta""><text><body><cb:mulu>目錄項</cb:mulu><p>佛法</p></body></text></TEI>");
            File.WriteAllText(tran, @"<TEI xmlns:cb=""urn:cbeta""><text><body><cb:mulu>目錄項</cb:mulu><p>佛法 dharma</p></body></text></TEI>");

            var status = TranslationStatusService.ComputeStatus(orig, tran, root, "T/T48/T48n2005.xml", verboseLog: false);

            Assert.Equal(TranslationStatus.Yellow, status);
        }
        finally
        {
            if (Directory.Exists(root))
                Directory.Delete(root, recursive: true);
        }
    }
}