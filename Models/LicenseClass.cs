// Models/LicenseClass.cs
// Coarse license classification derived by TextLicenseExtractor from a TEI
// <availability> block. Drives badge color and downstream reuse decisions.
namespace ReadZen.App.Models;

public enum LicenseClass
{
    Unknown = 0,
    PublicDomain = 1,
    PermissiveAttribution = 2,   // CC-BY, MIT, etc.
    CopyleftAttribution = 3,     // CC-BY-SA
    NonCommercial = 4,           // CBETA, CC-BY-NC
    AllRightsReserved = 5
}
